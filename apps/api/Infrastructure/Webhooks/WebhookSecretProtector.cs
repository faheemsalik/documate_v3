namespace Documate.Api.Infrastructure.Webhooks;

using Microsoft.AspNetCore.DataProtection;

public interface IWebhookSecretProtector
{
    string Protect(string secret);
    string Unprotect(string protectedSecret);
}

public sealed class WebhookSecretProtector(IDataProtectionProvider provider) : IWebhookSecretProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("Documate.QueueWebhookSecret.v1");

    public string Protect(string secret) => _protector.Protect(secret);

    public string Unprotect(string protectedSecret) => _protector.Unprotect(protectedSecret);
}
