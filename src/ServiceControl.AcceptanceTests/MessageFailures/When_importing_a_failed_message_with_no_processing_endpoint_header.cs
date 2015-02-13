namespace ServiceBus.Management.AcceptanceTests.MessageFailures
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Remoting.Contexts;
    using System.Text;
    using System.Threading;
    using Contexts;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Satellites;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using Raven.Abstractions.Data;
    using Raven.Abstractions.Extensions;
    using Raven.Client.Document;
    using Raven.Json.Linq;
    using ServiceBus.Management.Infrastructure.Settings;
    using ServiceControl.Infrastructure;
    using ServiceControl.MessageFailures;
    using ServiceControl.Operations;

    [TestFixture]
    public class When_importing_a_failed_message_with_no_processing_endpoint_or_replyaddress_or_failedq_header : AcceptanceTest
    {
        [Test]
        public void Should_be_moved_to_the_service_control_error_queue()
        {
            //Clear the log dir
            var pathOfLogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Particular\\ServiceControl\\logs\\FailedImports\\Error");

            foreach (var file in Directory.EnumerateFiles(pathOfLogFile))
            {
                File.Delete(file);
            }

            JsonDocument failedImport = null;
            var context = new FailedMessageTestContext();
            Scenario.Define(context)
                .WithEndpoint<ManagementEndpoint>(c => c.AppConfig(PathToAppConfig).When(ctx => ctx.IsMessageWrittenToFile, b => b.Send("Particular.ServiceControl", new ReimportMessage())))
                .WithEndpoint<ServerEndpoint>()
                .Done(c =>
                {
                    var done = c.IsMessageWrittenToFile && Directory.EnumerateFiles(pathOfLogFile).Count() == 2;
                    if (done)
                    {
                        var importErrors = Directory.EnumerateFiles(pathOfLogFile).Select(Path.GetFileNameWithoutExtension).ToArray();
                        var newImportError = importErrors.Single(x => x != c.InitialImportErrorId);

                        using (var docStore = new DocumentStore()
                        {
                            Url = "http://localhost:33333/storage"
                        }.Initialize())
                        {
                            failedImport = docStore.DatabaseCommands.Get("FailedErrorImports/"+ newImportError);
                        }
                    }
                    return done;
                })
                .Run(TimeSpan.FromMinutes(2));

            Console.WriteLine(context.InitialImportErrorDoc);
            var failedError = failedImport.DataAsJson.ToString();
            Console.WriteLine(failedError);
            var message = (RavenJObject) failedImport.DataAsJson["Message"];
            var headers = (RavenJObject) message["Headers"];
            //Assert.AreEqual("4.6.5", headers["NServiceBus.Version"].Value<string>()); 
            Assert.IsNotNull(headers["SomeFunkyHeader"]); //Our beloved header is missing

        }

        public class ReimportingManagementEndpoint : ManagementEndpoint
        {
            public class ReimportHandler : IHandleMessages<ReimportMessage>
            {
                public AttemptToReImportFailedMessages Reimporter { get; set; }

                public void Handle(ReimportMessage message)
                {
                    Thread.Sleep(5000);
                    Reimporter.Start();
                }
            }
        }

        public class ServerEndpoint : EndpointConfigurationBuilder
        {
            public ServerEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class Foo : IWantToRunWhenBusStartsAndStops
            {
                string pathOfLogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Particular\\ServiceControl\\logs\\FailedImports\\Error");
                public ISendMessages SendMessages { get; set; }
                public FailedMessageTestContext Context { get; set; }

                public void Start()
                {
                    //hack until we can fix the types filtering in default server
                    if (Configure.EndpointName != "Particular.ServiceControl")
                    {
                        return;
                    }

                    const string messageId = "bde7ee4b-57ae-40f4-a9a6-a43300924379";
                    Context.MessageId = messageId;
                    var transportMessage = new TransportMessage(messageId,new Dictionary<string, string>()
                    {
                        {"SomeFunkyHeader","SomeFunkyValue"}
                    })
                    {
                        CorrelationId = "b8ecb7d9-644b-4990-9b57-a41f00f1c196",
                        MessageIntent = MessageIntentEnum.Send,
                        Body = new byte[100],
                        ReplyToAddress = Address.Parse("Access.Cloud.WebService.Api@GS2WEB20"),
                    };
                    SendMessages.Send(transportMessage, Address.Parse("error"));

                    var fileExists = false;
                    while (!fileExists)
                    {
                        fileExists = Directory.EnumerateFiles(pathOfLogFile).Any();
                    }
                    Context.InitialImportErrorId = Path.GetFileNameWithoutExtension(Directory.EnumerateFiles(pathOfLogFile).First());
                    Context.IsMessageWrittenToFile = true;
                    using (var docStore = new DocumentStore()
                    {
                        Url = "http://localhost:33333/storage"
                    }.Initialize())
                    {
                        var failedImport = docStore.DatabaseCommands.Get("FailedErrorImports/" + Context.InitialImportErrorId);
                        Context.InitialImportErrorDoc = failedImport.DataAsJson.ToString();
                    }
                }

                public void Stop()
                {
                }
            }
        }

        public class MyMessage : IMessage
        {
        }

        public class ReimportMessage : IMessage
        {
        }

        public class FailedMessageTestContext : ScenarioContext
        {
            public bool IsMessageWrittenToFile { get; set; }
            public string InitialImportErrorId { get; set; }
            public string MessageId { get; set; }
            public bool RetryIssued { get; set; }
            public string InitialImportErrorDoc { get; set; }
        }
    }
}
