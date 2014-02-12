using System;

namespace ServiceControl.Infrastructure.Elasticsearch
{
    using Nest;
    using NServiceBus;
    using NServiceBus.Logging;

    class ElasticsearchBootstrapper : INeedInitialization
    {
        public void Init()
        {
            var elasticClient = new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200"))
                .SetDefaultIndex("service-control"));

            Configure.Instance.Configurer.RegisterSingleton<ElasticClient>(elasticClient);
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(ElasticsearchBootstrapper));
    }
}
