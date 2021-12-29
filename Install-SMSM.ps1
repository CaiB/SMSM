using namespace System.IO;

param
(
    [Parameter(Position = 0, Mandatory = $true)]
    [string]$ConfigFile
);

# Make sure path to JSON config is absolute
[string] $ConfigPath = [Path]::GetFullPath($ConfigFile);
if(![File]::Exists($ConfigPath))
{
    Write-Error "The config file `"$ConfigPath`" could not be found.";
    exit;
}
$ConfigContents = Get-Content -Path $ConfigPath | ConvertFrom-Json;

$ServerName = $ConfigContents.Name;
if([string]::IsNullOrWhiteSpace($ServerName))
{
    Write-Error "A valid server name could not be read from the config file. Make sure the file is valid and the server name is defined.";
    exit;
}

$SMSMPath = [Path]::Combine($PSScriptRoot, 'SMSMService.exe');
if(![File]::Exists($SMSMPath))
{
    Write-Error "The SMSM executable `"$SMSMPath`" could not be found.";
    exit;
}

# This sets the login account on the service to its associated virtual account. Why is this so complex D:
function Set-ServiceAccount
{
    param ([string] $ServiceName)

    Add-Type -ReferencedAssemblies @('System.Management') -TypeDefinition @"
    using System;
    using System.Management;

    namespace ServiceHelpers
    {
        public class Account
        {
            public static void SetUsername(string serviceName)
            {
                using (ManagementObject service = new ManagementObject(new ManagementPath("Win32_Service.Name='" + serviceName + "'")))
                {
                    object[] WMIParams = new object[11];
                    WMIParams[6] = "NT Service\\" + serviceName;
                    service.InvokeMethod("Change", WMIParams);
                }
            }
        }
    }
"@
    [ServiceHelpers.Account]::SetUsername($ServiceName);
}

$ServiceParams = 
@{
    Name = "SMSM-$ServerName";
    BinaryPathName = "`"$SMSMPath`" `"$ConfigPath`"";
    DisplayName = "Minecraft Server $ServerName (SMSM)";
    StartupType = 'Automatic';
};

Write-Host 'You are about to install the SMSM Service with the following settings:';
Write-Host "  Service Name: $($ServiceParams.Name)";
Write-Host "  Display Name: $($ServiceParams.DisplayName)";
Write-Host "  Executable: $($ServiceParams.BinaryPathName)";
Write-Host "  Service Account: `"NT Service\$($ServiceParams.Name)`"";

$IsReady = Read-Host -Prompt 'Is the above information correct? (y/n)';
if ($IsReady -NE 'y') { Write-Host 'Cancelled.'; }
else
{
    New-Service @ServiceParams;
    Set-ServiceAccount $ServiceParams.Name;
}