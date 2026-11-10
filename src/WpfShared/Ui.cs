using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GamesLab4.WpfShared;

internal static class Ui
{
    public static Brush Brush(string color) => (Brush)new BrushConverter().ConvertFromString(color)!;
    public static TextBlock Text(string value, double size = 14, string color = "#0F172A") =>
        new() { Text = value, FontSize = size, Foreground = Brush(color), TextWrapping = TextWrapping.Wrap };
    public static Border Card(UIElement child) =>
        new() { Child = child, Background = Brushes.White, Padding = new Thickness(20), CornerRadius = new CornerRadius(14) };

    public static Button Button(string label, RoutedEventHandler handler, bool secondary = false)
    {
        var button = new Button
        {
            Content = label, FontSize = 14, Padding = new Thickness(14, 9, 14, 9),
            Margin = new Thickness(0, 0, 8, 8), Background = Brush(secondary ? "#E2E8F0" : "#4F46E5"),
            Foreground = secondary ? Brush("#0F172A") : Brushes.White, BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        button.Click += handler;
        return button;
    }
    public static TextBlock Stat(Panel panel, string label)
    {
        panel.Children.Add(Text(label, 13, "#64748B"));
        var value = Text("—", 22);
        value.FontWeight = FontWeights.SemiBold;
        value.Margin = new Thickness(0, 3, 0, 13);
        panel.Children.Add(value);
        return value;
    }
    public static DockPanel Shell(Window window, string title, string subtitle)
    {
        window.Title = title; window.Width = 1080; window.Height = 820;
        window.MinWidth = 780; window.MinHeight = 620;
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.FontFamily = new FontFamily("Segoe UI"); window.Background = Brush("#F1F5F9");
        var dock = new DockPanel { Margin = new Thickness(24) };
        var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
        heading.Children.Add(Text(title, 29));
        heading.Children.Add(Text(subtitle, 14, "#64748B"));
        DockPanel.SetDock(heading, Dock.Top); dock.Children.Add(heading);
        window.Content = dock;
        return dock;
    }
}
