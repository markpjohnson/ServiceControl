namespace ServiceControl.MessageFailures.Handlers
{
    using System;
    using Contracts.MessageFailures;
    using Nest;
    using NServiceBus;

    public class MessageFailureResolvedHandler : IHandleMessages<MessageFailureResolvedByRetry>
    {
        public ElasticClient ESClient { get; set; }

        public void Handle(MessageFailureResolvedByRetry message)
        {
            var failedMessage = ESClient.Get<FailedMessage>(new Guid(message.FailedMessageId).ToString());

            if (failedMessage == null)
            {
                return; //No point throwing
            }

            failedMessage.Status = FailedMessageStatus.Resolved;    
        }
    }
}