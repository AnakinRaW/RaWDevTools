using System;

namespace RepublicAtWar.DevTools.Localization;

public readonly struct LocalizationEntry(string key, string value) : IEquatable<LocalizationEntry>
{
    public const string DeletedKeyValue = "[[[DELETED]]]";

    public string Key { get; } = key;

    public string Value { get; } = value;

    public bool Equals(LocalizationEntry other)
    {
        return Key == other.Key && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is LocalizationEntry other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Value);
    }

    public override string ToString()
    {
        return $"{Key}:{Value}";
    }

    public bool IsDeletedValue()
    {
        return Value == DeletedKeyValue;
    }
}