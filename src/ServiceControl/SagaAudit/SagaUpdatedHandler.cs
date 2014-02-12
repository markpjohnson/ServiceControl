namespace ServiceControl.SagaAudit
{
    using System.Collections.Generic;
    using System.Linq;
    using EndpointPlugin.Messages.SagaState;
    using Nest;
    using NServiceBus;

    public class SagaUpdatedHandler : IHandleMessages<SagaUpdatedMessage>
    {
        public ElasticClient ESClient { get; set; }

        public void Handle(SagaUpdatedMessage message)
        {
            var sagaHistory = ESClient.Get<SagaHistory>(message.SagaId.ToString()) ?? new SagaHistory
            {
                Id = message.SagaId,
                SagaId = message.SagaId,
                SagaType = message.SagaType
            };

            var sagaStateChange = sagaHistory.Changes.FirstOrDefault(x => x.InitiatingMessage.MessageId == message.Initiator.InitiatingMessageId);
            if (sagaStateChange == null)
            {
                sagaStateChange = new SagaStateChange();
                sagaHistory.Changes.Add(sagaStateChange);
            }

            sagaStateChange.FinishTime = message.FinishTime;
            sagaStateChange.StartTime = message.StartTime;
            sagaStateChange.StateAfterChange = message.SagaState;
            sagaStateChange.Endpoint = message.Endpoint;

            if (message.IsNew)
            {
                sagaStateChange.Status = SagaStateChangeStatus.New;    
            }
            else
            {
                sagaStateChange.Status = SagaStateChangeStatus.Updated;    
            }

            if (message.IsCompleted)
            {
                sagaStateChange.Status = SagaStateChangeStatus.Completed;
            }

            sagaStateChange.InitiatingMessage = CreateInitiatingMessage(message.Initiator);

            AddResultingMessages(message.ResultingMessages, sagaStateChange);

            ESClient.Index(sagaHistory);
        }

        static InitiatingMessage CreateInitiatingMessage(SagaChangeInitiator initiator)
        {
            return new InitiatingMessage
                {
                    MessageId = initiator.InitiatingMessageId,
                    IsSagaTimeoutMessage = initiator.IsSagaTimeoutMessage,
                    OriginatingEndpoint = initiator.OriginatingEndpoint,
                    OriginatingMachine = initiator.OriginatingMachine,
                    TimeSent = initiator.TimeSent,
                    MessageType = initiator.MessageType,
                };
        }

        static void AddResultingMessages(List<SagaChangeOutput> sagaChangeResultingMessages, SagaStateChange sagaStateChange)
        {
            foreach (var toAdd in sagaChangeResultingMessages)
            {
                var resultingMessage = sagaStateChange.OutgoingMessages.FirstOrDefault(x => x.MessageId == toAdd.ResultingMessageId);
                if (resultingMessage == null)
                {
                    resultingMessage = new ResultingMessage();
                    sagaStateChange.OutgoingMessages.Add(resultingMessage);
                }
                resultingMessage.MessageType = toAdd.MessageType;
                resultingMessage.MessageId = toAdd.ResultingMessageId;
                resultingMessage.TimeSent = toAdd.TimeSent;
                resultingMessage.DeliveryDelay = toAdd.DeliveryDelay;
                resultingMessage.DeliverAt = toAdd.DeliveryAt;
                resultingMessage.Destination = toAdd.Destination;
            }
        }
    }
}