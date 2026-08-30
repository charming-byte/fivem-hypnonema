using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Hypnonema.Shared.Extensions;

public static class DelegateExtensions
{
    public static string PrintCallback(this Delegate callback)
    {
        return PrintCallback(callback, 0);
    }

    private static string PrintCallback(Delegate callback, int depth)
    {
        var method = callback.Method;
        var type = method.DeclaringType;
        var name = method.Name;

        if (IsCompilerGenerated(type))
        {
            // A decorator (e.g. the permission wrapper in RpcMethodBinder) closes over the real handler delegate;
            // describe that instead of the anonymous wrapper. Guard the recursion in case a closure ever holds itself.
            if (depth < 4 && callback.Target != null)
            {
                var inner = callback.Target.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Select(field => field.GetValue(callback.Target))
                    .OfType<Delegate>()
                    .FirstOrDefault();

                if (inner != null) return PrintCallback(inner, depth + 1);
            }

            var open = name.IndexOf('<');
            var close = name.IndexOf('>');
            if (open == 0 && close > 1)
            {
                var member = name.Substring(1, close - 1);
                name = member == ".ctor" ? "constructor lambda" : $"{member} lambda";
            }

            // Nested display classes ("<>c", "<>c__DisplayClassN_M`1") hang off the type that owns the lambda.
            while (IsCompilerGenerated(type)) type = type!.DeclaringType;
        }

        var parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));

        return $"{type?.Name}.{name}({parameters})";
    }

    private static bool IsCompilerGenerated(Type? type)
    {
        return type != null &&
               (type.IsDefined(typeof(CompilerGeneratedAttribute), false) || type.Name.Contains("<") ||
                type.Name.Contains("__DisplayClass"));
    }
}
