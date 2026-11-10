using System.Text;

namespace GamesLab4.Core.Persistence;

internal static class JsonFile
{
    public static void Write(string path, string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, $".gameslab4-{Guid.NewGuid():N}.tmp");
        try
        {
            // Сначала записываем целый файл, затем заменяем прежнее сохранение.
            File.WriteAllText(temporary, json, new UTF8Encoding(false));
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
