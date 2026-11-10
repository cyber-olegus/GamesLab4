using System.Windows;
namespace EnemyEditor.Wpf;

internal static class Program
{
    [STAThread]
    public static void Main() => new Application().Run(new MainWindow());
}
