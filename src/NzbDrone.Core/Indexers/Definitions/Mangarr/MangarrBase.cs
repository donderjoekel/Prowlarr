using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BencodeNET.Objects;
using BencodeNET.Torrents;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Reflection;
using Torrent = BencodeNET.Torrents.Torrent;

namespace NzbDrone.Core.Indexers.Definitions.Mangarr;

public abstract class MangarrBase<TRequestGenerator, TResponseParser>
    : MangarrBase<TRequestGenerator, TResponseParser, MangarrBaseSettings>
    where TRequestGenerator : MangarrRequestGenerator
    where TResponseParser : MangarrResponseParser
{
    protected MangarrBase(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger, IServiceProvider provider)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger, provider)
    {
    }
}

public abstract class MangarrBase<TRequestGenerator, TResponseParser, TSettings> : TorrentIndexerBase<MangarrBaseSettings>
    where TRequestGenerator : MangarrRequestGenerator
    where TResponseParser : MangarrResponseParser
    where TSettings : MangarrBaseSettings
{
    private readonly IServiceProvider _provider;

    public sealed override IndexerPrivacy Privacy => IndexerPrivacy.Public;

    public sealed override IndexerCapabilities Capabilities => GetCapabilities();

    public override string Description => string.Empty;

    protected virtual string ImageReferrer => Settings.BaseUrl;

    protected MangarrBase(IIndexerHttpClient httpClient,
        IEventAggregator eventAggregator,
        IIndexerStatusService indexerStatusService,
        IConfigService configService,
        Logger logger,
        IServiceProvider provider)
        : base(httpClient,
            eventAggregator,
            indexerStatusService,
            configService,
            logger)
    {
        _provider = provider;
    }

    public sealed override IndexerCapabilities GetCapabilities()
    {
        var caps = new IndexerCapabilities()
        {
            TvSearchParams = new List<TvSearchParam>()
            {
                TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
            },
        };

        var categories = GetCategories().ToList();

        if (!categories.Any())
        {
            throw new Exception("No categories defined for indexer");
        }

        foreach (var category in categories)
        {
            caps.Categories.AddCategoryMapping("Books", category, category.Name);
        }

        return caps;
    }

    protected virtual IEnumerable<IndexerCategory> GetCategories()
    {
        return new List<IndexerCategory>
        {
            NewznabStandardCategory.Books
        };
    }

    public sealed override async Task<byte[]> Download(Uri link)
    {
        var bytes = await base.Download(link).ConfigureAwait(false);
        var content = Encoding.GetString(bytes);
        var parser = (TResponseParser)GetParser();
        var chapterImageLinks = parser.ParseChapterResponse(content);
        var files = new MultiFileInfoList();
        files.AddRange(chapterImageLinks.Select(ConvertUrl).Select(x => new MultiFileInfo() { FullPath = x, FileSize = 1 }));
        var torrent = new Torrent
        {
            ExtraFields = new BDictionary(),
            PieceSize = 1,
            Pieces = Enumerable.Range(0, 20).Select(x => (byte)x).ToArray(),
            Files = files
        };
        torrent.ExtraFields.Add("referer", ImageReferrer);
        using var stream = new MemoryStream();
        await torrent.EncodeToAsync(stream).ConfigureAwait(false);
        return stream.ToArray();
    }

    private string ConvertUrl(string url)
    {
        // Return url as base64 encoded string
        return Convert.ToBase64String(Encoding.GetBytes(url));
    }

    protected override void ValidateDownloadData(byte[] fileData)
    {
        // No validation needed
    }

    public sealed override IParseIndexerResponse GetParser()
    {
        return CreationHelper.Create<TResponseParser>(_provider, this, Definition, Settings);
    }

    public sealed override IIndexerRequestGenerator GetRequestGenerator()
    {
        return CreationHelper.Create<TRequestGenerator>(_provider, this, Definition, Settings);
    }
}
