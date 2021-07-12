# How to setup custom Docker container

## Summary

The JS and Python bots deployed in Azure for the functional tests are deployed in Docker containers by creating an Azure container registry.
This guide will show you how to create a custom container with a Linux/Python image and a bot sample, how to use it locally with BotFramework-Emulator, deploy it to a container's registry and create a Web App from the custom container.

![image](https://user-images.githubusercontent.com/38112957/124938561-c70a3200-dfde-11eb-9714-ac1fb28191e8.png "High overview structure to build, push and use a Bot Container in Azure.")

For additional information visit the official [Docker's website](https://www.docker.com/).

## Index

- [Create a Container](#create-a-container)
- [Deploy a Container](#deploy-a-container)
- [Create a Container WebApp](#create-a-container-webapp)

## Requirements

- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [BotFramework-Emulator](https://github.com/microsoft/BotFramework-Emulator/releases)
- [Ngrok](https://ngrok.com/download)
- [Active Azure Subscription](https://portal.azure.com/#home)
- Bot Resources
  - [Azure App Service Plan](https://portal.azure.com/#create/Microsoft.AppServicePlanCreate) ([guide](https://docs.microsoft.com/en-us/azure/app-service/app-service-plan-manage))
  - [Bot Channel Registration](https://portal.azure.com/#create/Microsoft.BotServiceConnectivityGalleryPackage) ([guide](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-quickstart-registration?view=azure-bot-service-4.0&tabs=cshap))
  - [App Registration](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/CreateApplicationBlade/quickStartType//isMSAApp/) ([guide](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)) (*MicrosoftAppId* and *Microsoft AppPassword* variables)

### Container specs

- Image: [python:3.8-buster](https://hub.docker.com/_/python) ([Dockerfile](https://github.com/docker-library/python/blob/master/3.8/buster/Dockerfile), [Packages](https://github.com/docker-library/repo-info/blob/master/repos/python/local/3.8-buster.md))
- System: Linux

## Create a container

1. Install docker (if you don't have it already). A restart might be required.
2. Use a bot sample (eg. [SimpleHostBotPython](https://github.com/microsoft/BotFramework-FunctionalTests/tree/main/Bots/Python/Consumers/CodeFirst/SimpleHostBot)). Clone the repository and move to the sample folder of the bot of your choice.

    > **Note**: when using [aiohttp](https://docs.aiohttp.org/en/stable/) to host the app, ensure the **host** property is empty when the **run_app** method is executed so the library starts broadcasting (0.0.0.0). This can be done by modifyin **app.py** in the source.

    ```python
    from aiohttp import web
    web.run_app(web.Application(), host="localhost", port=37000)
    ```

3. Create a file named **DockerFile** (without extension) inside the bot's folder. Add the following lines in the file.

    ```dockerfile
    # Specify the base image to use
    FROM python:3.8-buster

    # Specify the working directory where the bot is located. This will be the default location for all subsequent commands.
    WORKDIR /app

    # Take all the files located in the current directory and copy them into the image.
    COPY . .

    # Install bot's dependencies
    RUN pip3 install -r requirements.txt

    # Execute the bot
    CMD python app.py
    ```

4. Execute the [build](https://docs.docker.com/engine/reference/commandline/build/) command in the console to build the container.

    ```docker
    docker build --tag <Name and optionally a tag in the 'name:tag' format> .
    ```

    eg.

    ```docker
    docker build --tag simplehostbotpython .
    ```

5. Execute the [run](https://docs.docker.com/engine/reference/commandline/run/) command in the console to start the container

    ```docker
    docker run --publish <Publish a container's port(s) to the host> <Image name assigned in the tag>
    ```

    eg.

    ```docker
    docker run --publish 37000:37000 simplehostbotpython
    ```

    If you are presented with the following error, please refer to the step 2.

    ```docker
    OSError: [Errno 99] error while attempting to bind on address ('::1', 37000, 0, 0): cannot assign requested address
    ```

6. When using BotFramework-Emulator, ensure the **"Bypass ngrok from local addresses"** option is disabled.  
![image](https://user-images.githubusercontent.com/38112957/124956591-84505600-dfee-11eb-84f5-c2791b5ed25f.png)

## Deploy a container

1. Create an [Azure container registry](https://portal.azure.com/#create/Microsoft.ContainerRegistry).  
Once the resource is created, head over to the **Access Keys** section and enable **Admin user**. This will allow us to login remotely to the registry and push our container.  
Take note of the **Login server**, **Username**, and **password** values, we will need them later.  
![image](https://user-images.githubusercontent.com/38112957/124957304-515a9200-dfef-11eb-9e4d-7daee493bd55.png)

2. Login into the Azure container registry by executing the [login](https://docs.microsoft.com/en-us/cli/azure/acr?view=azure-cli-latest#az_acr_login) command in a console.

    ```cmd
    az acr login --name <Name of the Container Registry>
    ```

    eg.

    ```cmd
    az acr login --name bffncontainerregistry
    ```

    If you are presented with the following error.  
    ![image](https://user-images.githubusercontent.com/38112957/124957660-b1513880-dfef-11eb-875d-ef1a89539b0c.png)

    run

    ```cmd
    az login
    ```

3. Execute the [tag](https://docs.docker.com/engine/reference/commandline/tag/) command in the console to tag the container.

    ```docker
    docker tag <Image name assigned in the tag> <Container Registry Login server>/<Name and optionally a tag in the 'name:tag' format. Tag defaults to 'latest'>
    ```

    eg.

    ```docker
    docker tag simplehostbotpython bffncontainerregistry.azurecr.io/simplehostbotpython:v1
    ```

4. Execute the [push](https://docs.docker.com/engine/reference/commandline/push/) command in the console to push the container to the registry.

    ```docker
    docker push <Tag created in the previous step>
    ```

    eg.

    ```docker
    push bffncontainerregistry.azurecr.io/simplehostbotpython:v1
    ```

    If the error `unauthorized: authentication required` shows up, you are either, not logged in, or you used a mix of upper and lower case letters in the console commands. Please repeat tagging and pushing the container using all lowercase letters for the registry names.

    Your container is now deployed, you can check the uploaded containers in the container registry under the Repositories section.

    ![image](https://user-images.githubusercontent.com/38112957/124959801-faa28780-dff1-11eb-9632-a690dcf7092c.png)

## Create a container WebApp

1. Create a [WebApp](https://portal.azure.com/#create/Microsoft.WebSite).  
![image](https://user-images.githubusercontent.com/38112957/124960037-40f7e680-dff2-11eb-872a-fa01933b9395.png)

2. Access the WebApp configuration and assign the **WEBSITES_PORT** variable with the port to use to communicate with the container. Add the **MicrosoftAppId** and **MicrosoftAppPassword** obtained from the created **AppRegistration**.  
![image](https://user-images.githubusercontent.com/38112957/124960465-bd8ac500-dff2-11eb-9775-b04614df1f9b.png)

3. Add the WebApp URL in the Bot Channel Registration messaging endpoint. You can obtain the URL from WebApp's summary.  
![image](https://user-images.githubusercontent.com/38112957/124960921-3a1da380-dff3-11eb-857a-b538ffd55cdb.png)

4. The bot is ready. You can access the Bot Channel Registration and start chatting with the bot.  
![image](https://user-images.githubusercontent.com/38112957/124961032-5ae5f900-dff3-11eb-933d-04921017d533.png)
