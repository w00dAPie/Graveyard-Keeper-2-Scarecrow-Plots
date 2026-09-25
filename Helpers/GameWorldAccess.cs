namespace GK2ScarecrowPlots.Helpers
{
    internal static class GameWorldAccess
    {
        internal static WorldData Current
        {
            get
            {
                // MainGame.WorldData dereferences Instance.GameSave without null guards.
                // BepInEx starts the watcher before either is guaranteed to exist.
                MainGame game = MainGame.Instance;
                return game != null ? game.GameSave?.worldData : null;
            }
        }
    }
}
