namespace Documate.Api.Infrastructure.Pipeline.Stages;

/// <summary>
/// Split a File into logical Documents. Predetermined documentTypeKey skips this stage.
/// Real page-boundary split is deferred to a later phase.
/// </summary>
public interface IFileSplitStage
{
    Task ExecuteAsync(FilePipelineContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Assign DocumentType when not supplied by the caller. Predetermined type skips this stage.
/// Real classify is deferred to a later phase.
/// </summary>
public interface IFileClassifyStage
{
    Task ExecuteAsync(FilePipelineContext context, CancellationToken cancellationToken = default);
}

/// <summary>Bind each typed Document to an Agent via QueueRoute.</summary>
public interface IDocumentRouteStage
{
    Task ExecuteAsync(FilePipelineContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Extract into the routed Agent schema, validate, then mark Document Ready/Failed.
/// Post-processing (MCP) is a later DQ.
/// </summary>
public interface IDocumentExtractStage
{
    Task ExecuteAsync(FilePipelineContext context, CancellationToken cancellationToken = default);
}
