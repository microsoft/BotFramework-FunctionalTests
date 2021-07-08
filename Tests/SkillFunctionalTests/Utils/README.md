# Skills Functional Tests Utilities Scripts

## ConfigureAppSettings

This script configures the _appsettings.Development.json_ file from the SkillsFunctionalTest project to be able to run the tests locally connecting them with bots deployed in Azure. Its behavior consists on gathering the DirectLine Secrets from the deployed Azure Bot Resources and configure this file with them. For its purpose it uses the '**ResourceGroup**' and '**ResourceSuffix**' provided by the user when running it.

>**ResourceGroup**: the specific Resource Group where the Azure Resoruce Bots are deployed in. The default value is 'BFFN'. _Eg: bffnbots_
>
>**ResourceSuffix**: the suffix the resources name are built with. It will be a combination of the suffix itself and the build id. _Eg: gitali-218_

The user can execute the script providing one or both parameters and the process will being automatically, or execute it without parameters to insert them using the prompt.

<p align="center">
  <img src="https://user-images.githubusercontent.com/54330145/124808628-899d9a00-df35-11eb-9b1c-bc88c352636a.png" />
</p>

The script will comunicate with azure through the **azure-cli** tool and gather the DirectLine secrets from each Azure Resource Bot deployed in the Resource Group resultant from the combination of the provided one with the language (DotNet, JS or Python) extracted from the _HostBotClientOptions_ keys, and '**ResourceSuffix**' parameter.

Here you can see the entire process being executed
<p align="center">
  <img src="https://user-images.githubusercontent.com/54330145/124788742-0756ab00-df20-11eb-8eea-8f54c07708f1.png" />
</p>
