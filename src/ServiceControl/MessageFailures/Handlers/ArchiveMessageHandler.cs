namespace ServiceControl.MessageFailures.Handlers
{
    using System;
    using Contracts.MessageFailures;
    using InternalMessages;
    using Nest;
    using NServiceBus;

    public class ArchiveMessageHandler : IHandleMessages<ArchiveMessage>
    {
        public ElasticClient ESClient { get; set; }

        public IBus Bus { get; set; }

        public void Handle(ArchiveMessage message)
        {
            var failedMessage = ESClient.Get<FailedMessage>(new Guid(message.FailedMessageId).ToString());

            if (failedMessage == null)
            {
                return; //No point throwing
            }

            if (failedMessage.Status != FailedMessageStatus.Archived)
            {
                failedMessage.Status = FailedMessageStatus.Archived;

                Bus.Publish<FailedMessageArchived>(m=>m.FailedMessageId = message.FailedMessageId);
            }
        }
    }
}