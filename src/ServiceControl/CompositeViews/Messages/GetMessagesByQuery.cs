namespace ServiceControl.CompositeViews.Messages
{
    using System.Linq;
    using Infrastructure.Extensions;
    using MessageAuditing;
    using Nancy;
    using Raven.Client;
    using Raven.Client.Linq;
    using ServiceBus.Management.Infrastructure.Extensions;
    using ServiceBus.Management.Infrastructure.Nancy.Modules;

    public class GetMessagesByQuery : BaseModule
    {
        public GetMessagesByQuery()
        {
            Get["/messages/search"] = _ =>
            {
                string keyword = Request.Query.q;

                return SearchByKeyword(keyword);
            };

            Get["/messages/search/{keyword}"] = parameters =>
            {
                string keyword = parameters.keyword;

                return SearchByKeyword(keyword);
            };

            Get["/endpoints/{name}/messages/search"] = parameters =>
            {
                string keyword = Request.Query.q;
                string name = parameters.name;

                return SearchByKeyword(keyword, name);
            };

            Get["/endpoints/{name}/messages/search/{keyword}"] = parameters =>
            {
                string keyword = parameters.keyword;
                string name = parameters.name;

                return SearchByKeyword(keyword, name);
            };
        }

        dynamic SearchByKeyword(string keyword, string name)
        {
            var results = ESClient.Search<ProcessedMessage>(
                _ =>
                    _.Query(
                        q =>
                            q.Bool(
                                b =>
                                    b.Must(bc => bc.Term("ReceivingEndpointName", name),
                                        x => x.Match(y => y.QueryString(keyword).OnField("_all"))))))
                .Documents.ToArray();


            return Negotiate.WithModel(results)
                //.WithPagingLinksAndTotalCount(stats, Request)
                //.WithEtagAndLastModified(stats)
                ;
        }

        dynamic SearchByKeyword(string keyword)
        {
            using (var session = Store.OpenSession())
            {
                RavenQueryStatistics stats;
                var results = session.Query<MessagesViewIndex.SortAndFilterOptions, MessagesViewIndex>()
                    .Statistics(out stats)
                    .Search(x => x.Query, keyword)
                    .Sort(Request)
                    .Paging(Request)
                    .TransformWith<MessagesViewTransformer, MessagesView>()
                    .ToArray();

                return Negotiate.WithModel(results)
                    .WithPagingLinksAndTotalCount(stats, Request)
                    .WithEtagAndLastModified(stats);
            }
        }
    }
}