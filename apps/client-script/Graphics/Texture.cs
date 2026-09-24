using System;

namespace Hypnonema.Client.Graphics;

public sealed class Texture
{
    public Texture(string dictionaryName, string name)
    {
        DictionaryName = dictionaryName ?? throw new ArgumentNullException(nameof(dictionaryName));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string DictionaryName { get; }

    public string Name { get; }
}