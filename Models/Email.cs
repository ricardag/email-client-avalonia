using System;
using System.Collections.Generic;
using System.Linq;
using FluentNHibernate.Mapping;
using Microsoft.Graph.Models;
using Newtonsoft.Json;

namespace ClienteEmail.Models;

public class Email
    {
    // Chave primária
    public virtual int Id { get; set; }

    // Conta do usuário
    public virtual ContaEmail? Conta { get; set; }

    // Identificadores únicos
    public virtual string? MessageId { get; set; } // InternetMessageId
    public virtual string? GraphId { get; set; } // Id do Graph API
    public virtual string? ConversationId { get; set; }
    public virtual string? ChangeKey { get; set; }

    // Datas
    public virtual DateTime ReceivedDateTime { get; set; }
    public virtual DateTime? SentDateTime { get; set; }
    public virtual DateTime? CreatedDateTime { get; set; }
    public virtual DateTime? LastModifiedDateTime { get; set; }

    // Remetente
    public virtual string? FromAddress { get; set; }
    public virtual string? FromName { get; set; }
    public virtual string? SenderAddress { get; set; }
    public virtual string? SenderName { get; set; }

    // Destinatários (JSON ou separados por vírgula)
    public virtual string? ToRecipients { get; set; } // JSON array
    public virtual string? CcRecipients { get; set; } // JSON array
    public virtual string? BccRecipients { get; set; } // JSON array
    public virtual string? ReplyTo { get; set; } // JSON array

    // Conteúdo
    public virtual string? Subject { get; set; }
    public virtual string? BodyPreview { get; set; }
    public virtual string? BodyContent { get; set; }
    public virtual string? BodyContentType { get; set; } // Text, Html

    // Status e Flags
    public virtual bool IsRead { get; set; }
    public virtual bool IsDraft { get; set; }
    public virtual bool HasAttachments { get; set; }
    public virtual string? Importance { get; set; } // Low, Normal, High
    public virtual string? FlagStatus { get; set; } // NotFlagged, Complete, Flagged

    // Recibos
    public virtual bool IsDeliveryReceiptRequested { get; set; }
    public virtual bool IsReadReceiptRequested { get; set; }

    // Categorias e Classificação
    public virtual string? Categories { get; set; } // JSON array
    public virtual string InferenceClassification { get; set; } // Focused, Other

    // Pastas
    public virtual string? ParentFolderId { get; set; }

    // Links
    public virtual string? WebLink { get; set; }

    // Headers (JSON)
    public virtual string? InternetMessageHeaders { get; set; }

    // Anexos (JSON com informações básicas)
    public virtual string? AttachmentsInfo { get; set; }

    // Extras
    public virtual int? ConversationIndex { get; set; }

    public virtual Email Office365ToEmail(Message message, ContaEmail conta)
        {
        return new Email
            {
            // Identificadores
            MessageId = message.InternetMessageId ?? message.Id ?? Guid.NewGuid().ToString(),
            GraphId = message.Id,
            ConversationId = message.ConversationId,
            ChangeKey = message.ChangeKey,

            // Conta
            Conta = conta,

            // Datas
            ReceivedDateTime = message.ReceivedDateTime?.DateTime ?? DateTime.UtcNow,
            SentDateTime = message.SentDateTime?.DateTime,
            CreatedDateTime = message.CreatedDateTime?.DateTime,
            LastModifiedDateTime = message.LastModifiedDateTime?.DateTime,

            // Remetente
            FromAddress = message.From?.EmailAddress?.Address ?? "",
            FromName = message.From?.EmailAddress?.Name ?? "",
            SenderAddress = message.Sender?.EmailAddress?.Address ?? "",
            SenderName = message.Sender?.EmailAddress?.Name ?? "",

            // Destinatários (como JSON)
            ToRecipients = SerializeOffice365Recipients(message.ToRecipients),
            CcRecipients = SerializeOffice365Recipients(message.CcRecipients),
            BccRecipients = SerializeOffice365Recipients(message.BccRecipients),
            ReplyTo = SerializeOffice365Recipients(message.ReplyTo),

            // Conteúdo
            Subject = message.Subject ?? "(Sem assunto)",
            BodyPreview = message.BodyPreview ?? "",
            BodyContent = message.Body?.Content ?? "",
            BodyContentType = message.Body?.ContentType?.ToString() ?? "Text",

            // Status
            IsRead = message.IsRead ?? false,
            IsDraft = message.IsDraft ?? false,
            HasAttachments = message.HasAttachments ?? false,
            Importance = message.Importance?.ToString() ?? "Normal",
            FlagStatus = message.Flag?.FlagStatus?.ToString() ?? "NotFlagged",

            // Recibos
            IsDeliveryReceiptRequested = message.IsDeliveryReceiptRequested ?? false,
            IsReadReceiptRequested = message.IsReadReceiptRequested ?? false,

            // Categorias
            Categories = SerializeStringArray(message.Categories),
            InferenceClassification = message.InferenceClassification?.ToString(),

            // Pastas e Links
            ParentFolderId = message.ParentFolderId,
            WebLink = message.WebLink,

            // Headers
            InternetMessageHeaders = SerializeOffice365Headers(message.InternetMessageHeaders),

            // Anexos (informações básicas)
            AttachmentsInfo = SerializeOffice365Attachments(message.Attachments),

            // Extras
            ConversationIndex = null // Se precisar, pode adicionar lógica aqui
            };
        }

    private static string SerializeOffice365Recipients(IEnumerable<Recipient> recipients)
        {
        if (recipients == null || !recipients.Any())
            return "[]";

        var list = recipients.Select(r => new
            {
            Address = r.EmailAddress?.Address ?? "",
            Name = r.EmailAddress?.Name ?? ""
            }).ToList();

        return JsonConvert.SerializeObject(list);
        }

    private static string SerializeStringArray(IEnumerable<string>? items)
        {
        if (items == null || !items.Any())
            return "[]";

        return JsonConvert.SerializeObject(items);
        }

    private static string SerializeOffice365Headers(IEnumerable<InternetMessageHeader>? headers)
        {
        if (headers == null || !headers.Any())
            return "[]";

        var list = headers.Select(h => new
            {
            Name = h.Name ?? "",
            Value = h.Value ?? ""
            }).ToList();

        return JsonConvert.SerializeObject(list);
        }

    private static string SerializeOffice365Attachments(IEnumerable<Attachment>? attachments)
        {
        if (attachments == null || !attachments.Any())
            return "[]";

        var list = attachments.Select(a => new
            {
            Id = a.Id ?? "",
            Name = a.Name ?? "",
            ContentType = a.ContentType ?? "",
            Size = a.Size ?? 0,
            IsInline = a.IsInline ?? false
            }).ToList();

        return JsonConvert.SerializeObject(list);
        }
    }

public class EmailMap : ClassMap<Email>
    {
    public EmailMap()
        {
        Table("emails");

        // Chave primária
        Id(x => x.Id).GeneratedBy.Identity();

        // Identificadores
        Map(x => x.MessageId).Length(500).Not.Nullable().UniqueKey("UK_Email_MessageId_Conta");
        Map(x => x.GraphId).Length(200);
        Map(x => x.ConversationId).Length(200);
        Map(x => x.ChangeKey).Length(200);

        // Datas
        Map(x => x.ReceivedDateTime).Not.Nullable().Index("idx_received");
        Map(x => x.SentDateTime);
        Map(x => x.CreatedDateTime);
        Map(x => x.LastModifiedDateTime);

        // Remetente
        Map(x => x.FromAddress).Length(200);
        Map(x => x.FromName).Length(200);
        Map(x => x.SenderAddress).Length(200);
        Map(x => x.SenderName).Length(200);

        // Destinatários (TEXT para JSON)
        Map(x => x.ToRecipients).CustomSqlType("TEXT");
        Map(x => x.CcRecipients).CustomSqlType("TEXT");
        Map(x => x.BccRecipients).CustomSqlType("TEXT");
        Map(x => x.ReplyTo).CustomSqlType("TEXT");

        // Conteúdo
        Map(x => x.Subject).Length(1000);
        Map(x => x.BodyPreview).Length(2000);
        Map(x => x.BodyContent).CustomSqlType("TEXT");
        Map(x => x.BodyContentType).Length(20);

        // Status
        Map(x => x.IsRead).Not.Nullable().Index("idx_isread");
        Map(x => x.IsDraft).Not.Nullable();
        Map(x => x.HasAttachments).Not.Nullable();
        Map(x => x.Importance).Length(20);
        Map(x => x.FlagStatus).Length(20);

        // Recibos
        Map(x => x.IsDeliveryReceiptRequested).Not.Nullable();
        Map(x => x.IsReadReceiptRequested).Not.Nullable();

        // Categorias
        Map(x => x.Categories).CustomSqlType("TEXT");
        Map(x => x.InferenceClassification).Length(50);

        // Pastas e Links
        Map(x => x.ParentFolderId).Length(200);
        Map(x => x.WebLink).Length(500);

        // Headers e Anexos
        Map(x => x.InternetMessageHeaders).CustomSqlType("TEXT");
        Map(x => x.AttachmentsInfo).CustomSqlType("TEXT");

        // Extras
        Map(x => x.ConversationIndex);

        // Conta
        References(x => x.Conta).Column("conta_id").Not.Nullable().UniqueKey("UK_Email_MessageId_Conta");
        }
    }