using System;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Mangarr;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions.NepNep;

public abstract class NepNepBase : MangarrBase<NepNepRequestGenerator, NepNepResponseParser>
{
    protected NepNepBase(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger, IServiceProvider provider)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger, provider)
    {
    }
}
