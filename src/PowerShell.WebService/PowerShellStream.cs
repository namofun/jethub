namespace Xylab.Remoting.PowerShellWebService;

using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.Serialization;

public interface IPowerShellStream : IDisposable
{
    Task<string> ReadAsClixmlAsync();

    IAsyncEnumerable<KeyValuePair<string, string>> EnumerateAsStreamClixmlAsync();
}

internal sealed class PowerShellStream(PowerShell powershell, Runspace runspace) : IPowerShellStream
{
    public async Task<string> ReadAsClixmlAsync()
    {
        var result = await powershell.InvokeAsync();
        return PSSerializer.Serialize(result);
    }

    public IAsyncEnumerable<KeyValuePair<string, string>> EnumerateAsStreamClixmlAsync()
    {
        return new Enumerable(powershell);
    }

    public void Dispose()
    {
        powershell.Dispose();
        runspace.Dispose();
    }

    private class Enumerable(PowerShell powershell) : IAsyncEnumerable<KeyValuePair<string, string>>
    {
        public IAsyncEnumerator<KeyValuePair<string, string>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            PSDataCollection<PSObject> input = new(), output = new();
            input.Complete();

            CancellationTokenSource cts = new();
            CancellationTokenSource aggregatedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            Enumerator enumerator = new(output, powershell.Streams, aggregatedCts.Token);
            Task.Factory.FromAsync(powershell.BeginInvoke(input, output), powershell.EndInvoke).ContinueWith(_ => cts.Cancel());

            return enumerator;
        }
    }

    private class Enumerator : IAsyncEnumerator<KeyValuePair<string, string>>
    {
        private readonly ConcurrentQueue<KeyValuePair<string, string>> _queue = new();
        private readonly SemaphoreSlim _semaphoreSlim = new(0);
        private readonly PSDataStreams _streams;
        private readonly PSDataCollection<PSObject> _output;
        private readonly CancellationToken _queueNotifier;

        public KeyValuePair<string, string> Current { get; private set; }

        public Enumerator(PSDataCollection<PSObject> output, PSDataStreams streams, CancellationToken cancellationToken)
        {
            _streams = streams;
            _output = output;
            _queueNotifier = cancellationToken;

            _streams.Progress.DataAdding += OnWriteProgress;
            _streams.Debug.DataAdding += OnWriteDebug;
            _streams.Error.DataAdding += OnWriteError;
            _streams.Information.DataAdding += OnWriteInformation;
            _streams.Verbose.DataAdding += OnWriteVerbose;
            _streams.Warning.DataAdding += OnWriteWarning;
            _output.DataAdding += OnWriteOutput;
        }

        public ValueTask DisposeAsync()
        {
            _streams.Progress.DataAdding -= OnWriteProgress;
            _streams.Debug.DataAdding -= OnWriteDebug;
            _streams.Error.DataAdding -= OnWriteError;
            _streams.Information.DataAdding -= OnWriteInformation;
            _streams.Verbose.DataAdding -= OnWriteVerbose;
            _streams.Warning.DataAdding -= OnWriteWarning;
            _output.DataAdding -= OnWriteOutput;

            _semaphoreSlim.Dispose();
            return ValueTask.CompletedTask;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            do
            {
                while (_queue.TryDequeue(out var next))
                {
                    Current = next;
                    return true;
                }

                try
                {
                    await _semaphoreSlim.WaitAsync(_queueNotifier);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            while (!_queueNotifier.IsCancellationRequested);

            return false;
        }

        private void OnWriteRecord<TRecord>(string streamType, TRecord rawObject)
        {
            string serializedContent;
            using (MemoryStream memory = new())
            {
                DataContractSerializer dcs = new(typeof(TRecord));
                dcs.WriteObject(memory, rawObject);
                memory.Position = 0;

                using StreamReader sr = new(memory);
                serializedContent = sr.ReadToEnd();
            }

            this._queue.Enqueue(KeyValuePair.Create(streamType, serializedContent));
            this._semaphoreSlim.Release();
        }

        private void OnWriteOutput(object? sender, DataAddingEventArgs e)
        {
            this._queue.Enqueue(KeyValuePair.Create("Output", PSSerializer.Serialize((PSObject)e.ItemAdded)));
            this._semaphoreSlim.Release();
        }

        private void OnWriteProgress(object? sender, DataAddingEventArgs e)
        {
            this.OnWriteRecord("Progress", (ProgressRecord)e.ItemAdded);
        }

        private void OnWriteDebug(object? sender, DataAddingEventArgs e)
        {
            this.OnWriteRecord("Debug", (DebugRecord)e.ItemAdded);
        }

        private void OnWriteVerbose(object? sender, DataAddingEventArgs e)
        {
            this.OnWriteRecord("Verbose", (VerboseRecord)e.ItemAdded);
        }

        private void OnWriteInformation(object? sender, DataAddingEventArgs e)
        {
            this.OnWriteRecord("Information", (InformationRecord)e.ItemAdded);
        }

        private void OnWriteError(object? sender, DataAddingEventArgs e)
        {
            this.OnWriteRecord("Error", (ErrorRecord)e.ItemAdded);
        }

        private void OnWriteWarning(object? sender, DataAddingEventArgs e)
        {
            this.OnWriteRecord("Warning", (WarningRecord)e.ItemAdded);
        }
    }
}
