using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AudioUnitWrapper;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;

namespace ClienteEmail.Integrations;

public class GmailIntegration
    {
    private readonly IConfiguration _configuration;
    private readonly string _gmailScope = "https://mail.google.com/";
    public string? Token { get; set; }

    public GmailIntegration(IConfiguration configuration)
        {
        _configuration = configuration;
        }

    public async Task<GmailService> LoginGmailAsync()
        {
        // escopos: ajuste conforme sua necessidade
        var scopes = new[]
            {
            _gmailScope
            };

        var googleClientSecrets = new ClientSecrets
            {
            ClientId = _configuration.GetValue<string>("GMail:ClientId") ?? throw new InvalidOperationException(),
            ClientSecret = _configuration.GetValue<string>("GMail:ClientSecret") ??
                           throw new InvalidOperationException()
            };

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            googleClientSecrets,
            scopes,
            "user", // identificador local do perfil
            CancellationToken.None,
            new FileDataStore("token.json", true)
            );

        var gmailService = new GmailService(new BaseClientService.Initializer
            {
            HttpClientInitializer = credential,
            ApplicationName = "MeuClienteGmail"
            });

        return gmailService;
        }

    public async Task<ListMessagesResponse?> ListMessagesByLabelAsync(GmailService svc, string labelId,
        int pageSize = 50, string? query = null)
        {
        var req = svc.Users.Messages.List("me");
        req.LabelIds = labelId; // ex.: "INBOX" ou id de label do usuário
        req.Q = query; // ex.: "is:unread newer_than:7d"
        req.MaxResults = pageSize;

        var res = await req.ExecuteAsync();
        return res;
        }

    public async
        Task<(string Subject, string From, string Html, string Text, List<(string filename, string id)> Attachments)>
        ReadMessageAsync(GmailService svc, string messageId)
        {
        var get = svc.Users.Messages.Get("me", messageId);
        get.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
        var full = await get.ExecuteAsync();

        string hdr(string name)
            {
            return full.Payload?.Headers?.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?.Value ?? "";
            }

        string html = "", text = "";
        var atts = new List<(string, string)>();

        void walkParts(IList<MessagePart> parts)
            {
            foreach (var p in parts)
                {
                if (p.MimeType == "text/html" && p.Body?.Data != null)
                    html = Base64UrlDecode(p.Body.Data);
                else if (p.MimeType == "text/plain" && p.Body?.Data != null) text = Base64UrlDecode(p.Body.Data);

                if (!string.IsNullOrEmpty(p.Filename) && p.Body?.AttachmentId != null)
                    atts.Add((p.Filename, p.Body.AttachmentId));

                if (p.Parts != null && p.Parts.Count > 0) walkParts(p.Parts);
                }
            }

        if (full.Payload?.Parts != null)
            walkParts(full.Payload.Parts);
        else if (full.Payload?.Body?.Data != null) text = Base64UrlDecode(full.Payload.Body.Data);

        return (hdr("Subject"), hdr("From"), html, text, atts);
        }

    private static string Base64UrlDecode(string s)
        {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
            {
            case 2: s += "=="; break;
            case 3: s += "="; break;
            }

        return Encoding.UTF8.GetString(Convert.FromBase64String(s));
        }

    // baixar um anexo
    private static async Task<byte[]> DownloadAttachmentAsync(GmailService svc, string messageId, string attachmentId)
        {
        var att = await svc.Users.Messages.Attachments.Get("me", messageId, attachmentId).ExecuteAsync();
        var data = att.Data.Replace('-', '+').Replace('_', '/');
        switch (data.Length % 4)
            {
            case 2: data += "=="; break;
            case 3: data += "="; break;
            }

        return Convert.FromBase64String(data);
        }

    // lista todas as labels
    public async Task<IList<Label>> ListLabelsAsync(GmailService svc)
        {
        var res = await svc.Users.Labels.List("me").ExecuteAsync();
        return res.Labels ?? new List<Label>();
        }

    // pega detalhes/contadores de uma label específica (msgs/threads lidas/não-lidas)
    private static async Task<Label> GetLabelAsync(GmailService svc, string labelId)
        {
        return await svc.Users.Labels.Get("me", labelId).ExecuteAsync();
        }

    public async Task<Profile> GetMyProfile(GmailService svc)
        {
        var profile = await svc.Users.GetProfile("me").ExecuteAsync();
        return profile;
        }
    }