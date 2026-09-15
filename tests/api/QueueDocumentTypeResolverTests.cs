using Documate.Api.Domain;
using Documate.Api.Infrastructure.Intelligence;

namespace Documate.Api.Tests;

public sealed class QueueDocumentTypeResolverTests
{
    [Theory]
    [InlineData("delivery_note", "delivery_note")]
    [InlineData("Delivery note", "delivery_note")]
    [InlineData("Controlled Waste Transfer Note", "delivery_note")]
    [InlineData("DUTY OF CARE - CONTROLLED WASTE TRANSFER NOTE", "delivery_note")]
    [InlineData("Invoice", "invoice")]
    [InlineData("invoice", "invoice")]
    [InlineData("Sales Invoice", "invoice")]
    [InlineData("Service Invoice", "invoice")]
    [InlineData("Tax Invoice", "invoice")]
    public void Resolve_maps_labels_to_queue_route_keys(string identified, string expectedKey)
    {
        var types = new Dictionary<string, CorDocumentType>(StringComparer.OrdinalIgnoreCase)
        {
            ["invoice"] = new CorDocumentType { Id = 1, DocumentTypeKey = "invoice", Name = "Invoice" },
            ["delivery_note"] = new CorDocumentType
            {
                Id = 2,
                DocumentTypeKey = "delivery_note",
                Name = "Delivery note",
            },
        };

        var resolved = QueueDocumentTypeResolver.Resolve(identified, types);

        Assert.NotNull(resolved);
        Assert.Equal(expectedKey, resolved!.DocumentTypeKey);
    }

    [Fact]
    public void Resolve_returns_null_when_label_not_on_queue()
    {
        var types = new Dictionary<string, CorDocumentType>(StringComparer.OrdinalIgnoreCase)
        {
            ["invoice"] = new CorDocumentType { Id = 1, DocumentTypeKey = "invoice", Name = "Invoice" },
        };

        Assert.Null(QueueDocumentTypeResolver.Resolve("Controlled Waste Transfer Note", types));
    }
}
