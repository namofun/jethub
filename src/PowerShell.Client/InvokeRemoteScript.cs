using System.Collections.Generic;
using System.Management.Automation;

namespace Xylab.Management.Automation
{
    [Cmdlet(VerbsLifecycle.Invoke, "RemoteScript")]
    public class InvokeRemoteScript : RemoteActionBase
    {
        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true)]
        public string ScriptContent { get; set; } = string.Empty;

        protected override IAsyncEnumerable<KeyValuePair<string, string>> StartExecuteAsync(PowerShellRemoteClient client)
        {
            return client.GetStream("ExecuteScript", ScriptContent);
        }
    }
}
