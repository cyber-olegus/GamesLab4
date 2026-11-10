using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GamesLab4.Core.Persistence;
using GamesLab4.Core.Templates;
using GamesLab4.WpfShared;
using Microsoft.Win32;

namespace EnemyEditor.Wpf;

public sealed class MainWindow : Window
{
    private readonly CEnemyTemplateList catalog = new(new JsonEnemySaver());
    private readonly IconLibrary icons = new();
    private readonly ListBox list = new() { FontSize = 15, BorderThickness = new Thickness(0) };
    private readonly ComboBox kind = new() { ItemsSource = new[] { "Обычный", "Бронированный", "Лечащийся", "Со щитом" } };
    private readonly ComboBox iconName = new() { IsEditable = true, IsTextSearchEnabled = true };
    private readonly Dictionary<string, TextBox> fields = new();
    private readonly Dictionary<string, FrameworkElement> specialFields = new();
    private readonly Image preview = new() { Width = 96, Height = 96, Stretch = Stretch.Uniform };
    private readonly TextBlock previewText = Ui.Text("", 13, "#64748B");
    private readonly TextBlock status = Ui.Text("Создайте шаблон или откройте JSON.", 14, "#475569");
    private bool fillingForm, refreshingList, dirty;
    private string? currentFile;

    public MainWindow()
    {
        var root = Ui.Shell(this, "Редактор противников", "Лабораторная №4 · шаблоны разных типов в одном JSON");
        var buttons = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        buttons.Children.Add(Ui.Button("Открыть JSON", (_, _) => Run(Open)));
        buttons.Children.Add(Ui.Button("Сохранить JSON", (_, _) => Run(Save)));
        buttons.Children.Add(Ui.Button("Новый шаблон", (_, _) => NewDraft(), true));
        buttons.Children.Add(Ui.Button("Импорт PNG", (_, _) => Run(ImportIcons), true));
        DockPanel.SetDock(buttons, Dock.Top); root.Children.Add(buttons);
        status.Margin = new Thickness(0, 13, 0, 0);
        DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(275) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        root.Children.Add(grid);

        var left = new DockPanel();
        var listTitle = Ui.Text("Шаблоны", 20); listTitle.Margin = new Thickness(0, 0, 0, 10);
        DockPanel.SetDock(listTitle, Dock.Top); left.Children.Add(listTitle);
        var remove = Ui.Button("Удалить выбранный", (_, _) => Run(Remove), true);
        DockPanel.SetDock(remove, Dock.Bottom); left.Children.Add(remove);
        left.Children.Add(list); grid.Children.Add(Ui.Card(left));

        var form = new StackPanel();
        form.Children.Add(Ui.Text("Данные противника", 21));
        var general = new WrapPanel();
        general.Children.Add(Field("Тип", kind));
        general.Children.Add(Input("Имя", "name"));
        general.Children.Add(Field("Имя файла PNG", iconName));
        general.Children.Add(Input("Базовое здоровье", "life"));
        general.Children.Add(Input("Множитель здоровья", "lifeModifier"));
        general.Children.Add(Input("Награда золотом", "gold"));
        general.Children.Add(Input("Множитель золота", "goldModifier"));
        general.Children.Add(Input("Шанс появления (0–1)", "chance"));
        form.Children.Add(general);
        var extras = new WrapPanel();
        foreach (var (label, key) in new[] { ("Броня", "armor"), ("Шанс лечения (0–1)", "healChance"),
            ("Лечение за срабатывание", "healAmount"), ("Число блоков щитом", "shield") })
        {
            var field = Input(label, key); specialFields.Add(key, field); extras.Children.Add(field);
        }
        form.Children.Add(extras);
        var imageRow = new DockPanel { Margin = new Thickness(0, 12, 0, 16) };
        var picture = new Border { Background = Ui.Brush("#EEF2FF"), CornerRadius = new CornerRadius(10),
            Child = preview, Margin = new Thickness(0, 0, 14, 0) };
        DockPanel.SetDock(picture, Dock.Left); imageRow.Children.Add(picture); imageRow.Children.Add(previewText);
        form.Children.Add(imageRow);
        var apply = Ui.Button("Применить к списку", (_, _) => Run(Apply));
        apply.HorizontalAlignment = HorizontalAlignment.Left; form.Children.Add(apply);
        form.Children.Add(Ui.Text("Сохранение JSON также применяет правки открытой формы.", 12, "#64748B"));
        var card = Ui.Card(new ScrollViewer { Content = form, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Grid.SetColumn(card, 2); grid.Children.Add(card);

        kind.SelectionChanged += (_, _) => { ShowSpecialFields(); MarkDirty(); };
        iconName.SelectionChanged += (_, _) => MarkDirty();
        iconName.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler((_, _) => MarkDirty()));
        list.SelectionChanged += (_, _) => { if (!refreshingList) FillSelected(); };
        RenderOptions.SetBitmapScalingMode(preview, BitmapScalingMode.NearestNeighbor);
        Loaded += (_, _) => Run(() =>
        {
            RefreshIconChoices();
            string sample = Path.Combine(AppContext.BaseDirectory, "Data", "enemies.example.json");
            if (File.Exists(sample)) Load(sample);
            else Refresh(-1);
        });
    }

    private static FrameworkElement Field(string caption, Control control)
    {
        var panel = new StackPanel { Width = 225, Margin = new Thickness(0, 10, 12, 0) };
        panel.Children.Add(Ui.Text(caption, 13, "#64748B"));
        control.Margin = new Thickness(0, 4, 0, 0); control.MinHeight = 30;
        panel.Children.Add(control);
        return panel;
    }

    private FrameworkElement Input(string label, string key)
    {
        var input = new TextBox { Padding = new Thickness(6, 3, 6, 3) };
        fields.Add(key, input);
        input.TextChanged += (_, _) => MarkDirty();
        return Field(label, input);
    }

    private void MarkDirty()
    {
        if (!fillingForm) dirty = true;
    }

    private void ShowSpecialFields()
    {
        foreach (var (key, control) in specialFields)
            control.Visibility = (kind.SelectedIndex, key) switch
            {
                (1, "armor") or (2, "healChance") or (2, "healAmount") or (3, "shield") => Visibility.Visible,
                _ => Visibility.Collapsed
            };
    }

    private void Open()
    {
        var dialog = new OpenFileDialog { Title = "Открыть список противников", Filter = "Список противников (*.json)|*.json", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) Load(dialog.FileName);
    }
    private void Load(string path)
    {
        catalog.Load(path);
        currentFile = path; icons.UseTemplateFile(path);
        Refresh(catalog.Items.Count > 0 ? 0 : -1);
        status.Text = $"Открыт {Path.GetFileName(path)}. Шаблонов: {catalog.Items.Count}.";
    }
    private void Save()
    {
        if (dirty) Apply();
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить список противников", Filter = "Список противников (*.json)|*.json",
            AddExtension = true, DefaultExt = ".json", FileName = currentFile is null ? "enemies.json" : Path.GetFileName(currentFile)
        };
        if (dialog.ShowDialog(this) != true) return;
        catalog.Save(dialog.FileName); currentFile = dialog.FileName;
        status.Text = $"Сохранено шаблонов: {catalog.Items.Count}. Файл: {currentFile}";
    }
    private void NewDraft()
    {
        list.SelectedIndex = -1;
        FillSelected();
        status.Text = "Новый шаблон. Заполните поля и нажмите «Применить к списку».";
    }
    private void Apply()
    {
        CEnemyTemplate template = ReadForm();
        int index = list.SelectedIndex;
        if (index < 0) { catalog.Add(template); index = catalog.Items.Count - 1; }
        else catalog.Replace(index, template);
        Refresh(index);
        status.Text = $"«{template.Name}» сохранён в списке. Для записи на диск нажмите «Сохранить JSON».";
    }
    private void Remove()
    {
        int index = list.SelectedIndex;
        if (index < 0) return;
        catalog.RemoveAt(index); Refresh(Math.Min(index, catalog.Items.Count - 1));
        status.Text = $"Шаблон удалён. В списке: {catalog.Items.Count}.";
    }
    private void ImportIcons()
    {
        string? result = icons.Import(this);
        if (result is null) return;
        RefreshIconChoices();
        UpdatePreview(list.SelectedIndex >= 0 ? catalog.Items[list.SelectedIndex] : null);
        status.Text = result;
    }
    private void RefreshIconChoices()
    {
        string currentName = iconName.Text;
        fillingForm = true;
        try { iconName.ItemsSource = icons.Names(); iconName.Text = currentName; }
        finally { fillingForm = false; }
    }

    private void Refresh(int selection)
    {
        refreshingList = true;
        try
        {
            list.ItemsSource = catalog.Items.Select(t => $"{t.Name}\n{t.TypeLabel}").ToArray();
            list.SelectedIndex = selection;
        }
        finally { refreshingList = false; }
        FillSelected();
    }
    private void FillSelected()
    {
        var t = list.SelectedIndex >= 0 ? catalog.Items[list.SelectedIndex] : null;
        fillingForm = true;
        try
        {
            kind.SelectedIndex = t switch { CArmoredEnemyTemplate => 1, CHealingEnemyTemplate => 2, CShieldedEnemyTemplate => 3, _ => 0 };
            fields["name"].Text = t?.Name ?? "Новый противник";
            iconName.Text = t?.IconName ?? "goblin_1.png";
            Set("life", t?.BaseLife ?? 40); Set("lifeModifier", t?.LifeModifier ?? 1.04);
            Set("gold", t?.BaseGold ?? 25); Set("goldModifier", t?.GoldModifier ?? 1.08);
            Set("chance", t?.SpawnChance ?? 0.25);
            Set("armor", t is CArmoredEnemyTemplate a ? a.Armor : 3);
            Set("healChance", t is CHealingEnemyTemplate h ? h.HealChance : 0.25);
            Set("healAmount", t is CHealingEnemyTemplate heal ? heal.HealAmount : 8);
            Set("shield", t is CShieldedEnemyTemplate s ? s.ShieldHits : 2);
            ShowSpecialFields(); UpdatePreview(t); dirty = false;
        }
        finally { fillingForm = false; }
    }

    private void Set(string key, double number) => fields[key].Text = number.ToString(CultureInfo.CurrentCulture);
    private void UpdatePreview(CEnemyTemplate? template)
    {
        preview.Source = icons.Load(iconName.Text.Trim());
        string summary = template is ITemplateSummary detail ? detail.Summary : "Обычный урон.";
        previewText.Text = preview.Source is null ? $"{summary}\n{icons.LastError}" : $"{summary}\n{iconName.Text}";
    }
    private int Integer(string key, string label) =>
        int.TryParse(fields[key].Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out int value)
        ? value : throw new FormatException($"{label}: введите целое число.");
    private double Fraction(string key, string label)
    {
        string input = fields[key].Text.Trim().Replace(',', '.');
        if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && double.IsFinite(value))
            return value;
        throw new FormatException($"{label}: введите число, например 0,25.");
    }

    private CEnemyTemplate ReadForm()
    {
        string name = fields["name"].Text, icon = iconName.Text;
        int life = Integer("life", "Здоровье"), gold = Integer("gold", "Награда");
        double lifeModifier = Fraction("lifeModifier", "Множитель здоровья"), goldModifier = Fraction("goldModifier", "Множитель золота");
        double chance = Fraction("chance", "Шанс появления");
        return kind.SelectedIndex switch
        {
            1 => new CArmoredEnemyTemplate(name, icon, life, lifeModifier, gold, goldModifier, chance, Integer("armor", "Броня")),
            2 => new CHealingEnemyTemplate(name, icon, life, lifeModifier, gold, goldModifier, chance,
                Fraction("healChance", "Шанс лечения"), Integer("healAmount", "Лечение")),
            3 => new CShieldedEnemyTemplate(name, icon, life, lifeModifier, gold, goldModifier, chance, Integer("shield", "Блоки щитом")),
            _ => new CNormalEnemyTemplate(name, icon, life, lifeModifier, gold, goldModifier, chance)
        };
    }

    private void Run(Action action)
    {
        try { action(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException
            or ArgumentException or InvalidOperationException or FormatException)
        {
            status.Text = e.Message;
            MessageBox.Show(this, e.Message, "Редактор противников", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
