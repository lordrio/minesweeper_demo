#define QUEUE
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Utils;
using UniRx;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class ModalController : SingletonMonoBehaviour<ModalController>
{
    private class EventBlocker : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
        }
    }

    private Color blockingCanvasColor = new Color(0, 0, 0, 0.5f);
    private const float BlurAmount = 1.5f;
    private Vector2 referenceResolution = new Vector2(800, 600);
    private Canvas canvas;
    private RectTransform blockingCanvas = null;
    private Dictionary<string, RectTransform> prefabCache = new Dictionary<string, RectTransform>();
    private List<Tuple<RectTransform, ModalObject.ModalPriority>> child = new List<Tuple<RectTransform, ModalObject.ModalPriority>>();

#if QUEUE
    private Queue modalQueue = new Queue();
#endif

    public enum SortingLayerModeType
    {
        Normal,
        Battle,
    }
    public SortingLayerModeType sortingLayerMode = SortingLayerModeType.Normal;
    public SortingLayerModeType SortingLayerMode { set { sortingLayerMode = value; } }
    private Camera canvasCamera = null;
    public Camera CanvasCamera { set { canvasCamera = value; } }
    private string DefaultLayerTagName = "Default";

    public Vector2 CanvasSize
    {
        get { return canvas.pixelRect.size; }
    }

    // Use this for initialization
    private void Awake()
    {
        SortingLayerMode = SortingLayerModeType.Normal;

        canvas = transform.GetOrAddComponent<GraphicRaycaster>()
            .GetOrAddComponent<CanvasScaler>()
            .GetOrAddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.sortingOrder = 20000;
        canvas.sortingLayerName = this.SortingLayerName(null);

        CanvasScaler scale = transform.GetComponent<CanvasScaler>();

        scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scale.referenceResolution = referenceResolution;
        scale.referencePixelsPerUnit = 100;

        canvas.enabled = false;
    }

    private string SortingLayerName(ModalObject modal)
    {
        switch(sortingLayerMode){
            case SortingLayerModeType.Battle:
                return "UIFront";
            default:
                {
                    if (modal != null)
                    {
                        switch (modal.Priority)
                        {
                            case ModalObject.ModalPriority.URGENT:
                                return "UIFront";

                            default:
                                break;
                        }
                    }
                    return "UIMiddle";
                }
        }
    }

    //[System.Obsolete("deprecated")]
    private void SetupNormalMode(bool mode)
    {
        if (mode)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
    }

    private void SetupParticleMode(ModalObject modal)
    {
        var cnt = SceneManager.sceneCount;
        for (int i = 0; i < cnt; ++i)
        {
            var sc = SceneManager.GetSceneAt(i);

            // チェック②
            if (!sc.isLoaded || !sc.IsValid())
            {
                continue;
            }

            if (sc.GetRootGameObjects().FirstOrDefault((arg) =>
             {
                 var cam = arg.GetComponent<Camera>();
                 if (cam != null && cam.gameObject.activeSelf)
                 {
                     canvas.worldCamera = cam;
                     return true;
                 }

                 return false;
             }) != null)
            {
                break;
            }
        }

        canvas.sortingLayerName = this.SortingLayerName(modal);
    }

    private void SetupBattleMode(ModalObject modal)
    {
        if (sortingLayerMode == SortingLayerModeType.Battle)
        {
            // canvas用のcameraが設定されている場合はそれをセットする
            if (canvasCamera != null)
            {
                canvas.worldCamera = canvasCamera;
            }
            canvas.sortingLayerName = this.SortingLayerName(modal);
            var layerMask = LayerMask.NameToLayer("UI");
            gameObject.layer = layerMask;
            SetLayerTagToChild(canvas.gameObject, layerMask);
        }
        else
        {
            var layerMask = LayerMask.NameToLayer(DefaultLayerTagName);
            gameObject.layer = layerMask;
            SetLayerTagToChild(canvas.gameObject, layerMask);
        }
    }

    private void SetLayerTagToChild(GameObject obj, int layer)
    {
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            var c = obj.transform.GetChild(i);
            c.gameObject.layer = layer;
            if (0 < c.transform.childCount)
            {
                SetLayerTagToChild(c.gameObject, layer);
            }
        }
    }

    // スケールを交換
    public Vector2 ConvertScaling(ModalObject.ModalScaleEnum scaleType)
    {
        Vector2 scaling = ModalObject.AutoSize;
        RectTransform rt = canvas.transform as RectTransform;
        switch (scaleType)
        {
            case ModalObject.ModalScaleEnum.FullScreen:
                scaling = new Vector2(rt.sizeDelta.x - 100, rt.sizeDelta.y - 100);
                break;

            case ModalObject.ModalScaleEnum.OneThird:
                scaling = new Vector2(rt.sizeDelta.x / 3.0f, rt.sizeDelta.y / 3.0f);
                break;

            case ModalObject.ModalScaleEnum.TwoThird:
                scaling = new Vector2(rt.sizeDelta.x * 2.0f / 3.0f, rt.sizeDelta.y * 2.0f / 3.0f);
                //Debug.Log(scaling);
                break;

            default:
                break;
        }

        return scaling;
    }

    // Prefabをロードする
    public RectTransform LoadPrefab(string prefabName)
    {
        RectTransform modalItem = null;
        if (!prefabCache.TryGetValue(prefabName, out modalItem))
        {
            modalItem = Resources.Load<RectTransform>(prefabName);
            prefabCache.Add(prefabName, modalItem);
        }

        return modalItem;
    }

    private void OnDestroyModal(RectTransform modalItem, ModalObject modal)
    {
        if (modalItem == null) 
        {
            return;
        }

        Destroy(modalItem.gameObject);
        child.Clear();
        //Debug.Log("<color=red>child clear </color>");

        if (modal.StackedPopup != null &&
           modal.StackedPopup.Count > 0)
        {
            while (modal.StackedPopup.Count > 0)
            {
                var pop = modal.StackedPopup.Pop();
                pop.Item1.SetParent(blockingCanvas, true);
                pop.Item1.SetAsLastSibling();
                child.Add(pop);
                //Debug.Log("<color=red>child pop added </color>" + modalItem);
            }

            if (child.Count > 0 && sortingLayerMode != SortingLayerModeType.Battle)
            {
                var p = child[0].Item2;
                canvas.sortingLayerName = p == ModalObject.ModalPriority.NORMAL ? "UIMiddle" : "UIFront";
            }
        }
        else
        {
            OnCompleteDestroy();
        }
    }

    private void _OnShowModal(ModalObject modal)
    {
        if (blockingCanvas == null) 
        {
            return;
        }

        RectTransform canvasrt = canvas.transform as RectTransform;
        blockingCanvas.sizeDelta = canvasrt.sizeDelta;

        // ModalPrefabをインスタンス化する
        RectTransform modalItem = Instantiate<RectTransform>(modal.BasePrefab);
        modalItem.SetParent(blockingCanvas, false);
        child.Add(Tuple.Create(modalItem, modal.Priority));
        //Debug.Log("<color=red>child added </color>" + modalItem);

        modalItem.GetOrAddComponent<EventBlocker>();
        modalItem.GetOrAddComponent<CanvasGroup>();

        var cc = modalItem.GetComponentsInChildren<Canvas>(true);
        var pp = modalItem.GetComponentsInChildren<ParticleSystem>(true);

        // 次のフレームに実行
        Observable.NextFrame()
                  .Subscribe(_ =>
        {
            foreach (var i in cc)
            {
                i.sortingOrder += 20001;
                i.sortingLayerName = this.SortingLayerName(modal);
            }

            foreach (var i in pp)
            {
                var r = i.GetComponent<Renderer>();
                r.sortingOrder += 20001;
                r.sortingLayerName = this.SortingLayerName(modal);
            }
        });
        SetupNormalMode(modal.mode == ModalObject.ModalMode.WITH_PARTICLE);
        SetupParticleMode(modal);
        SetupBattleMode(modal);

        modalItem.gameObject.SetActive(false);

        modal.OnInitCreatorItems();

        // トップViewがある場合
        if (modal.TopPrefab != null)
        {
            foreach (RectTransform t in modal.TopPrefab)
            {
                t.SetParent(modalItem, false);
                t.SetAsLastSibling();
            }
        }

        // ボタンViewがある場合
        var buttonArea = modalItem.GetComponentInChildren<ModalButtonArea>();
        if (buttonArea != null)
        {
            RectTransform buttonArea2 = buttonArea.transform as RectTransform;
            if (modal.BottomPrefab != null)
            {
                foreach (var b in modal.BottomPrefab)
                {
                    b.Item2.SetParent(buttonArea2, false);
                    b.Item2.SetAsLastSibling();

                    var compbtn = b.Item2.GetComponent<ModalComponent>();
                    compbtn.ComponentDismiss = b.Item1.DismissType;

                    if (b.Item1.ChildIndex >= 0)
                    {
                        b.Item2.SetSiblingIndex(b.Item1.ChildIndex);
                    }
                }
            }

            buttonArea2.SetAsLastSibling();
        }

        // Modalサイズの設定
        if (modal.PreferedSizeEnum != ModalObject.ModalScaleEnum.NoScale)
        {
            modal.PreferedSize = ConvertScaling(modal.PreferedSizeEnum);
        }

        Vector2 newSize = modalItem.sizeDelta;
        if (modal.PreferedSize.x >= 0.0f)
        {
            newSize.x = Mathf.Min(modal.PreferedSize.x, canvasrt.sizeDelta.x);
        }

        if (modal.PreferedSize.y >= 0.0f)
        {
            newSize.y = Mathf.Min(modal.PreferedSize.y, canvasrt.sizeDelta.y);
        }

        if (System.Math.Abs(modalItem.sizeDelta.sqrMagnitude - newSize.sqrMagnitude) > 0.0f)
        {
            // Size設定があるので、AutoLayoutをdisableする
            var contentSizeFitter = modalItem.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null) { contentSizeFitter.enabled = false; };
            modalItem.sizeDelta = newSize;
        }

        // ModalComponentを取得し、Dictionaryに変更する
        ModalComponent[] components = modalItem.gameObject.GetComponentsInChildren<ModalComponent>();

        Dictionary<string, ModalComponent> dicto = components.ToDictionary(x => x.ComponentName, x => x);

        // remove items
        if (modal.ComponentToRemove != null)
        {
            ModalComponent item;
            foreach (string n in modal.ComponentToRemove)
            {
                if (dicto.TryGetValue(n, out item))
                {
                    Destroy(item.gameObject);
                    dicto.Remove(n);
                }
            }
        }

        modal.Components = dicto;

        UnityEngine.Events.UnityAction onSuccessCallback = () =>
        {
            StopAllCoroutines();

            if (modal.GetOnDisappear() != null)
            {
                Observable.FromCoroutine((arg) => modal.GetOnDisappear()(modalItem))
                          .Subscribe(_ =>
                {
                    if (modal.GetOnSuccess() != null)
                    {
                        modal.GetOnSuccess()(dicto);
                    }

                    OnDestroyModal(modalItem, modal);

                    if (modal.GetOnClose() != null)
                    {
                        modal.GetOnClose()();
                    }
                });
            }
            else
            {
                if (modal.GetOnSuccess() != null)
                {
                    modal.GetOnSuccess()(dicto);
                }
                
                OnDestroyModal(modalItem, modal);

                if (modal.GetOnClose() != null)
                {
                    modal.GetOnClose()();
                }
            }
        };

        UnityEngine.Events.UnityAction onFailureCallback = () =>
        {
            StopAllCoroutines();

            if (modal.GetOnDisappear() != null)
            {
                Observable.FromCoroutine((arg) => modal.GetOnDisappear()(modalItem))
                          .Subscribe(_ =>
                {
                    if (modal.GetOnCancel() != null)
                    {
                        modal.GetOnCancel()();
                    }

                    OnDestroyModal(modalItem, modal);

                    if (modal.GetOnClose() != null)
                    {
                        modal.GetOnClose()();
                    }
                });
            }
            else
            {
                if (modal.GetOnCancel() != null)
                {
                    modal.GetOnCancel()();
                }

                OnDestroyModal(modalItem, modal);

                if (modal.GetOnClose() != null)
                {
                    modal.GetOnClose()();
                }
            }
        };

        if (modal.GetOnInit() != null)
        {
            modal.GetOnInit()(dicto, onSuccessCallback, onFailureCallback);
        }

        // 基礎の設置
        var dismissCount = 0;
        foreach (ModalComponent c in components)
        {
            switch (c.ComponentDismiss)
            {
                case ModalComponent.ComponentDissmissEnum.None:
                    break;

                case ModalComponent.ComponentDissmissEnum.Success:
                    {
                        ++dismissCount;
                        var successComponent = c;
                        c.GetComponent<Button>()
                         .OnClickAsObservable()
                         .ThrottleFirst(System.TimeSpan.FromMilliseconds(1000))
                         .Subscribe(_ => 
                        {
                            dicto[ModalConstant.PRESSED_KEY] = successComponent;
                            onSuccessCallback();
                        }).AddTo(c);
                    }
                    break;

                case ModalComponent.ComponentDissmissEnum.Cancel:
                    ++dismissCount;
                    c.GetComponent<Button>()
                        .OnClickAsObservable()
                        .ThrottleFirst(System.TimeSpan.FromMilliseconds(1000))
                        .Subscribe(_ =>
                       {
                           onFailureCallback();
                       }).AddTo(c);
                    break;

                case ModalComponent.ComponentDissmissEnum.Fail:
                    // PlaceHolder
                    ++dismissCount;
                    break;

                default:
                    break;
            }
        }

        modalItem.gameObject.SetActive(true);

        if (modal.GetOnAppear() != null)
        {
            StartCoroutine(modal.GetOnAppear()(modalItem));
        }

        if (modal.DismissDataItem.after > 0.0f)
        {
            StartCoroutine(DismissAfterSubroutine(modal.DismissDataItem, onSuccessCallback, onFailureCallback));
        }

        // Dissmissが一つの場合。
        var btn = blockingCanvas.GetOrAddComponent<Button>();
        btn.OnClickAsObservable()
            .ThrottleFirst(System.TimeSpan.FromMilliseconds(1000))
            .Where(_ => modalItem.parent == blockingCanvas)
            .Subscribe(_ =>
            {
                onSuccessCallback();
            }).AddTo(modalItem.gameObject);

        if (dismissCount == 1 && !modal.DontDissmissOnTap)
        {
            btn.enabled = true;
        }
        else
        {
            btn.enabled = false;
        }

        modal.controllerDissmissCallbackSuccess = onSuccessCallback;
        modal.controllerDissmissCallbackCancel = modal.controllerDissmissCallbackFail = onFailureCallback;
    }

    private IEnumerator DismissAfterSubroutine(ModalObject.DissmissData data, UnityEngine.Events.UnityAction onSuccess, UnityEngine.Events.UnityAction onCancel)
    {
        yield return new WaitForSeconds(data.after);

        switch (data.type)
        {
            case ModalComponent.ComponentDissmissEnum.None:
                onSuccess();
                break;

            case ModalComponent.ComponentDissmissEnum.Success:
                onSuccess();
                break;

            case ModalComponent.ComponentDissmissEnum.Cancel:
                onCancel();
                break;

            case ModalComponent.ComponentDissmissEnum.Fail:
                // PlaceHolder
                onCancel();
                break;

            default:
                break;
        }
    }

    public void OnPushModal(ModalObject modal)
    {
        Observable.Timer(System.TimeSpan.FromSeconds(modal.ShowAfterSeconds))
                  .ThrottleFrame(1)
                  .Subscribe(_ =>
        {
            if (modal.BasePrefab == null)
            {
                return;
            }

            if (modal.StackedPopup == null)
            {
                return;
            }

            if (blockingCanvas == null)
            {
                OnShowModal(modal);
                return;
            }

            if (child.Count > 0)
            {
                if (child[0].Item2 == ModalObject.ModalPriority.URGENT && modal.Priority != ModalObject.ModalPriority.URGENT)
                {
                    modalQueue.Enqueue(modal);
                    return;
                }
            }

            var stack = modal.StackedPopup;
            for (int i = child.Count - 1; i >= 0; --i)
            {
                var rt = child[i].Item1;
                if (rt != null)
                {
                    stack.Push(child[i]);
                    rt.SetParent(blockingCanvas.parent, true);
                    rt.SetAsFirstSibling();
                }
                child.RemoveAt(i);
            }

            _OnShowModal(modal);
        });
    }

    private System.IDisposable waitTimer = null;
    public void OnShowModal(ModalObject modal, bool fromQueue = false)
    {
        if (modal.BasePrefab == null)
        {
            return;
        }

        // 何か表示中の場合、キューする
#if QUEUE
        if ((blockingCanvas != null && !fromQueue) || waitTimer != null)
        {
            modalQueue.Enqueue(modal);
            return;
        }
#endif

#if ENABLE_BLUR
        blurMat = blurMat ?? Instantiate<Material>(Resources.Load<Material>(ModalCreatorPrefabsName.UiBlurMaterial));
#endif

        RectTransform canvasrt = canvas.transform as RectTransform;

        // blocking image
        if (blockingCanvas == null)
        {
            canvas.enabled = true;
            canvas.sortingLayerName = this.SortingLayerName(modal);

            //addButton = true;
            blockingCanvas = new GameObject("BlockingAgent", typeof(Image)).GetComponent<RectTransform>();
            blockingCanvas.sizeDelta = canvasrt.sizeDelta;
            blockingCanvas.GetComponent<Image>().color = blockingCanvasColor;
            blockingCanvas.SetParent(canvas.gameObject.transform, false);
#if ENABLE_BLUR
            blockingCanvas.GetComponent<Image>().material = blurMat;

            blurMat.SetFloat("_Range", 0.0f);
            blurMat.DOFloat(BlurAmount, "_Range", 0.3f);
#endif
        }

        waitTimer = Observable.Timer(System.TimeSpan.FromSeconds(modal.ShowAfterSeconds))
                  .ThrottleFrame(1)
                  .Subscribe(_ =>
                  {
                      _OnShowModal(modal);
                      waitTimer = null;
                  });
    }

    //Modalを表示を削除時 バックキーを状態を変えないフラグ
    public bool IsCompleteBlockBackKeyNoChange = false;

    // Modalを表示を削除する
    public void OnCompleteDestroy()
    {
        if (blockingCanvas == null)
        {
            return;
        }

        // キューがある場合、
#if QUEUE
        if (modalQueue.Count != 0)
        {
            ModalObject q = modalQueue.Dequeue() as ModalObject;
            OnShowModal(q, true);
            return;
        }
#else
        if (blockingCanvas.childCount > 1)
        {
            return;
        }
#endif

#if ENABLE_BLUR
        var tweener = blurMat.DOFloat(0.0f, "_Range", 0.3f)
                        .OnComplete(() =>
                        {
                            Destroy(blockingCanvas.gameObject);
                            blockingCanvas = null;
                        });

        tweener.OnUpdate(() =>
                         {
                             if (modalQueue.Count != 0)
                             {
                                 tweener.Kill(true);
                                 ModalObject q = modalQueue.Dequeue() as ModalObject;
                                 OnShowModal(q, true);
                             }
                         });
#else
        Destroy(blockingCanvas.gameObject);
        blockingCanvas = null;
        canvas.enabled = false;

        if (modalQueue.Count != 0)
        {
            ModalObject q = modalQueue.Dequeue() as ModalObject;
            OnShowModal(q, true);
        }
#endif

    }

    public void ClearPrefab()
    {
        prefabCache.Clear();
    }
}