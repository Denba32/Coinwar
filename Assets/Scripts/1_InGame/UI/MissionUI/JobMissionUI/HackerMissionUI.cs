using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Missions;
using StockGame.Scripts.UI.Missions;
using StockGame.Scripts.Utility;
using System;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static StockGame.Scripts.Define.GameDefine.JobDefine.HackerSkill;

public sealed class HackerMissionUI : MissionUIBase
{
    [SerializeField] private RectTransform backgroundRect;
    [SerializeField] private Image successImage;
    [SerializeField] private Image failedImage;
    [SerializeField] private Image questionImage;

    [SerializeField] private TMP_Text problemText;
    [SerializeField] private TMP_Text questionText;

    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private HackerAction hackerAction;

    public string Question { get; private set;}
    public string Answer { get; private set; }
    private string questionAnswer = string.Empty;

    public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
    {
        base.Initilaize(sortOrder, uiLayer);
        yesButton.OnClickAsObservableFirst().Subscribe(OnClickYes).AddTo(this);
        noButton.OnClickAsObservableFirst().Subscribe(OnClickNo).AddTo(this);
    }

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);

        foreach (var arg in args)
        {
            if (arg is HackerAction hackerAction)
            {
                this.hackerAction = hackerAction;
                break;
            }
        }

        var table = Managers.Master.GetTable("HackerQuizTable");
        var list = table.GetAllData().ToList();
        if (list == null || list.Count <= 0)
        {
            Debug.LogError("해커 퀴즈 마스터 데이터를 찾을 수 없습니다");
            return;
        }

        list.Shuffle();
        Question = list[0].Get<string>(nameof(Question));
        Answer = list[0].Get<string>(nameof(Answer));

        var random = UnityEngine.Random.Range(0, 1f);
        bool isCorrect = random >= 0.5f;
        if(isCorrect)
            questionAnswer = Answer;
        else
        {
            var index = UnityEngine.Random.Range(1, list.Count);
            questionAnswer = list[index].Get<string>(nameof(Answer));
        }

        problemText.text = Question;
        questionText.text = $"???→{questionAnswer}";
    }

    private void OnClickYes(Unit _)
    {
        bool isCorrect = string.Equals(questionAnswer, Answer, StringComparison.OrdinalIgnoreCase);
        yesButton.enabled = false;
        noButton.enabled = false;
        if (isCorrect)
            ShowSuccess().Forget();
        else
            ShowFailed().Forget();
    }

    private void OnClickNo(Unit _)
    {
        bool isCorrect = !string.Equals(questionAnswer, Answer, StringComparison.OrdinalIgnoreCase);
        yesButton.enabled = false;
        noButton.enabled = false;
        if (isCorrect)
            ShowSuccess().Forget();
        else
            ShowFailed().Forget();
    }

    protected override async UniTask ShowSuccess()
    {
        MissionResolver.Notify(new MissionActionEvent(StockGame.Scripts.Define.GameDefine.MissionDefine.MissionActionType.CompleteInteraction, StockGame.Scripts.Define.GameDefine.MissionDefine.MissionTargetType.Laptop));

        backgroundRect.gameObject.SetActive(false);
        successImage.gameObject.SetActive(true);

        await UniTask.WaitForSeconds(1f, cancellationToken: destroyCancellationToken);
        if (destroyCancellationToken.IsCancellationRequested || this == null) return;
        successImage.gameObject.SetActive(false);
        isPopupAnimation = false;

        hackerAction.OnSuccess?.Invoke(); // 성공 콜백 → blindable 발동
        Close(Unit.Default);
    }

    protected override async UniTask ShowFailed()
    {
        backgroundRect.gameObject.SetActive(false);
        failedImage.gameObject.SetActive(true);

        await UniTask.WaitForSeconds(1f, cancellationToken: destroyCancellationToken);
        if (destroyCancellationToken.IsCancellationRequested || this == null) return;
        failedImage.gameObject.SetActive(false);
        backgroundRect.gameObject.SetActive(true);
        isPopupAnimation = false;
        Close(Unit.Default);
    }

    public override void OnDispose()
    {
        base.OnDispose();
        hackerAction.OnClose?.Invoke();
    }
}