using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Xylab.Management.Automation.WebServices
{
    internal class PowerShellInvoker : IAsyncEnumerable<KeyValuePair<string, string>>
    {
        private readonly PowerShell _powershell;

        public PowerShellInvoker(PowerShell powershell)
        {
            _powershell = powershell;
        }

        public IAsyncEnumerator<KeyValuePair<string, string>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            PSDataCollection<PSObject> input = new(), output = new();
            input.Complete();

            CancellationTokenSource cts = new();
            CancellationTokenSource aggregatedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            Enumerator enumerator = new(output, _powershell.Streams, aggregatedCts.Token);
            Task.Factory.FromAsync(_powershell.BeginInvoke(input, output), _powershell.EndInvoke).ContinueWith(_ => cts.Cancel());

            return enumerator;
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

            private void OnWriteMessage(string streamType, string message)
            {
                this._queue.Enqueue(KeyValuePair.Create(streamType, message));
                this._semaphoreSlim.Release();
            }

            private void OnWriteOutput(object sender, DataAddingEventArgs e)
            {
                this._queue.Enqueue(KeyValuePair.Create("Output", PSSerializer.Serialize((PSObject)e.ItemAdded)));
                this._semaphoreSlim.Release();
            }

            private void OnWriteProgress(object sender, DataAddingEventArgs e)
            {
                this.OnWriteRecord("Progress", (ProgressRecord)e.ItemAdded);
            }

            private void OnWriteDebug(object sender, DataAddingEventArgs e)
            {
                this.OnWriteMessage("Debug", ((DebugRecord)e.ItemAdded).Message);
            }

            private void OnWriteVerbose(object sender, DataAddingEventArgs e)
            {
                this.OnWriteMessage("Verbose", ((VerboseRecord)e.ItemAdded).Message);
            }

            private void OnWriteInformation(object sender, DataAddingEventArgs e)
            {
                this.OnWriteRecord("Information", (InformationRecord)e.ItemAdded);
            }

            private void OnWriteError(object sender, DataAddingEventArgs e)
            {
                this.OnWriteRecord("Error", (ErrorRecord)e.ItemAdded);
            }

            private void OnWriteWarning(object sender, DataAddingEventArgs e)
            {
                this.OnWriteMessage("Warning", ((WarningRecord)e.ItemAdded).Message);
            }
        }
    }

    public class PowerShellHub : Hub
    {
        public async IAsyncEnumerable<KeyValuePair<string, string>> ExecuteScript(string scriptContent)
        {
            using Runspace runspace = Bundle.CreateRunspace();
            using PowerShell pwsh = PowerShell.Create(runspace);
            pwsh.AddScript(scriptContent);

            await foreach (var entry in new PowerShellInvoker(pwsh).WithCancellation(Context.ConnectionAborted))
            {
                yield return entry;
            }
        }
    }
}
