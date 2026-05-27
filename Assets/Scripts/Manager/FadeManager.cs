using Cysharp.Threading.Tasks;
using Denba.Common;
using DG.Tweening;
using StockGame.Scripts.UI;
using System;
using System.Threading;
using UnityEngine;
public class FadeManager : MonoSingleton<FadeManager>
{
    [SerializeField] private UI_Fade fadePrefab;
    private UI_Fade fade;

    public override void Initialize()
    {
        base.Initialize();
        CreateFade();
    }

    private void CreateFade()
    {
        fade = Instantiate(fadePrefab);
        fade.transform.SetParent(gameObject.transform);
        fade.Initilaize(9999, StockGame.Scripts.Define.GameDefine.UIDefine.UILayer.Transition);

        fade.FadeOff();
    }

    public async UniTask FadeIn(float duration, Ease ease = Ease.Linear, bool continuous = false, CancellationToken token = default)
    {
        fade.gameObject.SetActive(true);
        try
        {
            await fade.FadeIn(duration, ease, token);
        }
        catch
        {
            Debug.Log("OperationCanceledException");
        }

        if (!continuous)
        {
            fade.gameObject.SetActive(false);
        }
    }

    public async UniTask FadeOut(float duration, Ease ease = Ease.Linear, CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return;
        fade.gameObject.SetActive(true);

        try
        {
            await fade.FadeOut(duration, ease, token);
        }
        catch
        {
            Debug.Log("OperationCanceledException");
        }

        fade.gameObject.SetActive(false);
    }
}