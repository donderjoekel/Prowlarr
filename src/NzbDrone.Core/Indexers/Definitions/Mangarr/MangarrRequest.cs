using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Indexers.Definitions.Mangarr;

public class MangarrRequest : IndexerRequest
{
    public bool IsRss => Query.IsNullOrWhiteSpace();
    public string Query { get; }
    public string Season { get; }
    public string Episode { get; }

    public MangarrRequest(string url, HttpAccept httpAccept)
        : base(url, httpAccept)
    {
    }

    public MangarrRequest(HttpRequest httpRequest)
        : base(httpRequest)
    {
    }

    public MangarrRequest(string url, HttpAccept httpAccept, string query, string season, string episode)
        : base(url, httpAccept)
    {
        Query = query;
        Season = season;
        Episode = TrimLeadingZeroes(episode);
    }

    public MangarrRequest(HttpRequest httpRequest, string query, string season, string episode)
        : base(httpRequest)
    {
        Query = query;
        Season = season;
        Episode = TrimLeadingZeroes(episode);
    }

    private string TrimLeadingZeroes(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        if (input == "0")
        {
            return input;
        }

        return input.TrimStart('0');
    }

    public static MangarrRequest FromIndexerRequest(IndexerRequest indexerRequest,
        string query = null,
        string season = null,
        string episode = null)
    {
        return new MangarrRequest(indexerRequest.HttpRequest, query, season, episode);
    }
}
