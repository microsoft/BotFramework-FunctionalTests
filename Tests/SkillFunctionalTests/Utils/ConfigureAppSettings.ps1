# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

<#
  .SYNOPSIS
  Gets DirectLine Secrets from Azure.

  .DESCRIPTION
  Configure AppSettings file with DirectLine Secrets gathered from Azure Bot Resources based on the Resource Group, Resource Suffix and Subscription provided by the user.

  .PARAMETER ResourceGroup
  Specifies the Name for the specific Resource Group where the resources are deployed. For each specific language will concatenate (DotNet, JS and Python). Default (BFFN).

  .PARAMETER ResourceSuffix
  Specifies the Suffix used to concatenate at the end of the Resource Name followed up by the ResourceSuffixSeparator.

  .PARAMETER Subscription
  Specifies the Name or Id of the Subscription where the resources are located. Default (current Subscription).

  .PARAMETER ResourceSuffixSeparator
  Specifies the separator used for the Suffix to split the Resource Name from the ResourceSuffix. Default (-).

  .EXAMPLE
  PS> .\ConfigureAppSettings.ps1 -ResourceGroup "bffnbots" -ResourceSuffix "{buildId}"

  .EXAMPLE
  PS> .\ConfigureAppSettings.ps1 -ResourceGroup "bffnbots"

  .EXAMPLE
  PS> .\ConfigureAppSettings.ps1 -Subscription 00000000-0000-0000-0000-000000000000

  .EXAMPLE
  PS> .\ConfigureAppSettings.ps1 -Subscription bffnbots-subscription

  .EXAMPLE
  PS> .\ConfigureAppSettings.ps1 -ResourceSuffixSeparator "" -ResourceSuffix "microsoft-396"

  .EXAMPLE
  PS> .\ConfigureAppSettings.ps1
#>

param (
  [Parameter(Mandatory = $false)]
  [string]$Subscription,
  [Parameter(Mandatory = $false)]
  [string]$ResourceGroup,
  [Parameter(Mandatory = $false)]
  [string]$ResourceSuffixSeparator = "-",
  [Parameter(Mandatory = $false)]
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

# Subscription Input
if ([string]::IsNullOrEmpty($Subscription)) {
  $Subscriptions = @(az account list | ConvertFrom-Json);

  if (-not $Subscriptions) {
    Write-Host "There are no Subscriptions available." -ForegroundColor Red;
    exit 1;
  }

  $SubscriptionDefault = ($Subscriptions | Where-Object { $_.isDefault }) ?? $Subscriptions.GetValue(0);

  if ($Subscriptions.Length -gt 1) {
    do {
      Write-Host "! " -ForegroundColor Yellow -NoNewline;
      Write-Host "Multiple Subscriptions were found." -ForegroundColor White;
  
      $Subscriptions | Format-Table @(
        @{Label = ' # '; Expression = { [array]::IndexOf($Subscriptions, $_) + 1 } },
        @{Label = 'Name'; Expression = { $_.name } },
        @{Label = 'Id'; Expression = { $_.id } },
        @{Label = 'Default'; Expression = { $_.isDefault } }
      ) -AutoSize

      Write-Host "? " -ForegroundColor Green -NoNewline;
      Write-Host "Enter a Subscription, by providing '#', 'Name' or 'Id': " -ForegroundColor White -NoNewline;
      Write-Host "default (" -ForegroundColor DarkGray -NoNewline;
      Write-Host $SubscriptionDefault.name -ForegroundColor Magenta -NoNewline;
      Write-Host ") " -ForegroundColor DarkGray -NoNewline;

      $UserInput = (Read-Host).Trim();
    
      if ([string]::IsNullOrEmpty($UserInput)) {
        $SubscriptionInput = $SubscriptionDefault;
        break;
      }
      else {
        $SubscriptionInput = $Subscriptions | Where-Object {
          $Sub = $_;
          $Number = [array]::IndexOf($Subscriptions, $Sub) + 1;
          if (@($Number, $Sub.id, $Sub.name) -contains $UserInput) {
            return $true;
          }
        }
  
        if ($SubscriptionInput) {
          break;
        }
        else {
          Write-Host "  - A Subscription must be provided, could be either ('#', 'Name' or 'Id').`n" -ForegroundColor Red;
        }
      }
    } while ($true)

    $Inputs.Subscription = $SubscriptionInput.name;
  }
  else {
    $Inputs.Subscription = $SubscriptionDefault.name;
  }
} 
else {
  $Inputs.Subscription = $Subscription;
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

# Resource Suffix Separator Input
$Inputs.ResourceSuffixSeparator = $ResourceSuffixSeparator;

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
Write-Host "  - Subscription              : " -ForegroundColor Gray -NoNewline;
Write-Host $Inputs.Subscription -ForegroundColor Magenta;
Write-Host "  - Resource Group            : " -ForegroundColor Gray -NoNewline;
Write-Host $Inputs.ResourceGroup -ForegroundColor Magenta;
Write-Host "  - Resource Suffix           : " -ForegroundColor Gray -NoNewline;
Write-Host $Inputs.ResourceSuffix -ForegroundColor Magenta;
Write-Host "  - Resource Suffix Separator : " -ForegroundColor Gray -NoNewline;
Write-Host $Inputs.ResourceSuffixSeparator -ForegroundColor Magenta;

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
  $ResourceSuffixSeparator = $using:Inputs.ResourceSuffixSeparator;
  $ResourceSuffix = $using:Inputs.ResourceSuffix;
  $Subscription = $using:Inputs.Subscription;

  $Bot = $_;
  $BotId = "bffn$($Bot.Name)".ToLower();
  $Resource = "$BotId$ResourceSuffixSeparator$ResourceSuffix";
  $ResourceGroupSuffix = $GroupsSuffix | Where-Object { $Bot.Name -like "*$($_)*" }
  $ResourceGroup = "$ResourceGroup-$ResourceGroupSuffix";

  $exists = (az webapp show --name $Resource --resource-group $ResourceGroup --subscription $Subscription 2>$null | ConvertFrom-Json).enabled;

  return [PSCustomObject]@{
    BotId            = $Resource
    'Resource Group' = $ResourceGroup
    Exists           = if ($exists) { $true } else { $false }
  }
} | Where-Object { $_.Exists -eq $false } | Sort-Object -Property BotId

if ($NonExistingResources) {
  Write-Host "`nThe following Bot Resources were not found. Check if they're still available in Azure." -ForegroundColor Red;
  $NonExistingResources | Select-Object 'BotId', 'Resource Group' | Format-Table -AutoSize;
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
  $ResourceSuffixSeparator = $using:Inputs.ResourceSuffixSeparator;
  $ResourceSuffix = $using:Inputs.ResourceSuffix;
  $Subscription = $using:Inputs.Subscription;
  $BotSettings = $using:Settings.BotSettings;
  $BotResources = $using:Settings.BotResources;

  $Bot = $_;
  $BotId = "bffn$($Bot.Name)".ToLower();
  $Resource = "$BotId$ResourceSuffixSeparator$ResourceSuffix";
  $ResourceGroupSuffix = $GroupsSuffix | Where-Object { $Bot.Name -like "*$($_)*" };
  $ResourceGroup = "$ResourceGroup-$ResourceGroupSuffix";

  $DirectLine = (az bot directline show --name $Resource --resource-group $ResourceGroup --subscription $Subscription --with-secrets true 2>$null | ConvertFrom-Json).properties.properties.sites.key;

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

$SortedBotSettings = [ordered]@{};
$Settings.BotSettings.GetEnumerator() | Sort-Object -Property Key | ForEach-Object { $SortedBotSettings[$_.Key] = $_.Value };
$AppSettings.HostBotClientOptions = $SortedBotSettings;

$AppSettings | ConvertTo-Json | Set-Content $Settings.AppSettingsDevPath;
Write-Host "  - AppSettings successfully configured" -ForegroundColor Gray;

# Bot Configuration output
Start-Sleep -Milliseconds 300;
Write-Host "`nConfiguration saved" -ForegroundColor Cyan;
$Settings.BotResources.GetEnumerator() | ForEach-Object { 
  return [PSCustomObject]@{ 
    BotId               = $_.Value.Resource
    'DirectLine Secret' = $Settings.BotSettings[$_.Key].DirectLineSecret
    'Resource Group'    = $_.Value.ResourceGroup
  }
} | Sort-Object -Property BotId | Format-Table -AutoSize;

Write-Host "Process Finished!" -ForegroundColor Green;
