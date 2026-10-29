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
            return ShouldRenderClientSurface(
                homeWorkspaceVisible,
                isActive,
                botRunning,
                false);
        }

        public static bool ShouldRenderClientSurface(
            bool homeWorkspaceVisible,
            bool isActive,
            bool botRunning,
            bool requiresVisibleInitialization)
        {
            // WebView2 suspends Ruffle when every native ancestor is hidden.
            // Keep an initializing Ruffle surface alive behind Home or its
            // loading placeholder, then return to the normal compositor policy.
            return requiresVisibleInitialization
                || botRunning
                || (!homeWorkspaceVisible && isActive);
        }
    }
}
