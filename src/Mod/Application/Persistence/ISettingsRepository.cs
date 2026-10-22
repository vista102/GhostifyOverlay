namespace GhostifyOverlay.Application.Persistence
{
    internal interface ISettingsRepository<T> where T : class
    {
        T Load();
        void Save(T value);
    }
}
