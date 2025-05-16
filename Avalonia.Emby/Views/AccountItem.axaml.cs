using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Emby.ViewModels;
using ReactiveUI;
using System.Threading.Tasks;
using Avalonia.Emby.Models;

namespace Avalonia.Emby.Views;

public partial class AccountItem : ReactiveUserControl<AccountViewModel>
{
    public AccountItem()
    {
        InitializeComponent();
        this.WhenActivated(action =>
            action(ViewModel!.ShowDialog.RegisterHandler(DoShowDialogAsync)));
    }

    private async Task DoShowDialogAsync(IInteractionContext<AddAccountViewModel, Account?> interaction)
    {
        var dialog = new AddAccountWindow();
        dialog.DataContext = interaction.Input;

        var result = await dialog.ShowDialog<Account?>((Window)TopLevel.GetTopLevel(this));
        interaction.SetOutput(result);
    }
}