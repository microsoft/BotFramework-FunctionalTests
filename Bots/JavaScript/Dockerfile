# syntax=docker/dockerfile:1
FROM node:lts-buster

# Set BotBuilder version env variable
ARG BotBuilderVersion
ENV BotBuilderVersion ${BotBuilderVersion}

# Specify the bot folder.
# When executing `docker build` remember to use the `--build-arg folder=./<bot-folder>`.
ARG folder=./ # Optional, default value to be `./` (current folder).

# Copy Source Code into the container
WORKDIR /app
COPY ${folder} .

# Install dependencies
RUN node Utils/prepare.mjs replace
RUN npm install --ignore-scripts

# Run the application
CMD npm start
