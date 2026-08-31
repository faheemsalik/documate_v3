namespace Documate.Api.Tests;

using System.Text.Json.Nodes;
using Documate.Api.Infrastructure.PostProcess;

public class PlatformMcpToolTests
{
    [Fact]
    public async Task Normalize_date_converts_common_formats()
    {
        var tool = new NormalizeDateTool();
        var payload = new JsonObject
        {
            ["invoice_date"] = "15/03/2026",
            ["vendor_name"] = "Acme",
        };
        await tool.ExecuteAsync(payload, ["*"]);
        Assert.Equal("2026-03-15", payload["invoice_date"]!.GetValue<string>());
        Assert.Equal("Acme", payload["vendor_name"]!.GetValue<string>());
    }

    [Fact]
    public async Task Normalize_currency_maps_symbols()
    {
        var tool = new NormalizeCurrencyTool();
        var payload = new JsonObject
        {
            ["currency"] = "€",
            ["total"] = 10,
        };
        await tool.ExecuteAsync(payload, ["*"]);
        Assert.Equal("EUR", payload["currency"]!.GetValue<string>());
    }
}
