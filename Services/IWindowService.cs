using System.Threading.Tasks;
using Avalonia.Controls;

namespace ClienteEmail.Services;

public interface IWindowService
    {
    Task<TResult> ShowDialogAsync<TResult>(Window owner, object viewModel);
    }