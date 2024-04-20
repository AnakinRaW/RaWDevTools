using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace RepublicAtWar.DevLauncher.Petroglyph;

// NOT THREAD-SAFE!
public class ValueListDictionary<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue?> _singleValueDictionary = new ();
    private readonly Dictionary<TKey, List<TValue?>> _multiValueDictionary = new();


    public bool ContainsKey(TKey key)
    {
        return _singleValueDictionary.ContainsKey(key) || _multiValueDictionary.ContainsKey(key);
    }

    public bool Add(TKey key, TValue? value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (!_singleValueDictionary.ContainsKey(key))
        {
            if (!_multiValueDictionary.TryGetValue(key, out var list))
            {
                _singleValueDictionary.Add(key, value);
                return false;
            }

            list.Add(value);
            return true;
        }

        Debug.Assert(_multiValueDictionary.ContainsKey(key) == false);

        var firstValue = _singleValueDictionary[key];
        _singleValueDictionary.Remove(key);

        _multiValueDictionary.Add(key, [
            firstValue,
            value
        ]);

        return true;
    }

    public TValue? GetLastValue(TKey key)
    {
        if (_singleValueDictionary.TryGetValue(key, out var value))
            return value;

        if (_multiValueDictionary.TryGetValue(key, out var valueList))
            return valueList.Last();

        throw new KeyNotFoundException($"The key '{key}' was not found.");
    }

    public IList<TValue?> GetValues(TKey key)
    {
        if (!TryGetValues(key, out var values))
            throw new KeyNotFoundException($"The key '{key}' was not found.");
        return values;
    }

    public bool TryGetValues(TKey key, [NotNullWhen(true)] out IList<TValue?>? values)
    {
        if (_singleValueDictionary.TryGetValue(key, out var value))
        {
            values = new List<TValue>(1)
            {
                value
            };
            return true;
        }

        if (_multiValueDictionary.TryGetValue(key, out var valueList))
        {
            values = valueList;
            return true;
        }

        values = null;
        return false;
    }
}