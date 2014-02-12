namespace ServiceControl.MessageFailures.Handlers
{
    using InternalMessages;
    using Nest;
    using NServiceBus;

    public class IssueRetryAllHandler : IHandleMessages<RequestRetryAll>
    {
        public ElasticClient ESClient { get; set; }
        public IBus Bus { get; set; }

        public void Handle(RequestRetryAll message)
        {
//            var query = ESClient.Search<FailedMessage>(_ => _.Q).Query<FailedMessageViewIndex.SortAndFilterOptions, FailedMessageViewIndex>();
//            
//            if (message.Endpoint != null)
//            {
//                query = query.Where(fm => fm.ReceivingEndpointName == message.Endpoint);
//            }
//
//            using (var ie = Session.Advanced.Stream(query.OfType<FailedMessage>()))
//            {
//                while (ie.MoveNext())
//                {
//                    var retryMessage = new RetryMessage {FailedMessageId = ie.Current.Document.UniqueMessageId};
//                    message.SetHeader("RequestedAt", Bus.CurrentMessageContext.Headers["RequestedAt"]);
//
//                    Bus.SendLocal(retryMessage);
//                }
//            }
        }
    }
}