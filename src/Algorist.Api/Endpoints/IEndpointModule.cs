namespace Algorist.Api.Endpoints;

/// <summary>
/// A group of API endpoints. Modules registered in DI are mapped under <c>/api</c> at startup.
/// </summary>
public interface IEndpointModule
{
    void MapEndpoints(IEndpointRouteBuilder api);
}
