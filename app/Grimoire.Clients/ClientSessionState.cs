namespace Grimoire.Clients
{
    public enum ClientSessionState
    {
        Created,
        Initializing,
        Ready,
        Running,
        Faulted,
        Closing,
        Disposed
    }
}
