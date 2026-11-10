using System.Text.Json;
using GamesLab4.Core.Numbers;
using GamesLab4.Core.Player;

namespace GamesLab4.Core.Persistence;

public sealed class PlayerSaver : ISaveList<PlayerProgress>
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private sealed class Document
    {
        public Document() { }
        public required int Version { get; init; }
        public required string Gold { get; init; }
        public required string Damage { get; init; }
        public required int Level { get; init; }
        public required string UpgradeCost { get; init; }
        public required int Victories { get; init; }
        public required double DamageModifier { get; init; }
        public required double UpgradeModifier { get; init; }
    }

    public PlayerProgress Load(string path)
    {
        var doc = JsonSerializer.Deserialize<Document>(File.ReadAllText(path), Options)
            ?? throw new JsonException("Файл прогресса пуст.");
        if (doc.Version != 1) throw new JsonException($"Неизвестная версия файла прогресса: {doc.Version}.");
        try
        {
            var progress = new PlayerProgress(new BigNumber(doc.Gold), new BigNumber(doc.Damage),
                doc.Level, new BigNumber(doc.UpgradeCost), doc.Victories, doc.DamageModifier, doc.UpgradeModifier);
            CPlayer.Validate(progress);
            return progress;
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        {
            throw new JsonException($"Некорректный прогресс: {e.Message}", e);
        }
    }

    public void Save(PlayerProgress data, string path)
    {
        CPlayer.Validate(data);
        var doc = new Document
        {
            Version = 1, Gold = data.Gold.ToString(), Damage = data.Damage.ToString(),
            Level = data.Level, UpgradeCost = data.UpgradeCost.ToString(), Victories = data.Victories,
            DamageModifier = data.DamageModifier, UpgradeModifier = data.UpgradeModifier
        };
        JsonFile.Write(path, JsonSerializer.Serialize(doc, Options));
    }
}
