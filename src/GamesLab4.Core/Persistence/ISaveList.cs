namespace GamesLab4.Core.Persistence;

/// <summary>Контракт хранения: потребитель работает с T независимо от формата файла.</summary>
public interface ISaveList<T>
{
    T Load(string path);
    void Save(T data, string path);
}
