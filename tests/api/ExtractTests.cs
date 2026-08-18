namespace Documate.Api.Tests;

using System.Text.Json.Nodes;
using Documate.Api.Infrastructure.Extract;

public class JsonSchemaLiteTests
{
    private const string InvoiceSchema =
        """{"type":"object","properties":{"invoice_number":{"type":"string"},"total":{"type":"number"}}}""";

    [Fact]
    public void Valid_object_passes()
    {
        var instance = JsonNode.Parse("""{"invoice_number":"INV-1","total":10.5}""");
        var result = JsonSchemaLite.Validate(InvoiceSchema, instance);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Wrong_type_fails()
    {
        var instance = JsonNode.Parse("""{"invoice_number":"INV-1","total":"ten"}""");
        var result = JsonSchemaLite.Validate(InvoiceSchema, instance);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("total", StringComparison.Ordinal));
    }

    [Fact]
    public void Missing_required_fails()
    {
        var schema = """{"type":"object","required":["invoice_number"],"properties":{"invoice_number":{"type":"string"}}}""";
        var result = JsonSchemaLite.Validate(schema, JsonNode.Parse("{}"));
        Assert.False(result.IsValid);
    }
}

public class SchemaGuidedExtractorTests
{
    private const string InvoiceSchema =
        """{"type":"object","properties":{"invoice_number":{"type":"string"},"invoice_date":{"type":"string"},"total":{"type":"number"},"currency":{"type":"string"},"vendor_name":{"type":"string"}}}""";

    [Fact]
    public void Reads_labeled_lines()
    {
        var text = """
            invoice_number: INV-0703
            invoice_date: 2026-08-18
            total: 42.5
            currency: USD
            vendor_name: Acme
            """;
        var payload = SchemaGuidedExtractor.Extract(InvoiceSchema, text);
        Assert.Equal("INV-0703", payload["invoice_number"]!.GetValue<string>());
        Assert.Equal(42.5m, payload["total"]!.GetValue<decimal>());
        Assert.Equal("USD", payload["currency"]!.GetValue<string>());
        Assert.Equal("Acme", payload["vendor_name"]!.GetValue<string>());
    }

    [Fact]
    public void Reads_json_payload()
    {
        var text = """{"invoice_number":"INV-JSON","total":9}""";
        var payload = SchemaGuidedExtractor.Extract(InvoiceSchema, text);
        Assert.Equal("INV-JSON", payload["invoice_number"]!.GetValue<string>());
        Assert.Equal(9m, payload["total"]!.GetValue<decimal>());
    }
}
