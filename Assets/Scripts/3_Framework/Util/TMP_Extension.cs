using System;
using TMPro;
using UniRx;
using UnityEngine.Events;

public static class TMP_Extension
{
    public static IObservable<string> OnValueChangedAsObservable(this TMP_InputField input)
    {
        return Observable.FromEvent<UnityAction<string>, string>(
            handler => new UnityAction<string>(handler),
            h => input.onValueChanged.AddListener(h),
            h => input.onValueChanged.RemoveListener(h)
        );
    }

    public static IObservable<string> OnEndEditAsObservable(this TMP_InputField input)
    {
        return Observable.FromEvent<UnityAction<string>, string>(
            handler => new UnityAction<string>(handler),
            h => input.onEndEdit.AddListener(h),
            h => input.onEndEdit.RemoveListener(h)
        );
    }
}