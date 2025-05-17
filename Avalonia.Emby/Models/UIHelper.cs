using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Animation;

namespace Avalonia.Emby.Models;

public static class UIHelper
{
    public static async Task ShowFlyoutMessage(string message, Window window)
    {
        var flyout = new Flyout
        {
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 300,
            },
            Placement = PlacementMode.Bottom,
            ShowMode = FlyoutShowMode.Transient,
            VerticalOffset = 5,
        };

        var content = (TextBlock)flyout.Content;
        content.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = TextBlock.OpacityProperty,
                Duration = TimeSpan.FromSeconds(0.2)
            }
        };

        content.Opacity = 0;
        flyout.ShowAt(window);
        content.Opacity = 1;
        await Task.Delay(1000);
        content.Opacity = 0;
        await Task.Delay(200);
        flyout.Hide();
    }
}