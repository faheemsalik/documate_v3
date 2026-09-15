namespace Documate.Api.Domain;

public sealed class OpsIntakeMailboxAllowlistEntry : CatalogEntity
{
    public string BusinessId { get; set; } = "";
    public Guid MailboxId { get; set; }
    public long MatchTypeEnumId { get; set; }
    public string Value { get; set; } = "";

    public OpsIntakeMailbox? Mailbox { get; set; }
    public CorEnum? MatchType { get; set; }
}
