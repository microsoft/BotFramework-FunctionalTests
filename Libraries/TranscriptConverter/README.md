# Transcript Converter

## Summary

Transcript Converter is a command line tool to convert `.transcript` files from different channels (BotFramework-Emulator, Teams, Slack, etc.)* into a test script used to replicate the messages sent by the user and evaluate the answers from the bot.
This test script is the input for the [Transcript Test Runner](../TranscriptTestRunner/TranscriptTestRunner.csproj).

(*) _This first version supports BotFramework-Emulator transcript files. Stay tuned for the next features._

## The Test Script
A Test Script is basically a JSON file with an array of [TestScriptItem](TestScriptItem.cs) that will be used by the `TranscriptTestRunner` as a test input.

You can also create a test script file using this [JSON schema](../TranscriptTestRunner/testscript.schema).
```json
[
 {
   "type": "conversationUpdate",
   "role": "user"
 },
 {
   "type": "message",
   "role": "bot",
   "text": "Hello and welcome!",
   "assertions": [
     "type == 'message'",
     "from.role == 'bot'",
     "recipient.role == 'user'",
     "text == 'Hello and welcome!'",
     "inputHint == 'acceptingInput'"
   ]
 },
 {
   "type": "message",
   "role": "user",
   "text": "Hi"
 }
]
```
> **Note:** The JSON Schema is still a work in progress.

## User Step-by-step Guide
This step-by-step guide shows how to convert a BotFramework-Emulator transcript file into a test script.

1- After installing the tool open a terminal and execute the following command:

```
btc convert "path-to-source-transcript" "path-to-target-test-script"
```
- The first argument is the absolute or relative path to the transcript file.
To create a transcript file, follow [these steps](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-debug-transcript?view=azure-bot-service-4.0#creatingstoring-a-bot-transcript-file).

- The second argument is optional, and sets the path to the folder where the test script will be created. If not provided, the test script will have the same name and location that the transcript.

2- Once the Test Script file is created, store it in a folder on your test project and pass it to the _RunTestAsync_ method of the `TestRunner` to execute the test.


