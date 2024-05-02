using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Definitions.Mangarr;

public interface IMangarrParseIndexerResponse : IParseIndexerResponse
{
    public IList<string> ParseChapterResponse(string content);
}

public abstract class MangarrResponseParser : IMangarrParseIndexerResponse
{
    private static readonly Regex InDaysRegex = new Regex(@"in (\d+) days", RegexOptions.IgnoreCase);
    private static readonly Regex InHoursRegex = new Regex(@"in (\d+) hours", RegexOptions.IgnoreCase);
    private static readonly Regex SecondsAgoRegex = new Regex(@"(\d+) seconds? ago", RegexOptions.IgnoreCase);
    private static readonly Regex MinutesAgoRegex = new Regex(@"(\d+) minutes? ago", RegexOptions.IgnoreCase);
    private static readonly Regex HoursAgoRegex = new Regex(@"(\d+) hours? ago", RegexOptions.IgnoreCase);
    private static readonly Regex DaysAgoRegex = new Regex(@"(\d+) days? ago", RegexOptions.IgnoreCase);
    private static readonly Regex MonthsAgoRegex = new Regex(@"(\d+) months? ago", RegexOptions.IgnoreCase);
    private static readonly Regex ChapterRegex = new Regex(@"[Cc]hapter\s(\d+(\.\d+)?)");

    private readonly ProviderDefinition _providerDefinition;

    public ProviderDefinition ProviderDefinition => _providerDefinition;
    public MangarrBaseSettings Settings => (MangarrBaseSettings)_providerDefinition.Settings;

    protected MangarrResponseParser(ProviderDefinition providerDefinition)
    {
        _providerDefinition = providerDefinition;
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

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

    protected string ParseChapterToEpisode(string chapter)
    {
        return ChapterRegex.Match(chapter).Groups[1].Value.Trim();
    }

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
            Title = $"[{_providerDefinition.Name}] {title} - S01E{chapterNumber}",
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

    protected static DateTime ParseHumanReleaseDate(string input)
        {
            if (InDaysRegex.IsMatch(input))
            {
                return DateTime.Now.AddDays(int.Parse(InDaysRegex.Match(input).Groups[1].Value));
            }

            if (string.Equals(input, "in a day", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.Now.AddDays(1);
            }

            if (InHoursRegex.IsMatch(input))
            {
                return DateTime.Now.AddHours(int.Parse(InHoursRegex.Match(input).Groups[1].Value));
            }

            if (string.Equals(input, "in an hour", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.Now.AddHours(1);
            }

            if (SecondsAgoRegex.IsMatch(input))
            {
                return DateTime.Now.AddSeconds(-int.Parse(SecondsAgoRegex.Match(input).Groups[1].Value));
            }

            if (MinutesAgoRegex.IsMatch(input))
            {
                return DateTime.Now.AddMinutes(-int.Parse(MinutesAgoRegex.Match(input).Groups[1].Value));
            }

            if (string.Equals(input, "an hour ago", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.Now.AddHours(-1);
            }

            if (HoursAgoRegex.IsMatch(input))
            {
                return DateTime.Now.AddHours(-int.Parse(HoursAgoRegex.Match(input).Groups[1].Value));
            }

            if (string.Equals(input, "a day ago", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.Now.AddDays(-1);
            }

            if (DaysAgoRegex.IsMatch(input))
            {
                return DateTime.Now.AddDays(-int.Parse(DaysAgoRegex.Match(input).Groups[1].Value));
            }

            if (string.Equals(input, "a month ago", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.Now.AddMonths(-1);
            }

            if (MonthsAgoRegex.IsMatch(input))
            {
                return DateTime.Now.AddMonths(-int.Parse(MonthsAgoRegex.Match(input).Groups[1].Value));
            }

            return DateTime.MinValue;
        }
}
