namespace ServiceControl.MessageAuditing.Handlers
{
    using Contracts.Operations;
    using Nest;
    using NServiceBus;

    class AuditMessageHandler : IHandleMessages<ImportSuccessfullyProcessedMessage>
    {
        public ElasticClient ESClient { get; set; }

        public void Handle(ImportSuccessfullyProcessedMessage message)
        {
            var auditMessage = new ProcessedMessage(message);

            ESClient.Index(auditMessage);
        }

    }
}
