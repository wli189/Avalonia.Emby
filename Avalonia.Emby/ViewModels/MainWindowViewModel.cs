using System;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Emby.Models;

namespace Avalonia.Emby.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly StorageService _storageService;
    public ICommand AddAccountCommand { get; }
    public ObservableCollection<AccountViewModel> AccountList { get; } = new();
    public Interaction<AddAccountViewModel, Account?> ShowDialog { get; }

    public MainWindowViewModel()
    {
        _storageService = new StorageService();
        ShowDialog = new Interaction<AddAccountViewModel, Account?>();

        AddAccountCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var addAccountVm = new AddAccountViewModel();
            var result = await ShowDialog.Handle(addAccountVm);

            if (result != null)
            {
                var accountVm = new AccountViewModel(result);
                accountVm.AccountDeleted += OnAccountDeleted;
                AccountList.Add(accountVm);
                await SaveAccounts();
            }
        });

        // Load servers when the view model is created
        _ = LoadAccounts();
    }

    private async void OnAccountDeleted(object? sender, Account account)
    {
        if (sender is AccountViewModel accountVm)
        {
            accountVm.AccountDeleted -= OnAccountDeleted;
            AccountList.Remove(accountVm);
            await SaveAccounts();
        }
    }

    private async Task LoadAccounts()
    {
        var accounts = await _storageService.LoadAccountsAsync();
        foreach (var account in accounts)
        {
            var accountVM = new AccountViewModel(account);
            accountVM.AccountDeleted += OnAccountDeleted;
            AccountList.Add(accountVM);
        }
    }

    private async Task SaveAccounts()
    {
        var accounts = AccountList.Select(vm => vm.Account).ToList();
        await _storageService.SaveAccountsAsync(accounts);
    }
}