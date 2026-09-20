namespace Documate.Api.Tests;

using Documate.Api.Infrastructure.Extract;

public class ExtractPromptComposerTests
{
    private readonly ExtractPromptComposer _composer = new();

    [Fact]
    public void Empty_system_prompt_falls_back_to_default()
    {
        var result = _composer.Compose(null, "Do it", """{"type":"object"}""", null, "OCR");
        Assert.Equal(ExtractPromptDefaults.SystemPrompt, result.SystemMessage);
        Assert.Contains("Do it", result.UserMessage);
        Assert.Contains("OCR", result.UserMessage);
        Assert.DoesNotContain("Additional post-process instructions:", result.UserMessage);
    }

    [Fact]
    public void Omits_post_process_section_when_empty()
    {
        var result = _composer.Compose("SYS", "inst", "{}", "  ", "text");
        Assert.Equal("SYS", result.SystemMessage);
        Assert.DoesNotContain("Additional post-process instructions:", result.UserMessage);
    }

    [Fact]
    public void Includes_post_process_and_schema_description()
    {
        const string schema = """{"type":"object","properties":{"invoice_number":{"type":"string","description":"Supplier number"}}}""";
        var result = _composer.Compose("SYS", "inst", schema, "Normalize dates to ISO.", "OCR body");
        Assert.Contains("Additional post-process instructions:", result.UserMessage);
        Assert.Contains("Normalize dates to ISO.", result.UserMessage);
        Assert.Contains("Supplier number", result.UserMessage);
        Assert.Contains("OCR body", result.UserMessage);
        Assert.DoesNotContain(ExtractPromptDefaults.DocumentTextPlaceholder, result.UserMessage);
    }

    [Fact]
    public void Null_document_text_uses_placeholder()
    {
        var result = _composer.Compose("SYS", "inst", "{}", "post", null);
        Assert.Contains(ExtractPromptDefaults.DocumentTextPlaceholder, result.UserMessage);
    }

    [Fact]
    public void Suggest_system_prompt_includes_instructions_schema_and_post()
    {
        const string schema =
            """{"type":"object","properties":{"invoice_number":{"type":"string","description":"Supplier number"}}}""";
        var draft = _composer.SuggestSystemPrompt("Extract totals carefully.", schema, "ISO dates only.");
        Assert.Contains(ExtractPromptDefaults.SystemPrompt, draft, StringComparison.Ordinal);
        Assert.Contains("Extract totals carefully.", draft, StringComparison.Ordinal);
        Assert.Contains("invoice_number: Supplier number", draft, StringComparison.Ordinal);
        Assert.Contains("ISO dates only.", draft, StringComparison.Ordinal);
    }
}
