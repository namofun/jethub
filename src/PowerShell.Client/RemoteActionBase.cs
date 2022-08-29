using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Runtime.Serialization;
using System.Text;

namespace Xylab.Management.Automation
{
    public abstract class RemoteActionBase : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string RemoteEndpoint { get; set; } = string.Empty;

        protected abstract IAsyncEnumerable<KeyValuePair<string, string>> StartExecuteAsync(HubConnection connection);

        protected override void ProcessRecord()
        {
            HubConnection? connection = null;
            try
            {
                connection = new HubConnectionBuilder().WithUrl(RemoteEndpoint).Build();

                connection.StartAsync().Wait();
                this.WriteCommandDetail("Connected to remote endpoint.");

                var stream = this.StartExecuteAsync(connection);
                var enumerator = stream.GetAsyncEnumerator();

                this.WriteCommandDetail("Start enumerating.");
                while (enumerator.MoveNextAsync().AsTask().Result)
                {
                    switch (enumerator.Current.Key)
                    {
                        case "Output":
                            this.WriteObject(PSSerializer.Deserialize(enumerator.Current.Value));
                            break;

                        case nameof(PSDataStreams.Error):
                            this.WriteError(Deserialize<ErrorRecord>(enumerator.Current.Value));
                            break;

                        case nameof(PSDataStreams.Warning):
                            this.WriteWarning(Deserialize<WarningRecord>(enumerator.Current.Value).Message);
                            break;

                        case nameof(PSDataStreams.Progress):
                            this.WriteProgress(Deserialize<ProgressRecord>(enumerator.Current.Value));
                            break;

                        case nameof(PSDataStreams.Information):
                            this.WriteInformation(Deserialize<InformationRecord>(enumerator.Current.Value));
                            break;

                        case nameof(PSDataStreams.Debug):
                            this.WriteDebug(Deserialize<DebugRecord>(enumerator.Current.Value).Message);
                            break;

                        case nameof(PSDataStreams.Verbose):
                            this.WriteVerbose(Deserialize<VerboseRecord>(enumerator.Current.Value).Message);
                            break;

                        default:
                            throw new PSInvalidOperationException($"Unsupported output stream {enumerator.Current.Key}");
                    }

                    static TRecord Deserialize<TRecord>(string content)
                    {
                        using MemoryStream ms = new(Encoding.UTF8.GetBytes(content));
                        return (TRecord)new DataContractSerializer(typeof(TRecord)).ReadObject(ms)!;
                    }
                }

                this.WriteCommandDetail("Stopped enumerating.");

                enumerator.DisposeAsync().AsTask().Wait();
                this.WriteCommandDetail("Disposed enumerating.");
            }
            finally
            {
                if (connection != null)
                {
                    if (connection.State != HubConnectionState.Disconnected)
                    {
                        connection.StopAsync().Wait();
                        this.WriteCommandDetail("Stopped connection.");
                    }

                    connection.DisposeAsync().AsTask().Wait();
                    this.WriteCommandDetail("Disposed connection.");
                }
            }
        }
    }
}
