namespace Grimoire.Clients
{
    /// <summary>
    /// Selects the renderer used by newly created AQW client sessions.
    /// Numeric values are persisted, so existing values must remain stable.
    /// </summary>
    public enum GameEngineKind
    {
        FlashActiveX = 0,
        Ruffle = 1
    }
}
