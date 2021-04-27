# Pipelines

## 01 - Create Shared Resources Pipeline

- **Description**: Creates all the long-term resources required.
- **Schedule**: Quarterly or on demand.
- **YAML**: [build\yaml\sharedResources\createSharedResources.yml](../build/yaml/sharedResources/createSharedResources.yml)

| Variable Name | Source | Description |
| - | - | - |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](./addARMServiceConnection.md) to see how to set it up. |
| **KeyVaultObjectId** | Azure | Object ID of the Service Principal configured in the pipeline. Click [here](./getServicePrincipalObjectID.md) to see how to get it. |
| **ResourceGroupName** | User | Name for the two resource groups that will contain the shared resources. |
| **AppServicePlanPricingTier** | User | (optional) Pricing tier for the App Service Plan. **Possible values are: F1 (default), S1.** |
| **ResourceSuffix** | User | (optional) Suffix to add to the resource names to avoid collisions. |

## 02 - Deploy Bot Resources Pipeline

- **Description:** Creates the test bot resources to be used in the functional tests, separated in one Resource Group for each language (DotNet, JS, and Python)
- **Schedule**: Nightly or on demand.
- **YAML**: [build\yaml\deployBotResources\deployBotResources.yml](../build/yaml/deployBotResources/deployBotResources.yml)

| Variable Name | Source | Description |
| - | - | - |
| **AppServicePlanGroupLinux** | Create Shared Resources | Name of the resource group containing the App Service Plan for Python. |
| **AppServicePlanNameLinux** | Create Shared Resources | Name of the App Service Plan for Python. |
| **AppServicePlanGroup** | Create Shared Resources | Name of the resource group containing the App Service Plan for DotNet and JS. |
| **AppServicePlanName** | Create Shared Resources | Name of the App Service Plan for DotNet and JS. |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](./addARMServiceConnection.md) to see how to set it up. |
| **ResourceGroup** | User | Prefix of the resource groups where the bots will be deployed. |
| **[BotName](#botnames) + AppId** | [App Registration Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) | (optional) App ID to use. If not configured, will be retrieved from the key vault. |
| **[BotName](#botnames) + AppSecret** | [App Registration Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) | (optional) App Secret to use. If not configured, will be retrieved from the key vault. |
| **BotPricingTier** | User | (optional) Pricing tier for the Web App resources. **Possible values are: F0 (default), S1.** |
| **ResourceSuffix** | Create Shared Resources | (optional) Suffix to add to the resource names to avoid collisions. |

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
| **ResourceGroup** | User | Prefix of the resource groups where the bots are deployed. |
| **[BotName](#botnames) + AppId** | [App Registration Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) | (optional) App ID to use. If not configured, it will be retrieved from the key vault. |
| **ResourceSuffix** | Create Shared Resources | (optional) Suffix added to the resource names. |

## 04 - Cleanup Resources Pipeline

- **Description:** Removes all resources, including all the shared resources, bots, and app registrations.
- **Schedule**: Quarterly or on demand.
- **YAML**: [build\yaml\cleanupResources\cleanupResources.yml](../build/yaml/cleanupResources/cleanupResources.yml)

| Variable Name | Source | Description |
| - | - | - |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](./addARMServiceConnection.md) to see how to set it up. |
| **DeployResourceGroup** | Deploy Bot Resources  | Prefix of the resource groups where the bots were deployed. |
| **SharedResourceGroup** | Create Shared Resources | Name for the resource groups that contains the shared resources. |
| **ResourceSuffix** | Create Shared Resources | (optional) Suffix added to the resource names. |

### Dependency Variables

These are the available lenguages for the dependencies registry and version variables:

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

The version parameters support LATEST (default), STABLE, or a specific version.

Note: npm and nuget feeds only supports stable versions, fill the corresponding variable with a specific version or set it to `stable`.

### BotNames

As of now, these are the bots available. This list will be expanded in the future.

- BffnSimpleHostBotDotNet
- BffnSimpleHostBotDotNet21
- BffnSimpleComposerHostBotDotNet
- BffnEchoComposerSkillBotDotNet
- BffnEchoSkillBotDotNet
- BffnEchoSkillBotDotNet21
- BffnEchoSkillBotDotNetV3
- BffnWaterfallHostBotDotNet
- BffnWaterfallSkillBotDotNet
- BffnSimpleHostBotJS
- BffnEchoSkillBotJS
- BffnEchoSkillBotJSV3
- BffnSimpleHostBotPython
- BffnEchoSkillBotPython
