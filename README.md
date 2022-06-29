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

 ![PubSub pattern with competing consumers](/docs/pubsub-overview-pattern-competing-consumers.png)

While there is much literature and examples across the web regarding Messaging integration patterns and architecture, there is no need to know more about it than what is highlighted here. Though, more knowledge is never a bad thing! The book  "[Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/)" is a great resources to discover more.

### Working with queues
Working with queues it is different, and rather unusual, than the most common data computing scenarios. If I can use a metaphor, working with queues is like building a dam in a large and unpredictable river, to stabilize its flow. In one side of the dam (the publisher) is the wild, unpredictable, ill-tempered flow of water, and on the other side (the subscriber) is the multi-channeled, regular, always controlled flow of water. In order to achieve a successful flow, and tame the wild body of water (messages), you will need to run through a series of analysis and strictly apply some fundamental rules.

> **Rule #1: The subscriber must process/ingests the message flow faster than the publisher publishes them.**

Just like when you build a dam, the reservoir on the top has a limited capacity of how much water can handle. The same for all services what are built to enable messaging solutions. They all come with a limited capacity of how much data they can hold in an unprocessed state. When that limit is reached, then the publisher gets hammered with errors of the service not being available, and at best, it will result in a massive loss of valuable data. Hence, the solution is to design a subscriber solution, that at any time and at any circumstance processes the data at a faster rate that it is being published in the queue. That means, that whatever your compute solution you decide it needs to autoscale at much faster rate and scale down as easier. Service such as Azure Functions or containers that can autoscale on demand are ideal solutions.

This is not the situation you want to find yourself in:

![Overflown dam](https://i.redd.it/f58a4ueg7m341.jpg)

> **Rule #2: Chose an appropriate lock period for your message.**

The advantage that all these messaging-based services have over any other storage mechanism, is better concurrency. They accomplish this in a very effective and lightning-fast manner, which is what you want. So, when a message is pulled from the queue from reading, that message is (almost) guaranteed that will not be available to any one subscriber to read, that way guaranteeing a read at-least-once method. However, there is not much magic that goes on here. These services are kind of blunt instruments. They do this by using something called a lock period on the message, which is a timespan for the message to be invisible to anyone, except to the one that triggered the read. Usually, the default for this is 10 seconds if you don’t set it up yourself. Don't take the default! You need to analyze your context and find out that this lock period is sufficient for you. Obviously, the queue has no idea of what you are trying to do with the message. So, if you are trying to process the message and save it somewhere, and that processing takes longer than the lock period (10 seconds) than that message becomes visible again for another subscriber, or the same subscriber, for processing. And if you are not careful, you will be creating an infinite loop of message processing and overflow the system with messages. Imagine if you have 1M messages in the queue, well, from a processing point of view this will double every 10 seconds, crashing down your infrastructure. Also, you will be creating millions and millions of duplicate data in your final store, via incomplete or delayed processing, as you will be saving the same messages repeatedly.

> **Rule #3: Implement idempotency for your message processing.**

This is a must! What is idempotency I am describing it below. There are many situations where the same message(s) becomes available more than once for reading/processing. The triggering of rule #2 above is one of them. But there are other, less common reasons when that happens. And when it does, you should protect your data from errors and corruption. Idempotency is the way to guarantee that the same data is saved once and only once. In databases, this is accomplished via the "upsert" method. By implementing this, you are mitigating at least half of the disaster that would come with an inappropriate implementation of Rule #2.

> **Rule #4: Chose an appropriate lifespan for your message.**

Another very important timespan is that of the lifetime of the message before expiration. This is also known as time-to-live, or TTL, and is usually by default set at 15 minutes. This is time that a message is available until it expires. At expiration, the message is either removed altogether and deleted it, or it is sent to a dead-letter queue (see below). Here I will focus more on this second option. Let's combine the disaster that could happen form failure of Rule #2 with this one. Let's assume that the subscriber is taking too long to process a message, and/or is failing with an error at around the 10 second mark. It will keep going back to the queue for processing, and it will do that for 90 times, within the 15 minutes that it takes the message to expire. And if change the TTL to make it 1 hour, that it will be 360 times. Or 8640 times, if you set it to 24 hours!!! So, you will be multiplying the number of messages many times and create a disaster before being aware of what's happening. Most likely your infrastructure, would of have crashed before seeing the end of it, or your queue would of have reached its maximum limits, and then crash. So why is this 15 minute? Well, it is a reasonable time period for you to troubleshoot any issues with the publisher and restart the process. You need to define what your process is into troubleshooting and restoring process. If it is less than 15 minutes, than set it lower, if it higher or much higher, like 20 hours, than you are risking way too much by making it higher and may want to send the messages to the dead-letter queue earlier, as you will not be restoring the system sooner anyways. 

> **Rule #5: Always process the dead-letter queue.**

Read below to find out about the dead-letter queue (DLQ). It is so important it gets to have its own section. If the messaging system is a car race, the dead-letter queue is the place you pull over to make repairs. It is the place where expired message, or messaging you have trouble processing get put in suspended animation and out of the way of the flood of messages in the main queue. Not all messaging systems offer a DLQ. Some of the more sophisticated ones, like Azure Service Bus, do offer it (optionally) when you construct your queues. So it is subqueue for troubled messages. But it is still part of the same queue, and ads to the volume limits of the main queue. Before you even start designing the subscribers and decide how to process the main queue, you will need to make plans about processing the dead-letter queue. If you don't that this will turn into a graveyard or trash can for messages, and over time will eventually fill in all the queue limits and overflow it.

There is no prescribed recipe on how to process the DLQ. But most scenarios call for a separate and independent process/subscriber that runs in a predefined interval, usually 60 or 90 minutes, and simply takes the messages from the DLQ and places them back into the main queue, thus clearing out the dead-letter queue.
But this comes with its own set of problems. Let's combine the issues of Rule #2 and Rule #4, with this one. While a message has a TTL that expires a message from the main queue, a message in the DLQ never expires! So if you are taking expired messages, or messages that fail due to processing errors, and putting them back into the main queue every hour, than you are simply replicating the problems created by a failed implementation of Rule #2 and Rule #4 throughout the day. So, you must always process the dead-letter queue, and never abandon messages in it, but you should do in a thoughtful way. Read some of the suggestions below on some of the best practices on how to implement this properly.  

> **Rule #6: Build lightweight subscribers.**

I will continue to use the metaphor of the river and the queue being the dam. It is very possible that your business requirements are to take water from the river, put in bottles, and sell it to the store. While these are very valid business requirements, you should not bundle them in the subscribers' logic, whose job is to extract messages from the queue. There is too much complexity and too many dependencies. You are taking too much risk, and it will be just a matter of time or circumstance, where you will be failing Rule #2 and Rule #4, and if Rule #3 and #5 are not implemented correctly, this will be an unmitigated disaster of data loss and failed infrastructure. 

Queue-based infrastructure and messaging architectures are infrastructure concepts and not development ones. They goal is to facilitate the transporting of data from Point A to Point B, where Point A is the wild and unpredictable part of the river, and Point B is structured, channeled and controlled part of it. It should be agnostic to what you do from a development or business logic/features point of view.

How lightweight should the subscribers be? That will depend very much in your context. However, in most instances, they complete two tasks: 1) retrieve a message and do a light inspection of properties and content and 2) save the message to a data store, that is close to your environment, and that is highly available and reliable.

In the cloud world, these subscribers are best fit for services like Azure Functions or light-weight containers, that can scale on demand, and run at a very specific time limits. Azure Functions or AWS Lambdas make good candidates. Azure Functions, for example, expire after 2 mins of running, so you cannot afford to place to much processing logic in it. 
 
> **Rule #7: Always handle exceptions.**

This seems as stating the obvious, but it cannot be stressed more. The subscribers should catch all exceptions; handle the queue-specific ones; handle any obvious known exceptions; and bubble the rest upward, to be handled by your code. 

Imagine that by a strange circumstance the processing of your message in a subscriber fails to a division-by-zero exception. This would create an infinite loop of failed messages, due to Rule #2, #4 and #5 above. It will like a tsunami of messages, for a short period of time, until you overflow the queue and bring down your service. Well, all could of have been avoided by simply handling a very common exception, and a good implementation of the Strategy #2 of handling deadletter queue below. This would be a difference of trying to process a message once or twice vs. one hundred thousand times.

A good exception handling strategy here, can avoid the worse of disasters, or at least alleviate unnecessary traffic strains on the queue.

> **Rule #8: Always analyze the flow of your messages and make business decisions and rules about your data flow.**      

Everything that was mentions up to this point is good, but there are no predefined recipes for it all. It all depends on the data and the business rules you need to apply. 

Though a queue transports all data the same way, data is not the same all the time. Let say you are using a queue to transport event logs, and another to transport accounting transaction data. The way you implement Rule #2, #4, and #5 are very different for both of them. And they should be! The risk tolerance for these types is very different.

So a proper analysis of the data flow, data type, and its importance to the business, should reveal enough information for you to properly implement all the above rules. 

The key here is to come up with enough strict business rules, to not leave room for ambiguity, and cover 100% of the probabilities. This will result in a series of business decisions that will regulate the data flow. You will need to find answers and define rules for questions such as: What if there are messages that a subscriber cannot recognize and process, what do we with them? What do we do with a message that fails while processing, more than 3 times, or after trying for 2 minutes? How do we handle expired messages? And the answer cannot be "nothing", or "let's put it aside until we know more", or "keep trying until it works". These are ambiguous answers that will eventually lead to a system failure. 


#### Important rules when dealing with messaging patterns in general, and PubSub in particular

##### Asynchronicity. 
It is important to understand the difference of synchronous vs. asynchronous communication between two systems. The differences are well explained in this post [here](https://aws.amazon.com/blogs/architecture/introduction-to-messaging-for-modern-cloud-architecture/). I recommend you read it! 

Practically when we make a call to an API or to a database, we wait for the response. Even though we may do other things (asynchronously) meanwhile, sooner or later we will need to go back and wait for that response, as without it we cannot go further. Versus, if we need information from a system (via an API) or a datastore, we simply post our request-for-information in a message queue, and we move on (literally). Optionally, we poll the message queue to see if there is a response to our request, and if there is we update our state. 

Why is this important? In Pub/Sub (as image above illustrates) or Fan/In or Fan/Out patterns, since the clients and the servers, compete with each-other to send messages to a message queue, it is impossible to predict in which order the messages have entered the queue and in which order they exit or are processed. For example, if there are 5 applications that are sending 10 messages each to the queue simultaneously, then App 'A' will send message 1 to the queue, and so will all other apps, than App 'A' will send message 2, which in the queue will now be in the position 6, after 5 messages for each app. Now, on the right side, if there are 10 processors processing messages, they will all pick 1 message to process, so messages '1' and '2' from Applications 'A' through 'E' are all processed simultaneously, and then messages '3' and '4' and then '5'. As you can see it is virtually impossible to determine an order (at least not easily and not without efforts) on how these messages are processed. You can only assume that they will be processed, but not when at in what order.

##### Idempotency. 

In computer science, a function or operation is idempotent when that function or operation is called more than once, with same input parameters, will produce always the same result. In messaging patterns this concept is very important. 

I said above that the order of messages cannot be guaranteed. Another thing that cannot be guaranteed is the fact that a message will be retrieved or executed only once. Since the consumers, on the right side are numerous and with high frequency, it is possible that two consumers will retrieve the same message, right before one of them being able to lock that message so it is not retrieved by another consumer. Or that the time that a consumer takes to process the message may go over the allotted time of the lock period of the message. In that case, the message will become available again for pick up.

So, to circumvent this pesky problem, you will need to write code that guarantees idempotency. For example, let say the messages in the message queue are records that need to be written to the datastore. If you'd write the software logic as an insert to the database, then the first message will go in, and the second will fail since the record already exists. Which will cause the 2nd message to repeat a few times and then needlessly go into the dead-letter queue or poison queue. Idempotency is not implemented here! But, if you'd write the software as an upsert instead, the first message will insert the record and the second will attempt to update it, but since values are the same it will make no material changes. That way the second message, or a third or a fourth, would make no difference and would leave the system in the same state as the message had run only once. Idempotency is preserved. When working with messaging pattern it is of critical importance that you write software that preserves idempotency. Unless you are a fan of surprises!

### Dead-letter queues

The dead-letter queue (DLQ) is an unusual, but very important concept, in messaging-based services. Such services, known for their ability to transfer messages at very high speed and volume, allow for an opportunity for those messages that cause trouble and slow down the flow (via system exceptions, or expiration, or user-specific errors), to be put aside in a sub-queue for later processing and inspection. It is like a pit-stop in car racing.

With this feature, you can separate the fast and furious flow of good messages from the ones that do or can create problems. Obviously, you will need to have two separate processes and plans in place to process both messages in the main queue and those in the DLQ. The processing in the DLQ is usually slower, with a lot more of exception handling, and with a lot more logic and rules that go in it.

While there are no prescribed recipes for handling the DLQ, there are two most common strategies that are deployed here:

* Strategy #1. Placing messages back into the main queue. This is the most common (and simple) approach that is used most of the time. It is rather simple. The subscriber that processes the DLQ runs in predefined intervals, 60 to 90 minutes, enough for whatever that caused the messages to fail or expire to be fixed and up and running. Then the messages are taken and placed back in the main queue for processing.
This strategy, though nice and simple, makes a few assumptions, like the 60 minutes time-period is enough to fix whatever is broken in the system, or that messages with errors can actually be fixed in the next round of processing. What happen if the broken subscribers takes longer than 60 - 90 mins to fix, or what if a new wave of messages arrive for which the subscriber does not recognize or has no logic to process them? Or what if the errors are something system specific like a division-by-zero or a deserialization error, which re-processing them again will not fix? If you simply putting them back in the queue, will create an infinite loop of message attempts to processing.

* Strategy #2. Create a custom processor that inspects the message in the DLQ and creates alternatives to just simply putting it back in the queue. For example, if message has been in the DLQ more than 3 times, archive it permanently somewhere and delete it. Or simply delete it, if archival is not necessary. Or if it is a division-by-zero error, log the error and context (notify the owner) and delete, etc.

The goal is here that once you retrieve a message from the DLQ, you should process it in such a way that it has close to 0% chance that it will go back to the DLQ again.

Not all messaging services provide a DLQ functionality out of the box. Azure Service Bus does, but for example Azure Storage Queues doesnt. For those who don't you will need to create a similar concept in your solution to benefit from this functionality.

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
