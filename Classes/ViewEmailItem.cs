using System;

namespace ClienteEmail.ViewModels;

public class ViewEmailItem
    {
    public int Id { get; set; } = 0;

    // Avatar/Inicial
    public string FromInitial => GetInitial(FromName);
    public string CorConta { get; set; }
    public string NomeConta { get; set; }

    // Dados do email
    public string ToName { get; set; }
    public string FromName { get; set; }
    public string FromEmail { get; set; }
    public string Subject { get; set; }
    public string BodyPreview { get; set; }
    public string Body { get; set; }
    public DateTime ReceivedDateTime { get; set; }
    public bool IsRead { get; set; }
    public bool HasAttachments { get; set; }

    // Data formatada para exibição
    public string ReceivedTime => FormatDateTime(ReceivedDateTime.ToLocalTime());

    // Extrair inicial do nome
    private string GetInitial(string name)
        {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        return name.Trim()[0].ToString().ToUpper();
        }

    // Formatar data/hora (hoje = hora, ontem/antes = data)
    private string FormatDateTime(DateTime dt)
        {
        var now = DateTime.Now;
        var diff = now - dt;

        if (dt.Date == now.Date)
            // Hoje - mostrar hora
            return dt.ToString("HH:mm");

        if (diff.TotalDays < 7)
            // Esta semana - mostrar dia da semana
            return dt.ToString("ddd");

        if (dt.Year == now.Year)
            // Este ano - mostrar dia/mês
            return dt.ToString("dd/MM");

        // Ano diferente - mostrar data completa
        return dt.ToString("dd/MM/yy");
        }
    }