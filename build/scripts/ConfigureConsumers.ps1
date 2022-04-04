
param (
  [Parameter(Mandatory = $true)]
  [string]$Scenario,
  [Parameter(Mandatory = $true)]
  [string]$ResourceGroup,
  [Parameter(Mandatory = $true)]
  [string]$ResourceSuffix,
  [Parameter(Mandatory = $true)]
  [string]$SharedResourceGroup,
  [Parameter(Mandatory = $true)]
  [string]$SharedResourceSuffix,
  [Parameter(Mandatory = $false)]
  [string]$ComposerSkillBotDotNetAppId,
  [Parameter(Mandatory = $false)]
  [string]$EchoSkillBotComposerDotNetAppId,
  [Parameter(Mandatory = $false)]
  [string]$EchoSkillBotDotNetAppId,
  [Parameter(Mandatory = $false)]
  [string]$EchoSkillBotDotNetSTAppId,
  [Parameter(Mandatory = $false)]
  [string]$EchoSkillBotDotNetV3AppId,
  [Parameter(Mandatory = $false)]
  [string]$EchoSkillBotJSAppId,
  [Parameter(Mandatory = $false)]
  [string]$EchoSkillBotJSSTAppId,
  [Parameter(Mandatory = $false)]
  [string]$EchoSkillBotJSV3AppId,
  [Parameter(Mandatory = $false)]
  [string]$EchoSkillBotPythonAppId,
  [Parameter(Mandatory = $false)]
  [string]$WaterfallSkillBotDotNetAppId,
  [Parameter(Mandatory = $false)]
  [string]$WaterfallSkillBotJSAppId,
  [Parameter(Mandatory = $false)]
  [string]$WaterfallSkillBotPythonAppId,
  [Parameter(Mandatory = $false)]
  [string]$KeyVault = ""
)

# Helper Functions.

$noBotsFoundMessage = "No bots were found in the configuration.";

function AddTimeStamp {
  param($text)
  return "$("[{0:MM/dd/yy} {0:HH:mm:ss}]" -f (Get-Date)): $text";
}

function AddBotsSuffix {
  param($bots, $suffix)
  # Add a suffix for each bot.

  if (-not $bots) {
    Write-Host $(AddTimeStamp -text $noBotsFoundMessage);
    return $bots;
  }

  return $bots | ForEach-Object {
    $bot = $_;
    $bot.resourceBotName = $bot.botName + $suffix;
    return $bot;
  }
}

function AddBotsAppIdFromKeyVault {
  param($bots, $keyVault)
  # Load AppIds from KeyVault.

  if (-not $bots) {
    Write-Host $(AddTimeStamp -text $noBotsFoundMessage);
    return $bots;
  }

  $AddTimeStampDef = $function:AddTimeStamp.ToString();

  return $bots | ForEach-Object -Parallel {
    $bot = $_;
    $function:AddTimeStamp = $using:AddTimeStampDef
    $keyVault = $using:keyVault
    $appTypes = $using:appTypes
    $sharedResourceSuffix = $using:SharedResourceSuffix
    $sharedResourceGroup = $using:SharedResourceGroup

    if ($appTypes.UserAssignedMSI -eq $bot.appType) {
      $bot.appId = (az identity show --name "$($bot.botName)$sharedResourceSuffix" --resource-group $sharedResourceGroup | ConvertFrom-Json).clientId;
      Write-Host $(AddTimeStamp -text "$($bot.key): Using AppId from the UserAssignedMSI resource.");

    }
    elseif ([string]::IsNullOrEmpty($bot.appId)) {
      Write-Host $(AddTimeStamp -text "$($bot.key): Unable to find the AppId in the Pipeline Variables, proceeding to search in the KeyVault '$keyVault'.");

      $entry = az keyvault secret list --vault-name $keyVault --query "[?name == 'Bffn$($bot.key)AppId']" | ConvertFrom-Json;

      if ($entry) {
        $secretVault = az keyvault secret show --id $entry.id | ConvertFrom-Json;
        $bot.appId = $secretVault.value;
      }
      else {
        Write-Host $(AddTimeStamp -text "$($bot.key): Unable to find the AppId in the KeyVault '$keyVault'.");
      }
    }
    else {
      Write-Host $(AddTimeStamp -text "$($bot.key): Using AppId from the Pipeline Variable.");
    }

    return $bot;
  }
}

function FilterBotsByScenario {
  param($bots, $scenarios, $scenario)
  # Filter bots by a specific test scenario.

  if (-not $bots) {
    Write-Host $(AddTimeStamp -text $noBotsFoundMessage);
    return $bots;
  }

  $scenarioSelected = $scenarios | Where-Object { $_.name -eq $scenario }

  if (-not $scenarioSelected) {
    Write-Host $(AddTimeStamp -text "$($scenario): Unable to find the Test Scenario.");
    return @();
  }

  return $bots | Where-Object {
    $bot = $_;

    $scenarioBots = $scenarioSelected.consumers + $scenarioSelected.skills;
    return $scenarioBots -contains $bot.key;
  }
}

function FilterResourceGroupsByExistence {
  param($groups)
  # Filter created resource groups.

  $AddTimeStampDef = $function:AddTimeStamp.ToString();
          
  return $groups.GetEnumerator() | ForEach-Object -Parallel {
    $function:AddTimeStamp = $using:AddTimeStampDef
    $group = $_;

    $exists = (az group exists -n $group.Value) -eq "true";
    if ($exists) {
      Write-Host $(AddTimeStamp -text "$($group.Value): Resource Group found.");
      return $group;
    }
    else {
      Write-Host $(AddTimeStamp -text "$($group.Value): Unable to find the Resource Group.");
    }
  }
}

function FilterBotsByResourceExistence {
  param($groups, $bots)
  # Filter bots only if their resource exists in Azure.

  if (-not $bots) {
    Write-Host $(AddTimeStamp -text $noBotsFoundMessage);
    return $bots;
  }

  $AddTimeStampDef = $function:AddTimeStamp.ToString();

  return $bots | ForEach-Object -Parallel {
    $groups = $using:groups
    $function:AddTimeStamp = $using:AddTimeStampDef
    $bot = $_;

    if ($groups.Value -contains $bot.resourceGroup) {
      $enabled = (az webapp show --name $bot.resourceBotName --resource-group $bot.resourceGroup 2>$null | ConvertFrom-Json).enabled;

      if ($enabled) {
        Write-Host $(AddTimeStamp -text "$($bot.key): Resource '$($bot.resourceBotName)' found.");
        return $bot;
      }
      else {
        Write-Host $(AddTimeStamp -text "$($bot.key): Unable to find the resource '$($bot.resourceBotName)'.");
      }
    }
  };
}

function FilterBotsWithAppId {
  param($bots)
  # Filter bots that have an AppId.

  if (-not $bots) {
    Write-Host $(AddTimeStamp -text $noBotsFoundMessage);
    return $bots;
  }

  return @($bots | Where-Object {
      $bot = $_;

      if ($bot.appId.Trim().Length -eq 0) {
        Write-Host $(AddTimeStamp -text "$($bot.key): AppId not found in the configuration, Skiping ...");
        return $false;
      }

      return $true;
    })
}

function AddAzureAppSettings {
  param($consumers, $skills)
  # Add Azure AppSettings to each Consumer. 

  if (-not $consumers) {
    Write-Host $(AddTimeStamp -text $noBotsFoundMessage);
    return $consumers;
  }

  return $consumers | Sort-Object { $_.key } | ForEach-Object {
    $consumer = $_;
    $consumer.appSettings = @();

    $orderedSkills = $skills | Sort-Object { $_.key }

    for ($index = 0; $index -lt $orderedSkills.Count; $index++) {
      $skill = $orderedSkills[$index];

      switch ($consumer.configType) {
        $types.Appsettings { 
          $consumer.appSettings += @{name = "BotFrameworkSkills:$($index):Id"; value = "$($skill.key)" };
          $consumer.appSettings += @{name = "BotFrameworkSkills:$($index):AppId"; value = "$($skill.appId)" };
          $consumer.appSettings += @{name = "BotFrameworkSkills:$($index):SkillEndpoint"; value = "https://$($skill.resourceBotName).azurewebsites.net/api/messages" };
          $consumer.appSettings += @{name = "BotFrameworkSkills:$($index):Group"; value = "$($skill.group)" };
        }
        $types.Env { 
          $consumer.appSettings += @{name = "skill_$($skill.key)_appId"; value = "$($skill.appId)" };
          $consumer.appSettings += @{name = "skill_$($skill.key)_endpoint"; value = "https://$($skill.resourceBotName).azurewebsites.net/api/messages" };
          $consumer.appSettings += @{name = "skill_$($skill.key)_group"; value = "$($skill.group)" };
        }
        $types.Composer {
          # NOTE: Composer uses different capitalization for the skill keys.
          $consumer.appSettings += @{name = "skill__$($skill.keyComposer)__msAppId"; value = "$($skill.appId)" };
          $consumer.appSettings += @{name = "skill__$($skill.keyComposer)__endpointUrl"; value = "https://$($skill.resourceBotName).azurewebsites.net/api/messages" };
        }
      }
    }

    return $consumer;
  }
}

function ConfigureTestProjectAppSettings {
  param($bots, $appSettingsPath)
  # Save each bot direct line into the Test Project AppSettings file.

  if (-not $bots) {
    Write-Host $(AddTimeStamp -text $noBotsFoundMessage);
    return $bots;
  }

  $appSettings = Get-Content -Raw $appSettingsPath | ConvertFrom-Json;
  $appSettings.HostBotClientOptions = [System.Collections.Concurrent.ConcurrentDictionary[string, object]]::new();

  $AddTimeStampDef = $function:AddTimeStamp.ToString();

  $bots | ForEach-Object -Parallel {
    # Gets the Bot DirectLine
    $function:AddTimeStamp = $using:AddTimeStampDef
    $options = $using:appSettings.HostBotClientOptions
    $bot = $_;

    $tries = 3;
    $directLine = "";

    while ($tries -gt 0) {
      $directLine = (az bot directline show --name $bot.resourceBotName --resource-group $bot.resourceGroup --with-secrets true 2>$null | ConvertFrom-Json).properties.properties.sites.key;
      Write-Host $(AddTimeStamp -text "$($bot.key): Getting the DirectLine secret ($($directLine.Substring(0, 3) + "***")).");
              
      if (-not [string]::IsNullOrEmpty($directLine)) {
        $settings = @{
          DirectLineSecret = $directLine
          BotId            = $bot.botName
        }
        $options.TryAdd($bot.key, $settings) 1>$null;
        break;
      }
      $tries--;
    }
  }

  if ($appSettings.HostBotClientOptions.Count -ne $bots.Length) {
    Write-Host "##vso[task.logissue type=error]Some host bots' DirectLine secrets couldn't be retrieved from Azure."
    $config = $appSettings.HostBotClientOptions | Out-String
    Write-Host "##vso[task.logissue type=error]$config"
    exit 1 # Force exit
  }

  $appSettings | ConvertTo-Json | Set-Content $appsettingsPath;

  Write-Host $(AddTimeStamp -text "Test Project AppSettings saved:");
  $appSettings.HostBotClientOptions.GetEnumerator() | ForEach-Object { 
    return [PSCustomObject]@{ 
      Key              = $_.Key
      BotId            = $_.Value.BotId
      DirectLineSecret = $_.Value.DirectLineSecret.Substring(0, 3) + "***"
    } 
  } | Format-Table -AutoSize
}

function ConfigureConsumers {
  param($consumers, $skills)
  # Configure Consumers with all the Skills to connect to. 

  $AddTimeStampDef = $function:AddTimeStamp.ToString();

  Write-Host $(AddTimeStamp -text "Waiting for configuration to finish ...");
  $consumers | ForEach-Object -Parallel {
    $function:AddTimeStamp = $using:AddTimeStampDef
    $skills = $using:skills
    $types = $using:types

    $consumer = $_;
    $output = @();

    $conditions = @(
      "BotFrameworkSkills*"
      "skill_(.*)"
    )
          
    $output += AddTimeStamp -text "$($consumer.key): Looking for existing Azure AppSettings ...";
          
    $json = (az webapp config appsettings list --name $consumer.resourceBotName --resource-group $consumer.resourceGroup) | ConvertFrom-Json
    $appSettings = @($json | Where-Object { $_.name -match ($conditions -join "|") })

    $settings = @{
      toSet    = [System.Collections.ArrayList]$consumer.appSettings;
      toRemove = [System.Collections.ArrayList]@();
    }

    # Lookup for Azure AppSettings that are needed to be added/updated, otherwise, skip.
    foreach ($appSetting in $appSettings) {
      $setting = $settings.toSet | Where-Object { $_.name -eq $appSetting.name } | Select-Object -Unique
      if ($setting) {
        if ($setting.value -eq $appSetting.value) {
          $settings.toSet.Remove($setting);
        }
      }
      else {
        $settings.toRemove.Add($appSetting);
      }
    }

    if ($settings.toRemove) {
      $output += AddTimeStamp -text "$($consumer.key): Removing unnecessary Azure AppSettings ...";

      $config = $settings.toRemove | ForEach-Object { $_.name }
      az webapp config appsettings delete --name $consumer.resourceBotName --resource-group $consumer.resourceGroup --setting-names $config --output none

      $output += AddTimeStamp -text "$($consumer.key): Azure AppSettings removed:";
      $output += $config | ForEach-Object { [PSCustomObject]@{ Name = $_ } } | Format-Table -AutoSize;
    }

    if ($settings.toSet) {
      $output += AddTimeStamp -text "$($consumer.key): Adding new Azure AppSettings ...";

      $config = $settings.toSet | ForEach-Object { "$($_.name)=$($_.value)" }
      az webapp config appsettings set --name $consumer.resourceBotName --resource-group $consumer.resourceGroup --settings $config --output none

      $output += AddTimeStamp -text "$($consumer.key): Azure AppSettings added:";
      # Format output
      $output += $settings.toSet | ForEach-Object { 
        $setting = $_;

        if ($setting.name.ToLower().EndsWith("appid")) {
          $setting.value = $setting.value.Substring(0, 3) + "***"
        }

        return [PSCustomObject]@{ 
          Name  = $setting.name
          Value = $setting.value
        }
      } | Format-Table -AutoSize
    }

    if (-not $settings.toSet -and -not $settings.toRemove) {
      $output += AddTimeStamp -text "$($consumer.key): Azure AppSettings are up to date.";
    }

    $output;
  }
}

# Configuration

# Type of setting to use for the AppSettings variables.
$types = @{
  Appsettings = 0
  Env         = 1
  Composer    = 2
}

# Type of bot authentication.
$appTypes = @{
  MultiTenant     = "MultiTenant"
  SingleTenant    = "SingleTenant"
  UserAssignedMSI = "UserAssignedMSI"
}

# Bots Resource Groups
$groups = @{
  DotNet = "$ResourceGroup-DotNet"
  JS     = "$ResourceGroup-JS"
  Python = "$ResourceGroup-Python"
}

# Bots Settings
$consumers = @(
  @{
    key           = "SimpleHostBotDotNet"
    botName       = "bffnsimplehostbotdotnet"
    resourceGroup = $groups.DotNet
    configType    = $types.Appsettings
  }
  @{
    key           = "SimpleHostBotDotNetMSI"
    botName       = "bffnsimplehostbotdotnetmsi"
    resourceGroup = $groups.DotNet
    configType    = $types.Appsettings
  }
  @{
    key           = "SimpleHostBotDotNetST"
    botName       = "bffnsimplehostbotdotnetst"
    resourceGroup = $groups.DotNet
    configType    = $types.Appsettings
  }
  @{
    key           = "SimpleHostBotComposerDotNet"
    botName       = "bffnsimplehostbotcomposerdotnet"
    resourceGroup = $groups.DotNet
    configType    = $types.Composer
  }
  @{
    key           = "ComposerHostBotDotNet"
    botName       = "bffncomposerhostbotdotnet"
    resourceGroup = $groups.DotNet
    configType    = $types.Composer
  }
  @{
    key           = "WaterfallHostBotDotNet"
    botName       = "bffnwaterfallhostbotdotnet"
    resourceGroup = $groups.DotNet
    configType    = $types.Appsettings
  }
  @{
    key           = "SimpleHostBotJS"
    botName       = "bffnsimplehostbotjs"
    resourceGroup = $groups.JS
    configType    = $types.Env
  }
  @{
    key           = "SimpleHostBotJSMSI"
    botName       = "bffnsimplehostbotjsmsi"
    resourceGroup = $groups.JS
    configType    = $types.Env
  }
  @{
    key           = "SimpleHostBotJSST"
    botName       = "bffnsimplehostbotjsst"
    resourceGroup = $groups.JS
    configType    = $types.Env
  }
  @{
    key           = "WaterfallHostBotJS"
    botName       = "bffnwaterfallhostbotjs"
    resourceGroup = $groups.JS
    configType    = $types.Env
  }
  @{
    key           = "SimpleHostBotPython"
    botName       = "bffnsimplehostbotpython"
    resourceGroup = $groups.Python
    configType    = $types.Env
  }
  @{
    key           = "WaterfallHostBotPython"
    botName       = "bffnwaterfallhostbotpython"
    resourceGroup = $groups.Python
    configType    = $types.Env
  }
)

$skills = @(
  @{
    appType       = $appTypes.MultiTenant
    key           = "EchoSkillBotDotNet"
    keyComposer   = "echoSkillBotDotNet" 
    botName       = "bffnechoskillbotdotnet"
    appId         = $EchoSkillBotDotNetAppId
    resourceGroup = $groups.DotNet
    group         = "Echo"
  }
  @{
    appType       = $appTypes.UserAssignedMSI
    key           = "EchoSkillBotDotNetMSI"
    keyComposer   = "echoSkillBotDotNetMSI" 
    botName       = "bffnechoskillbotdotnetmsi"
    appId         = $EchoSkillBotDotNetMSIAppId
    resourceGroup = $groups.DotNet
    group         = "Echo"
  }
  @{
    appType       = $appTypes.SingleTenant
    key           = "EchoSkillBotDotNetST"
    keyComposer   = "echoSkillBotDotNetST" 
    botName       = "bffnechoskillbotdotnetst"
    appId         = $EchoSkillBotDotNetSTAppId
    resourceGroup = $groups.DotNet
    group         = "Echo"
  }
  @{
    appType       = $appTypes.MultiTenant
    key           = "EchoSkillBotDotNetV3"
    keyComposer   = "echoSkillBotDotNetV3" 
    botName       = "bffnechoskillbotdotnetv3"
    appId         = $EchoSkillBotDotNetV3AppId
    resourceGroup = $groups.DotNet
    group         = "Echo"
  }
  @{
    appType       = $appTypes.MultiTenant
    key           = "EchoSkillBotComposerDotNet"
    keyComposer   = "echoSkillBotComposerDotNet" 
    botName       = "bffnechoskillbotcomposerdotnet"
    appId         = $EchoSkillBotComposerDotNetAppId
    resourceGroup = $groups.DotNet
    group         = "Echo"
  }
  @{
    appType       = $appTypes.MultiTenant
    key           = "WaterfallSkillBotDotNet"
    keyComposer   = "waterfallSkillBotDotNet" 
    botName       = "bffnwaterfallskillbotdotnet"
    appId         = $WaterfallSkillBotDotNetAppId
    resourceGroup = $groups.DotNet
    group         = "Waterfall"
  }
  @{
    appType       = $appTypes.MultiTenant
    key           = "ComposerSkillBotDotNet"
    keyComposer   = "composerSkillBotDotNet" 
    botName       = "bffncomposerskillbotdotnet"
    appId         = $ComposerSkillBotDotNetAppId
    resourceGroup = $groups.DotNet
    group         = "Waterfall"
  }
  @{
    appType       = $appTypes.MultiTenant
    key           = "EchoSkillBotJS"
    keyComposer   = "echoSkillBotJs" 
    botName       = "bffnechoskillbotjs"
    appId         = $EchoSkillBotJSAppId
    resourceGroup = $groups.JS
    group         = "Echo"
  }
  @{
    appType       = $appTypes.UserAssignedMSI
    key           = "EchoSkillBotJSMSI"
    keyComposer   = "echoSkillBotJsMSI" 
    botName       = "bffnechoskillbotjsmsi"
    appId         = $EchoSkillBotJSMSIAppId
    resourceGroup = $groups.JS
    group         = "Echo"
  }
  @{
    appType       = $appTypes.SingleTenant
    key           = "EchoSkillBotJSST"
    keyComposer   = "echoSkillBotJsST" 
    botName       = "bffnechoskillbotjsst"
    appId         = $EchoSkillBotJSSTAppId
    resourceGroup = $groups.JS
    group         = "Echo"
  }
  @{
    appType       = $appTypes.MultiTenant
    key           = "EchoSkillBotJSV3"
    keyComposer   = "echoSkillBotJsV3" 
    botName       = "bffnechoskillbotjsv3"
    appId         = $EchoSkillBotJSV3AppId
    resourceGroup = $groups.JS
    group         = "Echo"
  }
  @{
    appType       = $appTypes.MultiTenant
    key           = "WaterfallSkillBotJS"
    keyComposer   = "waterfallSkillBotJS" 
    botName       = "bffnwaterfallskillbotjs"
    appId         = $WaterfallSkillBotJSAppId
    resourceGroup = $groups.JS
    group         = "Waterfall"
  }
  @{
    appType       = $appTypes.MultiTenant
    key           = "EchoSkillBotPython"
    keyComposer   = "echoSkillBotPython" 
    botName       = "bffnechoskillbotpython"
    appId         = $EchoSkillBotPythonAppId
    resourceGroup = $groups.Python
    group         = "Echo"
  }
  @{
    appType       = $appTypes.MultiTenant
    key           = "WaterfallSkillBotPython"
    keyComposer   = "waterfallSkillBotPython" 
    botName       = "bffnwaterfallskillbotpython"
    appId         = $WaterfallSkillBotPythonAppId
    resourceGroup = $groups.Python
    group         = "Waterfall"
  }
)

# Bots Test Scenarios
$scenarios = @(
  @{ 
    name      = "SingleTurn"; 
    consumers = @(
      "SimpleHostBotComposerDotNet",
      "SimpleHostBotDotNet",
      "SimpleHostBotJS",
      "SimpleHostBotPython"
    );
    skills    = @(
      "EchoSkillBotComposerDotNet",
      "EchoSkillBotDotNet",
      "EchoSkillBotDotNetV3",
      "EchoSkillBotJS",
      "EchoSkillBotJSV3",
      "EchoSkillBotPython"
    );
  }
  @{ 
    name      = "Authentication"; 
    consumers = @(
      "SimpleHostBotDotNetMSI",
      "SimpleHostBotDotNetST",
      "SimpleHostBotJSMSI",
      "SimpleHostBotJSST"
    );
    skills    = @(
      "EchoSkillBotDotNetMSI",
      "EchoSkillBotDotNetST",
      "EchoSkillBotJSMSI",
      "EchoSkillBotJSST"
    );
  }
  @{ 
    name      = "Waterfall"; 
    consumers = @(
      "ComposerHostBotDotNet",
      "WaterfallHostBotDotNet",
      "WaterfallHostBotJS",
      "WaterfallHostBotPython"
    );
    skills    = @(
      "WaterfallSkillBotDotNet",
      "WaterfallSkillBotJS",
      "WaterfallSkillBotPython",
      "ComposerSkillBotDotNet"
    );
  }
)

# Pre-configure and filter bots.
Write-Host $(AddTimeStamp -text "Filtering bots by '$Scenario' scenario ...");
$consumersToConfigure = FilterBotsByScenario -bots $consumers -scenarios $scenarios -scenario $Scenario;
$skillsToConfigure = FilterBotsByScenario -bots $skills -scenarios $scenarios -scenario $Scenario;

Write-Host $(AddTimeStamp -text "Adding the suffix '$ResourceSuffix' to the bot resources ...");
$consumersToConfigure = AddBotsSuffix -bots $consumersToConfigure -suffix $ResourceSuffix
$skillsToConfigure = AddBotsSuffix -bots $skillsToConfigure -suffix $ResourceSuffix

Write-Host $(AddTimeStamp -text "Loading the Skills AppIds from the KeyVault '$KeyVault' when no Pipeline Variable is provided.");
$skillsToConfigure = AddBotsAppIdFromKeyVault -bots $skillsToConfigure -keyVault $KeyVault

Write-Host $(AddTimeStamp -text "Filtering bots that have an AppId assigned ...");
$skillsToConfigure = FilterBotsWithAppId -bots $skillsToConfigure

Write-Host $(AddTimeStamp -text "Filtering existing Resource Groups ...");
$resourceGroups = FilterResourceGroupsByExistence -groups $groups

Write-Host $(AddTimeStamp -text "Filtering deployed bots in Azure ...");
$consumersToConfigure = FilterBotsByResourceExistence -groups $resourceGroups -bots $consumersToConfigure
$skillsToConfigure = FilterBotsByResourceExistence -groups $resourceGroups -bots $skillsToConfigure

Write-Host $(AddTimeStamp -text "Adding Azure AppSettings to Consumers' configuration.");
$consumersToConfigure = AddAzureAppSettings -consumers $consumersToConfigure -skills $skillsToConfigure

if (-not $consumersToConfigure) {
  Write-Error $(AddTimeStamp -text "No Consumers were found to configure. Cancelling the configuration ...");
  return;
}

if (-not $skillsToConfigure) {
  Write-Error $(AddTimeStamp -text "No Skills were found to configure each Consumer. Cancelling the configuration ...");
  return;
}

# Configure steps.
Write-Host $(AddTimeStamp -text "Configuring the Test Project.");
ConfigureTestProjectAppSettings -bots $consumersToConfigure -appSettingsPath "Tests/Functional/appsettings.json";

Write-Host $(AddTimeStamp -text "Configuring the Consumer bots App Settings in Azure.");
ConfigureConsumers -consumers $consumersToConfigure -skills $skillsToConfigure

Write-Host $(AddTimeStamp -text "Process Finished!");
