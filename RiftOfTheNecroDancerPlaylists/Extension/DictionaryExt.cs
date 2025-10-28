using System.Collections.Generic;

namespace RiftOfTheNecroDancerPlaylists.Extension;

public static class DictionaryExt
{
    public static TCollection GetOrCreate<TKey, TCollection>(
        this Dictionary<TKey, TCollection> dictionary,
        TKey key
    )
    where TKey : notnull
    where TCollection : class, new()
    {
        if (!dictionary.TryGetValue(key, out var collection))
        {
            collection = new TCollection();
            dictionary[key] = collection;
        }
        return collection;
    }
}
