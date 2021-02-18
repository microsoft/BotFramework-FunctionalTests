# Pipelines

## Create Shared Resources Pipeline

- Description: Creates all the long-term resources required.
- Schedule: Quarterly or on demand.
- YAML: [build\yaml\sharedResources\createSharedResources.yml](../build/yaml/sharedResources/createSharedResources.yml)

| Variable Name | Source | Description |
| - | - | - |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](Guides/addARMServiceConnection.md) to see how to set it up. |
| **KeyVaultObjectId** | Azure | Object ID of the Service Principal configured in the pipeline. Click [here](Guides/getServicePrincipalObjectID.md) to see how to get it. |
| **ResourceGroupName** | User | Name for the two resource groups that will contain the shared resources. |
| **AppServicePlanPricingTier** | User | (optional) Pricing tier for the App Service Plan. Possible values are: F1 (default), S1. |
| **ResourceSuffix** | User | (optional) Suffix to add to the resource names to avoid collisions. |

## Deploy Bot Resources Pipeline

- Description: Creates the test bot resources to be used in the functional tests, separated in one Resource Group r each language (DotNet, JS, and Python)
- Schedule: Nightly or on demand.
- YAML: [build\yaml\deployBotResources\deployBotResources.yml](../build/yaml/deploybotresources/deployBotResources.yml)

| Variable Name | Source | Description |
| - | - | - |
| **AppServicePlanGroupLinux** | Create Shared Resources | Name of the resource group containing the App Service Plan for Python. |
| **AppServicePlanNameLinux** | Create Shared Resources | Name of the App Service Plan for Python. |
| **AppServicePlanGroup** | Create Shared Resources | Name of the resource group containing the App Service Plan for DotNet and JS. |
| **AppServicePlanName** | Create Shared Resources | Name of the App Service Plan for DotNet and JS. |
| **AzureDeploymentPassword** | [Webapp Deployment User](https://docs.microsoft.com/en-us/cli/azure/webapp/deployment/user?view=azure-cli-latest#az-webapp-deployment-user-set) | Azure Deployment Password, required to deploy Python bots. Click [here](Guides/createWebAppDeploymentCredentials.md) to see how to set it up. |
| **AzureDeploymentUser** | [Webapp Deployment User](https://docs.microsoft.com/en-us/cli/azure/webapp/deployment/user?view=azure-cli-latest#az-webapp-deployment-user-set) | Azure Deployment User, required to deploy Python bots. Click [here](Guides/createWebAppDeploymentCredentials.md) to see how to how to set it up. |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](Guides/addARMServiceConnection.md) to see how to set it up. |
| **ResourceGroup** | User | Prefix of the resource groups where the bots will be deployed. |
| **[BotName](#botnames) + AppId** | [App Registration Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) | (optional) App ID to use. If not configured, will be retrieved from the key vault. |
| **[BotName](#botnames) + AppSecret** | [App Registration Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) | (optional) App Secret to use. If not configured, will be retrieved from the key vault. |
| **BotPricingTier** | User | (optional) Pricing tier for the Web App resources.Possible values are: F0 (default), S1. |
| **DependenciesRegistryHosts** | User | (optional) Source from which the Bot Builder dependencies will be downloaded for all host bots. Possible values are: Artifacts (default), MyGet, NuGet, or specific sources. |
| **DependenciesRegistrySkills** | User | (optional) Source from which the Bot Builder dependencies will be downloaded for all skill bots. Possible values are: Artifacts (default), MyGet, NuGet, or specific sources. |
| **DependenciesRegistrySkillsV3** | User | (optional) Source from which the Bot Builder dependencies will be downloaded for all V3 skill bots. Possible values are: Artifacts, MyGet (default), NuGet, or specific sources. |
| **DependeciesVersionHosts** | User | (optional) Bot Builder dependency version to use for all hosts. Possible values are: Latest (default), Stable, or specific version numbers. |
| **DependeciesVersionSkills** | User | (optional) Bot Builder dependency version to use for all hosts. Possible values are: Latest (default), Stable, or specific version numbers. |
| **DependeciesVersionSkillsV3** | User | (optional) Bot Builder dependency version to use for all hosts. Possible values are: Latest (default), Stable, or specific version numbers. |
| **ResourceSuffix** | Create Shared Resources | (optional) Suffix to add to the resource names to avoid collisions. |

## Run Test Scenarios Pipeline

- Description: Configures and executes the test scenarios.
- Schedule: Nightly (after Deploy Bot Resources) or on demand.
- YAML: [build\yaml\testScenarios\runTestScenarios.yml](../build/yaml/testScenarios/runTestScenarios.yml)

| Variable Name | Source | Description |
| - | - | - |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](Guides/addARMServiceConnection.md) to see how to set it up. |
| **ResourceGroup** | User | Prefix of the resource groups where the bots are deployed. |
| **[BotName](#botnames) + AppId** | [App Registration Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) | (optional) App ID to use. If not configured, it will be retrieved from the key vault. |
| **ResourceSuffix** | Create Shared Resources | (optional) Suffix added to the resource names. |

## Cleanup Resources Pipeline

- Description: Removes all resources, including all the shared resources, bots, and app registrations.
- Schedule: Quarterly or on demand.
- YAML: [build\yaml\cleanupResources\cleanupResources.yml](../build/yaml/cleanupResources/cleanupResources.yml)

| Variable Name | Source | Description |
| - | - | - |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](Guides/addARMServiceConnection.md) to see how to set it up. |
| **DeployResourceGroup** | Deploy Bot Resources  | Prefix of the resource groups where the bots were deployed. |
| **SharedResourceGroup** | Create Shared Resources | Name for the resource groups that contains the shared resources. |
| **ResourceSuffix** | Create Shared Resources | (optional) Suffix added to the resource names. |

## BotNames

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
