using System;

namespace Hypnonema.Server.Rpc;

public class PermissionRequiredAttribute(string requiredPermission) : Attribute
{
    public string RequiredPermission { get; } = requiredPermission;

    public string? DeniedEvent { get; set; }
}