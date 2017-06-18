using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;
using System.Linq;

namespace UniRx.Triggers
{
    public static class ObservableLongPointerDownTriggerExtension
    {
        public static IObservable<bool> OnLongPointerDownAsObservable(this Transform gameObject)
        {
            if (gameObject == null) return Observable.Empty<bool>();
            return gameObject.GetOrAddComponent<ObservableLongPointerDownTrigger>().OnLongPointerDownAsObservable();
        }
    }

    [DisallowMultipleComponent]
    public class ObservableLongPointerDownTrigger : ObservableTriggerBase,
    IPointerClickHandler,
    IPointerDownHandler,
    //IPointerUpHandler,
    IDragHandler,
    IEndDragHandler,
    IBeginDragHandler
    //IPointerExitHandler
    {
        public float IntervalSecond = 1f;

        Subject<bool> onLongPointerDown;
        private ScrollRect parentScrollRect;

        float? raiseTime;

        private void Awake()
        {
            Transform query = this.transform;

            do
            {
                parentScrollRect = query.GetComponent<ScrollRect>();

                if ((null != parentScrollRect) && parentScrollRect && parentScrollRect.enabled)
                {
                    break;
                }

                query = query.parent;
            }
            while ((null != query) && query && (null != query.parent) && query.parent);
        }

        private void Update()
        {
            if (raiseTime != null && raiseTime <= Time.realtimeSinceStartup)
            {
                if (onLongPointerDown != null) onLongPointerDown.OnNext(true);
                raiseTime = null;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            dragged = false;
            raiseTime = Time.realtimeSinceStartup + IntervalSecond;
        }

        //public void OnPointerUp(PointerEventData eventData)
        //{
        //    Debug.Log("[y3] up");
        //    return;

        //    if (raiseTime.HasValue && raiseTime <= Time.realtimeSinceStartup)
        //    {
        //        if (onLongPointerDown != null) onLongPointerDown.OnNext(true);
        //    }
        //    raiseTime = null;

        //    this.OnPointerExit(eventData);
        //}

        public IObservable<bool> OnLongPointerDownAsObservable()
        {
            return onLongPointerDown ?? (onLongPointerDown = new Subject<bool>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (onLongPointerDown != null)
            {
                onLongPointerDown.OnCompleted();
            }
        }

        private bool dragged = false;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (onLongPointerDown != null && !dragged && raiseTime != null)
            {
                if (!raiseTime.HasValue || raiseTime > Time.realtimeSinceStartup)
                {
                    onLongPointerDown.OnNext(false);
                    raiseTime = null;
                }
            }
        }

        //public void OnPointerExit(PointerEventData eventData)
        //{
        //    return;
        //    //Debug.Log("[y3] exit");
        //    //raiseTime = null;
        //    //if (parentScrollRect != null)
        //    //{
        //    //    parentScrollRect.enabled = true;

        //    //    // This is for Touch based input, ScrollRect needs to be notified for elastic movement.
        //    //    parentScrollRect.OnEndDrag(eventData);
        //    //}
        //}

        public void OnDrag(PointerEventData eventData)
        {
            if ((start - eventData.position).sqrMagnitude >= 10 * 10)
            {
                //Debug.Log("overdrag");
                raiseTime = null;
                dragged = true;
                if (parentScrollRect != null)
                {
                    parentScrollRect.OnDrag(eventData);
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if ((start - eventData.position).sqrMagnitude < 10 * 10)
            {
                //Debug.Log("ok");
                if (raiseTime.HasValue && raiseTime <= Time.realtimeSinceStartup)
                {
                    if (onLongPointerDown != null) onLongPointerDown.OnNext(true);
                }
                raiseTime = null;
            }
            if (parentScrollRect != null)
            {
                parentScrollRect.OnEndDrag(eventData);
            }
        }

        private Vector2 start;
        public void OnBeginDrag(PointerEventData eventData)
        {
            start = eventData.position;
            OnPointerDown(eventData);

            if (parentScrollRect != null)
            {
                parentScrollRect.OnBeginDrag(eventData);
            }
        }
    }
}