# Pipelines
This page describes the four pipelines that will need to be created to run functional tests.

[Create Shared Resources Pipeline](#01---create-shared-resources-pipeline) : Creates all the long-term resources required to deploy the bots and run tests (executed manually).

[Deploy Bot Resources Pipeline](#02---deploy-bot-resources-pipeline) :  Builds and deploys test bot resources to be used in the functional tests (executed nightly).

[Run Test Scenarios Pipeline](#03---run-test-scenarios-pipeline) : Runs nightly after the previous pipeline finishes and configures and executes the test scenarios.

[Cleanup Resources Pipeline](#04---cleanup-resources-pipeline) : Deletes all the resources created for running the functional tests (executed manually).

## 01 - Create Shared Resources Pipeline

- **YAML**: [build\yaml\sharedResources\createSharedResources.yml](../build/yaml/sharedResources/createSharedResources.yml)

| Variable Name | Source | Description | Example
| - | - | - | - |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](./addARMServiceConnection.md) to see how to set it up. | TestSubscription |
| **KeyVaultObjectId** | Azure | Suscription's Object Id to create the keyvault to store App Registrations in Azure. Click [here](./getServicePrincipalObjectID.md) to see how to get it. | 0x0x0x0x-0x00-000x-xx0x-000000000x00 |
| **AppServicePlanPricingTier** | User | (optional) Pricing Tier for App Service Plans. **Default value is F1.** | S1 |
| **ResourceGroupName** | User | (optional) Name for the resource group that will contain the shared resources. | shared-resource-group |
| **ResourceSuffix** | User | (optional) Suffix to add to the resources' name to avoid collisions (use lowercase). | -test |

## 02 - Deploy Bot Resources Pipeline

- **YAML**: [build\yaml\deployBotResources\deployBotResources.yml](../build/yaml/deployBotResources/deployBotResources.yml)

| Variable Name | Source | Description | Example |
| - | - | - | - |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](./addARMServiceConnection.md) to see how to set it up. | TestSubscription |
| **AppServicePlanGroup** | Create Shared Resources | (optional) Name of the Resource Group where the Windows App Service Plan is located. | shared-resource-group |
| **AppServicePlanGroupLinux** | Create Shared Resources | (optional) Name of the Resource Group where the Linux App Service Plan is located. | shared-resource-group-linux |
| **AppServicePlanDotNetName** | Create Shared Resources | (optional) Name of the DotNet App Service Plan. | appservicedotnet |
| **AppServicePlanJSName** | Create Shared Resources | (optional) Name of the JavaScript App Service Plan. | appservicejs |
| **AppServicePlanPythonName** | Create Shared Resources | (optional) Name of the Python App Service Plan. | appservicepython |
| **BotPricingTier** | User | (optional) Pricing tier for the Web App resources. ***Default value is F0.** | S1 |
| **ResourceGroup** | User | (optional) Name of the Resource Group where the bots will be deployed. | bots-group |
| **ResourceSuffix** | Create Shared Resources | (optional) Suffix to add to the resources' name to avoid collisions (use lowercase). | -test |
| **ConnectionName** | User | (optional) Name for the OAuth connection to use in the skill bots. | TestOAuthProvider |


### List of Bots AppIDs and Secrets
The pipeline need to have a pair of these variables for each bot (see [Available Bots List](./availableBotsList.md)).

These variables are optional, no need to configure them if the key vault was created in [Create Shared Resources](#01---create-shared-resources-pipeline) pipeline.
| Variable Name | Source | Description | Example |
| - | - | - | - |
 **BotName + AppId** | [App Registration Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) | (optional) App ID to use.  | x0x000x-000x-000x-xxx0-x00x0000xxx |
| **BotName + AppSecret** | [App Registration Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) | (optional) App Secret to use. | x0x000x-000x-000x-xxx0-x00x0000xxx |

This repo contains a script to create badges of App Registrations to ease pipeline setup. Follow the instruction in [Setup App Registrations](./setupAppRegistrations.md) page.

### Dependency Variables

The following parameters will be displayed in the run pipeline blade. 

The version parameters can be set here or you can create variables to set the values for the next pipeline's runs. 
Supported values are: LATEST (default), STABLE, or a specific version.
For example:
DEPENDENCIESVERSIONDOTNETHOSTS = 4.13.1

![dependenciesParameters](./media/dependenciesParameters.png)

| Parameter Name | Source | Description | Example |
| - | - | - | - |
| **DotNet/JS/Python Hosts Registry** | User | (optional) Source from which the Bot Builder dependencies will be downloaded for selected host bots. | NuGet |
| **DotNet/JS/Python Skills Registry** | User | (optional) Source from which the Bot Builder dependencies will be downloaded for selected skill bots. | Artifacts |
| **DotNet/JS Skills V3 Registry** | User | (optional) Source from which the Bot Builder dependencies will be downloaded for selected V3 skill bots. | MyGet |
| **DotNet/JS/Python Hosts Version** | User | (optional) Bot Builder dependency version to use for selected host bots. | Latest |
| **DotNet/JS/Python Skills Version** | User | (optional) Bot Builder dependency version to use for selected skill bots. | Stable |
| **DotNet/JS Skills V3 Version** | User | (optional) Bot Builder dependency version to use for selected V3 skill bots. | 3.30.0


These are the available registry options for each bot language:

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


## 03 - Run Test Scenarios Pipeline

- **YAML**: [build\yaml\testScenarios\runTestScenarios.yml](../build/yaml/testScenarios/runTestScenarios.yml)

| Variable Name | Source | Description | Example |
| - | - | - | - |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](./addARMServiceConnection.md) to see how to set it up. | TestSubscription |
| **ResourceGroup** | User | (optional) Name of the Resource Group where the bots are deployed. | bots-group |
| **ResourceSuffix** | Create Shared Resources | (optional) Suffix to add to the resources' name to avoid collitions (use lowercase). | -test |
| **[BotName](#botnames) + AppId** | [App Registration Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) | (optional) App ID to use. If not configured, it will be retrieved from the key vault. | x0x000x-000x-000x-xxx0-x00x0000xxx |
| **DeployBotResourcesGuid** | Deploy Bot Resources | (optional) Name set for the Deploy Bot Resources pipeline. | 02-Deploy Bot Resources |

## 04 - Cleanup Resources Pipeline

- **YAML**: [build\yaml\cleanupResources\cleanupResources.yml](../build/yaml/cleanupResources/cleanupResources.yml)

| Variable Name | Source | Description | Example |
| - | - | - | - |
| **AzureSubscription** | Azure DevOps | Name of the Azure Resource Manager Service Connection configured in the DevOps organization. Click [here](./addARMServiceConnection.md) to see how to set it up. | TestSubscription |
| **DeployResourceGroup** | Deploy Bot Resources | (optional) Name of the Resource Group containing the bots. | bots-group |
| **ResourceSuffix** | Create Shared Resources | (optional) Suffix to add to the resources' name to avoid collitions (use lowercase). | -test |
| **SharedResourceGroup** | Create Shared Resources | (optional) Name of the Resource Group containing the shared resources. | shared-resource-group |


For the list of available bots, see [Available Bots List](./availableBotsList.md) page.
