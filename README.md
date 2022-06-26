<h1 align="center">
  <a href="https://github.com/hgjura/command-pattern-with-queues">
    <img src=".github/logo.png" alt="Logo" width="650" height="150">
  </a>
</h1>

<div align="center">
  <h1>Command pattern with queues</h1>
  
  <h2>An example on how to use ServerTools.ServerCommands package.</h2>
  <!-- <a href="#about"><strong>Explore the screenshots »</strong></a>
  <br />
  <br /> -->
  <a href="https://github.com/hgjura/command-pattern-with-queues/issues/new?assignees=&labels=&template=01_bug_report.yml&title=%5BBUG%5D">Report a Bug</a>
  ·
  <a href="https://github.com/hgjura/command-pattern-with-queues/issues/new?assignees=&labels=&template=02_feature_request.yml&title=%5BFEATURE+REQ%5D">Request a Feature</a>
  .
  <a href="https://github.com/hgjura/command-pattern-with-queues/issues/new?assignees=&labels=&template=03_question.yml&title=%5BQUERY%5D">Ask a Question</a>
</div>

<div align="center">
<br />

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) [![PRs welcome](https://img.shields.io/badge/PRs-welcome-ff69b4.svg?style=flat-square)](https://github.com/hgjura/command-pattern-with-queues/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22) [![Gitter](https://badges.gitter.im/hgjura/ServerCommands.svg)](https://gitter.im/hgjura/ServerCommands?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

[![code with hearth by Herald Gjura](https://img.shields.io/badge/%3C%2F%3E%20with%20%E2%99%A5%20by-hgjura-ff1414.svg?style=flat-square)](https://github.com/hgjura)

</div>

<details open="open">
<summary>Table of Contents</summary>

- [About](#about)
  - [Built With](#built-with)
- [Getting Started](#getting-started)
  - [Some Concepts](#some-concepts)
  - [Usage](#usage)
- [Roadmap](#roadmap)
- [Support](#support)
- [Project assistance](#project-assistance)
- [Contributing](#contributing)
- [Authors & contributors](#authors--contributors)
- [Security](#security)
- [License](#license)
<!-- - [Acknowledgements](#acknowledgements) -->

</details>

---

## About

This is a sample project on how to best integrate and work with the ServerTools.ServerCommands nuget package. It explains and incorporates what the Command development pattern is, and some of the principles of Messaging Architecture. This sample is built with Azure Functions, assuming that the server that, that executes the commands, is Eternal Durable Function, and for simplicity the client that generates and posts the commands is a http-triggered Azure Function. Obviously, this last could be anything from a command line, app a windows app, a mobile, web app, api, etc., anything that runs .NET 6 or higher.

This sample application primarily illustrates the usage of [ServerTools.ServerCommands](https://github.com/hgjura/ServerTools.ServerCommands):
> ServerCommands facilitates running of units of code or commands remotely. It incorporates principles of messaging architectures used by most messaging tools and frameworks, like [Azure Service Bus](https://docs.microsoft.com/en-ca/azure/service-bus-messaging/), [AWS SQS](https://aws.amazon.com/sqs/), [RabbitMQ](https://www.rabbitmq.com/), or [Azure Storage Queues](https://docs.microsoft.com/en-ca/azure/storage/queues/storage-dotnet-how-to-use-queues?tabs=dotnet), [Apache Kafka](https://kafka.apache.org/) without any of the knowledge and configuration expertise to manage such installations and configurations. This sample uses the implementation of ServerCommands that has as the underlying service the Azure Storage Queues.

- [Release Notes](https://github.com/hgjura/command-pattern-with-queues/releases/tag/v0.0.2-preview2) :: [Previous Versions](RELEASENOTES.md)

### Built With
- C# (NET 6.0)
- Azure Functions


# Getting Started

## Some Concepts
### Basic understanding of the Messaging Architectures

Let's start from the top. 
> In software architecture, a messaging pattern is an architectural pattern which describes how two different parts of an application, or different systems connect and communicate with each other. [Wikipedia, especially section on Software communictaion.](https://en.wikipedia.org/wiki/Messaging_pattern)

To get more in details, messaging patterns fall within intergration patterns, and according to the very valuable book "[Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/)", there are at least 65 that have been identified, organized as follows:
  ![Enterprise Integration Pattern, 65 patterns chart](/docs/enterprise-integration-patterns-book-messaging-graph.png)

While the messaging patterns are not new, they have found a new life in the Cloud computing world and the Microservices architectures, where disparate and uncoupled systems and services seek to communicate with each other.

One of those patterns of interest to us is the [Publisher/Subscriber channel](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PublishSubscribeChannel.html) pattern (or PubSub).

In a nutshell, following the graph above, Application 'A' needs to execute and communicate with Application 'B' without being bound to it, or tied to its changes or evolution. And Application 'B' needs to reply to Application 'A', again, without being bound to it, or even without being aware if 'A' is running or not. They will communicate with each-other using message, where 'A' is the publisher and 'B' is the subscriber.

An illustration of PubSub pattern, with competing consumers. On the left side, are a series of client applications that generate messages. They will post their messages in a message queue, being unaware of how or when these messages will be processed/consumed. On the right side are a series of consumer server applications whose job it to process as many of those messages as possible and as fast as they can, unaware of where these messages are coming from and when. 

 ![PubSub pattern with competing cnosumers](/docs/pubsub-overview-pattern-competing-consumers.png)

While there is much literature and examples across the web regarding Messaging integration patterns and architecture, there is no need to know more about it than what is highlighted here. Though, more knowledge is never a bad thing! The book  "[Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/)" is a great resources to discover more.

### Working with queues




### Deadletter queues



#### Two important rules when dealing with messaging patterns in general, and PubSub in particular

##### Asynchronicity. 
It is important to understand the difference of synchronous vs. asynchronous communication between two systems. The differences are well explained in this post [here](https://aws.amazon.com/blogs/architecture/introduction-to-messaging-for-modern-cloud-architecture/). I recommend you read it! 

Practically when we make a call to an API or to a database, we wait for the response. Even though we may do other things (asynchronously) meanwhile, sooner or later we will need to go back and wait for that response, as without it we cannot go further. Versus, if we need information from a system (via an API) or a datastore, we simply post our request-for-information in a message queue, and we move on (literally). Optionally, we poll the message queue to see if there is a response to our request, and if there is we update our state. 

Why is this important? In Pub/Sub (as image above illustrates) or Fan/In or Fan/Out patterns, since the clients and the servers, compete with each-other to send messages to a message queue, it is impossible to predict in which order the messages have entered the queue and in which order they exit or are processed. For example, if there are 5 applications that are sending 10 messages each to the queue simultaneously, then App 'A' will send message 1 to the queue, and so will all other apps, than App 'A' will send message 2, which in the queue will now be in the position 6, after 5 messages for each app. Now, on the right side, if there are 10 processors processing messages, they will all pick 1 message to process, so messages '1' and '2' from Applications 'A' through 'E' are all processed simultaneously, and then messages '3' and '4' and then '5'. As you can see it is virtually impossible to determine an order (at least not easily and not without efforts) on how these messages are processed. Yu can only assume that they will be processed, but not when at in what order.

##### Idempotency. 
In computer science, a function or operation is idempotent when that function or operation is called more than once, with same input parameters, will produce always the same result. In messaging patterns this concept is very important. 

I said above that the order of messages cannot be guaranteed. Another thing that cannot be guaranteed is the fact that a message will be retrieved or executed only once. Since the consumers, on the right side are numerous and with high frequency, it is possible that two consumers will retrieve the same message, right before one of them being able to lock that message so it is not retrieved by another consumer. Or that the time that a consumer takes to process the message may go over the allotted time of the lock period of the message. In that case, the message will become available again for pick up.

So, to circumvent this pesky problem, you will need to write code that guarantees idempotency. For example, let say the messages in the message queue are records that need to be written to the datastore. If you'd write the software logic as an insert to the database, then the first message will go in, and the second will fail since the record already exists. Which will cause the 2nd message to repeat a few times and then needlessly go into the dead-letter queue or poison queue. Idempotency is not implemented here! But, if you'd write the software as an upsert instead, the first message will insert the record and the second will attempt to update it, but since values are the same it will make no material changes. That way the second message, or a third or a fourth, would make no difference and would leave the system in the same state as the message had run only once. Idempotency is preserved. When working with messaging pattern it is of critical importance that you write software that preserves idempotency. Unless you are a fan of surprises!

### Understanding of the Command pattern in software development

The Command pattern is an important pattern heavily used here, so will take some time to go over some of the concepts.

> In object-oriented programming, the command pattern is a behavioral design pattern in which an object is used to encapsulate all information needed to perform an action or trigger an event at a later time....

> Four terms always associated with the command pattern are command, receiver, invoker and client. A command object knows about receiver and invokes a method of the receiver. Values for parameters of the receiver method are stored in the command. The receiver object to execute these methods is also stored in the command object by aggregation. The receiver then does the work when the execute() method in command is called. An invoker object knows how to execute a command, and optionally does bookkeeping about the command execution. The invoker does not know anything about a concrete command, it knows only about the command interface. Invoker object(s), command objects and receiver objects are held by a client object, the client decides which receiver objects it assigns to the command objects, and which commands it assigns to the invoker. The client decides which commands to execute at which points. To execute a command, it passes the command object to the invoker object.

> Using command objects makes it easier to construct general components that need to delegate, sequence or execute method calls at a time of their choosing without the need to know the class of the method or the method parameters. Using an invoker object allows bookkeeping about command executions to be conveniently performed, as well as implementing different modes for commands, which are managed by the invoker object, without the need for the client to be aware of the existence of bookkeeping or modes. [Wikipedia.](https://en.wikipedia.org/wiki/Command_pattern)

A more detailed and easy explanation is illustrated at the [Refactoring Guru](https://refactoring.guru/design-patterns/command) website.

In short, a simple implementation of the command pattern follows as:

```cs

public interface ICommand
{
    public void Execute();
}
public class Read : ICommand
{
    string title { get; set; }
    public Read(string Title) => title = Title;
    public void Execute()
    {
        Console.Write($"Reading: {title}.");
    }
}
public class Write : ICommand
{
    string desc { get; set; }
    public Write(string Description) => desc = Description;
    public void Execute()
    {
        Console.Write("Writing: {desc}.");
    }
}
public static class Processor
{
    public static void Run(ICommand command) => command.Execute();

    public static void Run(IEnumerable<ICommand> commands) => commands.ForEach(t => t.Execute()));
}

class Program
{
    static void Main(string[] args)
    {
        var relaxing_activites = new List<ICommand>();

        relaxing_activites.Add(new Read("Robinson Crusoe"));
        relaxing_activites.Add(new Write("a blog"));
        relaxing_activites.Add(new Read("War & Peace"));

        relaxing_activites.Run(relaxing_activites);
    }
}

```

It is sort of self-evident of what is happening in this piece of code. The requests, that generally are methods to a class, are now objects. And they have all they need to "execute" themselves, in terms of state and functionality. We do create a stack or unit of work of various requests/commands and whenever we feel that is enough, we defer their processing to a processor that executes them in the order they were received.

This solution follows the same pattern or behavior.



## Usage

The solution is made of four parts:

- **The packages**. The first, and the simplest, is a separate project that act as a collection of all the commands. In real life examples, this project would be separate for each team or projects, depending on the project needs.
It is best to categorize them by functionality. For example, here I am creating groups such as ```WebCommands```, where I would place commands that would interact with the Web, like making http calls or API post requests. Here I create a few sample commands that call a public API and return the URL of images of a fox, a dog and a cat. They are all API calls. This also tells me that most certainly I will need to inject an ```HttpClient``` object as a dependency because they will all need it. Another folder would be ```AzureCommands```, for Azure related commands, which may require dependencies such as a ```TableClient``` or ```StorageClient```. Or a ```GenericCommands```, in this case a sample command that adds two numbers. If there are commands that return ```Responses```, then a folder for them as well.

- **The unit tests**. Unit testing this solution is especially hard. While you can unit test the execution of each individual command, the goal here is to unit test the mechanism on how these commands are integrated and executed. As such they are more of integration tests. And because they integrate with a single remote processor, it is unreliable to run the tests as a suite, because you cannot control the order in which each test runs, hence you will get false positives all the time. The tests are here as an illustration, and you would get positive results if you run them one-by-one, to see everything working, without having to deploy Azure Functions or build a Console App. As such, they are useful, though limited.

- **The ServerTools packages**. There are two flavors of the packages: [the Core package](https://www.nuget.org/packages/ServerTools.ServerCommands/), it is only used if you want to create your own implementation, for a service that do not have a current implementation (see details below on how to create your own implementation for this), and [the implemented packages](https://www.nuget.org/packages/ServerTools.ServerCommands/), which currently are two, one for Azure Storage Queues, and Azure Service Bus (more to come). Most of the time you will use one of these implemented packages. Add one of these packages [![NuGet Badge](https://buildstats.info/nuget/ServerTools.ServerCommands)](https://www.nuget.org/packages/ServerTools.ServerCommands/) to both the Unit Tests project and the Azure Function project. Follow the evolution of this package and its release notes [here](https://github.com/hgjura/ServerTools.ServerCommands). There is a possibility to mix and match these packages, i.e., using both Storage Queues and Service Bus in your project. However, most of the time you would use one or the other.

- **The Azure Function project**. This is the project that handles the processing of remote commands. I have chose to deploy this using an [Eternal Durable Function](https://github.com/hgjura/example-of-eternal-durable-azure-functions). But any service or technology that allows for always-on and some compute power can do this. A windows service, a command line prompt, an AWS Lambda, etc. In implementing this, it is a choice of mine to separate, when possible, the development logic from the configurations and personalisation, and plumping logic that goes into Functions code. I have created a set of static classes (```FunctionsDurable``` and ```FunctionsScheduled```) that holds the generic versions of the durable functions I need (the FunctionsScheduled is a bit  out of place here as it not something used in the Durable Functions, but I needed a place to place a Timer triggered function I need to run in longer intervals, let say of one hour). I  have also created the ```FunctionSettingsEternalDurable``` class that simply holds any configurations and/or personalisations to the generic functions. And than, the third layer, the ```FunctionImplementations``` that holds all the business logic of what all these functions do. Most of the work goes into this third file. This acts as a facade to the Functions, so once Functions code is good, I don’t have to touch it anymore and only make changes to this class/file. Eternal durable functions are made of 3-4 components so, mixing the code with plumbing code it gets complicated.

Initially, in the ```FunctionSettingsEternalDurable``` I setup some wiring-up code for the Functions. I extend the Function ```Startup``` function to feed as a dependency an ```IHttpFactory``` object with the retry policy I want. ```IHttpClient`` is an expensive object and I don't want to create a new one every time I run a command.

I also extend the ```ConfigureAppConfiguration``` function of Startup and feed it some configuration options, to get the account key for the storage account, either from a local json file, user secrets, or app settings when deployed to Azure. If you'd be using the version of the library that uses Azure Service Bus or any other service, here you wold get and inject all the connection information.

Next, in the ```FunctionImplementations``` I define the orchestrator code as follows:

 ```csharp
public static async Task<int> FunctionWrapperExecuteCommandsAsync(ILogger logger)
{
            int r = 0;

            try
            {
                r = await ExecuteCommandsAsync(config, logger);

                if (r > 0)
                {
                    logger.LogWarning($"{r} commands were succesfuly executed.");
                }
                else
                {
                    logger.LogWarning($"No commands were executed. Pausing for {FunctionSettingsEternalDurable.MinutesToWaitAfterNoCommandsExecuted} min(s).");

                    r = FunctionSettingsEternalDurable.MinutesToWaitAfterNoCommandsExecuted;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"{ex.Message} [{ex.InnerException?.Message}]");
                logger.LogWarning($"An error ocurred. Pausing for {FunctionSettingsEternalDurable.MinutesToWaitAfterErrorInCommandsExecution} min(s).");

                r = FunctionSettingsEternalDurable.MinutesToWaitAfterErrorInCommandsExecution;
            }

            return r;
}
  ``` 
This piece of code orchestrates the lifecycle of the Durable Function. Practically, it calls the main function ```ExecuteCommandsAsync```and acts accordingly depending on the returned value. If the function return > 0, it means 1 or more commands were remotely executed, so re-run the function again. If the function returns 0, means that no commands were executed, and so snooze the next run by as many minutes as defined in ```MinutesToWaitAfterNoCommandsExecuted```. If the function throws an unhandled exception, than log exception and snooze by as many minutes as defined in ```MinutesToWaitAfterErrorInCommandsExecution```. 

Next is the function that executes all remote commands + responses.

```csharp
private static async Task<int> ExecuteCommandsAsync(ILogger logger)
{
    var _container = new CommandContainer();
    using var client = new HttpClient();
    _container
        .Use(logger)
        .Use(client)
        .RegisterCommand<RandomCatCommand>()
        .RegisterCommand<RandomDogCommand>()
        .RegisterCommand<RandomFoxCommand>()
        .RegisterCommand<AddNumbersCommand>()
        .RegisterResponse<AddNumbersCommand, AddNumbersResponse>();
    var c = new Commands(_container, Configuration["StorageAccountName"], Configuration["StorageAccountKey"], logger);
    var result1 = await c.ExecuteCommands();
    var result2 = await c.ExecuteResponses();
    // return number of commands + responses executed
    return result1.Item2 + result2.Item2;
}
```

Prior to calling ```ExecuteCommands``` or ```ExecuteResponses```, need to register all possible commands and all their dependencies. If a command is encountered that does not have a corresponding registration it will throw an unhandled exception. And after 5 times of such exception, it will be put aside in the dead-letter queue. So, **make sure** you complete all the registrations for all commands. The same for dependencies that need to be injected into each command. So, for example, a ```RandomCatCommand``` is a web command. It gets instantiated by passing an ```HttpClient``` object. If you don’t register the HttpClient object the container cannot resolve the ```RandomCatCommand``` and will throw an error. 

Next, and optionally, I created an http triggered function that simply creates and posts a few sample commands, for the above to process.

```csharp
        public static async Task<string> PostCommandsAsync(IConfiguration config, ILogger logger)
        {

            try
            {

                var c = await new CloudCommands().InitializeAsync(new CommandContainer(), new AzureStorageQueuesConnectionOptions(config["StorageAccountName"], config["StorageAccountKey"], 3, logger, QueueNamePrefix: "test-project"));

                _ = await c.PostCommandAsync<RandomCatCommand>(new { Name = "Laika" });

                _ = await c.PostCommandAsync<RandomDogCommand>(new { Name = "Scooby-Doo" });

                _ = await c.PostCommandAsync<RandomFoxCommand>(new { Name = "Penny" });

                _ = await c.PostCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 });


                return "Ok.";
            }
            catch (Exception ex)
            {
                return ex.Message;

            }

        }

```
Here you dont need to register all commands before you post. The ```PostCommand``` only need to kow the type of the command and the command context, before posting it. The container, at this time, does not resolve the object, only needs to know its type.

Also make sure to pass to the command context, as a ```dynamic``` object, to the container. If the context properties are incorrect or missing, the command execution will fail. For example, ```AddNumbersCommand``` expect as context a ```dynamic``` object that has two properties: ```Number1``` and ```Number2```. If these properties are not there, or named differently, the command execution will fail, and the command will eventually end up in the dead-letter queue.

And that's that!





## Roadmap

See the [open issues](https://github.com/hgjura/command-pattern-with-queues/issues) for a list of proposed features (and known issues).

- [Top Feature Requests](https://github.com/hgjura/command-pattern-with-queues/issues?q=label%3Aenhancement+is%3Aopen+sort%3Areactions-%2B1-desc) (Add your votes using the 👍 reaction)
- [Top Bugs](https://github.com/hgjura/command-pattern-with-queues/issues?q=is%3Aissue+is%3Aopen+label%3Abug+sort%3Areactions-%2B1-desc) (Add your votes using the 👍 reaction)
- [Newest Bugs](https://github.com/hgjura/command-pattern-with-queues/issues?q=is%3Aopen+is%3Aissue+label%3Abug)



## Support

<!-- > **[?]**
> Provide additional ways to contact the project maintainer/maintainers. -->

Reach out to the maintainer at one of the following places:

- [GitHub issues](https://github.com/hgjura/command-pattern-with-queues/issues/new?assignees=&labels=question&template=04_SUPPORT_QUESTION.md&title=support%3A+)
- The email which is located [in GitHub profile](https://github.com/hgjura)

## Project assistance

If you want to say **thank you** or/and support active development of ServerTools.ServerCommands:

- Add a [GitHub Star](https://github.com/hgjura/command-pattern-with-queues) to the project.
- Tweet about the ServerTools.ServerCommands on your Twitter.
- Write interesting articles about the project on [Dev.to](https://dev.to/), [Medium](https://medium.com/) or personal blog.

Together, we can make ServerTools.ServerCommands **better**!

## Contributing

First off, thanks for taking the time to contribute! Contributions are what make the open-source community such an amazing place to learn, inspire, and create. Any contributions you make will benefit everybody else and are **greatly appreciated**.

We have set up a separate document containing our [contribution guidelines](.github/CONTRIBUTING.md).

Thank you for being involved!

## Authors & contributors

The original setup of this repository is by [Herald Gjura](https://github.com/hgjura).

For a full list of all authors and contributors, check [the contributor's page](https://github.com/hgjura/command-pattern-with-queues/contributors).

## Security

ServerTools.ServerCommands follows good practices of security, but 100% security can't be granted in software.
ServerTools.ServerCommands is provided **"as is"** without any **warranty**. Use at your own risk.

_For more info, please refer to the [security](.github/SECURITY.md)._

## License

This project is licensed under the **MIT license**.

Copyright 2021 [Herald Gjura](https://github.com/hgjura)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

<!-- ## Acknowledgements

> **[?]**
> If your work was funded by any organization or institution, acknowledge their support here.
> In addition, if your work relies on other software libraries, or was inspired by looking at other work, it is appropriate to acknowledge this intellectual debt too. -->
