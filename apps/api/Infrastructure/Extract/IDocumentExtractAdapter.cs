namespace Documate.Api.Infrastructure.Extract;

using System.Text.Json.Nodes;

/// <summary>Mode 1 Documate meta-provider: fill the Agent output schema from document text.</summary>
public interface IDocumentExtractAdapter
{
    Task<ExtractAdapterResult> ExtractAsync(ExtractAdapterRequest request, CancellationToken cancellationToken = default);
}

public sealed record ExtractAdapterRequest(
    Guid FileId,
    Guid DocumentId,
    long DocumentSequenceId,
    string? StorageBucket,
    string? TextArtifactKey,
    string OutputSchemaJson,
    string Instructions,
    string? SourceText,
    /// <summary>Agent/template LLM provider key (e.g. gpt_5_6); falls back to Llm:DefaultProviderKey.</summary>
    string? PreferredLlmProviderKey = null);

public sealed record ExtractAdapterResult(
    string ProviderKey,
    JsonObject Payload,
    string ResultJson);
