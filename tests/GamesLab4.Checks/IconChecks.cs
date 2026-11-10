using GamesLab4.Core.Assets;

namespace GamesLab4.Checks;

internal static class IconChecks
{
    // Маленькая PNG используется только как тестовый файл, а не вместо пользовательских иконок.
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/l9sAAAAASUVORK5CYII=");

    private sealed class Sandbox : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "gameslab4-icons-" + Guid.NewGuid().ToString("N"));
        public Sandbox() => Directory.CreateDirectory(Root);

        // На Windows Path.Combine может оставить '/' внутри относительного фрагмента.
        // Нормализуем тестовые пути до создания файлов и сравнения результатов.
        public string At(string relative) => Path.GetFullPath(Path.Combine(Root, relative));
        public string Write(string relative)
        {
            string path = At(relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, Png);
            return path;
        }
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }

    [Check("Иконки: поиск во вложенных папках и без учёта регистра имени")]
    public static void FindNestedPng()
    {
        using var box = new Sandbox();
        string file = box.Write("app/Assets/Icons/Monsters/GOBLIN_1.PNG");
        var assets = new EnemyIconAssets(box.At("app"));
        Verify.Equal(file, assets.Find("goblin_1.png"));
    }

    [Check("Иконки: PNG в папке Icons рядом с JSON")]
    public static void FindNearJson()
    {
        using var box = new Sandbox();
        string file = box.Write("templates/Icons/Monsters/troll_1.png");
        var assets = new EnemyIconAssets(box.At("app"));
        assets.SetTemplateFile(box.At("templates/enemies.json"));
        Verify.Equal(file, assets.Find("troll_1.png"));
    }

    [Check("Иконки: PNG непосредственно рядом с JSON")]
    public static void FindBesideJsonFile()
    {
        using var box = new Sandbox();
        string file = box.Write("templates/skeleton_2.png");
        var assets = new EnemyIconAssets(box.At("app"));
        assets.SetTemplateFile(box.At("templates/enemies.json"));
        Verify.Equal(file, assets.Find("skeleton_2.png"));
    }

    [Check("Иконки: импорт сохраняет PNG в репозитории и в выходной папке")]
    public static void ImportIntoProjectAndOutput()
    {
        using var box = new Sandbox();
        string source = box.Write("old-icons/Monsters/goblin_1.png");
        box.Write("old-icons/Monsters/troll_1.PNG");
        string project = box.At("repo/src/EnemyClicker.Wpf");
        Directory.CreateDirectory(project);
        File.WriteAllText(Path.Combine(project, "EnemyClicker.Wpf.csproj"), "<Project />");
        string app = Path.Combine(project, "bin/Debug/net8.0-windows");
        var assets = new EnemyIconAssets(app);
        Verify.Equal(project, assets.ProjectDirectory);
        Verify.Equal(2, assets.Import(box.At("old-icons")));
        Verify.True(File.Exists(Path.Combine(project, "Assets/Icons/Monsters/goblin_1.png")));
        Verify.True(File.Exists(Path.Combine(app, "Assets/Icons/Monsters/goblin_1.png")));
        Verify.True(File.ReadAllBytes(source).SequenceEqual(
            File.ReadAllBytes(Path.Combine(project, "Assets/Icons/Monsters/goblin_1.png"))));
    }

    [Check("Иконки: поиск в исходниках проекта до копирования в bin")]
    public static void FindInProject()
    {
        using var box = new Sandbox();
        string project = box.At("repo/src/EnemyClicker.Wpf");
        Directory.CreateDirectory(project);
        File.WriteAllText(Path.Combine(project, "EnemyClicker.Wpf.csproj"), "<Project />");
        string file = box.Write("repo/src/EnemyClicker.Wpf/Assets/Icons/Monsters/goblin_1.png");
        var assets = new EnemyIconAssets(Path.Combine(project, "bin/Debug/net8.0-windows"));
        Verify.Equal(file, assets.Find("goblin_1.png"));
    }

    [Check("Иконки: повторяющиеся имена отклоняются до копирования")]
    public static void RejectDuplicateNames()
    {
        using var box = new Sandbox();
        box.Write("old-icons/First/goblin.png");
        box.Write("old-icons/Second/GOBLIN.PNG");
        var assets = new EnemyIconAssets(box.At("app"));
        Verify.Throws<InvalidDataException>(() => assets.Import(box.At("old-icons")));
        Verify.True(!Directory.Exists(assets.OutputIconsDirectory));
    }

    [Check("Иконки: пустая или отсутствующая папка не считается успешным импортом")]
    public static void RejectMissingPng()
    {
        using var box = new Sandbox();
        var assets = new EnemyIconAssets(box.At("app"));
        Directory.CreateDirectory(box.At("empty"));
        Verify.Throws<DirectoryNotFoundException>(() => assets.Import(box.At("missing")));
        Verify.Throws<InvalidDataException>(() => assets.Import(box.At("empty")));
    }

    [Check("Иконки: произвольный путь из JSON не разрешён")]
    public static void RejectExternalPath()
    {
        using var box = new Sandbox();
        box.Write("secret.png");
        var assets = new EnemyIconAssets(box.At("app"));
        Verify.True(assets.Find("../secret.png") is null);
        Verify.True(assets.Find(@"..\secret.png") is null);
        Verify.True(assets.Find("missing.png") is null);
    }

    [Check("Иконки: повторный импорт собственной папки не копирует файл сам в себя")]
    public static void ImportExistingProjectIcons()
    {
        using var box = new Sandbox();
        string project = box.At("repo/src/EnemyClicker.Wpf");
        Directory.CreateDirectory(project);
        File.WriteAllText(Path.Combine(project, "EnemyClicker.Wpf.csproj"), "<Project />");
        string file = box.Write("repo/src/EnemyClicker.Wpf/Assets/Icons/Monsters/goblin_1.png");
        var assets = new EnemyIconAssets(Path.Combine(project, "bin/Debug/net8.0-windows"));
        Verify.Equal(1, assets.Import(Path.Combine(project, "Assets/Icons")));
        Verify.True(File.Exists(file));
        Verify.True(File.Exists(Path.Combine(assets.OutputIconsDirectory, "Monsters/goblin_1.png")));
    }

    [Check("Иконки: можно выбрать целиком старый проект Лабы 2")]
    public static void ImportOldProjectRoot()
    {
        using var box = new Sandbox();
        box.Write("old-lab/src/ClickerGame.Wpf/Assets/Icons/Monsters/goblin_1.png");
        var assets = new EnemyIconAssets(box.At("app"));
        Verify.Equal(1, assets.Import(box.At("old-lab")));
        Verify.True(File.Exists(Path.Combine(assets.OutputIconsDirectory, "Monsters/goblin_1.png")));
    }
}
