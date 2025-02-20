using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentValidation.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Applications.Sonarr
{
    public class Sonarr : ApplicationBase<SonarrSettings>
    {
        public override string Name => "Mangarr";

        private readonly ICached<List<SonarrIndexer>> _schemaCache;
        private readonly ISonarrV3Proxy _sonarrV3Proxy;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IIndexerFactory _indexerFactory;

        public Sonarr(ICacheManager cacheManager, ISonarrV3Proxy sonarrV3Proxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, IIndexerFactory indexerFactory, Logger logger)
            : base(appIndexerMapService, logger)
        {
            _schemaCache = cacheManager.GetCache<List<SonarrIndexer>>(GetType());
            _sonarrV3Proxy = sonarrV3Proxy;
            _configFileProvider = configFileProvider;
            _indexerFactory = indexerFactory;
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            var testIndexer = new IndexerDefinition
            {
                Id = 0,
                Name = "Test",
                Protocol = DownloadProtocol.Torrent,
                Capabilities = new IndexerCapabilities()
            };

            foreach (var cat in NewznabStandardCategory.AllCats)
            {
                testIndexer.Capabilities.Categories.AddCategoryMapping(1, cat);
            }

            try
            {
                failures.AddIfNotNull(_sonarrV3Proxy.TestConnection(BuildSonarrIndexer(testIndexer, testIndexer.Capabilities, DownloadProtocol.Torrent), Settings));
            }
            catch (HttpException ex)
            {
                switch (ex.Response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        _logger.Warn(ex, "API Key is invalid");
                        failures.AddIfNotNull(new ValidationFailure("ApiKey", "API Key is invalid"));
                        break;
                    case HttpStatusCode.BadRequest:
                        _logger.Warn(ex, "Prowlarr URL is invalid");
                        failures.AddIfNotNull(new ValidationFailure("ProwlarrUrl", "Prowlarr URL is invalid, Mangarr cannot connect to Prowlarr"));
                        break;
                    case HttpStatusCode.SeeOther:
                        _logger.Warn(ex, "Mangarr returned redirect and is invalid");
                        failures.AddIfNotNull(new ValidationFailure("BaseUrl", "Mangarr URL is invalid, Prowlarr cannot connect to Mangarr - are you missing a URL base?"));
                        break;
                    case HttpStatusCode.NotFound:
                        _logger.Warn(ex, "Mangarr not found");
                        failures.AddIfNotNull(new ValidationFailure("BaseUrl", "Mangarr URL is invalid, Prowlarr cannot connect to Mangarr. Is Mangarr running and accessible? Mangarr v2 is not supported."));
                        break;
                    default:
                        _logger.Warn(ex, "Unable to complete application test");
                        failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to Mangarr. {ex.Message}"));
                        break;
                }
            }
            catch (JsonReaderException ex)
            {
                _logger.Error(ex, "Unable to parse JSON response from application");
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to parse JSON response from application. {ex.Message}"));
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to complete application test");
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to Mangarr. {ex.Message}"));
            }

            return new ValidationResult(failures);
        }

        public override List<AppIndexerMap> GetIndexerMappings()
        {
            var indexers = _sonarrV3Proxy.GetIndexers(Settings)
                .Where(i => i.Implementation is "Torznab");

            var mappings = new List<AppIndexerMap>();

            foreach (var indexer in indexers)
            {
                var baseUrl = (string)indexer.Fields.FirstOrDefault(x => x.Name == "baseUrl")?.Value ?? string.Empty;

                if (!baseUrl.StartsWith(Settings.ProwlarrUrl.TrimEnd('/')) &&
                    (string)indexer.Fields.FirstOrDefault(x => x.Name == "apiKey")?.Value != _configFileProvider.ApiKey)
                {
                    continue;
                }

                var match = AppIndexerRegex.Match(baseUrl);

                if (match.Groups["indexer"].Success && int.TryParse(match.Groups["indexer"].Value, out var indexerId))
                {
                    // Add parsed mapping if it's mapped to a Indexer in this Prowlarr instance
                    mappings.Add(new AppIndexerMap { IndexerId = indexerId, RemoteIndexerId = indexer.Id });
                }
            }

            return mappings;
        }

        public override void AddIndexer(IndexerDefinition indexer)
        {
            var indexerCapabilities = _indexerFactory.GetInstance(indexer).GetCapabilities();

            if (indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Empty() &&
                indexerCapabilities.Categories.SupportedCategories(Settings.AnimeSyncCategories.ToArray()).Empty())
            {
                _logger.Trace("Skipping add for indexer {0} [{1}] due to no app Sync Categories supported by the indexer", indexer.Name, indexer.Id);

                return;
            }

            _logger.Trace("Adding indexer {0} [{1}]", indexer.Name, indexer.Id);

            var sonarrIndexer = BuildSonarrIndexer(indexer, indexerCapabilities, indexer.Protocol);

            var remoteIndexer = _sonarrV3Proxy.AddIndexer(sonarrIndexer, Settings);

            if (remoteIndexer == null)
            {
                _logger.Debug("Failed to add {0} [{1}]", indexer.Name, indexer.Id);

                return;
            }

            _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = remoteIndexer.Id });
        }

        public override void RemoveIndexer(int indexerId)
        {
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);

            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexerId);

            if (indexerMapping != null)
            {
                //Remove Indexer remotely and then remove the mapping
                _sonarrV3Proxy.RemoveIndexer(indexerMapping.RemoteIndexerId, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer, bool forceSync = false)
        {
            _logger.Debug("Updating indexer {0} [{1}]", indexer.Name, indexer.Id);

            var indexerCapabilities = _indexerFactory.GetInstance(indexer).GetCapabilities();
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);

            var sonarrIndexer = BuildSonarrIndexer(indexer, indexerCapabilities, indexer.Protocol, indexerMapping?.RemoteIndexerId ?? 0);

            var remoteIndexer = _sonarrV3Proxy.GetIndexer(indexerMapping.RemoteIndexerId, Settings);

            if (remoteIndexer != null)
            {
                _logger.Debug("Remote indexer {0} [{1}] found", remoteIndexer.Name, remoteIndexer.Id);

                if (!sonarrIndexer.Equals(remoteIndexer) || forceSync)
                {
                    _logger.Debug("Syncing remote indexer with current settings");

                    if (indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any() || indexerCapabilities.Categories.SupportedCategories(Settings.AnimeSyncCategories.ToArray()).Any())
                    {
                        // Retain user fields not-affiliated with Prowlarr
                        sonarrIndexer.Fields.AddRange(remoteIndexer.Fields.Where(f => sonarrIndexer.Fields.All(s => s.Name != f.Name)));

                        // Retain user tags not-affiliated with Prowlarr
                        sonarrIndexer.Tags.UnionWith(remoteIndexer.Tags);

                        // Retain user settings not-affiliated with Prowlarr
                        sonarrIndexer.DownloadClientId = remoteIndexer.DownloadClientId;
                        sonarrIndexer.SeasonSearchMaximumSingleEpisodeAge = remoteIndexer.SeasonSearchMaximumSingleEpisodeAge;

                        // Update the indexer if it still has categories that match
                        _sonarrV3Proxy.UpdateIndexer(sonarrIndexer, Settings);
                    }
                    else
                    {
                        // Else remove it, it no longer should be used
                        _sonarrV3Proxy.RemoveIndexer(remoteIndexer.Id, Settings);
                        _appIndexerMapService.Delete(indexerMapping.Id);
                    }
                }
            }
            else
            {
                _appIndexerMapService.Delete(indexerMapping.Id);

                if (indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any() || indexerCapabilities.Categories.SupportedCategories(Settings.AnimeSyncCategories.ToArray()).Any())
                {
                    _logger.Debug("Remote indexer not found, re-adding {0} [{1}] to Mangarr", indexer.Name, indexer.Id);
                    sonarrIndexer.Id = 0;
                    var newRemoteIndexer = _sonarrV3Proxy.AddIndexer(sonarrIndexer, Settings);
                    _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = newRemoteIndexer.Id });
                }
                else
                {
                    _logger.Debug("Remote indexer not found for {0} [{1}], skipping re-add to Mangarr due to indexer capabilities", indexer.Name, indexer.Id);
                }
            }
        }

        private SonarrIndexer BuildSonarrIndexer(IndexerDefinition indexer, IndexerCapabilities indexerCapabilities, DownloadProtocol protocol, int id = 0)
        {
            var cacheKey = $"{Settings.BaseUrl}";
            var schemas = _schemaCache.Get(cacheKey, () => _sonarrV3Proxy.GetIndexerSchema(Settings), TimeSpan.FromDays(7));
            var syncFields = new List<string> { "baseUrl", "apiPath", "apiKey", "categories", "animeCategories", "animeStandardFormatSearch", "minimumSeeders", "seedCriteria.seedRatio", "seedCriteria.seedTime", "seedCriteria.seasonPackSeedTime", "rejectBlocklistedTorrentHashesWhileGrabbing" };

            if (id == 0)
            {
                // Ensuring backward compatibility with older versions on first sync
                syncFields.AddRange(new List<string> { "additionalParameters" });
            }

            var schema = schemas.First(i => i.Implementation == "Torznab");

            var sonarrIndexer = new SonarrIndexer
            {
                Id = id,
                Name = $"{indexer.Name} (Sourcerarr)",
                EnableRss = indexer.Enable && indexer.AppProfile.Value.EnableRss,
                EnableAutomaticSearch = indexer.Enable && indexer.AppProfile.Value.EnableAutomaticSearch,
                EnableInteractiveSearch = indexer.Enable && indexer.AppProfile.Value.EnableInteractiveSearch,
                Priority = indexer.Priority,
                Implementation = "Torznab",
                ConfigContract = schema.ConfigContract,
                Fields = new List<SonarrField>(),
                Tags = new HashSet<int>()
            };

            sonarrIndexer.Fields.AddRange(schema.Fields.Where(x => syncFields.Contains(x.Name)));

            sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value = $"{Settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/";
            sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath").Value = "/api";
            sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey").Value = _configFileProvider.ApiKey;
            sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "categories").Value = JArray.FromObject(indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()));
            sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "animeCategories").Value = JArray.FromObject(indexerCapabilities.Categories.SupportedCategories(Settings.AnimeSyncCategories.ToArray()));

            if (sonarrIndexer.Fields.Any(x => x.Name == "animeStandardFormatSearch"))
            {
                sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "animeStandardFormatSearch").Value = Settings.SyncAnimeStandardFormatSearch;
            }

            return sonarrIndexer;
        }
    }
}
