using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Definitions.Mangarr;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Definitions.BuddyComplex;

public class BuddyComplexResponseParser : MangarrResponseParser
{
    private readonly IIndexerHttpClient _httpClient;

    public BuddyComplexResponseParser(ProviderDefinition providerDefinition, IIndexerHttpClient httpClient)
        : base(providerDefinition)
    {
        _httpClient = httpClient;
    }

    protected override IList<TorrentInfo> ParseRssResponse(HttpResponse response)
    {
        var releases = new List<TorrentInfo>();
        var document = new HtmlParser().ParseDocument(response.Content);
        var elements = document.QuerySelectorAll<IHtmlDivElement>(".book-item.latest-item");
        foreach (var element in elements)
        {
            var anchor = element.QuerySelector<IHtmlAnchorElement>("div.title a");
            var title = anchor.TextContent.Trim();
            var chapterElements = element.QuerySelectorAll<IHtmlDivElement>("div.chapters div.chap-item");
            foreach (var chapterElement in chapterElements)
            {
                var anchorElement = chapterElement.QuerySelector<IHtmlAnchorElement>("a");
                var updatedElement = chapterElement.QuerySelector<IHtmlDivElement>("div.updated-date");
                var episode = ParseChapterToEpisode(anchorElement.TextContent.Trim());
                if (episode.IsNullOrWhiteSpace())
                {
                    continue;
                }

                var parsedDate = ParseHumanReleaseDate(updatedElement.TextContent.Trim());
                if (parsedDate > DateTime.Now)
                {
                    continue;
                }

                releases.Add(
                    CreateTorrentInfo(
                        Settings.BaseUrl + anchorElement.PathName.Trim('/'),
                        title,
                        episode,
                        parsedDate));
            }
        }

        return releases;
    }

    protected override IList<TorrentInfo> ParseSearchResponse(HttpResponse response, string query, string season, string episode)
    {
        var releases = new List<TorrentInfo>();
        var document = new HtmlParser().ParseDocument(response.Content);
        var elements = document.QuerySelectorAll<IHtmlAnchorElement>("div.book-detailed-item .meta .title a");
        foreach (var element in elements)
        {
            var request = new HttpRequest(Settings.BaseUrl + "api/manga/" + element.PathName.Trim('/') +
                                          "/chapters?source=detail");
            var result = _httpClient.Execute(request);
            document = new HtmlParser().ParseDocument(result.Content);
            var anchorElements = document.QuerySelectorAll<IHtmlAnchorElement>("li a");
            foreach (var anchorElement in anchorElements)
            {
                var titleElement = anchorElement.QuerySelector<IHtmlElement>(".chapter-title");
                var parsedEpisode = ParseChapterToEpisode(titleElement.TextContent.Trim());

                if (episode.IsNotNullOrWhiteSpace() && episode != parsedEpisode)
                {
                    continue;
                }

                var dateElement = anchorElement.QuerySelector<IHtmlElement>(".chapter-update");
                var date = dateElement.TextContent.Trim();
                DateTime.TryParse(date, out var parsedDate);

                releases.Add(CreateTorrentInfo(
                    Settings.BaseUrl + anchorElement.PathName.Trim('/'),
                    element.TextContent.Trim(),
                    parsedEpisode,
                    parsedDate));
            }
        }

        return releases;
    }

    public override IList<string> ParseChapterResponse(string content)
    {
        var match = Regex.Match(content, @"chapImages\s=\s'(.+)(?=')");
        return match.Groups[1].Value.Split(',');
    }
}
