# Transcript Test Runner

## Summary

Transcript Test Runner aims to simplify complex conversation scenarios against bots.
Starting from a transcript file containing the user-bot interactions to be tested, the Transcript Test Runner converts the transcript into a test script used to replicate the messages sent by the user and evaluate the answers from the bot.

The Transcript Test Runner also offers the possibility of running the tests directly from a test script (link to schema), adding flexibility to the assertions for the bot's messages.

The Test Runner supports different formats of transcript files (Emulator, Teams, Slack, etc.) and
can be connected to the bots using different channels (*):
- DirectLineClient
- TeamsClient
- SlackClient
- FacebookClient

(*) _This first version implements the DirectLineClient and uses Emulator transcript files. Stay tuned for the next features._

## User Step-by-step Guide
