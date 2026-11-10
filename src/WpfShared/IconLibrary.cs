using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GamesLab4.Core.Assets;
using Microsoft.Win32;

namespace GamesLab4.WpfShared;

/// <summary>Оба приложения используют одну папку исходных PNG и один способ загрузки.</summary>
internal sealed class IconLibrary
{
    private readonly EnemyIconAssets assets = new(AppContext.BaseDirectory);
    private readonly Dictionary<string, ImageSource> cache = new(StringComparer.OrdinalIgnoreCase);
    public string LastError { get; private set; } = "";

    public void UseTemplateFile(string path)
    {
        assets.SetTemplateFile(path);
        cache.Clear();
    }

    public string[] Names()
    {
        if (!Directory.Exists(assets.StorageDirectory)) return [];
        return Directory.EnumerateFiles(assets.StorageDirectory, "*", SearchOption.AllDirectories)
            .Where(p => Path.GetExtension(p).Equals(".png", StringComparison.OrdinalIgnoreCase))
            .Select(p => Path.GetFileName(p)!).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public string? Import(Window owner)
    {
        var dialog = new OpenFolderDialog { Title = "Выберите Assets/Icons или папку проекта Лабы 1, 2 или 3" };
        if (dialog.ShowDialog(owner) != true) return null;
        int count = assets.Import(dialog.FolderName);
        cache.Clear();
        return $"Импортировано PNG: {count}. Папка: {assets.StorageDirectory}";
    }

    public ImageSource? Load(string name)
    {
        LastError = "";
        if (cache.TryGetValue(name, out ImageSource? saved)) return saved;
        try
        {
            string? path = assets.Find(name);
            if (path is null)
            {
                LastError = $"Не найдена «{name}». Используйте «Импорт PNG».";
                return null;
            }
            var bitmap = new BitmapImage();
            bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 256;
            bitmap.UriSource = new Uri(Path.GetFullPath(path), UriKind.Absolute);
            bitmap.EndInit(); bitmap.Freeze();
            cache[name] = bitmap;
            return bitmap;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException
            or NotSupportedException or InvalidOperationException or FormatException)
        {
            LastError = $"Не удалось открыть «{name}»: {e.Message}";
            return null;
        }
    }
}
