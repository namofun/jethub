namespace Xylab.Remoting.PowerShellClient;

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;

public sealed class PowerShellRemoteClient : IDisposable
{
    private readonly string remoteEndpoint;
    private HubConnection? connection;

    public PowerShellRemoteClient(string remoteEndpoint)
    {
        this.remoteEndpoint = remoteEndpoint;
    }

    public void Connect(SecureString? accessToken, X509Certificate2? clientCertificate)
    {
        Action<HttpConnectionOptions> authOptions = options =>
        {
            if (accessToken != null)
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(Unwrap(accessToken));
            }
            else if (clientCertificate != null)
            {
                options.ClientCertificates = [clientCertificate];
            }
        };

        this.connection = new HubConnectionBuilder().WithUrl(remoteEndpoint, authOptions).Build();
        this.connection.StartAsync().Wait();

        static string Unwrap(SecureString value)
        {
            var pointer = Marshal.SecureStringToGlobalAllocUnicode(value);
            try
            {
                return Marshal.PtrToStringUni(pointer)
                    ?? throw new InvalidOperationException("Failed to read the access token.");
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(pointer);
            }
        }
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
