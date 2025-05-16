using Avalonia.ReactiveUI;
using Avalonia.Emby.ViewModels;
using ReactiveUI;
using System.Threading.Tasks;
using Avalonia.Emby.Models;

namespace Avalonia.Emby.Views;
public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        this.WhenActivated(action =>
            action(ViewModel!.ShowDialog.RegisterHandler(DoShowDialogAsync)));
    }

    private async Task DoShowDialogAsync(IInteractionContext<AddAccountViewModel, Account?> interaction)
    {
        var dialog = new AddAccountWindow();
        dialog.DataContext = interaction.Input;

        var result = await dialog.ShowDialog<Account?>(this);
        interaction.SetOutput(result);
    }
}