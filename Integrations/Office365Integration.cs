using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.MailFolders;
using Microsoft.Graph.Users.Item.SendMail;
using Microsoft.Identity.Client;
using Prompt = Microsoft.Identity.Client.Prompt;

namespace ClienteEmail.Integrations;

public class Office365Integration
    {
    private static readonly string[] Scopes = ["Mail.Read", "Mail.Send", "User.Read"];

    private readonly string _cacheFilePath;
    private readonly string _clientId;
    private readonly string _email;
    private readonly string _tenantId = "common";
    private GraphServiceClient? _graphClient;

    public Office365Integration(IConfiguration configuration, string email)
        {
        _clientId = configuration.GetValue<string>("Office365:ClientId") ?? throw new InvalidOperationException();
        _email = email;
        _cacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClienteEmail",
            $"{_email}.token_cache.json"
            );
        }

    public async Task Inicializar()
        {
        // Criar diretório se não existir
        var cacheDirectory = Path.GetDirectoryName(_cacheFilePath);
        if (!Directory.Exists(cacheDirectory)) Directory.CreateDirectory(cacheDirectory!);

        // Criar aplicação MSAL com cache personalizado
        var app = PublicClientApplicationBuilder
            .Create(_clientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, _tenantId)
            .WithRedirectUri("http://localhost")
            .Build();

        // Configurar cache de token
        ConfigurarCacheDeToken(app.UserTokenCache);

        AuthenticationResult? result = null;

        try
            {
            // Tentar obter token silenciosamente (do cache)
            IEnumerable<IAccount>? accounts = await app.GetAccountsAsync();
            if (accounts.Any())
                result = await app.AcquireTokenSilent(Scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
        catch (MsalUiRequiredException)
            {
            // Token expirado ou não existe - precisa fazer login, abrindo o navegador
            }

        // Se não conseguiu do cache, faz login interativo
        result ??= await app.AcquireTokenInteractive(Scopes)
            .WithPrompt(Prompt.SelectAccount)
            .ExecuteAsync();

        if (result == null) return; // não autenticou

        // Criar um authentication provider personalizado
        var authProvider = new MsalAuthenticationProvider(app, Scopes);

        // Criar GraphServiceClient
        _graphClient = new GraphServiceClient(authProvider);
        }

    private void ConfigurarCacheDeToken(ITokenCache tokenCache)
        {
        tokenCache.SetBeforeAccess(notificationArgs =>
            {
            // Carregar cache do arquivo
            if (File.Exists(_cacheFilePath))
                try
                    {
                    var cacheData = File.ReadAllBytes(_cacheFilePath);
                    notificationArgs.TokenCache.DeserializeMsalV3(cacheData);
                    }
                catch (Exception ex)
                    {
                    Console.WriteLine($"Erro ao carregar cache: {ex.Message}");
                    throw;
                    }
            });

        tokenCache.SetAfterAccess(notificationArgs =>
            {
            // Salvar cache no arquivo se mudou
            if (notificationArgs.HasStateChanged)
                try
                    {
                    var cacheData = notificationArgs.TokenCache.SerializeMsalV3();
                    File.WriteAllBytes(_cacheFilePath, cacheData);
                    }
                catch (Exception ex)
                    {
                    Console.WriteLine($"Erro ao salvar cache: {ex.Message}");
                    throw;
                    }
            });
        }

    public async Task<MailFolderCollectionResponse?> ListarPastas()
        {
        if (_graphClient == null)
            return null;

        try
            {
            // Buscar a lista de pastas de email do usuário
            return await _graphClient
                .Users[_email]
                .MailFolders
                .GetAsync();
            }
        catch (Exception ex)
            {
            Console.WriteLine($"Erro ao listar pastas: {ex.Message}");
            return null;
            }
        }

    public async Task<List<MailFolder>> ListarArvoreDePastasAsync()
        {
        if (_graphClient == null) return new List<MailFolder>();

        try
            {
            List<MailFolder> allFolders = new();
            Console.WriteLine("Buscando a primeira página de pastas raiz...");
            var currentPage = await _graphClient.Users[_email].MailFolders.GetAsync(requestConfiguration =>
                {
                requestConfiguration.QueryParameters.Select = new[]
                    {
                    "id", "displayName", "parentFolderId", "childFolderCount", "totalItemCount", "unreadItemCount"
                    };
                });

            var pageNum = 1;
            while (currentPage?.Value?.Count > 0)
                {
                Console.WriteLine($"Página {pageNum}: Encontradas {currentPage.Value.Count} pastas.");
                allFolders.AddRange(currentPage.Value);
                if (currentPage.OdataNextLink != null)
                    {
                    Console.WriteLine("Há mais pastas. Buscando próxima página...");
                    currentPage =
                        await new MailFoldersRequestBuilder(currentPage.OdataNextLink, _graphClient.RequestAdapter)
                            .GetAsync();
                    pageNum++;
                    }
                else
                    {
                    Console.WriteLine("Não há mais páginas de pastas raiz.");
                    break;
                    }
                }

            Console.WriteLine($"Total de pastas raiz encontradas: {allFolders.Count}");

            foreach (var folder in allFolders)
                if (folder.ChildFolderCount > 0)
                    folder.ChildFolders = await ObterSubpastasRecursivamenteAsync(folder.Id);

            return allFolders;
            }
        catch (Exception ex)
            {
            Console.WriteLine($"Erro ao listar a árvore de pastas: {ex.Message}");
            return new List<MailFolder>();
            }
        }

    private async Task<List<MailFolder>> ObterSubpastasRecursivamenteAsync(string pastaId)
        {
        if (_graphClient == null) return new List<MailFolder>();

        List<MailFolder> allChildFolders = new();
        Console.WriteLine($"Buscando a primeira página de subpastas para a pasta ID: {pastaId}");
        var currentPage = await _graphClient.Users[_email].MailFolders[pastaId].ChildFolders
            .GetAsync(requestConfiguration =>
                {
                requestConfiguration.QueryParameters.Select = new[]
                    {
                    "id", "displayName", "parentFolderId", "childFolderCount", "totalItemCount", "unreadItemCount"
                    };
                });

        var pageNum = 1;
        while (currentPage?.Value?.Count > 0)
            {
            Console.WriteLine($"Página {pageNum} de subpastas: Encontradas {currentPage.Value.Count} pastas.");
            allChildFolders.AddRange(currentPage.Value);
            if (currentPage.OdataNextLink != null)
                {
                Console.WriteLine("Há mais subpastas. Buscando próxima página...");
                currentPage =
                    await new MailFoldersRequestBuilder(currentPage.OdataNextLink, _graphClient.RequestAdapter)
                        .GetAsync();
                pageNum++;
                }
            else
                {
                Console.WriteLine("Não há mais páginas de subpastas.");
                break;
                }
            }

        Console.WriteLine($"Total de subpastas encontradas para a pasta {pastaId}: {allChildFolders.Count}");

        foreach (var subFolder in allChildFolders)
            if (subFolder.ChildFolderCount > 0)
                subFolder.ChildFolders = await ObterSubpastasRecursivamenteAsync(subFolder.Id);

        return allChildFolders;
        }

    public async Task<MessageCollectionResponse?> ListarEmails()
        {
        if (_graphClient == null)
            return null;

        try
            {
            // Buscar emails da caixa de entrada
            var messages = await _graphClient
                .Users[_email]
                .Messages
                .GetAsync(requestConfig =>
                    {
                    requestConfig.QueryParameters.Top = 10; // Os 10 primeiros e-mails
                    requestConfig.QueryParameters.Select =
                        [
                        "id",
                        "internetMessageId",
                        "conversationId",
                        "subject",
                        "bodyPreview",
                        "body",
                        "from",
                        "sender",
                        "toRecipients",
                        "ccRecipients",
                        "bccRecipients",
                        "replyTo",
                        "receivedDateTime",
                        "sentDateTime",
                        "createdDateTime",
                        "lastModifiedDateTime",
                        "isRead",
                        "isDraft",
                        "hasAttachments",
                        "importance",
                        "flag",
                        "categories",
                        "inferenceClassification",
                        "parentFolderId",
                        "webLink",
                        "changeKey",
                        "isDeliveryReceiptRequested",
                        "isReadReceiptRequested"
                        ];
                    requestConfig.QueryParameters.Orderby =
                            ["receivedDateTime DESC"]; // Ordenar por data de recebimento, mais recentes primeiro
                    });

            return messages;
            }
        catch (Exception ex)
            {
            Console.WriteLine($"Erro ao listar emails: {ex.Message}");
            return null;
            }
        }

    public async Task EnviarEmail(string to)
        {
        if (_graphClient == null)
            return;

        try
            {
            var message = new Message
                {
                Subject = "Teste de Email via Graph API",
                Body = new ItemBody
                    {
                    ContentType = BodyType.Html,
                    Content = "<h1>Olá!</h1><p>Este é um email enviado via Microsoft Graph API.</p>"
                    },
                ToRecipients =
                    [
                    new Recipient
                        {
                        EmailAddress = new EmailAddress
                            {
                            Address = to
                            }
                        }
                    ]
                };

            await _graphClient
                .Users[_email]
                .SendMail
                .PostAsync(new SendMailPostRequestBody
                    {
                    Message = message,
                    SaveToSentItems = true
                    });

            Console.WriteLine("Email enviado com sucesso!");
            }
        catch (Exception ex)
            {
            Console.WriteLine($"Erro ao enviar email: {ex.Message}");
            }
        }
    }