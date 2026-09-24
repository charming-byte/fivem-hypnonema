using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Hypnonema.Client.Graphics;

public static class NamedRenderTargetRegistry
{
    private static readonly Dictionary<string, int> ReferenceCounts = new(StringComparer.OrdinalIgnoreCase);

    public static int Acquire(string name, Model model)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));

        if (!API.IsNamedRendertargetRegistered(name))
            API.RegisterNamedRendertarget(name, false);

        if (!API.IsNamedRendertargetLinked(model))
            API.LinkNamedRendertarget(model);

        if (!API.IsNamedRendertargetRegistered(name))
            return 0;

        ReferenceCounts[name] = ReferenceCounts.TryGetValue(name, out var count) ? count + 1 : 1;

        return API.GetNamedRendertargetRenderId(name);
    }

    public static void Release(string name)
    {
        if (name == null || !ReferenceCounts.TryGetValue(name, out var count)) return;

        if (count > 1)
        {
            ReferenceCounts[name] = count - 1;
            return;
        }

        ReferenceCounts.Remove(name);

        API.ReleaseNamedRendertarget(name);
    }
}