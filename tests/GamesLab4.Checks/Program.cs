using System.Reflection;
using System.Text;

namespace GamesLab4.Checks;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class CheckAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

internal static class Verify
{
    public static void True(bool value, string message = "Условие не выполнено.")
    {
        if (!value) throw new InvalidOperationException(message);
    }
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Ожидалось: {expected}; получено: {actual}");
    }
    public static void Near(double expected, double actual) =>
        True(Math.Abs(expected - actual) <= 0.000001, $"Ожидалось {expected}; получено {actual}");
    public static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; } // Принимаем наследников исключения, как ThrowsAny в xUnit.
        throw new InvalidOperationException($"Ожидалось исключение {typeof(T).Name}.");
    }
}

internal static class Program
{
    public static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        int passed = 0, failed = 0;
        var methods = Assembly.GetExecutingAssembly().GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(m => m.GetCustomAttribute<CheckAttribute>() is not null).OrderBy(m => m.DeclaringType!.Name).ThenBy(m => m.Name);
        foreach (var method in methods)
        {
            string name = method.GetCustomAttribute<CheckAttribute>()!.Name;
            try { method.Invoke(null, null); passed++; Console.WriteLine($"УСПЕХ: {name}"); }
            catch (Exception e)
            {
                failed++; Console.WriteLine($"ОШИБКА: {name}: {(e.InnerException ?? e).Message}");
            }
        }
        Console.WriteLine($"Всего: {passed + failed}; успешно: {passed}; ошибок: {failed}");
        return failed == 0 ? 0 : 1;
    }
}
