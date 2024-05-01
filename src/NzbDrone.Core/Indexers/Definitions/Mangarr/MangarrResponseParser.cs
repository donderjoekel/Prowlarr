using System;
using System.Collections.Generic;
using System.Globalization;
using NzbDrone.Common.Http;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions.Mangarr;

public interface IMangarrParseIndexerResponse : IParseIndexerResponse
{
    public IList<string> ParseChapterResponse(string content);
}

public abstract class MangarrResponseParser : IMangarrParseIndexerResponse
{
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

    protected abstract string IndexerName { get; }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        if (indexerResponse == null)
        {
            throw new ArgumentNullException(nameof(indexerResponse));
        }

        if (indexerResponse.HttpResponse == null)
        {
            throw new ArgumentNullException(nameof(indexerResponse.HttpResponse));
        }

        if (indexerResponse.HttpResponse.Content == null)
        {
            throw new ArgumentNullException(nameof(indexerResponse.HttpResponse.Content));
        }

        if (indexerResponse.Request is not MangarrRequest mangarrRequest)
        {
            throw new ArgumentException("Request must be of type MangarrRequest", nameof(indexerResponse.Request));
        }

        var parsed = new List<ReleaseInfo>();

        parsed.AddRange(mangarrRequest.IsRss
            ? ParseRssResponse(indexerResponse.HttpResponse)
            : ParseSearchResponse(indexerResponse.HttpResponse,
                mangarrRequest.Query,
                mangarrRequest.Season,
                mangarrRequest.Episode));

        return parsed;
    }

    protected abstract IList<TorrentInfo> ParseRssResponse(HttpResponse response);

    protected abstract IList<TorrentInfo> ParseSearchResponse(HttpResponse response,
        string query,
        string season,
        string episode);

    public abstract IList<string> ParseChapterResponse(string content);

    protected TorrentInfo CreateTorrentInfo(string url, string title, int chapterNumber, DateTime parsedDate)
    {
        return CreateTorrentInfo(url, title, chapterNumber.ToString(NumberFormatInfo.InvariantInfo), parsedDate);
    }

    protected TorrentInfo CreateTorrentInfo(string url, string title, double chapterNumber, DateTime parsedDate)
    {
        return CreateTorrentInfo(url, title, chapterNumber.ToString(NumberFormatInfo.InvariantInfo), parsedDate);
    }

    protected TorrentInfo CreateTorrentInfo(string url, string title, string chapterNumber, DateTime parsedDate)
    {
        return new TorrentInfo
        {
            Title = $"[{IndexerName}] {title} - S01E{chapterNumber}",
            PublishDate = parsedDate,
            DownloadUrl = url,
            Categories = new List<IndexerCategory> { NewznabStandardCategory.TV },
            Guid = url,
            Size = 1,
            Files = 1,
            Seeders = 1,
            Peers = 1,
            MinimumRatio = 1,
            MinimumSeedTime = 172800,
            DownloadVolumeFactor = 1,
            UploadVolumeFactor = 1
        };
    }
}
