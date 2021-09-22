# Skills Functional Tests Utilities Scripts

## ConfigureAppSettings

### Description

This script configures the `appsettings.Development.json` file from the SkillsFunctionalTest project to be able to run the tests locally, connecting them with bots deployed in Azure. Its behavior consists on gathering the DirectLine Secrets from the deployed Azure Bot Resources and configure this file with them.

For the process to be able to find the resources, the following inputs must be provided.

### Requirements

- [Azure CLI][azure-cli]
- [PowerShell 7+][powershell]

### Inputs

| Input                   | Description                                                                                                                                                   | Condition | Default              | Example                                                           |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------- | -------------------- | ----------------------------------------------------------------- |
| ResourceGroup           | The Name for the specific Resource Group where the resources are deployed. For each specific language will concatenate (DotNet, JS and Python).               | Required  | BFFN                 | "bffnbots"                                                        |
| ResourceSuffix          | The Suffix used to concatenate at the end of the Resource Name followed up by the ResourceSuffixSeparator.                                                    | Required  |                      | "microsoft-396"                                                   |
| Subscription            | The Name or Id of the Subscription where the resources are located.                                                                                           | Optional  | Current Subscription | "00000000-0000-0000-0000-000000000000" or "bffnbots-subscription" |
| ResourceSuffixSeparator | The separator used for the Suffix to split the Resource Name from the ResourceSuffix. <br> **Note:** _Only available when providing it through `parameters`_. | Optional  | -                    | "" or "-microsoft-"                                               |

### The inputs can be provided as `prompts` or `parameters`

1. When using `prompts`, it will ask for the `required` inputs to be provided. When no input is entered, it will use the `default (...)` value instead. For more information about, see [Inputs][inputs].

   ![prompts][prompts]

2. When using `parameters`, all listed inputs can be provided before executing the script. When no input is entered, it will use the `default` value instead. For more information, see [Inputs][inputs].

   ![parameters][parameters]

### Result

After providing the desired [Inputs][inputs], the script will start looking for the Bot Resources, followed up by gathering each DirectLine Secret from the Azure Bot Resource and set it in the `appsettings.Development.json`. Moreover, all steps will be shown in the terminal as well as the result with each DirectLine Secret set for each Bot.

![sample][sample]

> **Note:** When no `appsettings.Development.json` file is found, it will proceed by generating a copy from the `appsettings.json` baseline file.

> **Note:** Applies to `Subscription` input. When multiple Subscriptions are detected, a prompt to choose the desired one will appear. `Can be entered by Number (#), Name or Id`, otherwise the `default` one will be used instead.

> **Note:** Applies to all `Inputs`. A mix between the two ways (`prompts` and `parameters`) to provide the inputs can be used. _E.g. prompts: ResourceSuffix and ResourceGroup, parameters: Subscription and ResourceSuffixSeparator_.

<!-- Requirements -->

[azure-cli]: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
[powershell]: https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows?view=powershell-7.1

<!-- Inputs -->

[inputs]: #inputs

<!-- Images -->

[prompts]: https://user-images.githubusercontent.com/62260472/134236938-b85fd5a1-6e32-4b78-a67f-cf9d8d98c3b4.png
[parameters]: https://user-images.githubusercontent.com/62260472/134376619-aa7c27b7-52e6-4d72-837a-ec92e59afff6.png
[sample]: https://user-images.githubusercontent.com/62260472/134376693-f8c109f8-a735-4be1-a601-5fdfb087078a.png
