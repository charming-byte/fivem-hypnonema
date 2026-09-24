using CitizenFX.Core.Native;

namespace Hypnonema.Client.Dui;

public sealed class RuntimeTexture
{
    public RuntimeTexture(long duiObject, string txdName, string txnName)
    {
        TxdName = txdName;

        TxnName = txnName;

        var duiHandle = API.GetDuiHandle(duiObject);

        var txdHandle = API.CreateRuntimeTxd(txdName);

        API.CreateRuntimeTextureFromDuiHandle(txdHandle, TxnName, duiHandle);
    }

    public string TxdName { get; }

    public string TxnName { get; }
}