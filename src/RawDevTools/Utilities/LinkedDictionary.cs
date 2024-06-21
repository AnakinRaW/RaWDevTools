using System;
using System.Collections.Generic;
using System.Linq;

namespace RepublicAtWar.DevTools.Utilities;

/// <summary>
/// Special dictionary which keeps key-value pairs in order they were inserted.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
/// <param name="capacity"></param>
public class LinkedDictionary<TKey, TValue>(int capacity) where TKey : notnull
{
    private readonly Dictionary<TKey, int> _keys = new(capacity, EqualityComparer<TKey>.Default);
    private readonly List<TValue> _items = new(capacity);

    public bool AddOrReplace(TKey key, TValue value) 
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (!_keys.TryGetValue(key, out var index))
        {
            var newIndex = _items.Count;
            _items.Add(value);
            _keys.Add(key, newIndex);
            return false;
        }

        _items[index] = value;
        return true;
    }

    public IList<TValue> GetValues()
    {
        return _items.ToList();
    }
}