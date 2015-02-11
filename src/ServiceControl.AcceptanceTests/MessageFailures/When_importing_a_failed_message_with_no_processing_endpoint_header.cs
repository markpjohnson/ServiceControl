namespace ServiceBus.Management.AcceptanceTests.MessageFailures
{
    using System;
    using System.IO;
    using System.Runtime.Remoting.Contexts;
    using Contexts;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Satellites;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using ServiceBus.Management.Infrastructure.Settings;
    using ServiceControl.Operations;

    [TestFixture]
    public class When_importing_a_failed_message_with_no_processing_endpoint_or_replyaddress_or_failedq_header : AcceptanceTest
    {
        [Test]
        public void Should_be_moved_to_the_service_control_error_queue()
        {
            var context = new FailedMessageTestContext()
            {
                MessageId = Guid.NewGuid()
            };

            FailedErrorImport failure = null;
            Scenario.Define(context)
                .WithEndpoint<ManagementEndpoint>(c => c.AppConfig(PathToAppConfig))
                .WithEndpoint<ServerEndpoint>()
                .Done(c => c.IsMessageWrittenToFile)
                .Run(TimeSpan.FromMinutes(2));

            // TODO: Need to check the FailedErrorImports in RavenDB and assert that the headers have the correct value. 
            // At the moment, the OriginatingEndpoint is getting stamped with `Particular.ServiceControl.`

        }

        public class ServerEndpoint : EndpointConfigurationBuilder
        {
            public ServerEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class Foo : IWantToRunWhenBusStartsAndStops
            {
                public ISendMessages SendMessages { get; set; }

                public FailedMessageTestContext TestContext { get; set; }

                public void Start()
                {
                    //hack until we can fix the types filtering in default server
                    if (TestContext == null || string.IsNullOrEmpty(TestContext.MessageId.ToString()))
                    {
                        return;
                    }

                    if (Configure.EndpointName != "Particular.ServiceControl")
                    {
                        return;
                    }

                    // Transport message has no headers for Processing endpoint and the ReplyToAddress is set to null
                    var transportMessage = new TransportMessage
                    {
                        ReplyToAddress = null
                    };
                    SendMessages.Send(transportMessage, Address.Parse("error"));
                }

                public void Stop()
                {
                }
            }
        }

        public class MyMessage : IMessage
        {
        }

        public class FailedMessageTestContext : ScenarioContext
        {
            public Guid MessageId { get; set; }

            public bool IsMessageWrittenToFile
            {
                get
                {
                    var pathOfLogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Particular\\ServiceControl\\logs\\FailedImports\\Error");
                    return File.Exists(Path.Combine(pathOfLogFile, string.Format("{0}.txt", MessageId)));
                }
            }
        }
    }
}
