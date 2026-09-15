namespace Documate.Api.Modules.FrontendSupport.Features.Auth;

using System.Security.Cryptography;
using System.Text;
using Documate.Api.Infrastructure.Auth;
using MediatR;
using Microsoft.Extensions.Options;

public sealed class LoginHandler(IOptions<AuthOptions> authOptions) : IRequestHandler<LoginCommand, LoginResponse?>
{
    public Task<LoginResponse?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var auth = authOptions.Value;
        var gate = auth.InterimFeGate;

        if (!gate.Enabled
            || string.IsNullOrWhiteSpace(gate.Username)
            || string.IsNullOrWhiteSpace(gate.Password)
            || string.IsNullOrWhiteSpace(gate.AccessToken))
        {
            return Task.FromResult<LoginResponse?>(null);
        }

        var usernameOk = FixedTimeEqualsUtf8(request.Username?.Trim() ?? "", gate.Username);
        var passwordOk = FixedTimeEqualsUtf8(request.Password ?? "", gate.Password);
        if (!usernameOk || !passwordOk)
        {
            return Task.FromResult<LoginResponse?>(null);
        }

        var bypass = auth.DevBypass;
        return Task.FromResult<LoginResponse?>(new LoginResponse(
            gate.AccessToken,
            "Bearer",
            bypass.UserId,
            bypass.TenantId,
            bypass.BusinessId));
    }

    private static bool FixedTimeEqualsUtf8(string left, string right)
    {
        var a = Encoding.UTF8.GetBytes(left);
        var b = Encoding.UTF8.GetBytes(right);
        if (a.Length != b.Length)
        {
            // Still touch both buffers so timing does not short-circuit on length alone.
            return CryptographicOperations.FixedTimeEquals(a, a) & false;
        }

        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
