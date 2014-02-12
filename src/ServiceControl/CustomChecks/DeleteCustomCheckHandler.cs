namespace ServiceControl.CustomChecks
{
    using Nest;
    using NServiceBus;

    class DeleteCustomCheckHandler : IHandleMessages<DeleteCustomCheck>
    {
        public ElasticClient ESClient { get; set; }

        public IBus Bus { get; set; }

        public void Handle(DeleteCustomCheck message)
        {
            var customCheck = ESClient.Get<CustomCheck>(message.Id.ToString());

            if (customCheck != null)
            {
                ESClient.Delete(customCheck);
            }

            Bus.Publish(new CustomCheckDeleted {Id = message.Id});
        }
    }
}