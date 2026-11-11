using JetBrains.Annotations;

namespace ST.DotNetSolutionKit.Samples.Common.Contracts.Responses;

[UsedImplicitly]
public abstract record BaseResponse(string? Message = null) : IBaseResponse;
