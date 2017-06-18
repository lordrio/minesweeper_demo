using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UniRx;

public class ModalObject
{
    // ポップアップのスケール
    public enum ModalScaleEnum
    {
        NoScale,
        OneThird,
        TwoThird,
        FullScreen,
    }

    // ModalFactoryに使われてるModalタイプ
    public enum ModalTypeEnum
    {
        OneView_HorizontalButton,
        OneView_VerticalButton,
    }

    /// <summary>
    /// モダルの優先順位
    /// </summary>
    public enum ModalPriority
    {
        NORMAL, // 普通
        URGENT, // 最優先
    }

    public struct DissmissData
    {
        public float after;
        public ModalComponent.ComponentDissmissEnum type;

        public void SetEmpty()
        {
            after = -1.0f;
            type = ModalComponent.ComponentDissmissEnum.None;
        }
    }

    // 初期化
    public delegate void OnInitDelegate(Dictionary<string, ModalComponent> component, UnityEngine.Events.UnityAction OnSuccess, UnityEngine.Events.UnityAction OnFailure);

    // 成功
    public delegate void OnSuccessDelegate(Dictionary<string, ModalComponent> component);

    // キャンセル
    public delegate void OnCancelDelegate();

    // 閉じる時に呼ばれる、成功またはキャンセルは関係なく
    public delegate void OnCloseDelegate();

    // アニメーション開始
    public delegate IEnumerator OnModalAppearDelegate(RectTransform baseObject);

    // アニメーション終了
    public delegate IEnumerator OnModalDisappearDelegate(RectTransform baseObject);

    public static readonly Vector2 AutoSize = new Vector2(-1, -1);

    public bool playOpenSE = true;
    public bool playSuccessCloseSE = true;
    public bool playFailCloseSE = true;

    private OnInitDelegate onInitFunc = null;
    private OnSuccessDelegate onSuccessFunc = null;
    private OnCancelDelegate onCancelFunc = null;
    private OnCloseDelegate onCloseFunc = null;
    private OnModalAppearDelegate onModalAppearFunc = null;
    private OnModalDisappearDelegate onModalDisappearFunc = null;

    private string prefabName = string.Empty;
    private Vector2 preferedSize = AutoSize;
    private ModalScaleEnum preferedSizeEnum = ModalScaleEnum.NoScale;
    private RectTransform basePrefab = null;
    private ModalCreatorItem[] modalCreatorItemList = null;
    private RectTransform[] topPrefab = null;
    private UniRx.Tuple<ModalCreatorItem, RectTransform>[] bottomPrefab = null;
    private string[] componentToRemove = null;
    private DissmissData dismissData;
    private float showAfterSeconds = 0.0f;

    public UnityEngine.Events.UnityAction controllerDissmissCallbackSuccess { get; set; }
    public UnityEngine.Events.UnityAction controllerDissmissCallbackCancel { get; set; }
    public UnityEngine.Events.UnityAction controllerDissmissCallbackFail { get; set; }

    private ModalPriority priority = ModalPriority.NORMAL;
    public ModalPriority Priority
    {
        get
        {
            return priority;
        }
        set
        {
            priority = value;
        }
    }

    private Stack<Tuple<RectTransform, ModalPriority>> stackedPopup;
    public Stack<Tuple<RectTransform, ModalPriority>> StackedPopup
    {
        get
        {
            return stackedPopup;
        }
    }

    //[System.Obsolete("deprecated")]
    public enum ModalMode
    {
        NORMAL,
        WITH_PARTICLE,
    }

    //[System.Obsolete("deprecated")]
    public ModalMode mode { get; set; }
    public bool DontDissmissOnTap { get; set; }
    public Dictionary<string, ModalComponent> Components { get; set; }

    // ---- getters and setters ----
    public string PrefabName
    {
        get
        {
            return this.prefabName;
        }
    }

    public DissmissData DismissDataItem
    {
        get
        {
            return this.dismissData;
        }
    }

    public float ShowAfterSeconds
    {
        get
        {
            return this.showAfterSeconds;
        }
    }

    public string[] ComponentToRemove
    {
        get
        {
            return this.componentToRemove;
        }
    }

    public Vector2 PreferedSize
    {
        get
        {
            return this.preferedSize;
        }

        set
        {
            this.preferedSize = value;
        }
    }

    public ModalScaleEnum PreferedSizeEnum
    {
        get
        {
            return this.preferedSizeEnum;
        }
    }

    public RectTransform BasePrefab
    {
        get
        {
            return this.basePrefab;
        }
    }

    public ModalCreatorItem[] ModalCreatorItemList
    {
        get
        {
            return this.modalCreatorItemList;
        }
    }

    public RectTransform[] TopPrefab
    {
        get
        {
            return this.topPrefab;
        }
    }

    public UniRx.Tuple<ModalCreatorItem, RectTransform>[] BottomPrefab
    {
        get
        {
            return this.bottomPrefab;
        }
    }

    // ---- Methods ----

    /// <summary>
    /// Initializes a new instance of the <see cref="ModalObject"/> class.
    /// </summary>
    /// <param name="_prefabName">Prefab name.</param>
    public ModalObject(string _prefabName)
    {
        this.prefabName = _prefabName;
        this.dismissData.SetEmpty();
        this.DontDissmissOnTap = false;
        this.mode = ModalMode.WITH_PARTICLE;
        this.basePrefab = ModalController.Instance.LoadPrefab(this.prefabName);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:ModalObject"/> class.
    /// Resourceを介さずPrefab参照で生成する
    /// </summary>
    /// <param name="prefab">Prefab.</param>
    public ModalObject(ModalComponent prefab)
    {
        this.prefabName = prefab.name;
        this.dismissData.SetEmpty();
        this.DontDissmissOnTap = false;
        this.mode = ModalMode.WITH_PARTICLE;
        this.basePrefab = prefab.GetComponent<RectTransform>();
    }


    /// <summary>
    /// サイズの設定
    /// </summary>
    /// <returns>The prefered size.</returns>
    /// <param name="size">Size.</param>
    public ModalObject SetPreferedSize(Vector2 size)
    {
        this.preferedSize = size;
        return this;
    }

    /// <summary>
    /// サイズの設定
    /// </summary>
    /// <returns>The prefered size.</returns>
    /// <param name="size">Size.</param>
    public ModalObject SetPreferedSize(ModalScaleEnum size)
    {
        this.preferedSizeEnum = size;
        return this;
    }

    /// <summary>
    /// Sets the priority.
    /// </summary>
    /// <returns>The priority.</returns>
    /// <param name="prior">Prior.</param>
    public ModalObject SetPriority(ModalPriority prior)
    {
        this.priority = prior;
        return this;
    }

    /// <summary>
    /// Sets the extra component.
    /// </summary>
    /// <returns>The extra component.</returns>
    /// <param name="items">Items.</param>
    public ModalObject SetExtraComponent(params ModalCreatorItem[] items)
    {
        this.modalCreatorItemList = items;
        return this;
    }

    /// <summary>
    /// Removes the component.
    /// </summary>
    /// <returns>The component.</returns>
    /// <param name="removeItems">Remove items.</param>
    public ModalObject RemoveComponent(params string[] removeItems)
    {
        this.componentToRemove = removeItems;
        return this;
    }

    /// <summary>
    /// Modalを何秒後に閉じる。
    /// </summary>
    /// <returns>The after.</returns>
    /// <param name="seconds">Seconds.</param>
    /// <param name="dismissType">Dismiss type.</param>
    public ModalObject DismissAfter(float seconds, ModalComponent.ComponentDissmissEnum dismissType)
    {
        this.dismissData = new DissmissData();
        this.dismissData.after = seconds;
        this.dismissData.type = dismissType;
        return this;
    }

    /// <summary>
    /// Modal Controller用
    /// </summary>
    public void OnInitCreatorItems()
    {
        if (this.modalCreatorItemList == null)
        {
            return;
        }

        var top = new List<RectTransform>();
        var bottom = new List<UniRx.Tuple<ModalCreatorItem, RectTransform>>();
        string prefabname;

        foreach (ModalCreatorItem it in this.modalCreatorItemList)
        {
            RectTransform item = null;
            if (it.PrefabComponent == null)
            {
                prefabname = it.Type.ToValue() == PrefabTypeEnum.CustomPrefab.ToValue() ? it.PrefabName : it.Type.ToString();
                item = ModalController.Instance.LoadPrefab(prefabname);
            }
            else
            {
                item = it.PrefabComponent.gameObject.transform as RectTransform;
            }

            if (item != null)
            {
                // 先にインスタンス化する
                item = GameObject.Instantiate<RectTransform>(item);
                item.GetComponent<ModalComponent>().ComponentName = it.ItemName;
                switch (it.Location)
                {
                    case ModalCreatorItem.PrefabLocationEnum.Button:
                        bottom.Add(UniRx.Tuple.Create(it, item));
                        break;

                    case ModalCreatorItem.PrefabLocationEnum.FirstView:
                        top.Add(item);
                        break;

                    default:
                        break;
                }
            }
        }
            
        this.topPrefab = top.Count <= 0 ? null : top.ToArray();
        this.bottomPrefab = bottom.Count <= 0 ? null : bottom.ToArray();
    }
        
    /// <summary>
    /// PopUpを表示する
    /// </summary>
    /// <returns>The popup.</returns>
    /// <param name="popupAfterSeconds">何秒後を表示する</param>
    public ModalObject ShowPopup(float popupAfterSeconds = 0.0f)
    {
        this.showAfterSeconds = popupAfterSeconds;

        ModalController.Instance.OnShowModal(this);

        return this;
    }

    /// <summary>
    /// Popupのプッシュ
    /// </summary>
    /// <param name="popupAfterSeconds">Popup after seconds.</param>
    public ModalObject PushPopup(float popupAfterSeconds = 0.0f)
    {
        this.showAfterSeconds = popupAfterSeconds;

        this.stackedPopup = this.stackedPopup ?? new Stack<Tuple<RectTransform, ModalPriority>>();

        ModalController.Instance.OnPushModal(this);

        return this;
    }

    /// <summary>
    /// PopUPを非表示する
    /// 未対応
    /// </summary>
    /// <returns>The popup.</returns>
    public void DismissPopup(bool isSuccess = true)
    {
        if (isSuccess)
        {
            if (controllerDissmissCallbackSuccess != null)
            {
                controllerDissmissCallbackSuccess();
            }
        }
        else
        {
            if (controllerDissmissCallbackCancel != null)
            {
                controllerDissmissCallbackCancel();
            }
        }
    }

    /// <summary>
    /// Raises the init event.
    /// </summary>
    /// <param name="oninit">Oninit.</param>
    public ModalObject OnInit(OnInitDelegate oninit)
    {
        this.onInitFunc = oninit;
        return this;
    }

    /// <summary>
    /// Modal Controller用
    /// </summary>
    /// <returns>The on init.</returns>
    public OnInitDelegate GetOnInit()
    {
        return this.onInitFunc;
    }

    /// <summary>
    /// Raises the success event.
    /// </summary>
    /// <param name="onsuc">Onsuc.</param>
    public ModalObject OnSuccess(OnSuccessDelegate onsuc)
    {
        this.onSuccessFunc = onsuc;
        return this;
    }

    /// <summary>
    /// Modal Controller用
    /// </summary>
    /// <returns>The on success.</returns>
    public OnSuccessDelegate GetOnSuccess()
    {
        return this.onSuccessFunc;
    }

    /// <summary>
    /// Raises the cancel event.
    /// </summary>
    /// <param name="oncan">Oncan.</param>
    public ModalObject OnCancel(OnCancelDelegate oncan)
    {
        this.onCancelFunc = oncan;
        return this;
    }

    /// <summary>
    /// Modal Controller用
    /// </summary>
    /// <returns>The on cancel.</returns>
    public OnCancelDelegate GetOnCancel()
    {
        return this.onCancelFunc;
    }

    /// <summary>
    /// Raises the close event.
    /// </summary>
    /// <param name="onclo">Onclo.</param>
    public ModalObject OnClose(OnCloseDelegate onclo)
    {
        this.onCloseFunc = onclo;
        return this;
    }

    /// <summary>
    /// Modal Controller用
    /// </summary>
    /// <returns>The on close.</returns>
    public OnCloseDelegate GetOnClose()
    {
        return this.onCloseFunc;
    }

    /// <summary>
    /// Raises the modal appear event.
    /// </summary>
    /// <param name="onappear">Onappear.</param>
    public ModalObject OnModalAppear(OnModalAppearDelegate onappear)
    {
        this.onModalAppearFunc = onappear;
        return this;
    }

    /// <summary>
    /// Modal Controller用
    /// </summary>
    /// <returns>The on appear.</returns>
    public OnModalAppearDelegate GetOnAppear()
    {
        return this.onModalAppearFunc;
    }

    /// <summary>
    /// Raises the modal disapppear event.
    /// </summary>
    /// <param name="ondissapear">Ondissapear.</param>
    public ModalObject OnModalDisapppear(OnModalDisappearDelegate ondissapear)
    {
        this.onModalDisappearFunc = ondissapear;
        return this;
    }

    /// <summary>
    /// Modal Controller用
    /// </summary>
    /// <returns>The on disappear.</returns>
    public OnModalDisappearDelegate GetOnDisappear()
    {
        return this.onModalDisappearFunc;
    }

    //[System.Obsolete("deprecated")]
    public ModalObject OnSetModalMode(ModalMode newMode)
    {
        this.mode = newMode;
        return this;
    }

    public ModalObject SetDontDissmissOnTap(bool value)
    {
        this.DontDissmissOnTap = value;
        return this;
    }

    public ModalCreatorItem InsertButton(PrefabTypeEnum type, int location, string name, ModalComponent.ComponentDissmissEnum dismissType = ModalComponent.ComponentDissmissEnum.None)
    {
        var comp = new ModalCreatorItem(
                    type,
                    ModalCreatorItem.PrefabLocationEnum.Button,
                    name);
        comp.ChildIndex = location;
        comp.DismissType = dismissType;

        return comp;
    }
    // -------------------------------------------------------------------------------------
    // Fast Setup
    // -------------------------------------------------------------------------------------

    /// <summary>
    /// 簡単なPopup作成
    /// </summary>
    /// <param name="onSuccess">On success.</param>
    /// <param name="onCancel">On cancel.</param>
    /// <param name="setting">Setting.</param>
    public ModalObject Popup(
        System.Action onSuccess,
        System.Action onCancel,
        params string[] setting)
    {
        return this.Popup(
            _ =>
            {
                if (onSuccess != null)
                {
                    onSuccess();
                }
            },
            () =>
            {
                if (onCancel != null)
                {
                    onCancel();
                }
            },
            setting);
    }

    private void UpdateText(UnityEngine.UI.Text textItem, string data)
    {
        textItem.text = data;
    }

    private ModalObject PopupConstructor(
        OnSuccessDelegate onSuccess,
        OnCancelDelegate onCancel,
        params string[] setting)
    {
        ModalComponent comp;
        UnityEngine.UI.Text t;

        this.OnInit((component, OnSuccess, OnFailure) =>
            {
                for (int i = 0; i < setting.Length; i += 2)
                {
                    if (component.TryGetValue(setting[i], out comp))
                    {
                        if ((t = comp.GetComponent<UnityEngine.UI.Text>()) != null)
                        {
                            UpdateText(t, setting[i + 1]);
                        }
                        else if ((t = comp.GetComponentInChildren<UnityEngine.UI.Text>()) != null)
                        {
                            UpdateText(t, setting[i + 1]);
                        }
                    }
                }
            })
            .OnModalAppear(ModalControllerAnimation.ModalAnimFadeIn)
            .OnModalDisapppear(ModalControllerAnimation.ModalAnimFadeOut);

        if (onSuccess != null)
        {
            this.OnSuccess(onSuccess);
        }

        if (onCancel != null)
        {
            this.OnCancel(onCancel);
        }

        return this;
    }

    /// <summary>
    /// 簡単なPopup作成
    /// </summary>
    /// <param name="onSuccess">On success.</param>
    /// <param name="onCancel">On cancel.</param>
    /// <param name="setting">Setting.</param>
    public ModalObject Popup(
        OnSuccessDelegate onSuccess,
        OnCancelDelegate onCancel,
        params string[] setting)
    {
        PopupConstructor(onSuccess, onCancel, setting);
        return this;
    }

    // -------------------------------------------------------------------------------------
    // Static Class
    // -------------------------------------------------------------------------------------

    /// <summary>
    /// ModalのFactory
    /// </summary>
    /// <returns>The factory.</returns>
    /// <param name="items">Items.</param>
    public static ModalObject ModalFactory(params ModalCreatorItem[] items)
    {
        var obj = new ModalObject("");
        obj.modalCreatorItemList = items;

        return obj;
    }

    /// <summary>
    /// Creates the modal.
    /// </summary>
    /// <returns>The modal.</returns>
    /// <param name="prefabName">Prefab name.</param>
    public static ModalObject CreateModal(string prefabName)
    {
        return new ModalObject(prefabName);
    }

    public struct ExtraButtonData
    {
        public string ComponentName;
        public string TextData;
        public int ButtonLocation;
        public ModalComponent.ComponentDissmissEnum DismissType;
        public PrefabTypeEnum PrefabType;
    }

    public static ModalObject PopupExtra(
        string prefabName,
        string title,
        string content,
        string cancelButton = null,
        System.Action<int> onOK = null,
        System.Action onCancel = null,
        bool showPopup = true,
        params string[] okButton
        )
    {
        var button = new ExtraButtonData[okButton.Length];
        for (int i = 0; i < okButton.Length; ++i)
        {
            button[i] = new ModalObject.ExtraButtonData()
            {
                ButtonLocation = 1 + i,
                DismissType = ModalComponent.ComponentDissmissEnum.Success,
                ComponentName = "extra" + i,
                TextData = okButton[i],
                PrefabType = PrefabTypeEnum.GenericButtonGreen,
            };
        }

        var result = 0;

        var p = Popup(prefabName, title, content, null, cancelButton, () =>
        {
            if (onOK != null)
            {
                onOK(result);
            }
        }, onCancel, showPopup, button);

        var prev = p.onInitFunc;
        p.OnInit((component, OnSuccess, OnFailure) =>
        {
            Debug.Log("construct");
            for (int i = 0; i < okButton.Length; ++i)
            {
                var idx = i;
                var b = component["extra" + i].FetchComponent<Button>();
                b.OnClickAsObservable()
                 .ThrottleFirst(System.TimeSpan.FromMilliseconds(1000))
                 .Subscribe(_ =>
                {
                    result = idx;
                }).AddTo(b);
            }

            if (prev != null)
            {
                prev(component, OnSuccess, OnFailure);
            }
        });

        return p;
    }

    public static ModalObject Popup(
        string prefabName,
        string title,
        string content,
        string okButton,
        string cancelButton = null,
        System.Action onOK = null,
        System.Action onCancel = null,
        bool showPopup = true,
        // string - componentName,
        // string - textdata,
        // string - buttonLocation,
        // ComponentDissmissEnum - dismisstype,
        // PrefabTypeEnum - button type
        params ExtraButtonData[] extraButton)
    {
        List<string> setting = new List<string>();

        if (!string.IsNullOrEmpty(title))
        {
            setting.Add(ModalConstant.Title);
            setting.Add(title);
        }

        if (!string.IsNullOrEmpty(content))
        {
            setting.Add(ModalConstant.Label);
            setting.Add(content);
        }

        if (!string.IsNullOrEmpty(okButton))
        {
            setting.Add(ModalConstant.Ok);
            setting.Add(okButton);
        }

        if (!string.IsNullOrEmpty(cancelButton))
        {
            setting.Add(ModalConstant.Cancel);
            setting.Add(cancelButton);
        }

        var p = CreateModal(prefabName);

        if (extraButton != null && extraButton.Length > 0)
        {
            var item = new List<ModalCreatorItem>();
            foreach (var i in extraButton)
            {
                var ex = p.InsertButton(i.PrefabType, i.ButtonLocation, i.ComponentName, i.DismissType);
                item.Add(ex);

                if (!string.IsNullOrEmpty(i.TextData))
                {
                    setting.Add(i.ComponentName);
                    setting.Add(i.TextData);
                }
            }

            p.SetExtraComponent(item.ToArray());
        }

        p.Popup(
            onOK,
            onCancel,
            setting.ToArray());

        return showPopup ? p.PushPopup() : p;
    }

    /// <summary>
    /// ヘルパーさん
    /// </summary>
    /// <param name="component">Component.</param>
    /// <param name="isEnable">If set to <c>true</c> is enable.</param>
    public static void ModalEnableHelper(Dictionary<string, ModalComponent> component, bool isEnable = true)
    {
        UnityEngine.UI.Selectable g;
        foreach (KeyValuePair<string, ModalComponent> c in component)
        {
            g = c.Value.GetComponent<UnityEngine.UI.Selectable>();
            if (g != null)
            {
                g.interactable = isEnable;
            }
        }
    }
}
