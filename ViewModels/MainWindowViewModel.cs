using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using ClienteEmail.Classes;
using ClienteEmail.Integrations;
using ClienteEmail.Models;
using ClienteEmail.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;

namespace ClienteEmail.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
    {
    private readonly ISession _session;
    private readonly IWindowService _windowService;
    private readonly IConfiguration _configuration;

    [ObservableProperty] private List<Relationship> _contas = [];
    [ObservableProperty] private Relationship? _contaSelecionada;
    [ObservableProperty] private string _mensagemErro = "";

    [ObservableProperty] private bool _gridVisivel = true;

    [ObservableProperty] private bool _podeAtualizar;

    [ObservableProperty] private ViewEmailItem _selectedItem = new();

    public MainWindowViewModel()
        {
        }

    public MainWindowViewModel(ISession session, IWindowService windowService, IConfiguration configuration)
        {
        _session = session;
        _windowService = windowService;
        _configuration = configuration;

        // Pega a Lista de contas existentes
        Contas = _session.Query<ContaEmail>()
            .Select(p => new Relationship { Id = p.Id, Nome = p.Nome! })
            .ToList();

        ContaSelecionada = Contas.FirstOrDefault();
        }

    public ObservableCollection<ViewEmailItem> Items { get; } = [];

    // Método chamado quando a View é carregada
    public async Task OnViewLoadedAsync()
        {
        await AtualizaListaEmails();
        }

    partial void OnContaSelecionadaChanged(Relationship? value)
        {
        PodeAtualizar = value != null && value.Id != 0;
        }

    partial void OnSelectedItemChanged(ViewEmailItem? value)
        {
        if (value != null)
            SelectedItem = value;
        }

    private async Task AtualizaListaEmails()
        {
        Items.Clear();
        SelectedItem = new ViewEmailItem();
        GridVisivel = false;

        // Agora busca do banco e monta a lista de e-mails
        var lista = await _session
            .Query<Email>()
            .OrderByDescending(e => e.ReceivedDateTime)
            .ToListAsync();

        foreach (var email in lista)
            {
            var toName = "";
            if (!string.IsNullOrWhiteSpace(email.ToRecipients))
                {
                var to = JsonConvert.DeserializeObject<List<dynamic>>(email.ToRecipients)!.FirstOrDefault();
                toName = $"{to?.Name} <{to?.Address}>";
                }

            string[] coresConta = ["Blue", "Green", "Red", "Yellow"];

            Items.Add(new ViewEmailItem
                {
                Id = email.Id,
                ToName = toName,
                CorConta = coresConta[(email.Conta?.Id ?? 0) % coresConta.Length],
                NomeConta = email.Conta?.Nome,
                FromName = email.FromName ?? email.FromAddress ?? "Desconhecido",
                FromEmail = email.FromAddress ?? "Desconhecido",
                Subject = $"#{email.Id} - {email.Subject ?? "(Sem assunto)"}",
                BodyPreview = email.BodyPreview ?? "",
                Body = email.BodyContent ?? "(Sem conteúdo)",
                ReceivedDateTime = email.ReceivedDateTime,
                IsRead = email.IsRead,
                HasAttachments = email.HasAttachments
                });
            }

        // Seleciona o primeiro da lista
        SelectedItem = Items.FirstOrDefault()!;
        GridVisivel = SelectedItem != null;
        }

    [RelayCommand]
    private async Task Processar()
        {
        Items.Clear();
        SelectedItem = new ViewEmailItem();
        GridVisivel = false;

        // Recupera a conta
        var contaEmail = _session.Get<ContaEmail>(ContaSelecionada?.Id);
        if (contaEmail == null)
            return;

        var cliente = new Office365Integration(_configuration, contaEmail.EmailAddress!);
        await cliente.Inicializar();

        var pastas = await cliente.ListarArvoreDePastasAsync();

        var messages = await cliente.ListarEmails();

        if (messages?.Value != null)
            {
            using (var transaction = _session.BeginTransaction())
                {
                foreach (var msg in messages.Value)
                    {
                    var email = _session
                        .Query<Email>()
                        .FirstOrDefault(e =>
                            e.Conta != null && e.Conta.Id == contaEmail.Id && e.MessageId == msg.InternetMessageId);

                    if (email == null)
                        {
                        email = new Email();
                        email = email.Office365ToEmail(msg, contaEmail);
                        }
                    else
                        {
                        // Atualiza alguns campos do e-mail
                        email.IsRead = msg.IsRead ?? false;
                        email.IsDraft = msg.IsDraft ?? false;
                        email.HasAttachments = msg.HasAttachments ?? false;
                        email.Importance = msg.Importance?.ToString() ?? "Normal";
                        email.FlagStatus = msg.Flag?.FlagStatus?.ToString() ?? "NotFlagged";
                        }

                    await _session.SaveOrUpdateAsync(email);
                    }

                await transaction.CommitAsync();
                }

            await AtualizaListaEmails();
            }
        //Items.Add("Nenhum email encontrado.");
        }

    [RelayCommand]
    private async Task GerenciarContasEmail(Window owner)
        {
        var viewModel = new ContaEmailViewModel(_session);

        var result = await _windowService.ShowDialogAsync<ContaEmail>(owner, viewModel);

        if (result != null)
            using (var transaction = _session.BeginTransaction())
                {
                await _session.SaveOrUpdateAsync(result);
                await transaction.CommitAsync();
                }
        }
    }