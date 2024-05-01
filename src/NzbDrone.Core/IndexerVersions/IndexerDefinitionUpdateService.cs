using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NzbDrone.Core.IndexerVersions
{
    public interface IIndexerDefinitionUpdateService
    {
        List<string> GetBlocklist();
    }

    public class IndexerDefinitionUpdateService : IIndexerDefinitionUpdateService, IExecute<IndexerDefinitionUpdateCommand>, IHandle<ApplicationStartedEvent>
    {
        /* Update Service will fall back if version # does not exist for an indexer  per Ta */

        private const string DEFINITION_BRANCH = "master";
        private const int DEFINITION_VERSION = 9;

        // Used when moving yml to C#
        private readonly List<string> _definitionBlocklist = new ()
        {
            "aither",
            "animeworld",
            "audiobookbay",
            "beyond-hd-oneurl",
            "beyond-hd",
            "blutopia",
            "brsociety",
            "danishbytes",
            "datascene",
            "desitorrents",
            "hdbits",
            "lat-team",
            "mteamtp",
            "mteamtp2fa",
            "reelflix",
            "shareisland",
            "skipthecommercials",
            "tellytorrent"
        };

        private readonly IHttpClient _httpClient;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;
        private readonly IIndexerDefinitionVersionService _versionService;
        private readonly Logger _logger;

        private readonly IDeserializer _deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        public IndexerDefinitionUpdateService(IHttpClient httpClient,
                                          IAppFolderInfo appFolderInfo,
                                          IDiskProvider diskProvider,
                                          IIndexerDefinitionVersionService versionService,
                                          ICacheManager cacheManager,
                                          Logger logger)
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
            _versionService = versionService;
            _httpClient = httpClient;
            _logger = logger;
        }

        public List<string> GetBlocklist()
        {
            return _definitionBlocklist;
        }

        public void Handle(ApplicationStartedEvent message)
        {
            // Sync indexers on app start
            UpdateLocalDefinitions();
        }

        public void Execute(IndexerDefinitionUpdateCommand message)
        {
            UpdateLocalDefinitions();
        }

        private void EnsureDefinitionsFolder()
        {
            var definitionFolder = Path.Combine(_appFolderInfo.AppDataFolder, "Definitions");

            _diskProvider.CreateFolder(definitionFolder);
        }

        private void UpdateLocalDefinitions()
        {
            var startupFolder = _appFolderInfo.AppDataFolder;

            try
            {
                EnsureDefinitionsFolder();

                var definitionsFolder = Path.Combine(startupFolder, "Definitions");
                var saveFile = Path.Combine(definitionsFolder, "indexers.zip");

                _httpClient.DownloadFile($"https://indexers.prowlarr.com/{DEFINITION_BRANCH}/{DEFINITION_VERSION}/package.zip", saveFile);

                using (var archive = ZipFile.OpenRead(saveFile))
                {
                    archive.ExtractToDirectory(definitionsFolder, true);
                }

                _diskProvider.DeleteFile(saveFile);

                _logger.Debug("Updated indexer definitions");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Definition update failed");
            }
        }
    }
}
