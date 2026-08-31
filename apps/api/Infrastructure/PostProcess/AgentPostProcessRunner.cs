namespace Documate.Api.Infrastructure.PostProcess;

using System.Text.Json;
using System.Text.Json.Nodes;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed record PostProcessResult(string ResultJson, bool Ran, string? WorkflowKey);

public interface IAgentPostProcessRunner
{
    /// <summary>
    /// Runs Agent.DefaultWorkflowId steps via internal MCP. No-op when workflow unset/inactive.
    /// Mutates ResultJson in place when tools change fields.
    /// </summary>
    Task<PostProcessResult> RunAsync(OpsAgent agent, string resultJson, CancellationToken cancellationToken = default);
}

/// <summary>Plan 01 §13 Agent-primary post-process (DQ-1101).</summary>
public sealed class AgentPostProcessRunner(
    DocumateDbContext db,
    IInternalMcpHost mcp,
    ILogger<AgentPostProcessRunner> logger) : IAgentPostProcessRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public async Task<PostProcessResult> RunAsync(
        OpsAgent agent,
        string resultJson,
        CancellationToken cancellationToken = default)
    {
        if (agent.DefaultWorkflowId is not long workflowId)
        {
            return new PostProcessResult(resultJson, false, null);
        }

        var workflow = await db.CorWorkflowDefinitions.AsNoTracking().FirstOrDefaultAsync(
            w => w.Id == workflowId
                && w.BusinessId == agent.BusinessId
                && w.IsActive
                && !w.IsDeleted,
            cancellationToken);

        if (workflow is null)
        {
            throw new InvalidOperationException(
                $"Agent DefaultWorkflowId={workflowId} not found or inactive for this Business.");
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(resultJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"ResultJson is not valid JSON before post-process: {ex.Message}");
        }

        if (root is not JsonObject payload)
        {
            throw new InvalidOperationException("ResultJson root must be a JSON object for post-process.");
        }

        var steps = ParseSteps(workflow.DefinitionJson);
        foreach (var step in steps)
        {
            await mcp.InvokeAsync(step.Tool, payload, step.Fields, cancellationToken);
        }

        var updated = payload.ToJsonString(JsonOptions);
        logger.LogInformation(
            "Post-process ran workflow {WorkflowKey} ({StepCount} steps) for Agent {AgentId}",
            workflow.WorkflowKey,
            steps.Count,
            agent.Id);

        return new PostProcessResult(updated, true, workflow.WorkflowKey);
    }

    private static IReadOnlyList<WorkflowStep> ParseSteps(string definitionJson)
    {
        JsonNode? def;
        try
        {
            def = JsonNode.Parse(string.IsNullOrWhiteSpace(definitionJson) ? "{}" : definitionJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Workflow DefinitionJson is invalid: {ex.Message}");
        }

        var stepsNode = def?["steps"] as JsonArray;
        if (stepsNode is null || stepsNode.Count == 0)
        {
            return [];
        }

        var list = new List<WorkflowStep>();
        foreach (var item in stepsNode)
        {
            if (item is not JsonObject obj)
            {
                continue;
            }

            var tool = obj["tool"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(tool))
            {
                continue;
            }

            var fields = new List<string>();
            if (obj["fields"] is JsonArray arr)
            {
                foreach (var f in arr)
                {
                    if (f?.GetValue<string>() is { Length: > 0 } name)
                    {
                        fields.Add(name);
                    }
                }
            }

            list.Add(new WorkflowStep(tool.Trim(), fields));
        }

        return list;
    }

    private sealed record WorkflowStep(string Tool, IReadOnlyList<string> Fields);
}
