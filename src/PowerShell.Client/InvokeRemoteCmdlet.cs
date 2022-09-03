using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Xylab.Management.Automation
{
    [Cmdlet(VerbsLifecycle.Invoke, "RemoteCmdlet")]
    public class InvokeRemoteCmdlet : RemoteActionBase
    {
        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true)]
        public string CmdletName { get; set; } = string.Empty;

        [Parameter(Position = 2)]
        public Hashtable? BoundParameters { get; set; }

        protected override IAsyncEnumerable<KeyValuePair<string, string>> StartExecuteAsync(PowerShellRemoteClient client)
        {
            return client.GetStream(
                "ExecuteCmdlet",
                this.CmdletName,
                this.BoundParameters == null
                    ? null
                    : PSSerializer.Serialize(new PSObject(this.BoundParameters)));
        }
    }
}
