using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Emby.Models;
using Avalonia.Emby.Views;
using ReactiveUI;

namespace Avalonia.Emby.ViewModels;

public class LibraryWindowViewModel
{
    private readonly Account _account;
    public ICommand BackCommand { get; }
    public LibraryWindowViewModel(Account account)
    {
        _account = account;

        BackCommand = ReactiveCommand.Create<Window>(window =>
        {
            var mainWindow = new Views.MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
            mainWindow.Show();
            window.Close();
        });
    }
}