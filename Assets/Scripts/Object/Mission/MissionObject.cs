using static StockGame.Scripts.Define.GameDefine;
using UnityEngine;
using StockGame.Scripts.UI.Missions;
using StockGame.Scripts.Manager;
using Cysharp.Threading.Tasks;
using static StockGame.Scripts.Define.GameDefine.MissionDefine;
using System;
using System.Threading;
using StockGame.Scripts.Datas;
using StockGame.Scripts.Players;
using static StockGame.Scripts.Define.GameDefine.JobDefine;
using StockGame.Scripts.Define;



#if UNITY_EDITOR
using UnityEditor;
#endif
public interface IInteractable
{
    bool CanInteract(InteractorContext ctx);
    void Interact(InteractorContext ctx);
    string GetPrompt();
}

public interface IHighlightable
{
    void SetHighlight(bool value);
}
public struct InteractorContext
{
    public NetworkPlayerData Player { get; }
    public PlayerNetwork PlayerObject { get; set; }
    public bool IsValid => PlayerObject != null;
    public InteractorContext(NetworkPlayerData player, PlayerNetwork playerObject)
    {
        Player = player;
        PlayerObject = playerObject;
    }
}

public class MissionObject : MonoBehaviour, IInteractable, IHighlightable
{
    [SerializeField] protected SpriteRenderer spriteRenderer = null;
    [SerializeField] protected bool locked = false;

    [SerializeField] private Sprite enterSprite = null;
    [SerializeField] private Sprite exitSprite = null;

    [SerializeField] private int missionId;
    [SerializeField] private MissionType missionType;
    [SerializeField] private JobType jobType;
    private CancellationTokenSource cts;
    private bool isInteracting = false;
    private Action onClose = null;
    private InteractorContext context;

    public int MissionId => missionId;
    public MissionType MissionType => missionType;
    public JobType JobType => jobType;

    public virtual bool CanInteract(InteractorContext ctx) => !locked;

    public string GetPrompt() => locked ? "Lock" : "Unlock";

    private void Start()
    {
        onClose = OnClosePopup;
    }

    public virtual void Interact(InteractorContext ctx)
    {
        if (isInteracting || locked) return;
        context = ctx;
        isInteracting = true;
        var mission = missionType == MissionType.Job ? MissionManager.Instance.GetJobMission(jobType, missionId) : MissionManager.Instance.GetMission(missionId);
        Managers.UI.Open<MissionUIBase>(uiLayer: UIDefine.UILayer.Popup, path: GetUIAddress(), mission, onClose).Forget();
    }

    private void OnClosePopup()
    {
        cts?.Cancel();
        cts = new();
        OnCloseActionAsync(cts.Token).Forget();
    }

    private async UniTask OnCloseActionAsync(CancellationToken token)
    {
        if (context.IsValid) context.PlayerObject.IsInteracted.Value = false;
        await UniTask.WaitForSeconds(1.5f, cancellationToken: token);
        isInteracting = false;
    }

    public void SetHighlight(bool isActive)
    {
        if (locked)
        {
            Debug.Log("Lock");
            spriteRenderer.sprite = exitSprite;
            return;
        }
        Debug.Log("UnLock");
        spriteRenderer.sprite = isActive ? enterSprite : exitSprite;
    }

    public void ResetMission(Mission mission)
    {
        if (mission.MissionId != missionId)
        {
            Debug.Log("Mission Object : 미션 리셋이 불가능한 오브젝트 : Id가 틀림");
            return;
        }
        Debug.Log("Mission Object : 미션 오브젝트 리셋 : Id가 맞음");
        locked = false;
    }

    public void Complete(Mission mission)
    {
        if (mission.MissionId != missionId)
        {
            Debug.Log("Mission Object : 미션 클리어가 불가능한 오브젝트 : Id가 틀림");
            return;
        }
        locked = true;
        spriteRenderer.sprite = exitSprite;
    }

    public void Lock(bool isLock)
    {
        locked = isLock;
    }

    public void SetInteract(bool isInteract) => isInteracting = isInteract;

    private string GetUIAddress()
    {
        return $"Mission/MSS{missionId}";
    }

#if UNITY_EDITOR

    [ContextMenu(nameof(AutoInsertImage))]
    public void AutoInsertImage()
    {
        var targetRenderer = GetComponentInChildren<SpriteRenderer>();
        spriteRenderer = targetRenderer;
        var sprite = targetRenderer.sprite;
        var spriteName = sprite.name;
        missionId = int.Parse(name.Substring(name.Length-3, 3));
        
        Debug.Log(spriteName);

        var path = AssetDatabase.GetAssetPath(sprite);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path);

        Debug.Log("Path " + path);

        var index = spriteName.LastIndexOf("_");
        var length = spriteName.Length;
        var number = spriteName.Substring(index + 1, length - index - 1);
        if (int.TryParse(number, out var parsedNumber))
        {
            var targetNumber = parsedNumber + 1;
            var targetName = spriteName.Substring(0, index) + "_" + targetNumber;
            Debug.Log($"Target Name : {targetName}");
            var lastIndex = path.LastIndexOf('_');
            var lastPath = path.Substring(0, lastIndex);
            Debug.Log(lastPath);
            var targetPath = path.Replace(".png", "") + "_" + targetNumber + ".png";
            Debug.Log(targetPath);


            foreach (var s in sprites)
            {
                if (s is Sprite sp && sp.name == targetName)
                {
                    enterSprite = sp;
                    break;
                }
            }
            exitSprite = targetRenderer.sprite;
        }
        if (gameObject.GetComponent<Rigidbody2D>() == null)
        {
            var rigid = gameObject.AddComponent<Rigidbody2D>();
            rigid.bodyType = RigidbodyType2D.Static;
        }

    }
#endif

    private void OnDestroy()
    {
        cts?.Dispose();
    }
}