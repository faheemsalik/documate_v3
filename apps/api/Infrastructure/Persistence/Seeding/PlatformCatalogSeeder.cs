namespace Documate.Api.Infrastructure.Persistence.Seeding;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Extract;
using Microsoft.EntityFrameworkCore;

public static class PlatformCatalogSeeder
{
    public static async Task SeedAsync(DocumateDbContext db, ICorEnumIdResolver enumIds, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var ocrId = enumIds.Require("provider_category", "ocr");
        var llmId = enumIds.Require("provider_category", "llm");
        var metaId = enumIds.Require("provider_category", "meta");

        await EnsureProvider(db, "documate_meta", "Documate Meta (façade)", metaId, "Documate", now, cancellationToken);

        // OpenAI
        await EnsureProvider(db, "gpt_5_6", "GPT 5.6", llmId, "OpenAI", now, cancellationToken);
        await EnsureProvider(db, "gpt_4o", "GPT-4o", llmId, "OpenAI", now, cancellationToken);
        await EnsureProvider(db, "gpt_4o_mini", "GPT-4o mini", llmId, "OpenAI", now, cancellationToken);
        await EnsureProvider(db, "gpt_4_1", "GPT-4.1", llmId, "OpenAI", now, cancellationToken);
        await EnsureProvider(db, "gpt_4_1_mini", "GPT-4.1 mini", llmId, "OpenAI", now, cancellationToken);
        await EnsureProvider(db, "o4_mini", "o4-mini", llmId, "OpenAI", now, cancellationToken);

        // Anthropic
        await EnsureProvider(db, "claude_sonnet_6", "Claude Sonnet 6", llmId, "Anthropic", now, cancellationToken);
        await EnsureProvider(db, "claude_sonnet_4", "Claude Sonnet 4", llmId, "Anthropic", now, cancellationToken);
        await EnsureProvider(db, "claude_3_5_sonnet", "Claude 3.5 Sonnet", llmId, "Anthropic", now, cancellationToken);
        await EnsureProvider(db, "claude_3_5_haiku", "Claude 3.5 Haiku", llmId, "Anthropic", now, cancellationToken);
        await EnsureProvider(db, "claude_haiku_4_5", "Claude Haiku 4.5", llmId, "Anthropic", now, cancellationToken);

        // Google Gemini
        await EnsureProvider(db, "gemini_2_5_flash", "Gemini 2.5 Flash", llmId, "Google", now, cancellationToken);
        await EnsureProvider(db, "gemini_2_0_flash", "Gemini 2.0 Flash", llmId, "Google", now, cancellationToken);
        await EnsureProvider(db, "gemini_1_5_pro", "Gemini 1.5 Pro", llmId, "Google", now, cancellationToken);

        // DeepSeek
        await EnsureProvider(db, "deepseek_v3", "DeepSeek V3 (chat)", llmId, "DeepSeek", now, cancellationToken);
        await EnsureProvider(db, "deepseek_r1", "DeepSeek R1", llmId, "DeepSeek", now, cancellationToken);

        // Zhipu GLM
        await EnsureProvider(db, "glm_4", "GLM-4", llmId, "Zhipu", now, cancellationToken);
        await EnsureProvider(db, "glm_4_flash", "GLM-4-Flash", llmId, "Zhipu", now, cancellationToken);

        // Moonshot / Kimi
        await EnsureProvider(db, "kimi_k2", "Kimi K2", llmId, "Moonshot", now, cancellationToken);
        await EnsureProvider(db, "moonshot_v1_8k", "Moonshot v1 8k", llmId, "Moonshot", now, cancellationToken);
        await EnsureProvider(db, "moonshot_v1_32k", "Moonshot v1 32k", llmId, "Moonshot", now, cancellationToken);
        await EnsureProvider(db, "moonshot_v1_128k", "Moonshot v1 128k", llmId, "Moonshot", now, cancellationToken);

        await EnsureProvider(db, "aws_textract", "AWS Textract", ocrId, "AWS", now, cancellationToken);
        await EnsureProvider(db, "google_document_ai", "Google Document AI", ocrId, "Google", now, cancellationToken);

        var invoiceTypeId = await EnsureDocumentType(db, "invoice", "Invoice", "Supplier invoice", now, cancellationToken);
        var creditNoteTypeId = await EnsureDocumentType(db, "credit_note", "Credit note", null, now, cancellationToken);
        var deliveryNoteTypeId = await EnsureDocumentType(db, "delivery_note", "Delivery note", null, now, cancellationToken);
        await EnsureDocumentType(db, "purchase_order", "Purchase order", null, now, cancellationToken);
        await EnsureDocumentType(db, "vendor_statement", "Vendor Statement", null, now, cancellationToken);
        await EnsureDocumentType(db, "bank_statement", "Bank Statement", null, now, cancellationToken);
        await EnsureDocumentType(db, "passport_canada", "Passport Canada", null, now, cancellationToken);
        await EnsureDocumentType(db, "passport_france", "Passport France", null, now, cancellationToken);
        await EnsureDocumentType(db, "passport_uk", "Passport UK", null, now, cancellationToken);
        await EnsureDocumentType(db, "passport_india", "Passport India", null, now, cancellationToken);
        await EnsureDocumentType(db, "passport_ksa", "Passport KSA", null, now, cancellationToken);

        var defaultLlmProvider = await db.CorProviders.SingleAsync(p => p.ProviderKey == "gpt_5_6", cancellationToken);

        await EnsureTemplate(
            db,
            "invoice_generic_v1",
            "Invoice (generic)",
            "Starter invoice extraction agent",
            invoiceTypeId,
            """{"type":"object","properties":{"invoice_number":{"type":"string"},"invoice_date":{"type":"string"},"total":{"type":"number"},"currency":{"type":"string"},"vendor_name":{"type":"string"}}}""",
            "Extract invoice header fields accurately. Prefer printed values over handwritten notes.",
            defaultLlmProvider.Id,
            now,
            cancellationToken);

        await EnsureTemplate(
            db,
            "credit_note_generic_v1",
            "Credit note (generic)",
            null,
            creditNoteTypeId,
            """{"type":"object","properties":{"credit_note_number":{"type":"string"},"related_invoice_number":{"type":"string"},"total":{"type":"number"},"currency":{"type":"string"}}}""",
            "Extract credit note fields and link to original invoice when present.",
            defaultLlmProvider.Id,
            now,
            cancellationToken);

        await EnsureTemplate(
            db,
            "delivery_note_generic_v1",
            "Delivery note (generic)",
            null,
            deliveryNoteTypeId,
            """{"type":"object","properties":{"delivery_note_number":{"type":"string"},"delivery_date":{"type":"string"},"ship_to":{"type":"string"},"line_items":{"type":"array"}}}""",
            "Extract delivery note header and line items.",
            defaultLlmProvider.Id,
            now,
            cancellationToken);
    }

    private static async Task EnsureProvider(
        DocumateDbContext db,
        string key,
        string name,
        long categoryEnumId,
        string vendorHint,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var row = await db.CorProviders.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.ProviderKey == key, cancellationToken);
        if (row is null)
        {
            db.CorProviders.Add(new CorProvider
            {
                ProviderKey = key,
                Name = name,
                CategoryEnumId = categoryEnumId,
                VendorHint = vendorHint,
                IsPlatformManaged = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (row.IsDeleted)
        {
            row.IsDeleted = false;
            row.DeletedAt = null;
            row.IsActive = true;
            row.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var dirty = false;
        if (!string.Equals(row.Name, name, StringComparison.Ordinal))
        {
            row.Name = name;
            dirty = true;
        }

        if (!string.Equals(row.VendorHint, vendorHint, StringComparison.Ordinal))
        {
            row.VendorHint = vendorHint;
            dirty = true;
        }

        if (row.CategoryEnumId != categoryEnumId)
        {
            row.CategoryEnumId = categoryEnumId;
            dirty = true;
        }

        if (dirty)
        {
            row.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<long> EnsureDocumentType(
        DocumateDbContext db,
        string key,
        string name,
        string? description,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var row = await db.CorDocumentTypes.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.DocumentTypeKey == key, cancellationToken);
        if (row is null)
        {
            row = new CorDocumentType
            {
                DocumentTypeKey = key,
                Name = name,
                Description = description,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.CorDocumentTypes.Add(row);
            await db.SaveChangesAsync(cancellationToken);
            return row.Id;
        }

        if (row.IsDeleted)
        {
            row.IsDeleted = false;
            row.DeletedAt = null;
            row.IsActive = true;
            row.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        return row.Id;
    }

    private static async Task EnsureTemplate(
        DocumateDbContext db,
        string key,
        string name,
        string? description,
        long documentTypeId,
        string schemaJson,
        string instructions,
        long providerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var row = await db.CorAgentTemplates.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.AgentTemplateKey == key, cancellationToken);
        if (row is null)
        {
            db.CorAgentTemplates.Add(new CorAgentTemplate
            {
                AgentTemplateKey = key,
                Name = name,
                Description = description,
                DocumentTypeId = documentTypeId,
                DefaultSchemaJson = schemaJson,
                DefaultInstructions = instructions,
                SystemPrompt = ExtractPromptDefaults.SystemPrompt,
                DefaultPostProcessPrompt = "",
                DefaultAdditionalDocumentInstructions = "",
                DefaultProviderId = providerId,
                IsPublished = true,
                Version = 1,
                CreatedAt = now,
                UpdatedAt = now,
            });
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (row.IsDeleted)
        {
            row.IsDeleted = false;
            row.DeletedAt = null;
            row.IsPublished = true;
            row.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        // DQ-0704: templates use LLM DefaultProviderId (not documate_meta).
        var dirty = false;
        if (row.DefaultProviderId != providerId)
        {
            row.DefaultProviderId = providerId;
            dirty = true;
        }

        if (string.IsNullOrWhiteSpace(row.SystemPrompt))
        {
            row.SystemPrompt = ExtractPromptDefaults.SystemPrompt;
            dirty = true;
        }

        if (dirty)
        {
            row.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
