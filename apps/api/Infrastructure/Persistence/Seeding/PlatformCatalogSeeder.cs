namespace Documate.Api.Infrastructure.Persistence.Seeding;

using Documate.Api.Domain;
using Microsoft.EntityFrameworkCore;

public static class PlatformCatalogSeeder
{
    public static async Task SeedAsync(DocumateDbContext db, ICorEnumIdResolver enumIds, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var ocrId = enumIds.Require("provider_category", "ocr");
        var llmId = enumIds.Require("provider_category", "llm");
        var metaId = enumIds.Require("provider_category", "meta");

        await EnsureProvider(db, "documate_meta", "Documate Meta", metaId, "Documate", now, cancellationToken);
        await EnsureProvider(db, "gpt_5_6", "GPT 5.6", llmId, "OpenAI", now, cancellationToken);
        await EnsureProvider(db, "claude_sonnet_6", "Claude Sonnet 6", llmId, "Anthropic", now, cancellationToken);
        await EnsureProvider(db, "aws_textract", "AWS Textract", ocrId, "AWS", now, cancellationToken);

        var invoiceTypeId = await EnsureDocumentType(db, "invoice", "Invoice", "Supplier invoice", now, cancellationToken);
        var creditNoteTypeId = await EnsureDocumentType(db, "credit_note", "Credit note", null, now, cancellationToken);
        var deliveryNoteTypeId = await EnsureDocumentType(db, "delivery_note", "Delivery note", null, now, cancellationToken);
        await EnsureDocumentType(db, "purchase_order", "Purchase order", null, now, cancellationToken);

        var metaProvider = await db.CorProviders.SingleAsync(p => p.ProviderKey == "documate_meta", cancellationToken);

        await EnsureTemplate(
            db,
            "invoice_generic_v1",
            "Invoice (generic)",
            "Starter invoice extraction agent",
            invoiceTypeId,
            """{"type":"object","properties":{"invoice_number":{"type":"string"},"invoice_date":{"type":"string"},"total":{"type":"number"},"currency":{"type":"string"},"vendor_name":{"type":"string"}}}""",
            "Extract invoice header fields accurately. Prefer printed values over handwritten notes.",
            metaProvider.Id,
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
            metaProvider.Id,
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
            metaProvider.Id,
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
    }
}
