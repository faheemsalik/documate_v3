namespace Documate.Api.Infrastructure.PostProcess;

using System.Text.Json.Nodes;

/// <summary>Platform tool exposed on the internal MCP bus (Plan 01 §13). Not customer MCP.</summary>
public interface IPlatformMcpTool
{
    string ToolName { get; }
    Task ExecuteAsync(JsonObject payload, IReadOnlyList<string> fields, CancellationToken cancellationToken = default);
}

public interface IInternalMcpHost
{
    Task InvokeAsync(string toolName, JsonObject payload, IReadOnlyList<string> fields, CancellationToken cancellationToken = default);
}

public sealed class InternalMcpHost(
    IEnumerable<IPlatformMcpTool> tools,
    ILogger<InternalMcpHost> logger) : IInternalMcpHost
{
    private readonly Dictionary<string, IPlatformMcpTool> _tools =
        tools.ToDictionary(t => t.ToolName, StringComparer.OrdinalIgnoreCase);

    public async Task InvokeAsync(
        string toolName,
        JsonObject payload,
        IReadOnlyList<string> fields,
        CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            throw new InvalidOperationException($"Unknown platform MCP tool '{toolName}'.");
        }

        logger.LogDebug("Internal MCP invoke {Tool} fields={FieldCount}", toolName, fields.Count);
        await tool.ExecuteAsync(payload, fields, cancellationToken);
    }
}
