using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;
using System.Management.Automation;

namespace Xylab.Management.Automation
{
    [Cmdlet(VerbsLifecycle.Invoke, "RemoteScript")]
    public class InvokeRemoteScript : RemoteActionBase
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string ScriptContent { get; set; } = string.Empty;

        protected override IAsyncEnumerable<KeyValuePair<string, string>> StartExecuteAsync(HubConnection connection)
        {
            return connection.StreamAsync<KeyValuePair<string, string>>("ExecuteScript", ScriptContent);
        }
    }
}
