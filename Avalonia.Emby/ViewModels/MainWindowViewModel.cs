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
    public ICommand AddServerCommand { get; }
    public ObservableCollection<ServerViewModel> ServerList { get; } = new();
    public Interaction<AddServerViewModel, Server?> ShowDialog { get; }

    public MainWindowViewModel()
    {
        _storageService = new StorageService();
        ShowDialog = new Interaction<AddServerViewModel, Server?>();

        AddServerCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var viewModel = new AddServerViewModel();
            var result = await ShowDialog.Handle(viewModel);

            if (result != null)
            {
                var serverVm = new ServerViewModel(result);
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
            ServerList.Add(new ServerViewModel(server));
        }
    }

    private async Task SaveServers()
    {
        var servers = ServerList.Select(vm => vm.Server).ToList();
        await _storageService.SaveServersAsync(servers);
    }
}