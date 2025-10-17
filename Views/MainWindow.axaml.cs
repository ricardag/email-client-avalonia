using Avalonia.Controls;
using Avalonia.Interactivity;
using ClienteEmail.ViewModels;

namespace ClienteEmail.Views;

public partial class MainWindow : Window
    {
    private readonly MainWindowViewModel _viewModel;

    // Parameterless constructor for XAML loader and previewer
    public MainWindow()
        {
        InitializeComponent();
        }

    public MainWindow(MainWindowViewModel viewModel)
        {
        InitializeComponent();

        DataContext = viewModel;
        _viewModel = viewModel;

        Loaded += OnWindowLoaded;
        }

    private async void OnWindowLoaded(object? sender, RoutedEventArgs e)
        {
        if (_viewModel != null) await _viewModel.OnViewLoadedAsync();
        }
    }