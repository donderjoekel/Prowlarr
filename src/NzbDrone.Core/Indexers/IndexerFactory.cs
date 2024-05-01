using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers
{
    public interface IIndexerFactory : IProviderFactory<IIndexer, IndexerDefinition>
    {
        List<IIndexer> Enabled(bool filterBlockedIndexers = true);
        List<IIndexer> AllProviders(bool filterBlockedIndexers = true);
    }

    public class IndexerFactory : ProviderFactory<IIndexer, IndexerDefinition>, IIndexerFactory
    {
        private readonly IIndexerDefinitionUpdateService _definitionService;
        private readonly IIndexerStatusService _indexerStatusService;
        private readonly Logger _logger;

        public IndexerFactory(IIndexerDefinitionUpdateService definitionService,
                              IIndexerStatusService indexerStatusService,
                              IIndexerRepository providerRepository,
                              IEnumerable<IIndexer> providers,
                              IServiceProvider container,
                              IEventAggregator eventAggregator,
                              Logger logger)
            : base(providerRepository, providers, container, eventAggregator, logger)
        {
            _definitionService = definitionService;
            _indexerStatusService = indexerStatusService;
            _logger = logger;
        }

        public override List<IndexerDefinition> All()
        {
            var definitions = base.All();
            var filteredDefinitions = new List<IndexerDefinition>();

            foreach (var definition in definitions)
            {
                filteredDefinitions.Add(definition);
            }

            return filteredDefinitions;
        }

        public override IndexerDefinition Get(int id)
        {
            var definition = base.Get(id);

            return definition;
        }

        protected override List<IndexerDefinition> Active()
        {
            return base.Active().Where(c => c.Enable).ToList();
        }

        public override IEnumerable<IndexerDefinition> GetDefaultDefinitions()
        {
            foreach (var provider in _providers)
            {
                if (provider.IsObsolete())
                {
                    continue;
                }

                var definitions = provider.DefaultDefinitions
                    .Where(v => v.Name != null)
                    .Cast<IndexerDefinition>();

                foreach (var definition in definitions)
                {
                    SetProviderCharacteristics(provider, definition);
                    yield return definition;
                }
            }
        }

        public override IEnumerable<IndexerDefinition> GetPresetDefinitions(IndexerDefinition providerDefinition)
        {
            return Array.Empty<IndexerDefinition>();
        }

        public override void SetProviderCharacteristics(IIndexer provider, IndexerDefinition definition)
        {
            base.SetProviderCharacteristics(provider, definition);

            definition.Protocol = provider.Protocol;
            definition.SupportsRss = provider.SupportsRss;
            definition.SupportsSearch = provider.SupportsSearch;
            definition.SupportsRedirect = provider.SupportsRedirect;
            definition.SupportsPagination = provider.SupportsPagination;

            definition.IndexerUrls = provider.IndexerUrls;
            definition.LegacyUrls = provider.LegacyUrls;
            definition.Privacy = provider.Privacy;
            definition.Description ??= provider.Description;
            definition.Encoding = provider.Encoding;
            definition.Language ??= provider.Language;
            definition.Capabilities ??= provider.Capabilities;
        }

        public List<IIndexer> Enabled(bool filterBlockedIndexers = true)
        {
            var enabledIndexers = GetAvailableProviders().Where(n => ((IndexerDefinition)n.Definition).Enable);

            if (filterBlockedIndexers)
            {
                return FilterBlockedIndexers(enabledIndexers).ToList();
            }

            return enabledIndexers.ToList();
        }

        public List<IIndexer> AllProviders(bool filterBlockedIndexers = true)
        {
            var enabledIndexers = All().Select(GetInstance);

            if (filterBlockedIndexers)
            {
                return FilterBlockedIndexers(enabledIndexers).ToList();
            }

            return enabledIndexers.ToList();
        }

        private IEnumerable<IIndexer> FilterBlockedIndexers(IEnumerable<IIndexer> indexers)
        {
            var blockedIndexers = _indexerStatusService.GetBlockedProviders().ToDictionary(v => v.ProviderId, v => v);

            foreach (var indexer in indexers)
            {
                if (blockedIndexers.TryGetValue(indexer.Definition.Id, out var blockedIndexerStatus))
                {
                    _logger.Debug("Temporarily ignoring indexer {0} till {1} due to recent failures.", indexer.Definition.Name, blockedIndexerStatus.DisabledTill.Value.ToLocalTime());
                    continue;
                }

                yield return indexer;
            }
        }

        public override ValidationResult Test(IndexerDefinition definition)
        {
            var result = base.Test(definition);

            if (definition.Id == 0)
            {
                return result;
            }

            if (result == null || result.IsValid)
            {
                _indexerStatusService.RecordSuccess(definition.Id);
            }
            else
            {
                _indexerStatusService.RecordFailure(definition.Id);
            }

            return result;
        }

        public override IndexerDefinition Create(IndexerDefinition definition)
        {
            definition.Added = DateTime.UtcNow;

            var provider = _providers.First(v => v.GetType().Name == definition.Implementation);

            SetProviderCharacteristics(provider, definition);

            return base.Create(definition);
        }

        public override void Update(IndexerDefinition definition)
        {
            var provider = _providers.First(v => v.GetType().Name == definition.Implementation);

            SetProviderCharacteristics(provider, definition);

            base.Update(definition);
        }
    }
}
