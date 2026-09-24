using System.Globalization;
using System.IO;
using System.Text;
using Ionic.Zlib;
using Newtonsoft.Json;

namespace Hypnonema.Server.Rpc;

public static class RpcPacker
{
    private static readonly JsonSerializer Serializer = new()
    {
        Culture = CultureInfo.CurrentCulture,
        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
        NullValueHandling = NullValueHandling.Ignore
    };

    public static byte[] Serialize(object obj)
    {
        using (var jsonStream = new MemoryStream())
        using (var streamWriter = new StreamWriter(jsonStream, new UnicodeEncoding(false, false)))
        {
            Serializer.Serialize(streamWriter, obj);
            streamWriter.Flush();

            return jsonStream.ToArray();
        }
    }

    public static T Deserialize<T>(string json)
    {
        using (var jsonReader = new JsonTextReader(new StringReader(json)))
        {
            return Serializer.Deserialize<T>(jsonReader);
        }
    }

    public static byte[] Pack(OutboundMessage message)
    {
        var json = Serialize(message);

        using (var compressedStream = new ZlibStream(new MemoryStream(json, false), CompressionMode.Compress,
                   CompressionLevel.Default, false))
        using (var outputStream = new MemoryStream())
        {
            compressedStream.CopyTo(outputStream);

            return outputStream.ToArray();
        }
    }

    public static InboundMessage Unpack(byte[] data)
    {
        using (var compressionStream = new ZlibStream(new MemoryStream(data, false), CompressionMode.Decompress, false))
        using (var streamReader = new StreamReader(compressionStream, new UnicodeEncoding(false, false)))
        {
            return Deserialize<InboundMessage>(streamReader.ReadToEnd());
        }
    }
}