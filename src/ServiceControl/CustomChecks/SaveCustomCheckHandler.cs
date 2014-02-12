namespace ServiceControl.CustomChecks
{
    using Contracts.Operations;
    using Infrastructure;
    using Nest;
    using NServiceBus;
    using Plugin.CustomChecks.Messages;

    class SaveCustomCheckHandler : IHandleMessages<ReportCustomCheckResult>
    {
        public ElasticClient ESClient { get; set; }
        public IBus Bus { get; set; }

        public void Handle(ReportCustomCheckResult message)
        {
            var originatingEndpoint = EndpointDetails.SendingEndpoint(Bus.CurrentMessageContext.Headers);
            var id = DeterministicGuid.MakeId(message.CustomCheckId, originatingEndpoint.Name,
                originatingEndpoint.Machine);
            var customCheck = ESClient.Get<CustomCheck>(id.ToString()) ?? new CustomCheck
            {
                Id = id,
            };

            customCheck.CustomCheckId = message.CustomCheckId;
            customCheck.Category = message.Category;
            customCheck.Status = message.Result.HasFailed ? Status.Fail : Status.Pass;
            customCheck.ReportedAt = message.ReportedAt;
            customCheck.FailureReason = message.Result.FailureReason;
            customCheck.OriginatingEndpoint = originatingEndpoint;

            ESClient.Index(customCheck);
        }
    }
}