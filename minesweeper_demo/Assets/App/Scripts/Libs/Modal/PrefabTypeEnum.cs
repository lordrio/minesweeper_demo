using System;
using System.Collections.Generic;
using UnityEngine;

// type safe-enum
public sealed class PrefabTypeEnum
{
    // dictionary
    private static readonly Dictionary<string, PrefabTypeEnum> Cache = new Dictionary<string, PrefabTypeEnum>();

    private readonly string name;
    private readonly int value;

    public static readonly PrefabTypeEnum GenericButton = new PrefabTypeEnum(1, ModalCreatorPrefabsName.CreatorButton);
    public static readonly PrefabTypeEnum GenericButtonGreen = new PrefabTypeEnum(4, ModalCreatorPrefabsName.CreatorButtonGreen);
    public static readonly PrefabTypeEnum GenericText = new PrefabTypeEnum(2, ModalCreatorPrefabsName.CreatorText);
    public static readonly PrefabTypeEnum GenericInput = new PrefabTypeEnum(3, ModalCreatorPrefabsName.CreatorInputField);

    // 色々追加する (未対応)
    public static readonly PrefabTypeEnum GenericWeb = new PrefabTypeEnum(100, "GenericWeb");
    public static readonly PrefabTypeEnum GenericToggle = new PrefabTypeEnum(101, "GenericToggle");
    public static readonly PrefabTypeEnum GenericToggleGroup = new PrefabTypeEnum(102, "GenericToggleGroup");

    // CustomPrefab
    public static readonly PrefabTypeEnum CustomPrefab = new PrefabTypeEnum(-1, "CustomPrefab");

    private PrefabTypeEnum(int value, string name)
    {
        this.name = name;
        this.value = value;
        Cache[name] = this;
    }

    public override string ToString()
    {
        return this.name;
    }

    public int ToValue()
    {
        return this.value;
    }

    // Casting
    public static explicit operator PrefabTypeEnum(string str)
    {
        PrefabTypeEnum result;
        if (Cache.TryGetValue(str, out result))
        {
            return result;
        }
        else
        {
            throw new InvalidCastException();
        }
    }
}
