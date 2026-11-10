using GamesLab4.Core.Persistence;
using GamesLab4.Core.Player;
using GamesLab4.Core.Templates;

namespace GamesLab4.Core.Combat;

/// <summary>Игровой сценарий: WPF отдаёт команды, но не присваивает здоровье или золото.</summary>
public sealed class GameSession
{
    private readonly ISaveList<List<CEnemyTemplate>> templateSaver;
    private readonly ISaveList<PlayerProgress> playerSaver;
    private readonly Random random;
    private EnemyPool? pool;
    public CPlayer Player { get; private set; } = new();
    public IEnemy? CurrentEnemy { get; private set; }
    public int Victories { get; private set; }
    public int TemplateCount => pool?.Count ?? 0;
    public bool IsStarted => pool is not null;
    public string? TemplateFile { get; private set; }
    public string LastAction { get; private set; } = "Откройте JSON с противниками.";

    public GameSession(ISaveList<List<CEnemyTemplate>> templateSaver, ISaveList<PlayerProgress> playerSaver, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(templateSaver);
        ArgumentNullException.ThrowIfNull(playerSaver);
        this.templateSaver = templateSaver; this.playerSaver = playerSaver; this.random = random ?? new Random();
    }

    public void LoadTemplates(string path)
    {
        var candidate = new EnemyPool(templateSaver.Load(path));
        IEnemy first = Spawn(candidate, 0);
        pool = candidate; TemplateFile = path; Player = new(); Victories = 0; CurrentEnemy = first;
        LastAction = $"Новая игра. Загружено шаблонов: {TemplateCount}.";
    }
    public void Restart()
    {
        EnemyPool current = RequirePool();
        IEnemy first = Spawn(current, 0);
        Player = new(); Victories = 0; CurrentEnemy = first;
        LastAction = "Игра начата заново.";
    }
    public bool Attack()
    {
        if (CurrentEnemy is null || pool is null) return false;
        if (Victories == int.MaxValue) throw new InvalidOperationException("Достигнут предел счётчика побед.");
        HitResult hit = CurrentEnemy.TakeDamage(Player.DealDamage());
        if (hit.DefeatedNow)
        {
            int count = Victories + 1;
            IEnemy next = Spawn(pool, count);
            Player.AddGold(hit.GoldReward); Victories = count; CurrentEnemy = next;
        }
        LastAction = hit.Message;
        return true;
    }
    public bool TryUpgrade()
    {
        bool success = IsStarted && Player.TryUpgrade();
        LastAction = success ? "Урон улучшен." : "Улучшение сейчас недоступно.";
        return success;
    }
    public void SaveProgress(string path)
    {
        RequirePool();
        if (TemplateFile is not null && string.Equals(Path.GetFullPath(path), Path.GetFullPath(TemplateFile), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Выберите отдельный файл для прогресса игрока, чтобы сохранить список противников.");
        playerSaver.Save(Player.Snapshot(Victories), path);
        LastAction = $"Прогресс сохранён: {Path.GetFileName(path)}.";
    }
    public void LoadProgress(string path)
    {
        EnemyPool current = RequirePool();
        PlayerProgress progress = playerSaver.Load(path);
        CPlayer restored = CPlayer.Restore(progress);
        IEnemy next = Spawn(current, progress.Victories);
        // Применяем только после успешной проверки всего файла.
        Player = restored; Victories = progress.Victories; CurrentEnemy = next;
        LastAction = "Прогресс восстановлен. Появился новый противник.";
    }

    private EnemyPool RequirePool() => pool ?? throw new InvalidOperationException("Сначала откройте список противников.");
    private IEnemy Spawn(EnemyPool source, int victories) => EnemyFactory.Create(source.Select(random.NextDouble()), victories, random);
}
