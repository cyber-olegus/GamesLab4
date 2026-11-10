using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GamesLab4.Core.Combat;
using GamesLab4.Core.Persistence;
using GamesLab4.WpfShared;
using Microsoft.Win32;

namespace EnemyClicker.Wpf;

public sealed class MainWindow : Window
{
    private readonly GameSession game = new(new JsonEnemySaver(), new PlayerSaver());
    private readonly IconLibrary icons = new();
    private readonly TextBlock source = Ui.Text("Список не загружен.", 13, "#64748B");
    private readonly TextBlock status = Ui.Text("", 14, "#475569");
    private readonly TextBlock enemyName = Ui.Text("Нет противника", 25);
    private readonly TextBlock enemyDetails = Ui.Text("", 14, "#475569");
    private readonly TextBlock imageHint = Ui.Text("", 13, "#64748B");
    private readonly Image enemyImage = new() { Width = 210, Height = 210, Stretch = Stretch.Uniform };
    private readonly TextBlock imageFallback = Ui.Text("Иконка отсутствует\nНажмите для атаки", 18, "#4338CA");
    private readonly TextBlock health, gold, damage, level, victories;
    private readonly Button attack, upgrade, restart, saveProgress, loadProgress;

    public MainWindow()
    {
        var root = Ui.Shell(this, "Битва с противниками", "Лабораторная №4 · разные типы противников и сохранение прогресса");
        var bar = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        var commands = new WrapPanel();
        commands.Children.Add(Ui.Button("Открыть список JSON", (_, _) => Run(OpenTemplates)));
        restart = Ui.Button("Начать заново", (_, _) => Run(game.Restart), true);
        saveProgress = Ui.Button("Сохранить прогресс", (_, _) => Run(SavePlayer), true);
        loadProgress = Ui.Button("Загрузить прогресс", (_, _) => Run(LoadPlayer), true);
        commands.Children.Add(restart); commands.Children.Add(saveProgress); commands.Children.Add(loadProgress);
        commands.Children.Add(Ui.Button("Импорт PNG", (_, _) => Run(ImportIcons), true));
        bar.Children.Add(commands); bar.Children.Add(source);
        DockPanel.SetDock(bar, Dock.Top); root.Children.Add(bar);
        status.Margin = new Thickness(0, 12, 0, 0); DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(282) });
        root.Children.Add(grid);

        var arena = new DockPanel();
        var title = new StackPanel();
        title.Children.Add(enemyName); title.Children.Add(enemyDetails);
        title.Margin = new Thickness(0, 0, 0, 12); DockPanel.SetDock(title, Dock.Top); arena.Children.Add(title);
        var footer = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        health = Ui.Stat(footer, "Здоровье противника"); footer.Children.Add(imageHint);
        DockPanel.SetDock(footer, Dock.Bottom); arena.Children.Add(footer);

        attack = Ui.Button("", (_, _) => Run(() => { game.Attack(); }), true);
        attack.Background = Ui.Brush("#EEF2FF"); attack.Margin = new Thickness(0);
        attack.MinHeight = 230; attack.ToolTip = "Нажмите, чтобы атаковать противника";
        System.Windows.Automation.AutomationProperties.SetName(attack, "Атаковать противника");
        var picture = new Grid();
        picture.Children.Add(enemyImage);
        imageFallback.HorizontalAlignment = HorizontalAlignment.Center; imageFallback.VerticalAlignment = VerticalAlignment.Center;
        imageFallback.TextAlignment = TextAlignment.Center; picture.Children.Add(imageFallback);
        RenderOptions.SetBitmapScalingMode(enemyImage, BitmapScalingMode.NearestNeighbor);
        attack.Content = picture; arena.Children.Add(attack);
        grid.Children.Add(Ui.Card(new ScrollViewer { Content = arena, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }));

        var stats = new StackPanel();
        gold = Ui.Stat(stats, "Золото");
        damage = Ui.Stat(stats, "Урон за клик");
        level = Ui.Stat(stats, "Уровень игрока");
        victories = Ui.Stat(stats, "Победы");
        upgrade = Ui.Button("Улучшить урон", (_, _) => Run(() => { game.TryUpgrade(); }));
        upgrade.Margin = new Thickness(0, 3, 0, 10); stats.Children.Add(upgrade);
        stats.Children.Add(Ui.Text("В файл прогресса входят золото, урон, уровень, цена улучшения и победы.", 13, "#64748B"));
        var sidebar = Ui.Card(new ScrollViewer { Content = stats, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Grid.SetColumn(sidebar, 2); grid.Children.Add(sidebar);

        Loaded += (_, _) => Run(() =>
        {
            string sample = Path.Combine(AppContext.BaseDirectory, "Data", "enemies.example.json");
            if (File.Exists(sample)) LoadTemplates(sample);
        });
        Refresh();
    }

    private void OpenTemplates()
    {
        var dialog = new OpenFileDialog { Title = "Выберите JSON из редактора противников",
            Filter = "Список противников (*.json)|*.json", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) LoadTemplates(dialog.FileName);
    }
    private void LoadTemplates(string path)
    {
        game.LoadTemplates(path);
        icons.UseTemplateFile(path);
    }

    private void SavePlayer()
    {
        var dialog = new SaveFileDialog { Title = "Сохранить прогресс игрока",
            Filter = "Прогресс игрока (*.json)|*.json", DefaultExt = ".json", AddExtension = true,
            FileName = "player-progress.json" };
        if (dialog.ShowDialog(this) == true) game.SaveProgress(dialog.FileName);
    }
    private void LoadPlayer()
    {
        var dialog = new OpenFileDialog { Title = "Загрузить прогресс игрока",
            Filter = "Прогресс игрока (*.json)|*.json", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) game.LoadProgress(dialog.FileName);
    }
    private void ImportIcons()
    {
        string? message = icons.Import(this);
        if (message is not null)
            MessageBox.Show(this, message, "Иконки импортированы", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void Run(Action action)
    {
        try { action(); Refresh(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException
            or ArgumentException or InvalidOperationException or NotSupportedException or OverflowException)
        {
            Refresh(); status.Text = e.Message;
            MessageBox.Show(this, e.Message, "Битва с противниками", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    private void Refresh()
    {
        var player = game.Player;
        IEnemy? enemy = game.CurrentEnemy;
        source.Text = game.TemplateFile is null ? "Список не загружен." :
            $"{Path.GetFileName(game.TemplateFile)} · шаблонов: {game.TemplateCount}";
        enemyName.Text = enemy?.Name ?? "Откройте список противников";
        enemyDetails.Text = enemy is null ? "" : $"{enemy.TypeLabel} · {enemy.Details} Награда: {enemy.Gold}.";
        health.Text = enemy is null ? "—" : $"{enemy.CurrentLife} / {enemy.BaseLife}";
        gold.Text = player.Gold.ToString(); damage.Text = player.Damage.ToString();
        level.Text = player.Level.ToString(); victories.Text = game.Victories.ToString();
        upgrade.Content = $"Улучшить урон · {player.UpgradeCost}";
        upgrade.IsEnabled = game.IsStarted && player.CanUpgrade;
        attack.IsEnabled = enemy is not null;
        restart.IsEnabled = saveProgress.IsEnabled = loadProgress.IsEnabled = game.IsStarted;
        enemyImage.Source = enemy is null ? null : icons.Load(enemy.IconName);
        enemyImage.Visibility = enemyImage.Source is null ? Visibility.Collapsed : Visibility.Visible;
        imageFallback.Visibility = enemyImage.Source is null ? Visibility.Visible : Visibility.Collapsed;
        imageFallback.Text = enemy is null ? "Выберите JSON из редактора" : "Иконка отсутствует\nНажмите для атаки";
        imageHint.Text = enemy is null || enemyImage.Source is not null ? "" : icons.LastError;
        status.Text = game.LastAction;
    }
}
