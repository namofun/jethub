using System;
using System.Management.Automation;

namespace Xylab.Management.Automation.Commandlets
{
    [Cmdlet("Say", "HelloWorld")]
    [Authorization(Role.ViewOnly)]
    public class SayHelloWorld : Cmdlet
    {
        [Parameter]
        public string? AdditionalParameter { get; set; }

        protected override void ProcessRecord()
        {
            this.WriteObject(
                $"Hello, this is {Environment.MachineName}. " +
                $"The additional parameter is: '{this.AdditionalParameter}'.");
        }
    }
}
