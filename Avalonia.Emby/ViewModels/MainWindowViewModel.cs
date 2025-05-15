using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;

namespace Avalonia.Emby.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ICommand AddServerCommand { get; }
    
    public Interaction<AddServerViewModel, ServerViewModel?> ShowDialog { get; }
    public MainWindowViewModel()
    {
        ShowDialog = new Interaction<AddServerViewModel, ServerViewModel?>();
        
        AddServerCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var server = new AddServerViewModel();
            
            var result = await ShowDialog.Handle(server);
        });
    }
}