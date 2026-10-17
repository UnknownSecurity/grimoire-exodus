using AxShockwaveFlashObjects;
using System;

namespace Grimoire.Clients
{
    public delegate void FlashBridgeCallHandler(AxShockwaveFlash flash, string function, params object[] args);

    public delegate void FlashBridgeErrorHandler(AxShockwaveFlash flash, Exception exception, string function, params object[] args);

    /// <summary>
    /// Represents the synchronous ExternalInterface boundary for one AQW client.
    /// Implementations must keep every call and callback bound to the same Flash
    /// control for their entire lifetime.
    /// </summary>
    public interface IFlashBridge : IDisposable
    {
        AxShockwaveFlash Control { get; }

        event Action<int> SwfLoadProgress;

        event FlashBridgeCallHandler FlashCall;

        event FlashBridgeErrorHandler FlashError;

        T Call<T>(string function, params object[] args);

        object Call(string function, Type resultType, params object[] args);

        T CallWithStringArguments<T>(string function, params string[] args);

        string CallString(string function, params object[] args);

        string CallGameFunction(string path, params object[] args);

        string GetGameObject(string path);

        string GetGameObjectStatic(string path);

        T GetGameObject<T>(string path, T defaultValue = default(T));

        void SetGameObject(string path, object value);
    }
}
