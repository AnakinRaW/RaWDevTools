using System;
using PG.Commons.DataTypes;
using PG.Commons.Hashing;
using RepublicAtWar.DevLauncher.Services;

namespace RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;

public sealed class GameObject(string name, Crc32 nameCrc, GameObjectType estimatedType, KeyValuePairList<string, object> properties)
    : IHasCrc32
{
    private KeyValuePairList<string, object> _properties = properties;

    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    public Crc32 Crc32 { get; } = nameCrc;

    public GameObjectType EstimatedType { get; } = estimatedType;
}

public enum GameObjectType
{
    Unknown,
    Planet,
}