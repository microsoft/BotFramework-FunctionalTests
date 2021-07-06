# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

<#
  .SYNOPSIS
  Gets DirectLine Secrets from Azure.

  .DESCRIPTION
  Configure AppSettings file with DirectLine Secrets gathered from Azure Bot Resources based on the Resource Group and Resource Suffix provided by the user.

  .PARAMETER ResourceGroup
  Specifies the name for the specific Resource Group where the resources are deployed at.

  .PARAMETER ResourceSuffix
  Specifies the suffix the resources name are built with.
#>

param (
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup,
    [Parameter(Mandatory=$false)]
    [string]$ResourceSuffix
)

$PSVersion = $PSVersionTable.PSVersion;
if ($PSVersion.Major -lt 7) {
  Write-Host "`n==============================================================================================================================";
  Write-Host "  This tool only supports " -ForegroundColor White -NoNewline;
  Write-Host "PowerShell 7+" -ForegroundColor Magenta -NoNewline;
  Write-Host "." -ForegroundColor White;
  Write-Host "  PowerShell '" -ForegroundColor White -NoNewline;
  Write-Host "$($PSVersion.Major).$($PSVersion.Minor)" -ForegroundColor Yellow -NoNewline;
  Write-Host "' was detected!. To run this tool successfully, please upgrade PowerShell to version '" -ForegroundColor White -NoNewline;
  Write-Host "7.0" -ForegroundColor Green -NoNewline;
  Write-Host "' or higher." -ForegroundColor White;
  Write-Host "  For more information, refer to the following documentation:" -ForegroundColor White;
  Write-Host "   - https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-windows?view=powershell-7.1" -ForegroundColor Cyan;
  Write-Host "==============================================================================================================================`n";
  exit 1;
}

$Inputs = @{};
$RootFolder = Resolve-Path -Path "$PSScriptRoot\..";
$Settings = @{
  ResourceGroup      = 'BFFN'
  GroupsSuffix       = @(
    'DotNet'
    'JS'
    'Python'
  )
  AppSettingsPath    = "$RootFolder\appsettings.json";
  AppSettingsDevPath = "$RootFolder\appsettings.Development.json";
  BotSettings        = [System.Collections.Concurrent.ConcurrentDictionary[string, object]]::new()
  BotResources       = [System.Collections.Concurrent.ConcurrentDictionary[string, object]]::new()
}

# Check if user is logged in
$LoginOutput = az ad signed-in-user show --only-show-errors --output none 2>&1;
if ($LoginOutput) {
  $LoginOutput;
  exit 1;
}

# Resource Group Input
if ([string]::IsNullOrEmpty($ResourceGroup)) {
  Write-Host "? " -ForegroundColor Green -NoNewline;
  Write-Host "Enter the Resource Group where the Bots are located: " -ForegroundColor White -NoNewline;
  Write-Host "default (" -ForegroundColor DarkGray -NoNewline;
  Write-Host $Settings.ResourceGroup -ForegroundColor Magenta -NoNewline;
  Write-Host ") " -ForegroundColor DarkGray -NoNewline;
  $Inputs.ResourceGroup = (Read-Host).Trim();
} 
else {
  $Inputs.ResourceGroup = $ResourceGroup;
}

# Resource Suffix Input
if ([string]::IsNullOrEmpty($ResourceSuffix)) {
  do {
    Write-Host "? " -ForegroundColor Green -NoNewline;
    Write-Host "Enter the Suffix used for the Bot Resources: " -ForegroundColor White -NoNewline;
    Write-Host "eg. (microsoft-396) " -ForegroundColor DarkGray -NoNewline;
    $Inputs.ResourceSuffix = (Read-Host).Trim();
    if ([string]::IsNullOrEmpty($Inputs.ResourceSuffix)) {
      Write-Host "  - The Bot Resource Suffix must be provided" -ForegroundColor Red;
    }
    else {
      break;
    }
  } while ($true)
} 
else {
  $Inputs.ResourceSuffix = $ResourceSuffix;
}

if ([string]::IsNullOrEmpty($Inputs.ResourceGroup)) {
  $Inputs.ResourceGroup = $Settings.ResourceGroup;
}

Write-Host "`nSummary" -ForegroundColor Cyan;
Write-Host "  - Resource Group  : " -ForegroundColor Gray -NoNewline;
Write-Host $Inputs.ResourceGroup -ForegroundColor Magenta;
Write-Host "  - Resource Suffix : " -ForegroundColor Gray -NoNewline;
Write-Host $Inputs.ResourceSuffix -ForegroundColor Magenta;

# Read AppSettings
Start-Sleep -Milliseconds 300;
Write-Host "`nAttempting to read the AppSettings" -ForegroundColor Cyan;
if (Test-Path -Path $Settings.AppSettingsDevPath -PathType Leaf) {
  Write-Host "  - Using existing '$($Settings.AppSettingsDevPath)' file" -ForegroundColor Gray;
}
else {
  Write-Host "  - Configuration file not found. Creating the '$($Settings.AppSettingsDevPath)' file" -ForegroundColor Gray;
  if (-not (Test-Path -Path $Settings.AppSettingsPath  -PathType Leaf)) {
    Write-Host "  - The 'appsettings.json' file to use as baseline was not found at '$($Settings.AppSettingsPath)'" -ForegroundColor Red;
    exit 1;
  }

  Copy-Item -Path $Settings.AppSettingsPath  -Destination $Settings.AppSettingsDevPath
  Write-Host "  - AppSettings file created" -ForegroundColor Gray;
}

$AppSettings = Get-Content $Settings.AppSettingsDevPath | ConvertFrom-Json;

# Check Bot Resources existence
Start-Sleep -Milliseconds 300;
Write-Host "`nChecking Bot Resources" -ForegroundColor Cyan;
Write-Host "  - Looking for Bot Resources existence..." -ForegroundColor Gray;
$NonExistingResources = $AppSettings.HostBotClientOptions.PSObject.Properties | ForEach-Object -Parallel {
  $GroupsSuffix = $using:Settings.GroupsSuffix;
  $ResourceGroup = $using:Inputs.ResourceGroup;
  $ResourceSuffix = $using:Inputs.ResourceSuffix;

  $Bot = $_;
  $BotId = "bffn$($Bot.Name)".ToLower();
  $Resource = "$BotId$ResourceSuffix";
  $ResourceGroupSuffix = $GroupsSuffix | Where-Object { $Bot.Name -like "*$($_)*" }
  $ResourceGroup = "$ResourceGroup-$ResourceGroupSuffix";

  $exists = (az webapp show --name $Resource --resource-group $ResourceGroup 2>$null | ConvertFrom-Json).enabled;

  return [PSCustomObject]@{
    Bot              = $Bot.Name
    'Resource Group' = $ResourceGroup
    Resource         = $Resource
    Exists           = if ($exists) { $true } else { $false }
  }
} | Where-Object { $_.Exists -eq $false }

if ($NonExistingResources) {
  Write-Host "`nThe following Bot Resources were not found. Check if they're still available in Azure." -ForegroundColor Red;
  $NonExistingResources | Select-Object 'Resource Group', 'Resource' | Format-Table -AutoSize;
  exit 1;
}
else {
  Write-Host "  - All Bot Resources were found" -ForegroundColor Gray;
}

# Configure AppSetting
Start-Sleep -Milliseconds 300;
Write-Host "`nConfiguring AppSettings" -ForegroundColor Cyan;
Write-Host "  - Getting DirectLine Secrets from Bot Resources..." -ForegroundColor Gray;
$AppSettings.HostBotClientOptions.PSObject.Properties | ForEach-Object -Parallel {
  $GroupsSuffix = $using:Settings.GroupsSuffix;
  $ResourceGroup = $using:Inputs.ResourceGroup;
  $ResourceSuffix = $using:Inputs.ResourceSuffix;
  $BotSettings = $using:Settings.BotSettings;
  $BotResources = $using:Settings.BotResources;

  $Bot = $_;
  $BotId = "bffn$($Bot.Name)".ToLower();
  $Resource = "$BotId$ResourceSuffix";
  $ResourceGroupSuffix = $GroupsSuffix | Where-Object { $Bot.Name -like "*$($_)*" };
  $ResourceGroup = "$ResourceGroup-$ResourceGroupSuffix";

  $DirectLine = (az Bot directline show --name $Resource --resource-group $ResourceGroup --with-secrets true 2>$null | ConvertFrom-Json).properties.properties.sites.key;

  $BotResource = @{
    Resource      = $Resource
    ResourceGroup = $ResourceGroup
  }
  $BotResources.TryAdd($Bot.Name, $BotResource) 1>$null;

  $Settings = @{
    BotId            = $Resource
    DirectLineSecret = $DirectLine
  }
  $BotSettings.TryAdd($Bot.Name, $Settings) 1>$null;
}

$AppSettings.HostBotClientOptions = $Settings.BotSettings;

$AppSettings | ConvertTo-Json | Set-Content $Settings.AppSettingsDevPath;
Write-Host "  - AppSettings successfully configured" -ForegroundColor Gray;

# Bot Configuration output
Start-Sleep -Milliseconds 300;
Write-Host "`nConfiguration saved" -ForegroundColor Cyan;
$Settings.BotResources.GetEnumerator() | ForEach-Object { 
  return [PSCustomObject]@{ 
    Bot                 = $_.Key
    'Resource Group'    = $_.Value.ResourceGroup
    Resource            = $_.Value.Resource
    'DirectLine Secret' = $Settings.BotSettings[$_.Key].DirectLineSecret
  }
} | Format-Table -AutoSize;

Write-Host "Process Finished!" -ForegroundColor Green;
