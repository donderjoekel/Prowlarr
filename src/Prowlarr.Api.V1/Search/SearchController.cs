using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;
using Prowlarr.Http;
using Prowlarr.Http.Extensions;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Search
{
    [V1ApiController]
    public class SearchController : RestController<ReleaseResource>
    {
        private readonly IReleaseSearchService _releaseSearchService;
        private readonly IDownloadService _downloadService;
        private readonly IIndexerFactory _indexerFactory;
        private readonly IDownloadMappingService _downloadMappingService;
        private readonly Logger _logger;

        private readonly ICached<ReleaseInfo> _remoteReleaseCache;

        public SearchController(IReleaseSearchService releaseSearchService, IDownloadService downloadService, IIndexerFactory indexerFactory, IDownloadMappingService downloadMappingService, ICacheManager cacheManager, Logger logger)
        {
            _releaseSearchService = releaseSearchService;
            _downloadService = downloadService;
            _indexerFactory = indexerFactory;
            _downloadMappingService = downloadMappingService;
            _logger = logger;

            PostValidator.RuleFor(s => s.IndexerId).ValidId();
            PostValidator.RuleFor(s => s.Guid).NotEmpty();

            _remoteReleaseCache = cacheManager.GetCache<ReleaseInfo>(GetType(), "remoteReleases");
        }

        [NonAction]
        public override ReleaseResource GetResourceById(int id)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Produces("application/json")]
        public Task<List<ReleaseResource>> GetAll([FromQuery] SearchResource payload)
        {
            return GetSearchReleases(payload);
        }

        private async Task<List<ReleaseResource>> GetSearchReleases([FromQuery] SearchResource payload)
        {
            try
            {
                var request = new NewznabRequest
                {
                    q = payload.Query,
                    t = payload.Type,
                    source = "Prowlarr",
                    cat = string.Join(",", payload.Categories),
                    server = Request.GetServerUrl(),
                    host = Request.GetHostName(),
                    limit = payload.Limit,
                    offset = payload.Offset
                };

                request.QueryToParams();

                var result = await _releaseSearchService.Search(request, payload.IndexerIds, true);
                var releases = result.Releases;

                return MapReleases(releases, Request.GetServerUrl());
            }
            catch (SearchFailedException ex)
            {
                throw new NzbDroneClientException(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Search failed: " + ex.Message);
            }

            return new List<ReleaseResource>();
        }

        protected virtual List<ReleaseResource> MapReleases(IEnumerable<ReleaseInfo> releases, string serverUrl)
        {
            var result = new List<ReleaseResource>();

            foreach (var releaseInfo in releases)
            {
                var release = releaseInfo.ToResource();

                _remoteReleaseCache.Set(GetCacheKey(release), releaseInfo, TimeSpan.FromMinutes(30));
                release.DownloadUrl = release.DownloadUrl.IsNotNullOrWhiteSpace() ? _downloadMappingService.ConvertToProxyLink(new Uri(release.DownloadUrl), serverUrl, release.IndexerId, release.Title).AbsoluteUri : null;
                release.MagnetUrl = release.MagnetUrl.IsNotNullOrWhiteSpace() ? _downloadMappingService.ConvertToProxyLink(new Uri(release.MagnetUrl), serverUrl, release.IndexerId, release.Title).AbsoluteUri : null;

                result.Add(release);
            }

            _remoteReleaseCache.ClearExpired();

            return result;
        }

        private string GetCacheKey(ReleaseResource resource)
        {
            return string.Concat(resource.IndexerId, "_", resource.Guid);
        }
    }
}
