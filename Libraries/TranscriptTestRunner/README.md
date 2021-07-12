# Transcript Test Runner

## Summary

Transcript Test Runner aims to simplify complex conversation scenarios against bots.
Starting from a [test script](testscript.schema) file containing the user-bot interactions to be tested, the Transcript Test Runner replicates the messages sent by the user and evaluates the answers from the bot.

A Test Script is basically a JSON file with an array of [TestScriptItem](TestScriptItem.cs) that will be used by the `TestRunner` as a test input.

## User Step-by-step Guide
This step-by-step guide shows how to run a test with the `TestRunner` configuring a `DirectLine` client to communicate with the bots.

### Creating the Test Script file
You can convert a transcript file into a test script using the [Transcript Converter](../TranscriptConverter/TranscriptConverter.csproj) tool or create one manually using this [JSON schema](testscript.schema).

### Using the TestRunner
1- Open your test project and install the `TranscriptTestRunner` package.

2- In the `appsettings.json` file add the following variables:
```json
{
  "DIRECTLINE": "<the bot's directline secret key>",
  "BOTID": "<the bot's name>"
}
```

> **Note:** For more information on how to obtain the bot `DIRECTLINE` secret key, follow [this guide](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-channel-connect-directline).

3- Add the `Test Script` file in a folder on your test project.

4- Add the `TestRunner` to your test.

```csharp
// Create the test options
var options = new DirectLineTestClientOptions { BotId = "<bot-id>", DirectLineSecret = "<direct-line-secret>" };

// Create a DirectLine client with the `TestClientFactory`.
var directLineClient = new TestClientFactory(ClientType.DirectLine, options).GetTestClient();

// Instantiate the TestRunner and set up the DirectLine client.
var runner = new TestRunner(directLineClient);

// Run the test recorded in the test script file.
await runner.RunTestAsync("<path to the test script file>");
```
The `TestRunner` will execute test script sending the user messages to the bot performing the assertions over the bot's answers.


### Creating tests programmatically
The `TestRunner` allows you to run tests in a programmatic way, sending Activities to the bot and asserting its reply.

The following sample shows how to create a simple test programmatically.

```csharp
// Create the test options
var options = new DirectLineTestClientOptions { BotId = "<bot-id>", DirectLineSecret = "<direct-line-secret>" };

// Create a DirectLine client instance that will be used to communicate with your bot.
var directLineClient = new TestClientFactory(ClientType.DirectLine, options).GetTestClient();

// Instantiate the TestRunner and set up the DirectLine client.
var runner = new TestRunner(directLineClient);

// Send an Activity to the bot.
await runner.SendActivityAsync(new Activity(ActivityTypes.ConversationUpdate));

// Asserts the reply received from the bot.
await runner.AssertReplyAsync(activity =>
{
    // Sample asserting Activity's type and text with xUnit.
    Assert.Equal(ActivityTypes.Message, activity.Type);
    Assert.Equal("Hello and welcome!", activity.Text);
});
```

**Methods**
- **SendActivityAsync:** Used to send an Activity to the bot.
- **AssertReplyAsync:** Used to create custom assertions to an expected reply when the bot responds.
- **ClientSignInAsync:** Used to sign in to the bot.

**ClientSignInAsync**

This method is used when your bot has an authentication implementation and you want to sign in.

The following sample shows how to use the `ClientSignInAsync` method to sign in.

```csharp
// Create the test options.
var options = new DirectLineTestClientOptions { BotId = "<bot-id>", DirectLineSecret = "<direct-line-secret>" };
// Create a DirectLine client instance that will be used to communicate with your bot.
var directLineClient = new TestClientFactory(ClientType.DirectLine, options).GetTestClient();
// Instantiate the TestRunner and set up the DirectLine client.
var runner = new TestRunner(directLineClient);
var signInUrl = string.Empty;

// Sends an Activity to the bot.
await runner.SendActivityAsync(new Activity { Type = ActivityTypes.Message, Text = "auth" });

// Obtain the sign in url.
await runner.AssertReplyAsync(activity =>
{
    Assert.Equal(ActivityTypes.Message, activity.Type);
    Assert.True(activity.Attachments.Count > 0);
    
    var attachment = JsonConvert.SerializeObject(activity.Attachments.FirstOrDefault().Content);
    var card = JsonConvert.DeserializeObject<SigninCard>(attachment);
    signInUrl = card.Buttons[0].Value?.ToString();

    Assert.False(string.IsNullOrEmpty(signInUrl));
});

// Execute the sign in.
await runner.ClientSignInAsync(signInUrl);

// If the sign in succeeded you can continue to execute the rest of the conversation
// either programmatically or with a test file.
```

### Setting up a Logger (Optional).
TestRunner uses [ILogger](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger) when it comes to display an output for each interaction between the test and the bot.

The following sample shows how to create and pass an `ILogger` to the `TestRunner` with a `LoggerFactory`.

```csharp
// Create a new Logger Factory.
var loggerFactory = LoggerFactory.Create(builder =>
{
    // Add new Providers to tell the Logger where to output the data.
    builder
        // This provider will output the logs in the console.
        .AddConsole()
        // This provider will output the logs in the Debug output window.
        .AddDebug();
});

// Create the `Logger` instance with a Category name.
var logger = loggerFactory.CreateLogger("Category");

// Create the test options
var options = new DirectLineTestClientOptions { BotId = "<bot-id>", DirectLineSecret = "<direct-line-secret>" };
// Create a DirectLine client instance that will be used to communicate with your bot.
var directLineClient = new TestClientFactory(ClientType.DirectLine, options).GetTestClient();
// Instantiate the TestRunner, set up the DirectLine client, and send the created `Logger`.
var runner = new TestRunner(directLineClient, logger = logger);
```

### Extend TestRunner functionality
TestRunner has an Xunit extension to allow the users that prefer this test framework, to override the `AssertActivityAsync` with Xunit assertions.

```csharp
public class XUnitTestRunner : TestRunner
{
    public XUnitTestRunner(TestClientBase client, int replyTimeout = 180000, int thinkTime = 0, ILogger logger = null)
            : base(client, replyTimeout, thinkTime, logger)
    {
    }

    protected override Task AssertActivityAsync(TestScriptItem expectedActivity, Activity actualActivity, CancellationToken cancellationToken = default)
    {
        Assert.Equal(expectedActivity.Type, actualActivity.Type);
        Assert.Equal(expectedActivity.Text, actualActivity.Text);

        return Task.CompletedTask;
    }
}
```
