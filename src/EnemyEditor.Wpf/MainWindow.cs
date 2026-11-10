using System.Windows;
using GamesLab4.WpfShared;

namespace EnemyEditor.Wpf;

public sealed class MainWindow : Window
{
    public MainWindow()
    {
        var root = Ui.Shell(this, "Лабораторная работа №4", "Общая основа готова");
        root.Children.Add(Ui.Card(Ui.Text("Добавьте соответствующую часть из архива лабораторной работы.")));
    }
}
