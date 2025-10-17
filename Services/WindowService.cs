using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using ClienteEmail.Models;
using ClienteEmail.ViewModels;
using ClienteEmail.Views;

namespace ClienteEmail.Services;

public class WindowService : IWindowService
    {
    public Task<TResult> ShowDialogAsync<TResult>(Window owner, object viewModel)
        {
        var view = GetViewForViewModel(viewModel);
        view.DataContext = viewModel;

        if (viewModel is ContaEmailViewModel vm)
            {
            vm.OnSave += conta =>
                {
                if (typeof(TResult).IsAssignableFrom(typeof(ContaEmail)))
                    view.Close((TResult)(object)conta);
                else
                    view.Close();
                };

            vm.OnCancel += () => { view.Close(default); };
            }

        return view.ShowDialog<TResult>(owner);
        }

    private Window GetViewForViewModel(object viewModel)
        {
        return viewModel switch
            {
            ContaEmailViewModel => new ContaEmailView(),
            _ => throw new ArgumentException($"No view found for view model of type {viewModel.GetType().FullName}")
            };
        }
    }