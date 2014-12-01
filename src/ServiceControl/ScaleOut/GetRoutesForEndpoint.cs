﻿namespace ServiceControl.MessageFailures.Api
{
    using System.Linq;
    using Nancy;
    using Raven.Client;
    using Raven.Client.Linq;
    using ServiceBus.Management.Infrastructure.Extensions;
    using ServiceBus.Management.Infrastructure.Nancy.Modules;

    public class GetRoutesForScaleOutGroup : BaseModule
    {
        public GetRoutesForScaleOutGroup()
        {
            Get["/routes/{id}"] = parameters =>
            {
                string groupId = parameters.id;

                using (var session = Store.OpenSession())
                {
                    RavenQueryStatistics stats;
                    var availableRoutes = session.Query<ScaleOutGroupRegistration>()
                        .Where(r => r.GroupId == groupId && r.Status == ScaleOutGroupRegistrationStatus.Connected)
                        .Statistics(out stats)                        
                        .Select(r => r.Address)
                        .ToArray();

                    if (availableRoutes.Length == 0)
                    {
                        return HttpStatusCode.NotFound;
                    }

                    return Negotiate.WithModel(availableRoutes)
                        .WithEtagAndLastModified(stats);
                }
            };
        }
    }
}
