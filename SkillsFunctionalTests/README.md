# Skills Functional Tests

### Summary

Skill functional testing aims to automate the testing for the combination of Host and Skill bots and its interactions in all available programming languages.

The matrix consists of all the available combinations to test in this repository. Each table cell has a badge that represents a pipeline that runs every night using the latest preview version of the BotBuilder packages.

<table>
    <tr>
        <th align="center">Host/Skill</th>
        <th align="center">C# Net Core 2.1</th>     
        <th align="center">C# Net Core 3.1</th>
        <th align="center">JavaScript</th>
        <th align="center">Python</th>
        <th align="center">v3 Javascript</th>
        <th align="center">v3 C#</th>
    </tr>
    <tr align="center">
        <tr align="center">
        <td>C# Net Core 2.1</td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/DotNet21DotNet21SkillBotFunctionalTest?branchName=master"></td>   
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/DotNet21DotNet31SkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/DotNet21JsSkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/DotNet21PySkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/V3SkillBots/DotNet21JsV3SkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/V3SkillBots/DotNet21DotNetV3SkillBotFunctionalTest?branchName=master"></td>
    </tr>
        <td>C# Net Core 3.1</td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/DotNet31DotNet21SkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/DotNet31DotNet31SkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/DotNet31JsSkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/DotNet31PySkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/V3SkillBots/DotNet31JsV3SkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/V3SkillBots/DotNet31DotNetV3SkillBotFunctionalTest?branchName=master"></td>
    </tr>
    <tr align="center">
        <td>JavaScript</td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/JsDotNet21SkillBotFunctionalTest?branchName=master"></td>       
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/JsDotNet31SkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/JsJsSkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/JsPySkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/V3SkillBots/JsJsV3SkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/V3SkillBots/JsDotNetV3SkillBotFunctionalTest?branchName=master"></td>
    </tr>
    <tr align="center">
        <td>Python</td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/PyDotNet21SkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/PyDotNet31SkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/PyJsSkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/PyPySkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/V3SkillBots/PyJsV3SkillBotFunctionalTest?branchName=master"></td>
        <td><img src="https://dev.azure.com/FuseLabs/SDK_v4/_apis/build/status/SkillBots/V3SkillBots/PyDotNetV3SkillBotFunctionalTest?branchName=master"></td>
    </tr>

</table>

### Content

This section contains a `SimpleHostBot` and an `EchoSkillBot` sample for each language available to be used in the functional tests. To run the test we use YAML files to set up a pipeline that deploys a pair of Host and Skill bots to Azure and then run functional tests where the HostBot consumes the SkillBot.

The functional tests are located in the `tests` folder. This test is written in DotNet and can be used to test the bots independently of the language these are written in. For this, the test communicate with the bots deployed to Azure using a [direct line channel](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-channel-directline) to the `HostBot`, and ask it to be delegated to the skill to then get an echo message.

### Tests

The following tests are run in the pipeline using the Host and Skill bots deployed in Azure through a DirectLine connection with the Host bot.

<table>
    <tr>
        <th align="center">Name</th>
        <th align="center">Description</th>
        <th align="center">Additional Info</th>
    </tr>
    <tr>
        <td>Host_WhenRequested_ShouldRedirectToSkill</td>
        <td>Tests that the host bot communicates with the skill bot when asked.</td>
        <td></td>
    </tr>
    <tr>
        <td>Host_WhenSkillEnds_HostReceivesEndOfConversation</td>
        <td>Tests that the skill bot returns the control of the conversation to the host bot when asked.</td>
        <td></td>
    </tr>
    <tr>
        <td>Skill_OAuthCard_SignInSuccessful</td>
        <td>Tests that the skill bot Sign In successfully using the OAuth service.</td>
        <td>Skipped when a v3 Skill bot is used</td>
    </tr>
</table>

## Usage

To run the functional tests, you have to set up a pipeline in Azure DevOps for each pair of combinations of different languages of host-skill, using the YAML files in the folder `build/yamls` at the root of the repository.

### Prerequisites

To run a pipeline with the functional test you will need:

- Azure DevOps Organization
- Azure Portal Subscription

### Set up pipeline

The following steps will guide you trough the creation of a pipeline that runs one of the YAMLs with the functional test for a pair of Host and Skill bots. If you want to test every combination, you have to create one pipeline for each pair, following these instructions for each.

1. Go to your Azure DevOps organization, go to the pipeline's section and create a new pipeline using the classic editor.
![image](media/new-pipeline.png)

2. Configure the repository and branch. Then, in the configuration as code section, click on **_Apply_** for the YAML file option.
![image](media/select-as-code.png)

3. In section YAML, write the build name and select the YAML file inside the folder build/yaml in the root of the directory.
![image](media/yaml-path.png)
  
   In the following table, you can see which YAML corresponds with each host-skill pair to be tested.

   | Host\Skill            | C# Net Core 3.1                                                                | JavaScript                                                                              | Python                                                                         |  C# Net Core 2.1                                                               |  v3 Javascript                                                                            |  v3 C#                                                                              |
   |-----------------------|--------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------|--------------------------------------------------------------------------------|--------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------|
   | **C# Net Core 3.1**   | [dotnetHost2DotnetSkill.yml](../build/yaml/dotnetHost2dotnetSkill.yml)         | [dotnetHost2JavascriptSkill.yml](../build/yaml/dotnetHost2JavascriptSkill.yml)          | [dotnetHost2PythonSkill.yml](../build/yaml/dotnetHost2PythonSkill.yml)         | [dotnetHost2DotnetSkill.yml](../build/yaml/dotnetHost2dotnetSkill.yml)         | [dotnetHost2JavascriptV3Skill.yml](../build/yaml/dotnetHost2JavascriptV3Skill.yml)        | [dotnetHost2DotnetV3Skill.yml](../build/yaml/dotnetHost2DotnetV3Skill.yml)          |
   | **JavaScript**        | [javascriptHost2DotnetSkill.yml](../build/yaml/javascriptHost2DotnetSkill.yml) | [javascriptHost2JavascriptSkill.yml](../build/yaml/javascriptHost2JavascriptSkill.yml)  | [javascriptHost2PythonSkill.yml](../build/yaml/javascriptHost2PythonSkill.yml) | [javascriptHost2DotnetSkill.yml](../build/yaml/javascriptHost2DotnetSkill.yml) | [javascriptHost2JavascriptV3Skill.yml](../build/yaml/javascriptHost2JavascriptV3Skill.yml)| [javascriptHost2DotnetV3Skill.yml](../build/yaml/javascriptHost2DotnetV3Skill.yml)  |
   | **Python**            | [pythonHost2DotnetSkill.yml](../build/yaml/pythonHost2DotnetSkill.yml)         | [pythonHost2JavascriptSkill.yml](../build/yaml/pythonHost2JavascriptSkill.yml)          | [pythonHost2PythonSkill.yml](../build/yaml/pythonHost2PythonSkill.yml)         | [pythonHost2DotnetSkill.yml](../build/yaml/pythonHost2DotnetSkill.yml)         | [pythonHost2JavascriptV3Skill.yml](../build/yaml/pythonHost2JavascriptV3Skill.yml)        | [pythonHost2DotnetV3Skill.yml](../build/yaml/pythonHost2DotnetSkill.yml)            |
   | **C# Net Core 2.1**   | [dotnetHost2DotnetSkill.yml](../build/yaml/dotnetHost2dotnetSkill.yml)         | [dotnetHost2JavascriptSkill.yml](../build/yaml/dotnetHost2JavascriptSkill.yml)          | [dotnetHost2PythonSkill.yml](../build/yaml/dotnetHost2PythonSkill.yml)         | [dotnetHost2DotnetSkill.yml](../build/yaml/dotnetHost2dotnetSkill.yml)         | [dotnetHost2JavascriptV3Skill.yml](../build/yaml/dotnetHost2JavascriptV3Skill.yml)        | [dotnetHost2DotnetV3Skill.yml](../build/yaml/dotnetHost2DotnetV3Skill.yml)          |

4. In the variables section add the following variables.
    
   #### **Variables**
 
   | Name                                                   | Source                                                                                                                                            | Description                                                                                                                                                                                                       | Options                                             |
   |--------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------|
   | **AzureSubscription**                                  | User                                                                                                                                              | The name of the *Azure Resource Manager service connection* configured in the pipeline.                                                                                                                           | <ul><li>**required**</li></ul>                      |
   | [[Prefix]](#Prefix-Variables) + **HostAppId**          | [App Registration](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) Portal                                     | The *Application (client) ID* of the App Registration for your HostBot.                                                                                                                                           | <ul><li>**required**</li><li>**private**</li></ul>  |
   | [[Prefix]](#Prefix-Variables) + **HostAppSecret**      | [App Registration](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) Portal                                     | The secret’s value from the table under *Client secrets* of the App Registration for your HostBot.                                                                                                                | <ul><li>**required**</li><li>**private**</li></ul>  |
   | [[Prefix]](#Prefix-Variables) + **HostBotName**        | User                                                                                                                                              | The name of the HostBot that will be used to deploy it to Azure.                                                                                                                                                  | <ul><li>**required**</li></ul>                      |
   | [[Prefix]](#Prefix-Variables) + **SkillAppId**         | [App Registration](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) Portal                                     | The *Application (client) ID* of the App Registration for your SkillBot.                                                                                                                                          | <ul><li>**required**</li><li>**private**</li></ul>  |
   | [[Prefix]](#Prefix-Variables) + **SkillAppSecret**     | [App Registration](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) Portal                                     | The secret’s value from the table under *Client secrets* of the App Registration for your SkillBot.                                                                                                               | <ul><li>**required**</li><li>**private**</li></ul>  |
   | [[Prefix]](#Prefix-Variables) + **SkillBotName**       | User                                                                                                                                              | The name of the SkillBot that will be used to deploy it to Azure.                                                                                                                                                 | <ul><li>**required**</li></ul>                      |
   | **AzureDeploymentUser**                                | [Webapp Deployment User](https://docs.microsoft.com/en-us/cli/azure/webapp/deployment/user?view=azure-cli-latest#az-webapp-deployment-user-set)   | The Azure deployment user required to deploy using git.                                                                                                                                                           | <ul><li>**python**</li></ul>                        |
   | **AzureDeploymentPassword**                            | [Webapp Deployment User](https://docs.microsoft.com/en-us/cli/azure/webapp/deployment/user?view=azure-cli-latest#az-webapp-deployment-user-set)   | The Azure deployment password required to deploy using git.                                                                                                                                                       | <ul><li>**python**</li></ul>                        |
   | **BotBuilderPackageVersionHost**                       | User                                                                                                                                              | The version of the BotBuilder package the Host bot will use. If not set, the latest preview version will be used, set it to **stable** to install latest stable version or specify the version you want to use.   | <ul><li>**optional**</li></ul>                      |
   | **BotBuilderPackageVersionSkill**                      | User                                                                                                                                              | The version of the BotBuilder package the Skill bot will use. If not set, the latest preview version will be used, set it to **stable** to install latest stable version or specify the version you want to use.  | <ul><li>**optional**</li></ul>                      |
   | **DeleteResourceGroup**                                | User                                                                                                                                              | Set this variable to **false** if you want to skip the step to delete the resources in Azure after the tests.                                                                                                     | <ul><li>**optional**</li></ul>                      |
   | **ExecutePipelinesPersonalAccessToken**                | User                                                                                                                                              | The Access Token for triggering the next build in the chain.                                                                                                                                                      | <ul><li>**optional**</li></ul>                      |
   | **NetCoreSdkVersionHost**                              | User                                                                                                                                              | The version of the NetCore SDK the Host bot will use. This variable is required for dotnet Host bots. The supported values are 2.1 and 3.1, any other value will cause the pipeline to fail.                      | <ul><li>**dotnet**</li></ul>                        |
   | **NetCoreSdkVersionSkill**                             | User                                                                                                                                              | The version of the NetCore SDK the Skill bot will use. This variable is required for dotnet Skill bots. The supported values are 2.1 and 3.1, any other value will cause the pipeline to fail.                    | <ul><li>**dotnet**</li></ul>                        |
   | **NextBuild**                                          | User                                                                                                                                              | The name of the next build in the chain.                                                                                                                                                                          | <ul><li>**optional**</li></ul>                      |
   | **TriggeredBy**                                        | User                                                                                                                                              | The name of the previous build from which was triggered.                                                                                                                                                          | <ul><li>**optional**</li></ul>                      |
   | **RegistryUrlHost**                                        | User                                                                                                                                              | The registry where the BotBuilder packages will be downloaded. If not set, the packages will be downloaded from **MyGet**. Set to **MyGet** to install the package from the **MyGet** registry. Set to **NuGet** to install the package from the **NuGet** registry. Also, you can enter a custom URL to install the package from a custom registry. Now it is only supporting custom MyGet feeds.                                                                                                 | <ul><li>**optional**</li></ul>                      |
   | **RegistryUrlSkill**                                        | User                                                                                                                                              | The registry where the BotBuilder packages will be downloaded. If not set, the packages will be downloaded from **MyGet**. Set to **MyGet** to install the package from the **MyGet** registry. Set to **NuGet** to install the package from the **NuGet** registry. Also, you can enter a custom URL to install the package from a custom registry. Now it is only supporting custom MyGet feeds.                                                                                                                                                          | <ul><li>**optional**</li></ul>                      |


   #### **Prefix Variables**

   | Host\Skill          | C# Net Core 3.1 | JavaScript | Python   | C# Net Core 2.1 | v3 Javascript | v3 C#          |
   |---------------------|-----------------|------------|----------|-----------------|---------------|----------------|
   | **C# Net Core 3.1** | DotNetDotNet    | DotNetJs   | DotNetPy | DotNetDotNet    | DotNetJsV3    | DotNetDotNetV3 |
   | **JavaScript**      | JsDotNet        | JsJs       | JsPy     | JsDotNet        | JsJsV3        | JsDotNetV3     |
   | **Python**          | PyDotNet        | PyJs       | PyPy     | PyDotNet        | PyJsV3        | PyDotNetV3     |
   | **C# Net Core 2.1** | DotNetDotNet    | DotNetJs   | DotNetPy | DotNetDotNet    | DotNetJsV3    | DotNetDotNetV3 |


   #### **Options**

   | Name         | Description                                             |
   |--------------|---------------------------------------------------------|
   | **required** | These variables are required/mandatory.                 |
   | **optional** | These variables are optional/non mandatory.             |
   | **private**  | These variables are private.                            |
   | **python**   | These variables are required when a python bot is used. |
   | **dotnet**   | These variables are required when a dotnet bot is used. |


5. (Optional) Configure the triggers for the pipeline. By default, this pipelines have all the triggers from commits and PRs disabled.
