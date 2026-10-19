namespace Grimoire.Clients
{
    /// <summary>
    /// Keeps native rendering surfaces out of the compositor when they are not
    /// needed while preserving the existing timing behavior of background bots.
    /// </summary>
    public static class ClientSurfacePolicy
    {
        public static bool ShouldRenderClientSurface(
            bool homeWorkspaceVisible,
            bool isActive,
            bool botRunning)
        {
            return botRunning || (!homeWorkspaceVisible && isActive);
        }
    }
}
