using System;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Emby.Models;
using Avalonia.Emby.Services;

namespace Avalonia.Emby.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly StorageService _storageService;
    public ICommand AddAccountCommand { get; }
    public ObservableCollection<AccountViewModel> ServerList { get; } = new();
    public Interaction<AddAccountViewModel, Account?> ShowDialog { get; }

    public MainWindowViewModel()
    {
        _storageService = new StorageService();
        ShowDialog = new Interaction<AddAccountViewModel, Account?>();

        AddAccountCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var viewModel = new AddAccountViewModel();
            var result = await ShowDialog.Handle(viewModel);

            if (result != null)
            {
                var serverVm = new AccountViewModel(result);
                ServerList.Add(serverVm);
                await SaveServers();
            }
        });

        // Load servers when the view model is created
        _ = LoadServers();
    }

    private async Task LoadServers()
    {
        var servers = await _storageService.LoadServersAsync();
        foreach (var server in servers)
        {
            ServerList.Add(new AccountViewModel(server));
        }
    }

    private async Task SaveServers()
    {
        var servers = ServerList.Select(vm => vm.Account).ToList();
        await _storageService.SaveServersAsync(servers);
    }
}