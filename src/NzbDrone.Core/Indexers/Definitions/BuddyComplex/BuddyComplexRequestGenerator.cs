using System.Collections.Specialized;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Definitions.Mangarr;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Definitions.BuddyComplex;

public class BuddyComplexRequestGenerator : MangarrRequestGenerator
{
    private readonly NoAuthTorrentBaseSettings _settings;

    public BuddyComplexRequestGenerator(NoAuthTorrentBaseSettings settings)
    {
        _settings = settings;
    }

    protected override IndexerRequest GetRssRequest()
    {
        return new IndexerRequest(_settings.BaseUrl, HttpAccept.Html);
    }

    protected override IndexerRequest GetSearchRequest(string query)
    {
        var parameters = new NameValueCollection()
        {
            { "name", query }
        };

        return new IndexerRequest(
            _settings.BaseUrl + "search?page=1&q=" + parameters.GetQueryString(),
            HttpAccept.Html);
    }
}
