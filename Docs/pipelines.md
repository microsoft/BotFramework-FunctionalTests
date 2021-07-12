# Pipelines

## 01 - Create Shared Resources Pipeline

- **Description**: Creates all the long-term resources required.
- **Schedule**: Quarterly or on demand.
- **YAML**: [build\yaml\sharedResources\createSharedResources.yml](../build/yaml/sharedResources/createSharedResources.yml)

| Variable Name | Source | Description |
| - | - | - |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](./addARMServiceConnection.md) to see how to set it up. |
| **AppServicePlanPricingTier** | User | (optional) Pricing Tier for App Service Plans. **Default value is F1.** |
| **ContainerRegistryPricingTier** | User | (optional) Pricing Tier for Container Registry. **Default value is Basic.** |
| **KeyVaultObjectId** | Azure | Suscription's Object Id to create the keyvault to store App Registrations in Azure. Click [here](./getServicePrincipalObjectID.md) to see how to get it. |
| **ResourceGroupName** | User | (optional) Name for the resource group that will contain the shared resources. |
| **ResourceSuffix** | User | (optional) Suffix to add to the resources' name to avoid collisions. |

## 02 - Deploy Bot Resources Pipeline

- **Description:** Creates the test bot resources to be used in the functional tests, separated in a Resource Group for each language (DotNet, JS, and Python)
- **Schedule**: Nightly or on-demand.
- **YAML**: [build\yaml\deployBotResources\deployBotResources.yml](../build/yaml/deployBotResources/deployBotResources.yml)

| Variable Name | Source | Description |
| - | - | - |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](./addARMServiceConnection.md) to see how to set it up. |
| **AppServicePlanGroup** | Create Shared Resources | (optional) Name of the Resource Group where the Windows App Service Plan is located. |
| **AppServicePlanGroupLinux** | Create Shared Resources | (optional) Name of the Resource Group where the Linux App Service Plan is located. |
| **AppServicePlanDotNetName** | Create Shared Resources | (optional) Name of the DotNet App Service Plan. |
| **AppServicePlanJSName** | Create Shared Resources | (optional) Name of the JavaScript App Service Plan. |
| **AppServicePlanPythonName** | Create Shared Resources | (optional) Name of the Python App Service Plan. |
| **BotPricingTier** | User | (optional) Pricing tier for the Web App resources. ***Default value is F0.** |
| **ContainerRegistryName** | User | (optional) Name of the container registry. |
| **ResourceGroup** | User | (optional) Name of the Resource Group where the bots will be deployed. |
| **ResourceSuffix** | Create Shared Resources | (optional) Suffix to add to the resources' name to avoid collisions. |
| **SharedResourceGroup** | Create Shared Resources | (optional) Name of the Shared Resource Group where the shared resources are located. |
| **VirtualNetworkName** | Create Shared Resources | (optional) Name of the Virtual Network resource. |
| **[BotName](#botnames) + AppId** | [App Registration Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) | (optional) App ID to use. If not configured, will be retrieved from the key vault. |
| **[BotName](#botnames) + AppSecret** | [App Registration Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) | (optional) App Secret to use. If not configured, will be retrieved from the key vault. |

The following parameters will be displayed in the run pipeline blade.

| Parameter Name | Source | Description |
| - | - | - |
| **[Language](#dependency-variables-language) Hosts Registry** | User | (optional) Source from which the Bot Builder dependencies will be downloaded for selected host bots. [**More info**](#dependency-variables-language) |
| **[Language](#dependency-variables-language) Skills Registry** | User | (optional) Source from which the Bot Builder dependencies will be downloaded for selected skill bots. [**More info**](#dependency-variables-language) |
| **[Language](#dependency-variables-language) Skills V3 Registry** | User | (optional) Source from which the Bot Builder dependencies will be downloaded for selected V3 skill bots. [**More info**](#dependency-variables-language) |
| **[Language](#dependency-variables-language) Hosts Version** | User | (optional) Bot Builder dependency version to use for selected host bots. **Possible values are: Latest (default), Stable, or specific version numbers.** |
| **[Language](#dependency-variables-language) Skills Version** | User | (optional) Bot Builder dependency version to use for selected skill bots. **Possible values are: Latest (default), Stable, or specific version numbers.** |
| **[Language](#dependency-variables-language) Skills V3 Version** | User | (optional) Bot Builder dependency version to use for selected V3 skill bots. **Possible values are: Latest (default), Stable, or specific version numbers.** |

## 03 - Run Test Scenarios Pipeline

- **Description:** Configures and executes the test scenarios.
- **Schedule**: Nightly (after Deploy Bot Resources) or on demand.
- **YAML**: [build\yaml\testScenarios\runTestScenarios.yml](../build/yaml/testScenarios/runTestScenarios.yml)

| Variable Name | Source | Description |
| - | - | - |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](./addARMServiceConnection.md) to see how to set it up. |
| **ResourceGroup** | User | (optional) Name of the Resource Group where the bots are deployed. |
| **ResourceSuffix** | Create Shared Resources | (optional) Suffix to add to the resources' name to avoid collitions. |
| **[BotName](#botnames) + AppId** | [App Registration Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) | (optional) App ID to use. If not configured, it will be retrieved from the key vault. |
| **ResourceSuffix** | Create Bot Resources | (optional) Deploy Bot Resources pipeline GUID. |

## 04 - Cleanup Resources Pipeline

- **Description:** Removes all resources, including all the shared resources, bots, and app registrations.
- **Schedule**: Quarterly or on demand.
- **YAML**: [build\yaml\cleanupResources\cleanupResources.yml](../build/yaml/cleanupResources/cleanupResources.yml)

| Variable Name | Source | Description |
| - | - | - |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](./addARMServiceConnection.md) to see how to set it up. |
| **DeployResourceGroup** | Deploy Bot Resources | (optional) Name of the Resource Group containing the bots. |
| **ResourceSuffix** | Create Shared Resources | (optional) Suffix to add to the resources' name to avoid collitions. |
| **SharedResourceGroup** | Create Shared Resources | (optional) Name of the Resource Group containing the shared resources. |

### Dependency Variables

These are the available languages for the dependencies registry and version variables:

You can choose between one of the following options to select the package's feed.

- DotNet
  - Artifacts (default)
  - MyGet (default for V3 skill)
  - NuGet
- JS
  - MyGet (default)
  - Npm
- Python (Not available for SkillsV3)
  - Artifacts (default)
  - Pypi
  - Test.Pypi

The version parameters support the following options.

- LATEST: this is the default, and will look for the most recent version.
- STABLE: will look for the most recent Stable version.
- custom: you can enter a specific version to look for in the feed, ie `4.14.0.20210416.dev236130`

Note: The artifact feed doesn't contain stable versions for DotNet packages.

### BotNames

As of now, these are the bots available. This list will be expanded in the future.

- DotNet
  - Consumers
    - BffnSimpleHostBotDotNet
    - BffnSimpleHostBotDotNet21
    - BffnSimpleComposerHostBotDotNet
    - BffnWaterfallHostBotDotNet
  - Skills
    - BffnEchoSkillBotDotNet
    - BffnEchoSkillBotDotNet21
    - BffnEchoSkillBotDotNetV3
    - BffnEchoComposerSkillBotDotNet
    - BffnWaterfallSkillBotDotNet

- JS
  - Consumers
    - BffnSimpleHostBotJS
    - BffnWaterfallHostBotJS
  - Skills
    - BffnEchoSkillBotJS
    - BffnEchoSkillBotJSV3
    - BffnWaterfallSkillBotJS

- Python
  - Consumers
    - BffnSimpleHostBotPython
    - BffnWaterfallHostBotPython
  - Skills
    - BffnEchoSkillBotPython
    - BffnWaterfallSkillBotPython
