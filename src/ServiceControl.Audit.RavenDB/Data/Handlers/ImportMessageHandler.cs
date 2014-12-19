﻿namespace ServiceControl.MessageAuditing.Handlers
{
    using System;
    using Contracts.Operations;
    using NServiceBus;
    using Raven.Client;
    using ServiceControl.Contracts.MessageFailures;

    class ImportMessageHandler :
        IHandleMessages<ImportSuccessfullyProcessedMessage>,
        IHandleMessages<ImportFailedMessage>,
        IHandleMessages<FailedMessageArchived>
    {
        public IDocumentSession Session { get; set; }

        public void Handle(ImportSuccessfullyProcessedMessage successfulMessage)
        {
            var documentId = ProdDebugMessage.MakeDocumentId(successfulMessage.UniqueMessageId);

            var message = Session.Load<ProdDebugMessage>(documentId) ?? new ProdDebugMessage();
            message.Update(successfulMessage);

            Session.Store(message);
        }

        public void Handle(ImportFailedMessage failedMessage)
        {
            var documentId = ProdDebugMessage.MakeDocumentId(failedMessage.UniqueMessageId);

            var message = Session.Load<ProdDebugMessage>(documentId) ?? new ProdDebugMessage();
            message.Update(failedMessage);
            
            Session.Store(message);
        }

        public void Handle(FailedMessageArchived message)
        {
            var failedMessage = Session.Load<ProdDebugMessage>(new Guid(message.FailedMessageId));

            if (failedMessage == null)
            {
                return; //No point throwing
            }

            failedMessage.Status = MessageStatus.ArchivedFailure;
        }
    }
}
