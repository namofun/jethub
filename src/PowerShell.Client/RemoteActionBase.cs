using System.Collections.Generic;
using System.Management.Automation;

namespace Xylab.Management.Automation
{
    public abstract class RemoteActionBase : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string RemoteEndpoint { get; set; } = string.Empty;

        protected abstract IAsyncEnumerable<KeyValuePair<string, string>> StartExecuteAsync(PowerShellRemoteClient client);

        protected override void ProcessRecord()
        {
            PowerShellRemoteClient? client = null;
            try
            {
                client = new(RemoteEndpoint);
                client.Connect();
                this.WriteCommandDetail("Connected to remote endpoint.");

                var stream = this.StartExecuteAsync(client);
                var enumerator = stream.GetAsyncEnumerator();

                this.WriteCommandDetail("Start enumerating.");
                while (enumerator.MoveNextAsync().AsTask().Result)
                {
                    PowerShellRemoteClient.DeserializeContent(enumerator.Current, this);
                }

                this.WriteCommandDetail("Stopped enumerating.");

                enumerator.DisposeAsync().AsTask().Wait();
                this.WriteCommandDetail("Disposed enumerating.");
            }
            finally
            {
                if (client != null)
                {
                    client.Dispose();
                    this.WriteCommandDetail("Disposed connection.");
                }
            }
        }
    }
}
