using System;
using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Definitions.Mangarr;

public abstract class MangarrRequestGenerator : IIndexerRequestGenerator
{
    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        throw new NotImplementedException();
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        throw new NotImplementedException();
    }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        var searchTerm = searchCriteria.SanitizedSearchTerm;
        var season = searchCriteria.Season?.ToString();

        var pageableRequests = new IndexerPageableRequestChain();
        pageableRequests.Add(searchTerm.IsNullOrWhiteSpace()
            ? new[] { MangarrRequest.FromIndexerRequest(GetRssRequest()) }
            : new[] { MangarrRequest.FromIndexerRequest(GetSearchRequest(searchTerm), searchTerm, season, searchCriteria.Episode) });
        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        throw new NotImplementedException();
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var searchTerm = searchCriteria.SanitizedSearchTerm;

        var pageableRequests = new IndexerPageableRequestChain();
        pageableRequests.Add(searchTerm.IsNullOrWhiteSpace()
            ? new[] { MangarrRequest.FromIndexerRequest(GetRssRequest()) }
            : new[] { MangarrRequest.FromIndexerRequest(GetSearchRequest(searchTerm), searchTerm) });
        return pageableRequests;
    }

    protected abstract IndexerRequest GetRssRequest();
    protected abstract IndexerRequest GetSearchRequest(string query);

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}
