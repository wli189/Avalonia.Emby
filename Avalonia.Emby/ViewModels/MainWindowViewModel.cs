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
    private ViewModelBase _currentView;
    private bool _isLibraryViewVisible;

    public ICommand AddAccountCommand { get; }
    public ObservableCollection<AccountViewModel> AccountList { get; } = new();
    public Interaction<AddAccountViewModel, Account?> ShowDialog { get; }

    public ViewModelBase CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    public bool IsLibraryViewVisible
    {
        get => _isLibraryViewVisible;
        set => this.RaiseAndSetIfChanged(ref _isLibraryViewVisible, value);
    }

    public MainWindowViewModel()
    {
        _storageService = new StorageService();
        ShowDialog = new Interaction<AddAccountViewModel, Account?>();
        _currentView = this;

        AddAccountCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var addAccountVm = new AddAccountViewModel();
            var result = await ShowDialog.Handle(addAccountVm);

            if (result != null)
            {
                var accountVm = new AccountViewModel(result);
                accountVm.AccountDeleted += OnAccountDeleted;
                accountVm.LibraryRequested += OnLibraryRequested;
                AccountList.Add(accountVm);
                await SaveAccounts();
            }
        });

        // Load servers when the view model is created
        _ = LoadAccounts();
    }

    private void OnLibraryRequested(object? sender, Account account)
    {
        var libraryViewModel = new LibraryViewModel(account);
        libraryViewModel.BackRequested += OnLibraryBackRequested;
        CurrentView = libraryViewModel;
        IsLibraryViewVisible = true;
    }

    private void OnLibraryBackRequested(object? sender, EventArgs e)
    {
        if (sender is LibraryViewModel libraryViewModel)
        {
            libraryViewModel.BackRequested -= OnLibraryBackRequested;
        }
        CurrentView = this;
        IsLibraryViewVisible = false;
    }

    private async void OnAccountDeleted(object? sender, Account account)
    {
        if (sender is AccountViewModel accountVm)
        {
            accountVm.AccountDeleted -= OnAccountDeleted;
            accountVm.LibraryRequested -= OnLibraryRequested;
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
            accountVM.LibraryRequested += OnLibraryRequested;
            AccountList.Add(accountVM);
        }
    }

    private async Task SaveAccounts()
    {
        var accounts = AccountList.Select(vm => vm.Account).ToList();
        await _storageService.SaveAccountsAsync(accounts);
    }
}