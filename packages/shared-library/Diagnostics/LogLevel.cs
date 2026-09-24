using System.Runtime.Serialization;

namespace Hypnonema.Shared.Diagnostics;

public enum LogLevel
{
    /// <summary>
    ///     Anything and everything you might want to know about
    ///     a running block of code.
    /// </summary>
    [EnumMember(Value = "verbose")] Verbose,

    /// <summary>
    ///     Internal system events that aren't necessarily
    ///     observable from the outside.
    /// </summary>
    [EnumMember(Value = "debug")] Debug,

    /// <summary>
    ///     The lifeblood of operational intelligence - things
    ///     happen.
    /// </summary>
    [EnumMember(Value = "info")] Information,

    /// <summary>
    ///     Service is degraded or endangered.
    /// </summary>
    [EnumMember(Value = "warning")] Warning,

    /// <summary>
    ///     Functionality is unavailable, invariants are broken
    ///     or data is lost.
    /// </summary>
    [EnumMember(Value = "error")] Error
}