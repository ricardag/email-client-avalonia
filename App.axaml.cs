using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using ClienteEmail.Classes;
using ClienteEmail.Data;
using ClienteEmail.Services;
using ClienteEmail.ViewModels;
using ClienteEmail.Views;
using FluentNHibernate;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using SQLitePCL;
using Environment = System.Environment;

namespace ClienteEmail;

public class App : Application
    {
    private static IServiceProvider? Services { get; set; }

    public override void Initialize()
        {
        AvaloniaXamlLoader.Load(this);

        // Inicializar SQLite
        Batteries.Init();
        }

    public override void OnFrameworkInitializationCompleted()
        {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            // Configurar DI
            var services = new ServiceCollection();

            // Caminho do banco SQLite
            const string databaseName = "clienteemail.db";
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "ClienteEmail");

            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);

            var dbPath = Path.Combine(appFolder, databaseName);

            // Configurar FluentNHibernate/SQLite com driver E dialect customizados
            var nhConfig = new Configuration
                {
                Properties =
                    {
                    // Propriedades essenciais com DRIVER e DIALECT CUSTOMIZADOS
                    [NHibernate.Cfg.Environment.ConnectionString] = $"Data Source={dbPath}",
                    [NHibernate.Cfg.Environment.Dialect] = typeof(MicrosoftDataSqliteDialect).AssemblyQualifiedName,
                    [NHibernate.Cfg.Environment.ConnectionDriver] =
                        typeof(MicrosoftDataSqliteDriver).AssemblyQualifiedName,
                    [NHibernate.Cfg.Environment.ShowSql] = "false",
                    [NHibernate.Cfg.Environment.FormatSql] = "false"
                    }
                };

            // Adicionar mappings do FluentNHibernate
            var model = new PersistenceModel();
            model.AddMappingsFromAssembly(Assembly.GetExecutingAssembly());
            model.Configure(nhConfig);

            // SchemaUpdate - Atualiza apenas o necessário (SEM perder dados)
            var schemaUpdate = new SchemaUpdate(nhConfig);
            schemaUpdate.Execute(
                false, // Não mostra SQL executado
                true); // Executa no banco

            // Build session factory
            var sqliteSessionFactory = nhConfig.BuildSessionFactory();
            services.AddSingleton(sqliteSessionFactory);
            services.AddScoped(factory => sqliteSessionFactory.OpenSession());

            // Registrar Services
            services.AddSingleton<IWindowService, WindowService>();

            // Registrar Views
            services.AddTransient<MainWindow>();

            // Registrar ViewModels
            services.AddTransient<MainWindowViewModel>();

            // Registra a instância de configuração
            services.AddSingleton<IConfiguration>(config);

            Services = services.BuildServiceProvider();

            // Obter MainWindow do DI
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
            }

        base.OnFrameworkInitializationCompleted();
        }

    private void DisableAvaloniaDataAnnotationValidation()
        {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove) BindingPlugins.DataValidators.Remove(plugin);
        }
    }