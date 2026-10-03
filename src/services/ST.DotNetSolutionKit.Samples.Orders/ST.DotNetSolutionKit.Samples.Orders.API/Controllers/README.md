# Controller Development Guide

The template ships no controllers: every service adds its own. This guide shows the shape they take.
`Sample*` below is an illustration, not code that exists in the template.

## Controller

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.DotNetSolutionKit.Samples.Common.Contracts.OrdersService.Samples;
using ST.DotNetSolutionKit.Samples.Common.Contracts.OrdersService.Samples.Models;
using ST.DotNetSolutionKit.Samples.Common.Web.Authorization;
using ST.DotNetSolutionKit.Samples.Orders.Application.Services.Samples;

namespace ST.DotNetSolutionKit.Samples.Orders.API.Controllers;

/// <summary>
/// Manages samples.
/// </summary>
[ApiController]
[Authorize]
[Route(SampleRoutes.SamplesRoot)]
public class SamplesController : ControllerBase
{
    private readonly ISampleService _sampleService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SamplesController"/>.
    /// </summary>
    public SamplesController(ISampleService sampleService)
    {
        _sampleService = sampleService;
    }

    /// <summary>
    /// Returns the samples matching the filter.
    /// </summary>
    /// <param name="filter">Filter criteria.</param>
    [HttpGet(SampleRoutes.GetAllSamples)]
    [RequiredPermissions(SamplePermissions.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<GetAllSamplesResponse> GetAllSamples([FromQuery] SampleFilter filter)
    {
        return _sampleService.GetAllSamplesAsync(filter, HttpContext.RequestAborted);
    }

    /// <summary>
    /// Returns one sample.
    /// </summary>
    /// <param name="id">Sample identifier.</param>
    [HttpGet(SampleRoutes.GetSampleById)]
    [RequiredPermissions(SamplePermissions.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<GetSampleResponse> GetSampleById([FromRoute] Guid id)
    {
        return _sampleService.GetSampleByIdAsync(id, HttpContext.RequestAborted);
    }

    /// <summary>
    /// Creates a sample.
    /// </summary>
    /// <param name="request">Sample data.</param>
    [HttpPost(SampleRoutes.CreateSample)]
    [RequiredPermissions(SamplePermissions.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<CreateSampleResponse> CreateSample([FromBody] CreateSampleRequest request)
    {
        return _sampleService.CreateSampleAsync(request, HttpContext.RequestAborted);
    }
}
```

## Route constants

Routes live in `Common.Contracts`, next to the request and response models, so a client of the
service and the service itself read the same strings:

```csharp
namespace ST.DotNetSolutionKit.Samples.Common.Contracts.OrdersService.Samples;

/// <summary>
/// Samples API endpoints.
/// </summary>
public static class SampleRoutes
{
    private const string ApiWithVersion = "/api/v1/";

    /// <summary>Samples root.</summary>
    public const string SamplesRoot = ApiWithVersion + "samples";

    /// <summary>Get all samples.</summary>
    public const string GetAllSamples = "";

    /// <summary>Get a sample by id.</summary>
    public const string GetSampleById = "{id}";

    /// <summary>Create a sample.</summary>
    public const string CreateSample = "";
}
```

## Rules

1. Routes come from constants in `Common.Contracts`, with the version in the path (`/api/v1/`).
   Swagger builds one document per version from these paths.
2. The controller only delegates. It returns the service's `Task<T>` directly, without
   `async`/`await`, and holds no logic of its own.
3. Actions return a concrete response type instead of `IActionResult`, so Swagger documents it.
4. Successful responses return the DTO as is. Errors are RFC 9457 problems written by the shared
   error handling in `Common.Web/Errors`; a controller does not build error bodies.
5. No try/catch in controllers. Throw the exceptions from `Common/Exceptions`
   (`NotFoundException`, `ConflictException` and the rest); the global handler maps them to status
   codes.
6. Permissions on every action through `[RequiredPermissions(...)]`; the global permission filter
   enforces them.
7. XML comments on every action and parameter. They become the Swagger descriptions.
8. Status codes match the operation: `201` for creation, `404` when a resource is looked up by id.
