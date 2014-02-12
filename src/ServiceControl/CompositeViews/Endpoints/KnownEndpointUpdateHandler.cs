namespace ServiceControl.CompositeViews.Endpoints
{
    using EndpointControl;
    using Nest;
    using NServiceBus;

    public class KnownEndpointUpdateHandler : IHandleMessages<KnownEndpointUpdate>
    {
        public ElasticClient ESClient { get; set; }

        public IBus Bus { get; set; }

        public void Handle(KnownEndpointUpdate message)
        {
            var knownEndpoint = ESClient.Get<KnownEndpoint>(message.KnownEndpointId.ToString());

            knownEndpoint.MonitorHeartbeat = message.MonitorHeartbeat;

            ESClient.Index(knownEndpoint);

            Bus.Publish(new KnownEndpointUpdated
            {
                KnownEndpointId = message.KnownEndpointId,
                Name = knownEndpoint.Name,
                HostDisplayName = knownEndpoint.HostDisplayName
            });
        }
    }
}