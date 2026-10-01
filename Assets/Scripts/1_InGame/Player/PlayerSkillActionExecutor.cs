using StockGame.Scripts.Manager;
using StockGame.Scripts.Players;
using System;
using UniRx;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.JobDefine;

public interface ISkillReceiver
{
    ulong OwnerId { get; }
    bool CanReceive();
    void ReceiveSkill(SkillContext context, Action apply);
}

public class PlayerSkillActionExecutor : MonoBehaviour
{
    [SerializeField] private PlayerNetwork player;
    [SerializeField] private LayerMask targetLayer;

    private JobInfo jobInfo;
    private ISkillScanner scanner;

    private float coolTime;
    private float rangeX;
    private float rangeY;
    private RangeType rangeType;

    private bool prevStatus;
    private int prevCoolSecond = -1;


    [SerializeField] private bool isSkillLocked;
    [SerializeField] private float coolTimer;
    [SerializeField] private bool isCool;
    [SerializeField] private bool isFreezeCool;

    private readonly Subject<bool> onUseSkillable = new();
    private readonly Subject<int> onSkillCooltime = new();
    public IObservable<bool> OnSkillReadyChanged => onUseSkillable;
    public IObservable<int> OnSkillCooltime => onSkillCooltime;
    public bool IsCool => isCool;
    public bool HasJob() => jobInfo?.Skill != null;
    public JobInfo Job => jobInfo;
    public float RangeX => jobInfo?.GetRangeX() ?? 0f;
    public float RangeY => jobInfo?.GetRangeY() ?? 0f;
    public RangeType RangeType => rangeType != RangeType.None ? rangeType : jobInfo.GetRangeType();
    public LayerMask TargetLayer => targetLayer;

    public void Initialize(JobInfo jobInfo)
    {
        if (jobInfo == null)
        {
            Debug.LogError("직업을 배정받지 못함");            
            return;
        }
        this.jobInfo = jobInfo;
        coolTime = jobInfo.GetCoolTime();
        coolTimer = 0;
        isSkillLocked = false;
        isCool = false;
        isFreezeCool = false;
        scanner = CreateScanner(jobInfo.GetSkillTargetType());
        targetLayer = GetSkillTargetLayer();
        NotifySkillCooltime();

        Debug.Log($"직업:{jobInfo.JobType} 스캐너 여부 : {scanner != null}");
        if (scanner != null)
        {
            rangeX = jobInfo.GetRangeX();
            rangeY = jobInfo.GetRangeY();
            rangeType = jobInfo.GetRangeType();
        }

        prevStatus = false;
    }

    public void ResetSkill() => prevStatus = false;

    public int GetSkillTargetLayer() => jobInfo.GetSkillTargetType() switch
    {
        SkillTargetType.Single => LayerMask.GetMask("SkillInteractor"),
        SkillTargetType.Multi => LayerMask.GetMask("SkillInteractor"),
        SkillTargetType.Instant => LayerMask.GetMask("SkillInteractor"),
        SkillTargetType.Object => LayerMask.GetMask("JobMissionInteractor"),
        _ => default
    };

    public ISkillScanner CreateScanner(SkillTargetType targetType)
    {
        return targetType switch
        {
            SkillTargetType.Single => new SingleSkillScanner(),
            SkillTargetType.Multi => new MultiSkillScanner(),
            SkillTargetType.Object => new ObjectSkillScanner(),
            _ => null
        };
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F12)) ResetCooltime();
#endif
        if (!player.IsOwner || !HasJob()) return;
        if (GameManager.Instance.CurrentRound.Value.RoundPhase != StockGame.Scripts.Define.GameDefine.RoundDefine.RoundPhase.Explore) return;

        if (scanner != null)
            Scan();
        else
            NotifyReady();

        UpdateCoolTime();
    }

    public void Execute(Unit _)
    {
        if (!player.IsOwner || !HasJob() || isCool || isSkillLocked || player.Condition.Value != PlayerConditionType.None)
        {
            return;
        }

        // 스킬 처리 결과를 ISkillReceiver(또는 스킬 자신)로부터 통지받는다.
        // 바로 쿨타임으로 넘기지 않고, 확실히 성공(Accepted)했을 때만 쿨타임을 시작한다.
        bool accepted = false;
        bool deferred = false;

        void OnSkillComplete(SkillResult result)
        {
            switch (result)
            {
                case SkillResult.Accepted: accepted = true; break;
                case SkillResult.Deferred: accepted = true; deferred = true; break;
                case SkillResult.Rejected: break; // 수신자가 거부 → 쿨타임 없음, 즉시 재시도 가능
            }
        }

        if (scanner == null)
        {
            // 자기 자신 대상 / 스캔 없는 스킬 (도박사, 은둔자, 설치물 등)
            jobInfo.Execute(new SkillContext(player, default, OnSkillComplete));
        }
        else
        {
            foreach (var receiver in scanner.GetAll())
            {
                if (!jobInfo.Skill.CanExecuteOn(receiver)) continue;
                jobInfo.Execute(new SkillContext(player, receiver, OnSkillComplete));
            }
        }

        // Deferred(해커·은둔자)는 스킬이 FreezeCooltime/UnfreezeCooltime 로 쿨타임을 직접 관리한다.
        if (accepted && !deferred && !isFreezeCool)
            StartCool();
    }

    private void StartCool()
    {
        isCool = true;
        coolTimer = coolTime;
        NotifyReady();
        NotifySkillCooltime();
    }

    private void UpdateCoolTime()
    {
        if (!isCool) return;
        coolTimer -= Time.deltaTime;
        if (coolTimer > 0)
        {
            NotifySkillCooltime();
            return;
        }

        isCool = false;
        coolTimer = 0;
        NotifyReady();
        NotifySkillCooltime();
    }

    private void Scan()
    {
        scanner.Scan(
            player.Receiver.gameObject,
            new Vector2(0, player.Height * 0.5f * player.transform.localScale.y),
            rangeX,
            rangeY,
            targetLayer,
            rangeType);

        NotifyReady();
    }

    private void NotifyReady()
    {
        bool isReady = !isCool && !isSkillLocked && jobInfo.CanExecutable;

        if (isReady)
            isReady = player.Condition.Value == PlayerConditionType.None;

        if (isReady && scanner != null)
            isReady = jobInfo.Skill.CanExecuteOn(scanner?.GetPrimary());

        if (prevStatus == isReady) return;
        prevStatus = isReady;
        onUseSkillable.OnNext(isReady);
    }

    public bool IsAnyReceiverInRange() => scanner.GetPrimary() != null;
    public void LockSkill(bool isLock)
    {
        isSkillLocked = isLock;
        NotifyReady();
    }

    /// <summary>
    /// 쿨타임 진행을 일시 정지한다 (효과가 지속되는 동안 쿨타임이 흐르지 않도록)
    /// </summary>
    public void FreezeCooltime()
    {
        isCool = true;
        isFreezeCool = true;
    }

    /// <summary>
    /// FreezeCooltime 상태를 해제하고 쿨타임 진행을 재개한다
    /// </summary>
    public void UnfreezeCooltime()
    {
        isFreezeCool = false;
        StartCool();
    }

    public void RechargeSkill()
    {
        coolTimer = 0;
        jobInfo?.Skill?.RechargeSkill();
    }

    private void ResetCooltime()
    {
        coolTime = 0;
        coolTimer = 0;
    }

    public void ResetData()
    {
        jobInfo = null;
        scanner = null;
    }

    public void CancelSkill() => jobInfo?.Cancel();

    private void NotifySkillCooltime()
    {
        int currentSecond = Mathf.CeilToInt(Mathf.Max(coolTimer, 0f));

        if (currentSecond == prevCoolSecond) return;

        prevCoolSecond = currentSecond;
        onSkillCooltime.OnNext(currentSecond);
    }

    private void OnDestroy()
    {
        onSkillCooltime?.Dispose();
        onUseSkillable?.Dispose();
    }
}