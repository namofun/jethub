[CmdletBinding()]

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $RemoteEndpoint,

    [Parameter(Mandatory = $true, Position = 1, ValueFromPipeline = $true)]
    [string] $ScriptContent
)

process {
    $client = $null
    try
    {
        $client = [Xylab.Remoting.PowerShellClient.PowerShellRemoteClient]::new($RemoteEndpoint)
        $client.Connect()

        $stream = $client.GetStream("ExecuteScript", $ScriptContent)
        $enumerator = $stream.GetAsyncEnumerator()

        while ($enumerator.MoveNextAsync().AsTask().Result)
        {
            $content = [Xylab.Remoting.PowerShellClient.PowerShellRemoteClient]::DeserializeContent($enumerator.Current, $null)
            switch ($enumerator.Current.Key)
            {
                "Output" { $content }
                "Error" { Write-Error -ErrorRecord $content }
                "Warning" { Write-Warning $content.Message }
                "Progress" { Write-Progress -Activity $content.Activity -Status $content.StatusDescription -Id $content.ActivityId -PercentComplete $content.PercentComplete -SecondsRemaining $content.SecondsRemaining -CurrentOperation $content.CurrentOperation -ParentId $content.ParentActivityId -Completed:($content.RecordType -eq 'Completed') }
                "Information" { Write-Information $content.MessageData }
                "Debug" { Write-Debug $content.Message }
                "Verbose" { Write-Verbose $content.Message }
            }
        }

        $enumerator.DisposeAsync().AsTask().Wait()
    }
    finally
    {
        if ($client -ne $null)
        {
            $client.Dispose()
        }
    }
}