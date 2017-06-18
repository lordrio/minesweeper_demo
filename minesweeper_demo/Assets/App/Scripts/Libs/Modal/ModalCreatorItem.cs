using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class ModalCreatorItem
{
    public enum PrefabLocationEnum
    {
        FirstView,
        SecondView,
        ThirdView,
        Button,
    }

    private PrefabTypeEnum type;

    public PrefabTypeEnum Type
    {
        get
        {
            return this.type;
        }

        set
        {
            this.type = value;
        }
    }

    private string prefabName;

    public string PrefabName
    {
        get
        {
            return this.prefabName;
        }

        set
        {
            this.prefabName = value;
        }
    }

    private PrefabLocationEnum location;

    public PrefabLocationEnum Location
    {
        get
        {
            return this.location;
        }

        set
        {
            this.location = value;
        }
    }

    private string itemName;

    public string ItemName
    {
        get
        {
            return this.itemName;
        }

        set
        {
            this.itemName = value;
        }
    }

    public Component PrefabComponent { get; set; }
    public int ChildIndex { get; set; }
    public ModalComponent.ComponentDissmissEnum DismissType { get; set; }

    public ModalCreatorItem(PrefabTypeEnum _type, PrefabLocationEnum _loc, string _itemName, string _prefabName = null)
    {
        this.type = _type;
        this.location = _loc;
        this.itemName = _itemName;
        this.prefabName = _prefabName;
    }
}