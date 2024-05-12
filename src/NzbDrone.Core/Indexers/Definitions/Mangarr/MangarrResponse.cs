using NzbDrone.Common.Http;

namespace NzbDrone.Core.Indexers.Definitions.Mangarr;

public class MangarrResponse : IndexerResponse
{
    private readonly MangarrRequest _request;

    public MangarrResponse(MangarrRequest indexerRequest, HttpResponse httpResponse)
        : base(indexerRequest, httpResponse)
    {
        _request = indexerRequest;
    }

    public new MangarrRequest Request => _request;
}
