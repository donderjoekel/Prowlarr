using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Definitions.Mangarr;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Definitions.NepNep;

public class NepNepResponseParser : MangarrResponseParser
{
    private readonly IIndexerHttpClient _httpClient;

    public NepNepResponseParser(ProviderDefinition providerDefinition, IIndexerHttpClient httpClient)
        : base(providerDefinition)
    {
        _httpClient = httpClient;
    }

    protected override IList<TorrentInfo> ParseRssResponse(HttpResponse response)
    {
        var releaseInfo = new List<TorrentInfo>();
        var match = Regex.Match(response.Content, @"(?=LatestJSON =).+?(\[.+?\])\;");
        var json = match.Groups[1].Value;
        var releases = JsonConvert.DeserializeObject<List<LatestRelease>>(json);
        foreach (var latestRelease in releases)
        {
            var url = CreateUrl(Settings.BaseUrl,
                latestRelease.IndexName,
                latestRelease.Chapter,
                out var chapterNumber);

            var release = CreateTorrentInfo(url,
                latestRelease.SeriesName,
                chapterNumber,
                DateTime.Parse(latestRelease.Date));

            releaseInfo.Add(release);
        }

        return releaseInfo;
    }

    protected override IList<TorrentInfo> ParseSearchResponse(HttpResponse response, string query, string season, string episode)
    {
        var releaseInfo = new List<TorrentInfo>();
        var match = Regex.Match(response.Content, @"(?=Directory =).+?(\[.+?\])\;");
        var json = match.Groups[1].Value;
        var directory = JsonConvert.DeserializeObject<List<DirectoryItem>>(json);
        var items = directory.Where(x => StringExtensions.ContainsIgnoreCase((string)x.Slug, query) || StringExtensions.ContainsIgnoreCase((IEnumerable<string>)x.al, query)).ToList();
        foreach (var directoryItem in items)
        {
            var request = new HttpRequest(Settings.BaseUrl + "manga/" + directoryItem.Index);
            response = _httpClient.Execute(request);
            match = Regex.Match(response.Content, @"(?=Chapters =).+?(\[.+?\])\;");
            json = match.Groups[1].Value;
            var chapters = JsonConvert.DeserializeObject<List<ChapterInfo>>(json);

            foreach (var chapter in chapters)
            {
                var url = CreateUrl(Settings.BaseUrl, directoryItem.Index, chapter.Chapter, out var chapterNumber);

                if (episode.IsNotNullOrWhiteSpace() && episode != chapterNumber.ToString(CultureInfo.InvariantCulture))
                {
                    continue;
                }

                var release = CreateTorrentInfo(url,
                    directoryItem.Slug,
                    chapterNumber,
                    DateTime.Parse(chapter.Date));

                releaseInfo.Add(release);
            }
        }

        return releaseInfo;
    }

    public override IList<string> ParseChapterResponse(string content)
    {
        var match = Regex.Match(content, @"(?=CurChapter =).+?(\{.+?\})\;");
        var json = match.Groups[1].Value;
        var chapterInfo = JsonConvert.DeserializeObject<ChapterInfo>(json);

        if (!int.TryParse(chapterInfo.Page, out var pageCount))
        {
            throw new InvalidOperationException("Unable to parse page count");
        }

        match = Regex.Match(content, @"(?=ng-src=).+\"".+\/manga\/(.+?)\/.+\""");
        var slug = match.Groups[1].Value;

        var directory = string.IsNullOrEmpty(chapterInfo.Directory) ? string.Empty : chapterInfo.Directory + "/";
        var chapterString = chapterInfo.Chapter[1..^1];
        if (chapterInfo.Chapter[^1] != '0')
        {
            chapterString += $".{chapterInfo.Chapter[^1]}";
        }

        match = Regex.Match(content, @"(?=CurPathName =).+?(\"".+?\"")\;");
        var urlBase = match.Groups[1].Value.Trim('"');

        var urls = new List<string>();
        for (var i = 0; i < pageCount; i++)
        {
            var s = "000" + (i + 1);
            var page = s[^3..];
            var url = $"https://{urlBase}/manga/{slug}/{directory}{chapterString}-{page}.png";
            urls.Add(url);
        }

        return urls;
    }

    private string CreateUrl(string baseUrl, string indexName, string chapterCode, out double chapterNumber)
    {
        var volume = int.Parse(chapterCode[..1]);
        var index = volume != 1 ? "-index-" + volume : string.Empty;
        var n = int.Parse(chapterCode[1..^1]);
        var a = int.Parse(chapterCode[^1].ToString());
        var m = a != 0 ? "." + a : string.Empty;
        var id = indexName + "-chapter-" + n + m + index + ".html";
        chapterNumber = n + (a * 0.1);
        var chapterUrl = baseUrl + "read-online/" + id;
        return chapterUrl;
    }

    private class DirectoryItem
    {
        [JsonProperty("i")]
        public string Index { get; set; }
        [JsonProperty("s")]
        public string Slug { get; set; }
        [JsonProperty("o")]
        public string Official { get; set; }
        [JsonProperty("ss")]
        public string ScanStatus { get; set; }
        [JsonProperty("ps")]
        public string PublishStatus { get; set; }
        [JsonProperty("t")]
        public string Type { get; set; }
        public string v { get; set; }
        public string vm { get; set; }
        [JsonProperty("y")]
        public string Year { get; set; }
        [JsonProperty("a")]
        public string[] Authors { get; set; }
        public string[] al { get; set; }
        [JsonProperty("l")]
        public string LatestChapter { get; set; }
        [JsonProperty("lt")]
        public long LastUpdated { get; set; }
        [JsonProperty("ls")]
        public string LastUpdatedString { get; set; }
        [JsonProperty("g")]
        public string[] Genres { get; set; }
        [JsonProperty("h")]
        public bool IsHot { get; set; }
    }

    private class ChapterInfo
    {
        public string Chapter { get; set; }
        public string Type { get; set; }
        public string Date { get; set; }
        public string ChapterName { get; set; }
        public string Page { get; set; }
        public string Directory { get; set; }
    }

    private class LatestRelease
    {
        [JsonProperty("SeriesID")]
        public string SeriesId { get; set; }
        public string IndexName { get; set; }
        public string SeriesName { get; set; }
        public string ScanStatus { get; set; }
        public string Chapter { get; set; }
        public string Genres { get; set; }
        public string Date { get; set; }
        public bool IsEdd { get; set; }
    }
}
