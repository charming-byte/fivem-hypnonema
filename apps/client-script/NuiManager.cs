using System;
using CitizenFX.Core.Native;
using Hypnonema.Client.Diagnostics;
using Hypnonema.Shared.Extensions;
using Hypnonema.Shared.Serialization;

namespace Hypnonema.Client;

public static class NuiManager
{
    private static readonly Logger logger = new("Nui");

    public static void UnregisterNuiCallback(string callback)
    {
        API.UnregisterRawNuiCallback(callback);
    }

    public static void Once(string eventName, Action action)
    {
        API.RegisterNuiCallback(eventName, new Action<dynamic, dynamic>((_, callback) =>
        {
            CallbackWrapper(action)(_, callback);
            UnregisterNuiCallback(eventName);
        }));
    }

    private static Action<dynamic, dynamic> CallbackWrapper(Action action)
    {
        void WrappedFunc(dynamic data, dynamic callback)
        {
            try
            {
                callback(new { success = true });
                action();
            }
            catch (Exception exception)
            {
                logger.Error(exception,
                    $"NUI callback failure for '{action.PrintCallback()}': {exception.Message}");
                callback(new { success = false, error = exception.Message });
            }
        }

        return WrappedFunc;
    }

    private static Action<dynamic, dynamic> CallbackWrapper<T>(Action<T> action)
    {
        return (data, callback) =>
        {
            try
            {
                string serialized = Serializer.Serialize(data, Serializer.NuiSerializerSettings);
                var typedData = Serializer.Deserialize<T>(serialized, Serializer.NuiSerializerSettings);

                action(typedData);
                callback(new { success = true });
            }
            catch (Exception exception)
            {
                logger.Error(exception,
                    $"NUI callback failure for '{action.PrintCallback()}': {exception.Message}");
                callback(new { success = false, error = exception.Message });
            }
        };
    }

    public static void RegisterNuiCallback(string callback, Action action)
    {
        API.RegisterNuiCallback(callback, CallbackWrapper(action));
    }

    public static void RegisterNuiCallback<T>(string @event, Action<T> action)
    {
        API.RegisterNuiCallback(@event, CallbackWrapper(action));
    }
}