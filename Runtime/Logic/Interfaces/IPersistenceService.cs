namespace Project.Runtime.Logic
{
    /// <summary>
    /// Abstracts save/load persistence for headless testability.
    /// Production: writes to disk/cloud. Tests: in-memory or mock.
    /// </summary>
    public interface IPersistenceService
    {
        void Save<T>(string key, T data);
        T Load<T>(string key);
        bool Exists(string key);
        void Delete(string key);
    }
}
