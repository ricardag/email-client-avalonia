using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using ClienteEmail.Classes;
using ClienteEmail.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryIoc.ImTools;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using NHibernate;
using static System.Text.RegularExpressions.Regex;
using Action = System.Action;

namespace ClienteEmail.ViewModels;

public partial class ContaEmailViewModel : ObservableObject
    {
    private readonly ISession _session;

    [ObservableProperty] private List<Relationship> _contas = [];
    [ObservableProperty] private Relationship? _contaSelecionada;
    [ObservableProperty] private string _mensagemErro = "";

    /// <summary>
    ///   Formulário "reativo"
    /// </summary>
    public partial class _Form : ObservableObject
        {
        public int Id { get; set; }

        [ObservableProperty] private TipoContaEmail _tipoConta;

        [ObservableProperty] private string? _nome;

        [ObservableProperty] private string? _userName;

        [ObservableProperty] private string? _emailAddress;
        }

    private _Form? _form = null;

    public _Form Form
        {
        get => _form;
        set
            {
            // Remove o evento do objeto antigo
            if (_form != null)
                _form.PropertyChanged -= OnFormPropertyChanged;

            SetProperty(ref _form, value);

            // Adiciona o evento ao novo objeto
            if (_form != null)
                _form.PropertyChanged += OnFormPropertyChanged;
            }
        }


    public IRelayCommand SalvarCommand { get; }
    public IRelayCommand ApagarCommand { get; }

    private void OnFormPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        (SalvarCommand as RelayCommand)?.NotifyCanExecuteChanged();
        (ApagarCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

    private bool PodeSalvar()
        {
        return !string.IsNullOrWhiteSpace(Form.Nome) &&
               !string.IsNullOrWhiteSpace(Form.UserName) &&
               !string.IsNullOrWhiteSpace(Form.EmailAddress) &&
               MyRegex().Match(Form.EmailAddress).Success &&
               Form.TipoConta != TipoContaEmail.Selecione;
        }

    // Parameterless constructor for XAML designer
    public ContaEmailViewModel()
        {
        Console.WriteLine("Construtor base");

        _form = new _Form();
        _form.PropertyChanged += OnFormPropertyChanged;

        SalvarCommand = new RelayCommand(Salvar, PodeSalvar);
        ApagarCommand = new RelayCommand(Apagar, PodeSalvar);
        }

    public ContaEmailViewModel(ISession session) : this() // Chama o construtor base
        {
        _session = session;

        // Pega a Lista de contas existentes
        Contas = _session.Query<ContaEmail>()
            .Select(p => new Relationship { Id = p.Id, Nome = p.Nome! })
            .ToList();

        // Adiciona uma "Nova conta" no inicio da lista
        Contas.Insert(0, new Relationship { Id = 0, Nome = "Nova conta" });

        ContaSelecionada = Contas.FirstOrDefault(p => p.Id != 0);
        }

    public List<TipoContaEmail> TiposConta { get; } =
        Enum.GetValues(typeof(TipoContaEmail)).Cast<TipoContaEmail>().ToList();

    public event Action<ContaEmail> OnSave;
    public event Action OnCancel;

    partial void OnContaSelecionadaChanged(Relationship value)
        {
        MensagemErro = "";

        if (value.Id == 0)
            {
            NovaConta();
            return;
            }

        if (_form == null)
            return;

        var conta = _session
            .Get<ContaEmail>(value.Id);

        if (conta == null) return;

        _form.EmailAddress = conta.EmailAddress;
        _form.Id = conta.Id;
        _form.Nome = conta.Nome;
        _form.TipoConta = conta.TipoConta;
        _form.UserName = conta.UserName;
        }

    [RelayCommand]
    private void NovaConta()
        {
        if (_form != null)
            {
            _form.Id = 0;
            _form.Nome = "";
            _form.TipoConta = TipoContaEmail.Selecione;
            _form.UserName = "";
            _form.EmailAddress = "";
            }

        ContaSelecionada = Contas.FirstOrDefault(p => p.Id == 0);
        }

    private void Apagar()
        {
        if (_form == null)
            return;

        MensagemErro = $"Apagando conta {_form.Id}-{_form.Nome}";

        var conta = _session.Get<ContaEmail>(_form.Id);
        if (conta == null)
            {
            MensagemErro = $"Conta {_form.Id}-{_form.Nome} não encontrada";
            return;
            }

        using (var transaction = _session.BeginTransaction())
            {
            _session.Delete(conta);
            transaction.Commit();
            }

        OnCancel?.Invoke();
        }

    private void Salvar()
        {
        if (_form == null)
            return;

        MensagemErro = "";

        // Verificar se já existe uma conta com este nome
        var x = _session.Query<ContaEmail>()
            .Where(c => c.Nome.ToLower() == _form.Nome.ToLower() && _form.Id != c.Id)
            .ToList();

        if (x.Any())
            {
            // Dá um alerta e termina.
            MensagemErro = "Já existe uma conta com este nome!";
            return;
            }

        var conta = new ContaEmail
            {
            Id = 0
            };

        if (_form.Id != 0) conta = _session.Get<ContaEmail>(_form.Id);

        // Salvo a conta aqui mesmo
        conta.Nome = _form.Nome;
        conta.TipoConta = _form.TipoConta;
        conta.UserName = _form.UserName;
        conta.EmailAddress = _form.EmailAddress;

        using (var transaction = _session.BeginTransaction())
            {
            _session.SaveOrUpdate(conta);
            transaction.Commit();
            }

        OnCancel?.Invoke();
        }

    [RelayCommand]
    private void Cancelar()
        {
        OnCancel?.Invoke();
        }

    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
    }