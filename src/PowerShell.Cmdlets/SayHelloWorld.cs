namespace Xylab.Management.Automation.Cmdlets;

using System;
using System.Management.Automation;

[Cmdlet("Say", "HelloWorld")]
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
