using System.Text.Json;
using System.Text.Json.Nodes;
using GamesLab4.Core.Combat;
using GamesLab4.Core.Numbers;
using GamesLab4.Core.Persistence;
using GamesLab4.Core.Player;
using GamesLab4.Core.Templates;

namespace GamesLab4.Checks;

internal static class BattleChecks
{
    private static BigNumber N(int value) => new(value);
    private static string Data(string name) => Path.Combine(AppContext.BaseDirectory, "Data", name);
    private sealed class FixedRandom(double value) : Random { public override double NextDouble() => value; }
    private sealed class Rolls(params double[] values) : Random
    {
        private readonly Queue<double> queue = new(values);
        public override double NextDouble() => queue.Dequeue();
    }
    private sealed class Temp : IDisposable
    {
        private readonly string folder = Path.Combine(Path.GetTempPath(), "lab4-battle-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(folder);
        public string FileAt(string name) => Path.Combine(folder, name);
        public void Dispose() => Directory.Delete(folder, true);
    }

    private static GameSession Game(Random? random = null) => new(new JsonEnemySaver(), new PlayerSaver(), random);
    private static CNormalEnemyTemplate Normal(string name = "Гоблин", double chance = 1) =>
        new(name, "goblin_1.png", 10, 1, 30, 1, chance);
    private static string SingleTemplate(Temp temp)
    {
        string path = temp.FileAt("enemies.json"); new JsonEnemySaver().Save([Normal()], path);
        return path;
    }

    [Check("Фабрика: все четыре типа создаются по именам через рефлексию")]
    public static void AllTypes()
    {
        var templates = new JsonEnemySaver().Load(Data("enemies.example.json"));
        IEnemy[] enemies = templates.Select(t => EnemyFactory.Create(t, 0, new FixedRandom(.5))).ToArray();
        Verify.True(enemies[0] is CNormalEnemy);
        Verify.Equal(3, ((CArmoredEnemy)enemies[1]).Armor);
        Verify.Near(.25, ((CHealingEnemy)enemies[2]).HealChance);
        Verify.Equal(8, ((CHealingEnemy)enemies[2]).HealAmount);
        Verify.Equal(2, ((CShieldedEnemy)enemies[3]).RemainingBlocks);
        Verify.Equal("skeleton_2.png", enemies[2].IconName);
    }

    [Check("Фабрика: неизвестный класс и неверные параметры отклоняются")]
    public static void BadFactoryInput()
    {
        Verify.Throws<NotSupportedException>(() => EnemyFactory.CreateEnemy("НесуществующийТип"));
        Verify.Throws<NotSupportedException>(() => EnemyFactory.CreateEnemy("BigNumber", 20));
        Verify.Throws<ArgumentException>(() => EnemyFactory.CreateEnemy("CNormalEnemy", "Имя", 20));
    }

    [Check("Обычный противник получает полный урон через IEnemy")]
    public static void NormalHit()
    {
        IEnemy enemy = EnemyFactory.CreateEnemy("CNormalEnemy", "A", N(20), N(7), "goblin_1.png");
        HitResult hit = enemy.TakeDamage(N(9));
        Verify.Equal(N(9), hit.Damage); Verify.Equal(N(11), enemy.CurrentLife);
        Verify.True(!hit.DefeatedNow); Verify.Equal(BigNumber.Zero, hit.GoldReward);
    }

    [Check("Броня уменьшает урон, но ненулевой удар не слабее единицы")]
    public static void Armor()
    {
        IEnemy enemy = new CArmoredEnemy("A", N(20), N(7), "troll_1.png", 8);
        Verify.Equal(N(2), enemy.TakeDamage(N(10)).Damage);
        Verify.Equal(BigNumber.One, enemy.TakeDamage(N(1)).Damage);
        Verify.Equal(BigNumber.Zero, enemy.TakeDamage(BigNumber.Zero).Damage);
        Verify.Equal(N(17), enemy.CurrentLife);
    }

    [Check("Лечение заменяет удар и не поднимает здоровье выше максимума")]
    public static void Healing()
    {
        IEnemy enemy = new CHealingEnemy("A", N(20), N(7), "skeleton_2.png", .5, 8, new Rolls(.9, .1, .1));
        Verify.Equal(N(6), enemy.TakeDamage(N(6)).Damage);
        Verify.Equal(N(14), enemy.CurrentLife);
        HitResult heal = enemy.TakeDamage(N(10));
        Verify.Equal(BigNumber.Zero, heal.Damage);
        Verify.Equal(N(6), heal.Healing); Verify.Equal(N(20), enemy.CurrentLife);
        Verify.Equal(BigNumber.Zero, enemy.TakeDamage(N(10)).Healing);
        Verify.Equal(N(20), enemy.CurrentLife);
    }

    [Check("Нулевой шанс лечения даёт обычный урон")]
    public static void ZeroHealChance()
    {
        IEnemy enemy = new CHealingEnemy("A", N(20), N(7), "a.png", 0, 8, new Rolls());
        Verify.Equal(N(10), enemy.TakeDamage(N(10)).Damage);
        Verify.Equal(N(10), enemy.CurrentLife);
    }

    [Check("Щит блокирует ровно заданное число ненулевых ударов")]
    public static void Shield()
    {
        IEnemy enemy = new CShieldedEnemy("A", N(20), N(7), "a.png", 2);
        Verify.Equal(BigNumber.Zero, enemy.TakeDamage(BigNumber.Zero).Damage);
        Verify.Equal(2, ((CShieldedEnemy)enemy).RemainingBlocks);
        for (int i = 0; i < 2; i++) Verify.Equal(BigNumber.Zero, enemy.TakeDamage(N(10)).Damage);
        Verify.Equal(0, ((CShieldedEnemy)enemy).RemainingBlocks);
        Verify.Equal(N(10), enemy.TakeDamage(N(10)).Damage);
    }

    [Check("Награда за победу возвращается один раз, здоровье не уходит в минус")]
    public static void RewardOnce()
    {
        IEnemy enemy = new CNormalEnemy("A", N(20), N(30), "a.png");
        HitResult hit = enemy.TakeDamage(N(100));
        Verify.True(hit.DefeatedNow); Verify.Equal(N(20), hit.Damage); Verify.Equal(N(30), hit.GoldReward);
        Verify.Equal(BigNumber.Zero, enemy.CurrentLife);
        HitResult again = enemy.TakeDamage(N(100));
        Verify.True(!again.DefeatedNow); Verify.Equal(BigNumber.Zero, again.GoldReward);
    }

    [Check("Появление: веса нормализуются и нулевой шанс не выбирается")]
    public static void Weighted()
    {
        var pool = new EnemyPool([Normal("Ноль", 0), Normal("Первый", .2), Normal("Второй", .3)]);
        Verify.Equal("Первый", pool.Select(0).Name);
        Verify.Equal("Второй", pool.Select(.4).Name);
        Verify.Equal("Второй", pool.Select(Math.BitDecrement(1)).Name);
    }

    [Check("Пустой список и нулевая сумма шансов не начинают бой")]
    public static void EmptyPool()
    {
        Verify.Throws<ArgumentException>(() => new EnemyPool([]));
        Verify.Throws<ArgumentException>(() => new EnemyPool([Normal("Ноль", 0)]));
    }

    [Check("Кликер открывает исходный файл Лабы 1")]
    public static void LegacyGame()
    {
        var game = Game(new FixedRandom(0));
        game.LoadTemplates(Data("enemies.lab1.json"));
        Verify.True(game.CurrentEnemy is CNormalEnemy); Verify.Equal(3, game.TemplateCount);
        Verify.Equal("80", game.CurrentEnemy!.BaseLife.ToString());
    }

    [Check("JSON редактора запускает бой с выбранным подтипом")]
    public static void EditorToBattle()
    {
        using var temp = new Temp();
        var saver = new JsonEnemySaver();
        var editor = new CEnemyTemplateList(saver);
        editor.Load(Data("enemies.example.json"));
        string file = temp.FileAt("editor.json");
        editor.Save(file);

        var game = Game(new FixedRandom(.9));
        game.LoadTemplates(file);
        Verify.True(game.CurrentEnemy is CShieldedEnemy);
        game.Attack(); game.Attack();
        Verify.Equal(N(40), game.CurrentEnemy!.CurrentLife);
        game.Attack(); Verify.Equal(N(30), game.CurrentEnemy.CurrentLife);
        Verify.Equal(0, game.Victories);
    }

    [Check("Бой и улучшение сохраняются и восстанавливаются после перезапуска")]
    public static void ProgressRoundTrip()
    {
        using var temp = new Temp();
        var game = Game(new FixedRandom(0));
        game.LoadTemplates(SingleTemplate(temp));
        game.Attack(); Verify.Equal(N(30), game.Player.Gold); Verify.Equal(1, game.Victories);
        Verify.True(game.TryUpgrade()); Verify.Equal(N(10), game.Player.Gold);
        string save = temp.FileAt("progress.json"); game.SaveProgress(save);
        game.Restart(); Verify.Equal(BigNumber.Zero, game.Player.Gold);
        Verify.Equal(0, game.Victories);
        game.LoadProgress(save);
        Verify.Equal(N(10), game.Player.Gold); Verify.Equal(N(12), game.Player.Damage);
        Verify.Equal(2, game.Player.Level); Verify.Equal(N(24), game.Player.UpgradeCost);
        Verify.Equal(1, game.Victories);
    }

    [Check("Большие числа в файле прогресса сохраняются без потери точности")]
    public static void BigProgress()
    {
        using var temp = new Temp();
        ISaveList<PlayerProgress> saver = new PlayerSaver();
        string number = "1" + new string('0', 149) + "7";
        var progress = new PlayerProgress(new BigNumber(number), new BigNumber(number), 50, N(30), 4, 1.3, 1.4);
        string path = temp.FileAt("progress.json"); saver.Save(progress, path);
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        Verify.Equal(number, doc.RootElement.GetProperty("Gold").GetString());
        Verify.Equal(progress, saver.Load(path));
        var updated = progress with { Victories = 5 };
        saver.Save(updated, path);
        Verify.Equal(updated, saver.Load(path));
    }

    [Check("Повреждённый прогресс сохраняет текущего игрока и противника")]
    public static void BadProgressKeepsGame()
    {
        using var temp = new Temp();
        var game = Game(new FixedRandom(0)); game.LoadTemplates(SingleTemplate(temp)); game.Attack();
        var player = game.Player; var enemy = game.CurrentEnemy;
        string file = temp.FileAt("progress.json"); game.SaveProgress(file);
        var json = JsonNode.Parse(File.ReadAllText(file))!.AsObject(); json["Version"] = 99;
        File.WriteAllText(file, json.ToJsonString());
        Verify.Throws<JsonException>(() => game.LoadProgress(file));
        Verify.True(ReferenceEquals(player, game.Player) && ReferenceEquals(enemy, game.CurrentEnemy));
        Verify.Equal(1, game.Victories);
    }

    [Check("Некорректный JSON противников оставляет уже начатую игру")]
    public static void BadTemplateKeepsGame()
    {
        using var temp = new Temp();
        var game = Game(new FixedRandom(0)); game.LoadTemplates(SingleTemplate(temp));
        var enemy = game.CurrentEnemy;
        string bad = temp.FileAt("bad.json"); File.WriteAllText(bad, "[]");
        Verify.Throws<ArgumentException>(() => game.LoadTemplates(bad));
        Verify.True(ReferenceEquals(enemy, game.CurrentEnemy));
    }

    [Check("Сохранение игрока не перезаписывает открытый список противников")]
    public static void ProtectTemplates()
    {
        using var temp = new Temp();
        string path = SingleTemplate(temp); var game = Game(new FixedRandom(0)); game.LoadTemplates(path);
        string before = File.ReadAllText(path);
        Verify.Throws<ArgumentException>(() => game.SaveProgress(path));
        Verify.Equal(before, File.ReadAllText(path));
    }

    [Check("Масштабирование по победам сохраняет большие числа и параметры типа")]
    public static void Scaling()
    {
        var template = new CArmoredEnemyTemplate("A", "a.png", 80, 1.1, 10, 1.2, 1, 3);
        IEnemy enemy = EnemyFactory.Create(template, 2, new FixedRandom(0));
        Verify.Equal(N(96), enemy.BaseLife); Verify.Equal(N(14), enemy.Gold);
        Verify.Equal(3, ((CArmoredEnemy)enemy).Armor);
    }

    [Check("Файл прогресса с пропущенным полем отклоняется")]
    public static void RequiredProgressField()
    {
        using var temp = new Temp();
        var saver = new PlayerSaver();
        string path = temp.FileAt("progress.json");
        saver.Save(new CPlayer().Snapshot(0), path);
        var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject(); json.Remove("Gold");
        File.WriteAllText(path, json.ToJsonString());
        Verify.Throws<JsonException>(() => saver.Load(path));
    }
}
