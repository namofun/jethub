@{
    RootModule = 'Xylab.Remoting.PowerShellClient.dll'
    ModuleVersion = '1.2.0'
    GUID = '3be47781-79c5-463d-81b8-715711b1a3b3'
    Author = 'yang-er'
    Description = 'PowerShell client for remote management'
    PowerShellVersion = '7.6'
    CompatiblePSEditions = @('Core')
    CmdletsToExport = @(
        'Invoke-RemoteCmdlet'
        'Invoke-RemoteScript'
    )
    FunctionsToExport = @()
    VariablesToExport = @()
    AliasesToExport = @()
    PrivateData = @{
        PSData = @{
            Tags = @('PowerShell', 'Remoting', 'SignalR')
            ProjectUri = 'https://github.com/namofun/jethub'
            LicenseUri = 'https://github.com/namofun/jethub/blob/main/LICENSE'
        }
    }
}
