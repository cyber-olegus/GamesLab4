using System.Windows;
namespace EnemyClicker.Wpf;

internal static class Program
{
    [STAThread]
    public static void Main() => new Application().Run(new MainWindow());
}
