using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Definitions.Mangarr;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Definitions.Madara;

public class MadaraResponseParser : MangarrResponseParser
{
    private readonly IIndexerHttpClient _httpClient;

    public MadaraResponseParser(ProviderDefinition providerDefinition, IIndexerHttpClient httpClient)
        : base(providerDefinition)
    {
        _httpClient = httpClient;
    }

    protected override IList<TorrentInfo> ParseRssResponse(HttpResponse response)
    {
        var releases = new List<TorrentInfo>();

        var document = new HtmlParser().ParseDocument(response.Content);
        var elements = document.QuerySelectorAll<IHtmlDivElement>(".page-item-detail");
        foreach (var element in elements)
        {
            var titleElement = element.QuerySelector<IHtmlAnchorElement>(".post-title a");
            var title = titleElement.TextContent.Trim();

            var chapterElements = element.QuerySelectorAll<IHtmlDivElement>(".chapter-item");
            foreach (var chapterElement in chapterElements)
            {
                var chapterUrlElement = chapterElement.QuerySelector<IHtmlAnchorElement>(".chapter a");
                var chapterUrl = chapterUrlElement.Href;
                var chapterTitle = chapterUrlElement.TextContent.Trim();
                var episode = ParseChapterToEpisode(chapterTitle);

                var parsedDate = new DateTime(1910, 1, 1, 0, 0, 0);
                var dateElement = chapterElement.QuerySelector<IHtmlAnchorElement>(".post-on a");
                if (dateElement != null)
                {
                    var date = dateElement.GetAttribute("title", string.Empty);
                    if (!string.IsNullOrWhiteSpace(date))
                    {
                        // TODO: Parse date
                        // parsedDate = DateTime.Parse(date);
                    }
                }

                releases.Add(CreateTorrentInfo(chapterUrl, title, episode, parsedDate));
            }
        }

        return releases;
    }

    protected override IList<TorrentInfo> ParseSearchResponse(HttpResponse response, string query, string season, string episode)
    {
        var releases = new List<TorrentInfo>();

        var document = new HtmlParser().ParseDocument(response.Content);
        var elements = document.QuerySelectorAll<IHtmlDivElement>(".c-tabs-item__content");
        foreach (var element in elements)
        {
            var titleElement = element.QuerySelector<IHtmlAnchorElement>(".post-title a");
            var title = titleElement.TextContent.Trim();

            var request = new HttpRequest(titleElement.Href + "ajax/chapters/")
            {
                Method = HttpMethod.Post
            };

            var result = _httpClient.Execute(request);

            document = new HtmlParser().ParseDocument(result.Content);
            var chapterElements = document.QuerySelectorAll<IHtmlListItemElement>(".wp-manga-chapter");
            foreach (var chapterElement in chapterElements)
            {
                var urlElement = chapterElement.QuerySelector<IHtmlAnchorElement>("a");
                var url = urlElement.Href;
                var chapterTitle = urlElement.TextContent.Trim();
                var parsedEpisode = ParseChapterToEpisode(chapterTitle);

                if (episode.IsNotNullOrWhiteSpace() && episode != parsedEpisode)
                {
                    continue;
                }

                var releaseDateElement = chapterElement.QuerySelector<IHtmlSpanElement>(".chapter-release-date");
                var parsedDate = DateTime.Today;
                if (releaseDateElement != null)
                {
                    var releaseDate = releaseDateElement.TextContent.Trim();
                    if (!string.IsNullOrWhiteSpace(releaseDate))
                    {
                        parsedDate = DateTime.Parse(releaseDate);
                    }
                }

                releases.Add(CreateTorrentInfo(url, title, parsedEpisode, parsedDate));
            }
        }

        return releases;
    }

    public override IList<string> ParseChapterResponse(string content)
    {
        var document = new HtmlParser().ParseDocument(content);
        var elements = document.QuerySelectorAll<IHtmlImageElement>(".wp-manga-chapter-img");

        if (!elements.Any())
        {
            elements = document.QuerySelectorAll<IHtmlImageElement>(".reading-content img");
        }

        return elements.Select(GetUrl).ToList();

        string GetUrl(IHtmlImageElement element)
        {
            var url = element.GetAttribute("data-src");
            if (string.IsNullOrEmpty(url))
            {
                url = element.GetAttribute("src");
            }

            return url?.Trim() ?? string.Empty;
        }
    }
}
