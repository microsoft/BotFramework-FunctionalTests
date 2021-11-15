# syntax=docker/dockerfile:1
FROM node:lts-buster

# Specify the bot folder.
# When executing `docker build` remember to use the `--build-arg folder=./<bot-folder>`.
ARG folder=./ # Optional, default value to be `./` (current folder).

# Copy Source Code into the container
WORKDIR /app
COPY ${folder} .

# Install dependencies
RUN npm install

# Run the application
CMD npm start