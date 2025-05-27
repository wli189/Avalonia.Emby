using System;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Emby.Models;
using ReactiveUI;

namespace Avalonia.Emby.ViewModels;

public class LibraryViewModel : ViewModelBase
{
    private readonly Account _account;
    public event EventHandler? BackRequested;
    public ICommand BackCommand { get; }

    public LibraryViewModel(Account account)
    {
        _account = account;

        BackCommand = ReactiveCommand.Create(() =>
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        });
    }
}