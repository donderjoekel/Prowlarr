using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Parser;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerResource : ProviderResource<IndexerResource>
    {
        public string[] IndexerUrls { get; set; }
        public string[] LegacyUrls { get; set; }
        public string DefinitionName { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string Encoding { get; set; }
        public bool Enable { get; set; }
        public bool Redirect { get; set; }
        public bool SupportsRss { get; set; }
        public bool SupportsSearch { get; set; }
        public bool SupportsRedirect { get; set; }
        public bool SupportsPagination { get; set; }
        public int AppProfileId { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public IndexerPrivacy Privacy { get; set; }
        public IndexerCapabilityResource Capabilities { get; set; }
        public int Priority { get; set; }
        public DateTime Added { get; set; }
        public IndexerStatusResource Status { get; set; }
        public string SortName { get; set; }
    }

    public class IndexerResourceMapper : ProviderResourceMapper<IndexerResource, IndexerDefinition>
    {
        private readonly IIndexerDefinitionUpdateService _definitionService;

        public IndexerResourceMapper(IIndexerDefinitionUpdateService definitionService)
        {
            _definitionService = definitionService;
        }

        public override IndexerResource ToResource(IndexerDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            var resource = base.ToResource(definition);

            resource.DefinitionName = definition.ImplementationName;

            var infoLinkName = definition.ImplementationName;

            resource.InfoLink = $"https://wiki.servarr.com/prowlarr/supported-indexers#{infoLinkName.ToLower().Replace(' ', '-')}";
            resource.AppProfileId = definition.AppProfileId;
            resource.IndexerUrls = definition.IndexerUrls;
            resource.LegacyUrls = definition.LegacyUrls;
            resource.Description = definition.Description;
            resource.Language = definition.Language;
            resource.Encoding = definition.Encoding?.EncodingName ?? null;
            resource.Enable = definition.Enable;
            resource.Redirect = definition.Redirect;
            resource.SupportsRss = definition.SupportsRss;
            resource.SupportsSearch = definition.SupportsSearch;
            resource.SupportsRedirect = definition.SupportsRedirect;
            resource.SupportsPagination = definition.SupportsPagination;
            resource.Capabilities = definition.Capabilities.ToResource();
            resource.Protocol = definition.Protocol;
            resource.Privacy = definition.Privacy;
            resource.Priority = definition.Priority;
            resource.Added = definition.Added;
            resource.SortName = definition.Name.NormalizeTitle();

            return resource;
        }

        public override IndexerDefinition ToModel(IndexerResource resource, IndexerDefinition existingDefinition)
        {
            if (resource == null)
            {
                return null;
            }

            var definition = base.ToModel(resource, existingDefinition);

            definition.AppProfileId = resource.AppProfileId;
            definition.Enable = resource.Enable;
            definition.Redirect = resource.Redirect;
            definition.IndexerUrls = resource.IndexerUrls;
            definition.Priority = resource.Priority;
            definition.Privacy = resource.Privacy;
            definition.Added = resource.Added;

            return definition;
        }

        public List<IndexerResource> ToResource(IEnumerable<IndexerDefinition> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
