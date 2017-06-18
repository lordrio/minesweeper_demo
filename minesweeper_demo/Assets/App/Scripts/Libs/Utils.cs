using System;
using UnityEngine;

public static class RioUtils
{
    public static T GetOrAddComponent<T>(this Transform gameObject)
        where T : Component
    {
        var component = gameObject.gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.gameObject.AddComponent<T>();
        }

        return component;
    }

    public static T GetOrAddComponent<T>(this Component gameObject)
        where T : Component
    {
        var component = gameObject.gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.gameObject.AddComponent<T>();
        }

        return component;
    }
}