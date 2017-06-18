using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public static class ModalControllerAnimation
{
    public static readonly Color Opaque = new Color(0, 0, 0, 0);

    // アニメーションの例
    public static IEnumerator ModalAnimFadeIn(RectTransform r)
    {
        var group = r.GetComponent<CanvasGroup>();
        DOTween.To(() => group.alpha, (pNewValue) => group.alpha = pNewValue, 0.0f, 0.3f)
               .From()
               .OnComplete(() =>
        {
        });

        yield return null;
    }

    public static IEnumerator ModalAnimFadeInUnlock(RectTransform r)
    {
		var group = r.GetComponent<CanvasGroup>();
		DOTween.To(() => group.alpha, (pNewValue) => group.alpha = pNewValue, 0.0f, 0.3f)
			   .From()
			   .OnComplete(() =>
		{
		});

        yield return null;
    }

    // アニメーションの例
    public static IEnumerator ModalAnimFadeOut(RectTransform r)
    {
        Animator[] animator = r.GetComponentsInChildren<Animator>();

        foreach (var a in animator)
        {
            a.enabled = false;
        }

		var group = r.GetComponent<CanvasGroup>();
		DOTween.To(() => group.alpha, (pNewValue) => group.alpha = pNewValue, 0.0f, 0.3f)
			   .OnComplete(() =>
		{
		});

        yield return new WaitForSeconds(0.3f);
    }
}
