namespace Documate.Api.Infrastructure.Extract;

using System.Text;
using System.Text.Json;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Storage;
using Microsoft.Extensions.Options;

/// <summary>
/// Mode 1 meta-provider. Always records providerKey <c>documate_meta</c>.
/// Fills the Agent schema from normalize text (heuristic). When
/// Providers:DocumateMetaApiKey or DefaultLlmApiKey is set, the same body runs —
/// live LLM can replace this without changing the interface.
/// </summary>
public sealed class Mode1DocumateMetaExtractAdapter(
    IObjectStorage storage,
    IOptions<ProviderCredentialsOptions> credentials,
    ILogger<Mode1DocumateMetaExtractAdapter> logger) : IDocumentExtractAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public async Task<ExtractAdapterResult> ExtractAsync(
        ExtractAdapterRequest request,
        CancellationToken cancellationToken = default)
    {
        var text = request.SourceText;
        if (string.IsNullOrWhiteSpace(text))
        {
            text = await ReadTextArtifactAsync(request, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("No source text available for extract.");
        }

        var armed = !string.IsNullOrWhiteSpace(credentials.Value.DocumateMetaApiKey)
            || !string.IsNullOrWhiteSpace(credentials.Value.DefaultLlmApiKey);
        const string providerKey = "documate_meta";

        var payload = SchemaGuidedExtractor.Extract(request.OutputSchemaJson, text);
        var json = payload.ToJsonString(JsonOptions);

        logger.LogInformation(
            "Extracted Document {DocumentId} via {ProviderKey} (llmArmed={Armed}); fields={FieldCount}",
            request.DocumentId,
            providerKey,
            armed,
            payload.Count);

        return new ExtractAdapterResult(providerKey, payload, json);
    }

    private async Task<string> ReadTextArtifactAsync(ExtractAdapterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.StorageBucket) || string.IsNullOrWhiteSpace(request.TextArtifactKey))
        {
            return "";
        }

        await using var stream = await storage.DownloadAsync(
            request.StorageBucket,
            request.TextArtifactKey,
            cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
