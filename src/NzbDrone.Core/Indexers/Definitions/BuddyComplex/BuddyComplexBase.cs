using System;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Mangarr;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions.BuddyComplex;

public abstract class BuddyComplexBase : MangarrBase<BuddyComplexRequestGenerator, BuddyComplexResponseParser>
{
    protected BuddyComplexBase(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger, IServiceProvider provider)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger, provider)
    {
    }
}
