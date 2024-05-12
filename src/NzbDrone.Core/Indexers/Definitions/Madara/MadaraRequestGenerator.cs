using System.Collections.Generic;
using System.Net.Http;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Definitions.Mangarr;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Definitions.Madara;

public class MadaraRequestGenerator : MangarrRequestGenerator
{
    private readonly NoAuthTorrentBaseSettings _settings;

    public MadaraRequestGenerator(NoAuthTorrentBaseSettings settings)
    {
        _settings = settings;
    }

    protected override IndexerRequest GetRssRequest()
    {
        var httpRequest = new HttpRequest(_settings.BaseUrl + "wp-admin/admin-ajax.php");
        httpRequest.Method = HttpMethod.Post;
        httpRequest.Headers.ContentType = "application/x-www-form-urlencoded";
        httpRequest.AllowAutoRedirect = true;

        var data = new Dictionary<string, string>()
        {
            { "action", "madara_load_more" },
            { "page", "0" },
            { "template", "madara-core/content/content-archive" },
            { "vars[meta_key]", "_latest_update" },
            { "vars[meta_query][0][relation]", "AND" },
            { "vars[meta_query][relation]", "AND" },
            { "vars[order]", "desc" },
            { "vars[orderby]", "meta_value_num" },
            { "vars[paged]", "1" },
            { "vars[post_status]", "publish" },
            { "vars[post_type]", "wp-manga" },
            { "vars[posts_per_page]", "20" },
            { "vars[timerange]", "" },
        };

        httpRequest.SetContent(data.GetQueryString());
        return new IndexerRequest(httpRequest);
    }

    protected override IndexerRequest GetSearchRequest(string query)
    {
        var httpRequest = new HttpRequest(_settings.BaseUrl + "wp-admin/admin-ajax.php");
        httpRequest.Method = HttpMethod.Post;
        httpRequest.Headers.ContentType = "application/x-www-form-urlencoded";
        httpRequest.AllowAutoRedirect = true;

        var data = new Dictionary<string, string>()
        {
            { "action", "madara_load_more" },
            { "page", "0" },
            { "template", "madara-core/content/content-search" },
            { "vars[manga_archives_item_layout]", "big_thumbnail" },
            { "vars[meta_query][0][relation]", "AND" },
            { "vars[meta_query][relation]", "AND" },
            { "vars[paged]", "1" },
            { "vars[post_status]", "publish" },
            { "vars[post_type]", "wp-manga" },
            { "vars[s]", query },
            { "vars[template]", "search" },
        };

        httpRequest.SetContent(data.GetQueryString());
        return new IndexerRequest(httpRequest);
    }
}
