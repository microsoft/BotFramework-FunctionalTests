# Functional Tests

## Summary

Skill functional testing aims to automate the testing of Host and Skill interactions through several scenarios in all available programming languages.

Head to [Docs](../../Docs/) directory for more information.

## Available Test Cases
### Skills
The following scenarios are being tested with the different host-skill bots combinations:
- Single Turn (simple echo with MultiTenant apps)
- Waterfall Bots:
   - Cards
   - File Upload
   - Message with Attachments
   - Proactive Messages
   - Sign in
- Authentication (SingleTenant and MSI app types)

## Branching 
In this repository, there are branches with pinned versions of the SDK to test patch releases.
From 4.9 to 4.15, each branch contains the yamls and tests adapted to run the current implementation of the pipelines against older versions of the BotBuilder packages.


This image describes the bots included in the different branches.
![branching](/Docs/media/branching.png)
