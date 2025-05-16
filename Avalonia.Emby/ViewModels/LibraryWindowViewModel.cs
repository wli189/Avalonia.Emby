using Avalonia.Emby.Models;

namespace Avalonia.Emby.ViewModels;

public class LibraryWindowViewModel
{
    private readonly Account _account;

    public LibraryWindowViewModel(Account account)
    {
        _account = account;
    }
}