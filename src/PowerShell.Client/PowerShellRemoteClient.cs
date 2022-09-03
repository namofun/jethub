using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Runtime.Serialization;
using System.Text;

namespace Xylab.Management.Automation
{
    public sealed class PowerShellRemoteClient : IDisposable
    {
        private readonly string remoteEndpoint;
        private HubConnection? connection;

        public PowerShellRemoteClient(string remoteEndpoint)
        {
            this.remoteEndpoint = remoteEndpoint;
        }

        public void Connect()
        {
            this.connection = new HubConnectionBuilder().WithUrl(remoteEndpoint).Build();
            this.connection.StartAsync().Wait();
        }

        public IAsyncEnumerable<KeyValuePair<string, string>> GetStream(string method, string arg1)
        {
            return this.connection!.StreamAsync<KeyValuePair<string, string>>(method, arg1);
        }

        public IAsyncEnumerable<KeyValuePair<string, string>> GetStream(string method, string arg1, string? arg2)
        {
            return this.connection!.StreamAsync<KeyValuePair<string, string>>(method, arg1, arg2);
        }

        public static object DeserializeContent(KeyValuePair<string, string> streamAndResult, Cmdlet? cmdlet)
        {
            switch (streamAndResult.Key)
            {
                case "Output":
                    var output = PSSerializer.Deserialize(streamAndResult.Value);
                    cmdlet?.WriteObject(output);
                    return output;

                case nameof(PSDataStreams.Error):
                    var error = Deserialize<ErrorRecord>(streamAndResult.Value);
                    cmdlet?.WriteError(error);
                    return error;

                case nameof(PSDataStreams.Warning):
                    var warning = Deserialize<WarningRecord>(streamAndResult.Value);
                    cmdlet?.WriteWarning(warning.Message);
                    return warning;

                case nameof(PSDataStreams.Progress):
                    var progress = Deserialize<ProgressRecord>(streamAndResult.Value);
                    cmdlet?.WriteProgress(progress);
                    return progress;

                case nameof(PSDataStreams.Information):
                    var info = Deserialize<InformationRecord>(streamAndResult.Value);
                    cmdlet?.WriteInformation(info);
                    return info;

                case nameof(PSDataStreams.Debug):
                    var debug = Deserialize<DebugRecord>(streamAndResult.Value);
                    cmdlet?.WriteDebug(debug.Message);
                    return debug;

                case nameof(PSDataStreams.Verbose):
                    var verbose = Deserialize<VerboseRecord>(streamAndResult.Value);
                    cmdlet?.WriteVerbose(verbose.Message);
                    return verbose;

                default:
                    throw new PSInvalidOperationException($"Unsupported output stream {streamAndResult.Key}");
            }

            static TRecord Deserialize<TRecord>(string content)
            {
                using MemoryStream ms = new(Encoding.UTF8.GetBytes(content));
                return (TRecord)new DataContractSerializer(typeof(TRecord)).ReadObject(ms)!;
            }
        }

        public void Dispose()
        {
            if (this.connection != null)
            {
                if (this.connection.State != HubConnectionState.Disconnected)
                {
                    this.connection.StopAsync().Wait();
                }

                this.connection.DisposeAsync().AsTask().Wait();
                this.connection = null;
            }
        }
    }
}
