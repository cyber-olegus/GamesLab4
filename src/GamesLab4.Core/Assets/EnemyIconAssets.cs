namespace GamesLab4.Core.Assets;

/// <summary>Поиск PNG и импорт в исходники проекта и выходную папку.
/// Не зависит от WPF, поэтому копирование можно проверить отдельно от окна.</summary>
public sealed class EnemyIconAssets
{
    private readonly string applicationDirectory;
    private string? templateDirectory;
    public string? ProjectDirectory { get; }
    public string OutputIconsDirectory => Path.Combine(applicationDirectory, "Assets", "Icons");
    public string StorageDirectory => ProjectDirectory is null ? OutputIconsDirectory :
        Path.Combine(ProjectDirectory, "Assets", "Icons");

    public EnemyIconAssets(string applicationDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDirectory);
        this.applicationDirectory = Path.GetFullPath(applicationDirectory);
        ProjectDirectory = FindProjectDirectory(this.applicationDirectory);
    }

    public void SetTemplateFile(string jsonPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonPath);
        templateDirectory = Path.GetDirectoryName(Path.GetFullPath(jsonPath));
    }

    public string? Find(string iconName)
    {
        // IconName из Лабы 1 — имя файла, а не произвольный внешний путь.
        if (string.IsNullOrWhiteSpace(iconName) || iconName.Contains('/') ||
            iconName.Contains('\\') || iconName.Contains(':')) return null;

        var roots = new List<string> { OutputIconsDirectory, StorageDirectory };
        if (templateDirectory is not null)
        {
            roots.Add(Path.Combine(templateDirectory, "Assets", "Icons"));
            roots.Add(Path.Combine(templateDirectory, "Icons"));
        }

        foreach (string root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(root)) continue;
            string[] matches = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(p => string.Equals(Path.GetFileName(p), iconName, StringComparison.OrdinalIgnoreCase))
                .Take(2).ToArray();
            if (matches.Length > 1)
                throw new InvalidDataException($"Найдено несколько иконок с именем «{iconName}». Имена PNG должны быть уникальными.");
            if (matches.Length == 1) return matches[0];
        }

        // Допускаем PNG непосредственно рядом с JSON, но не сканируем всю папку с документами.
        if (templateDirectory is not null && Directory.Exists(templateDirectory))
            return Directory.EnumerateFiles(templateDirectory)
                .FirstOrDefault(p => string.Equals(Path.GetFileName(p), iconName, StringComparison.OrdinalIgnoreCase));
        return null;
    }

    public int Import(string selectedDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedDirectory);
        string source = LocateSourceIcons(Path.GetFullPath(selectedDirectory));
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException("Папка с иконками не найдена.");

        string[] pngFiles = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
            .Where(p => string.Equals(Path.GetExtension(p), ".png", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (pngFiles.Length == 0)
            throw new InvalidDataException("В выбранной папке нет PNG. Выберите Assets/Icons из Лабы 1 или Лабы 2.");

        string[] duplicates = pngFiles.GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => group.Key!).ToArray();
        if (duplicates.Length > 0)
            throw new InvalidDataException("Есть повторяющиеся имена PNG: " +
                string.Join(", ", duplicates) + ". Выберите исходную папку Assets/Icons, а не весь каталог проектов.");

        // Снимок списка получен до копирования: вложенные папки сохраняются.
        // При запуске из Visual Studio PNG попадают и в репозиторий, и в текущую игру.
        foreach (string destinationRoot in new[] { StorageDirectory, OutputIconsDirectory }
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (string path in pngFiles)
            {
                string target = Path.GetFullPath(Path.Combine(destinationRoot, Path.GetRelativePath(source, path)));
                if (string.Equals(Path.GetFullPath(path), target, StringComparison.OrdinalIgnoreCase)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(path, target, overwrite: true);
            }
        }
        return pngFiles.Length;
    }

    private static string LocateSourceIcons(string selected)
    {
        if (!Directory.Exists(selected)) return selected;
        string direct = Path.Combine(selected, "Assets", "Icons");
        if (Directory.Exists(direct)) return direct;
        string src = Path.Combine(selected, "src");
        if (Directory.Exists(src))
        {
            string[] candidates = Directory.EnumerateDirectories(src)
                .Select(project => Path.Combine(project, "Assets", "Icons")).Where(Directory.Exists).ToArray();
            if (candidates.Length == 1) return candidates[0];
        }
        return selected;
    }

    private static string? FindProjectDirectory(string start)
    {
        for (DirectoryInfo? current = new(start); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "EnemyClicker.Wpf.csproj")))
                return current.FullName;
            string nested = Path.Combine(current.FullName, "src", "EnemyClicker.Wpf");
            if (File.Exists(Path.Combine(nested, "EnemyClicker.Wpf.csproj"))) return nested;
        }
        return null;
    }
}
