# syntax=docker/dockerfile:1

FROM node:lts-buster

# Copy Source Code into the container
WORKDIR /app
COPY . .

# Install dependencies
RUN npm install

# Run the application
CMD npm start
