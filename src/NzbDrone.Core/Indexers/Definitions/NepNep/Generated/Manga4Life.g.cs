// Auto generated

using System;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions.NepNep;

public class Manga4Life : NepNepBase
{
    public override string Name => "Manga Life";
    public override string[] IndexerUrls => new[] { "https://manga4life.com/" };

    public Manga4Life(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger, IServiceProvider provider)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger, provider)
    {
    }
}

