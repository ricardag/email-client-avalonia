using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace ClienteEmail.Integrations;

public class MsalAuthenticationProvider : IAuthenticationProvider
    {
    private readonly IPublicClientApplication _app;
    private readonly string[] _scopes;

    public MsalAuthenticationProvider(IPublicClientApplication app, string[] scopes)
        {
        _app = app;
        _scopes = scopes;
        }

    public async Task AuthenticateRequestAsync(RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
        {
        var accounts = await _app.GetAccountsAsync();
        AuthenticationResult result;

        try
            {
            // Tenta obter token silenciosamente
            result = await _app.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                .ExecuteAsync(cancellationToken);
            }
        catch (MsalUiRequiredException)
            {
            // Se expirou, faz novo login interativo
            result = await _app.AcquireTokenInteractive(_scopes)
                .ExecuteAsync(cancellationToken);
            }

        // Adicionar o token no header
        request.Headers.Add("Authorization", $"Bearer {result.AccessToken}");
        }
    }