using Cysharp.Threading.Tasks;
using StockGame.Scripts.Datas;
using StockGame.Scripts.Manager;
using System;
using UniRx;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.U2D.Animation;
using static StockGame.Scripts.Define.GameDefine;
using static StockGame.Scripts.Define.GameDefine.CharacterDefine;
using static StockGame.Scripts.Define.GameDefine.JobDefine;

namespace StockGame.Scripts.Players
{
    public enum PlayerStateType
    {
        Idle,
        Walk,
        Stun
    }

    public enum PlayerConditionType
    {
        None,
        Stun,
        Poisoned,
        Arrested,
        Slow,
        Shiled,
    }

    public class PlayerFiniteStateMachine
    {
        private PlayerIdleState idleState;
        private PlayerWalkState walkState;
        private PlayerStunState stunState;

        public PlayerIdleState IdleState => idleState;
        public PlayerWalkState WalkState => walkState;
        public PlayerStunState StunState => stunState;

        public IState CurrentState { get; private set; }

        public PlayerFiniteStateMachine(PlayerNetwork player)
        {
            idleState = new PlayerIdleState(player, this, PlayerStateType.Idle.ToString());
            walkState = new PlayerWalkState(player, this, PlayerStateType.Walk.ToString());
            stunState = new PlayerStunState(player, this, PlayerStateType.Stun.ToString());
            CurrentState = idleState;
        }

        public void ChangeState(IState state, params object[] param)
        {
            CurrentState?.Exit();
            CurrentState = state;
            CurrentState?.Enter(param);
        }

        public void LogicUpdate(float deltaTime)
        {
            CurrentState?.LogicUpdate(deltaTime);
        }

        public void PhysicsUpdate(float deltaTime)
        {
            CurrentState?.PhysicsUpdate(deltaTime);
        }
    }

    public partial class PlayerNetwork : NetworkBehaviour
    {
        [SerializeField] private ClientNetworkTransform clientNetworkTransform;
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody2D rigid;
        [SerializeField] private SpriteLibrary spriteLibrary;
        [SerializeField] private SpriteRenderer spriteRender;

        [SerializeField] private CharacterDataSO characterDataSO;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private PlayerSkillActionExecutor skillExecutor;
        [SerializeField] private PlayerSkillActionReceiver skillActionReceiver;
        [SerializeField] private AnimationTriggerEvent triggerEvent;
        [SerializeField] private Vector2 dir = Vector2.zero;

        [Header("Body Parts")]
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform head;
        [SerializeField] private Transform root;

        [SerializeField] private GameObject blinder;
        [SerializeField] private GameObject hole;

        private PlayerFiniteStateMachine stateMachine;

        private readonly Subject<float> onActivatedShieldSkill = new();
        public IObservable<float> OnActivatedSupplySkill => onActivatedShieldSkill;

        private CharacterData characterData;
        private NetworkPlayerData playerData;

        #region Network Data
        private NetworkVariable<float> speed = new(7.0f, writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<bool> isInteracted = new(false, writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<bool> isFlipped = new(false, writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<PlayerConditionType> condition = new(default, writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<bool> isShielded = new(false, writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> IsShielded => isShielded;
        public NetworkVariable<float> Speed => speed;
        public NetworkVariable<bool> IsInteracted => isInteracted;
        public NetworkVariable<bool> IsFlipped => isFlipped;
        public NetworkVariable<PlayerConditionType> Condition => condition;
        #endregion Network Data

        public PlayerFiniteStateMachine StateMachine => stateMachine;
        public PlayerSkillActionExecutor SkillExecutor => skillExecutor;
        public PlayerSkillActionReceiver Receiver => skillActionReceiver;
        public AnimationTriggerEvent TriggerEvent => triggerEvent;
        public CharacterData Data => characterData;
        public NetworkPlayerData NetData => playerData;
        public float Width => spriteRender.bounds.size.x;
        public float Height => spriteRender.bounds.size.y;
        public Vector2 Direction => dir;
        public Transform Root => root;
        public Transform LeftHand => leftHand;
        public Transform RightHand => rightHand;
        public Transform Head => head;

        private void Awake()
        {
            InputManager.Instance.OnMove.Subscribe(InputMove).AddTo(this);
            InputManager.Instance.OnInteract.Subscribe(_ => InputInteract()).AddTo(this);
            InputManager.Instance.OnSkill.Subscribe(skillExecutor.Execute).AddTo(this);
            InputManager.Instance.OnClickInteract.Subscribe(_ => InputClickInteract()).AddTo(this);

            rigid.bodyType = RigidbodyType2D.Dynamic;
            rigid.gravityScale = 0;
            characterData = new CharacterData(characterDataSO);

            skillActionReceiver?.Initialize(this);
            stateMachine = new PlayerFiniteStateMachine(this);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            LobbyManager.Instance.PlayerDataList.OnListChanged += OnChangedPlayer;
            GameManager.Instance.CurrentRound.OnValueChanged += OnRoundChanged;
            isFlipped.OnValueChanged += OnFlipValueChanged;

            // 초기값 즉시 반영
            OnFlipValueChanged(isFlipped.Value, isFlipped.Value);

            interactor?.Initialize(this);

            if (!IsLocalPlayer) return;
            playerData = LobbyManager.Instance.GetPlayerDataByClientId(NetworkManager.LocalClientId);
            CameraManager.Instance.SetFollow(clientNetworkTransform.transform);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            // 모든 이벤트 구독 해제 — 씬 전환/재시작 시 중복 구독 방지
            if (LobbyManager.Instance != null)
                LobbyManager.Instance.PlayerDataList.OnListChanged -= OnChangedPlayer;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CurrentRound.OnValueChanged -= OnRoundChanged;
            }

            isFlipped.OnValueChanged -= OnFlipValueChanged;
            isInteracted?.Dispose();
            isFlipped?.Dispose();
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;
            stateMachine?.LogicUpdate(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsOwner) return;
            stateMachine?.PhysicsUpdate(Time.deltaTime);
        }

        private void OnChangedPlayer(NetworkListEvent<NetworkPlayerData> changeEvent)
        {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<NetworkPlayerData>.EventType.Add:
                case NetworkListEvent<NetworkPlayerData>.EventType.Value:
                    var playerData = LobbyManager.Instance.GetPlayerDataByClientId(OwnerClientId);
                    if (playerData.ClientId != OwnerClientId) return;
                    spriteLibrary.spriteLibraryAsset = playerData.GetSpriteLibraryAsset();
                    break;
                default: break;
            }
        }

        private void OnRoundChanged(RoundDefine.RoundInfo previousValue, RoundDefine.RoundInfo newValue)
        {
            if (!IsOwner) return;
            switch (newValue.RoundPhase)
            {
                case RoundDefine.RoundPhase.Explore:
                    ResetAllSkillStates();
                    skillExecutor?.ResetSkill();
                    skillExecutor?.Job?.Skill?.Initialize();
                    interactor?.ResetData();
                    Managers.Token.Cancel(this);
                    break;
                case RoundDefine.RoundPhase.Finish:
                    break;
            }
        }

        private void ResetAllSkillStates()
        {
            SetSpeed(7f);
            ChangeCondition(PlayerConditionType.None);
            Managers.Input.StartPlayerInput();
            skillActionReceiver?.PlayDefault();
            isShielded.Value = false;
            isInteracted.Value = false;
        }


        [Rpc(SendTo.Owner)]
        public void InitializeJobRpc(NetworkJobInfo networkJobInfo)
        {
            if (!IsOwner) return;
            var job = Managers.Job.GetJobInfoByIndex(networkJobInfo.JobIndex);
            skillExecutor?.Initialize(job);
        }

        #region Input
        private void InputMove(Vector2 dir)
        {
            if (!IsOwner) return;
            this.dir = dir;
        }

        private void InputInteract()
        {
            if (!IsOwner) return;
            interactor.Interact(Unit.Default);
        }

        private void InputClickInteract()
        {
            if (!IsOwner) return;
            interactor.TryInteractByMouse();
        }
        #endregion

        #region Movement

        public void SetMove()
        {
            if (condition.Value == PlayerConditionType.Stun)
            {
                rigid.velocity = Vector2.zero;
                return;
            }
            rigid.velocity = dir * speed.Value;
        }

        public void Turn()
        {
            if (condition.Value == PlayerConditionType.Stun) return;

            if (dir.x * speed.Value > 0)
            {
                isFlipped.Value = true;
                root.transform.rotation = Quaternion.Euler(new Vector3(0, 180f, 0));
            }
            else if (dir.x * speed.Value < 0)
            {
                isFlipped.Value = false;
                root.transform.rotation = Quaternion.Euler(Vector3.zero);
            }
        }
        #endregion

        #region Sync

        private void OnFlipValueChanged(bool previousValue, bool newValue)
        {
            if (spriteRender == null) return;
            spriteRender.flipX = newValue;
        }

        #endregion

        public void ChangeSpriteAsset(SpriteLibraryAsset asset)
        {
            if (spriteLibrary == null) return;
            spriteLibrary.spriteLibraryAsset = asset;
            spriteLibrary.RefreshSpriteResolvers();
        }

        public void ResetPosition()
        {
            isFlipped.Value = false;
            clientNetworkTransform?.Teleport(Vector3.zero, Quaternion.identity, Vector3.one);
        }

        public void Teleport(Transform destination)
        {
            clientNetworkTransform?.Teleport(destination.position, Quaternion.identity, Vector3.one);
        }

        public void Teleport(Vector3 position)
        {
            clientNetworkTransform?.Teleport(position, Quaternion.identity, Vector3.one);
        }

        [Rpc(SendTo.Owner)]
        public void TeleportOwnerRpc(Vector3 position)
        {
            if (condition.Value == PlayerConditionType.Arrested) return;
            clientNetworkTransform?.Teleport(new Vector3(position.x, position.y, 0), Quaternion.identity, Vector3.one);
        }

        public void PlayAnimation(string animation)
        {
            if (!IsOwner) return;
            animator?.Play(animation, 0);
        }

        public void SetSpeed(float speedMultiplier)
        {
            if (!IsOwner) return;
            speed.Value = speedMultiplier;
        }

        public void StopMove() => rigid.velocity = Vector2.zero;

        public void ChangeCondition(PlayerConditionType condition)
        {
            if (!IsOwner) return;
            this.condition.Value = condition;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (isFlipped != null)
            {
                isFlipped.OnValueChanged -= OnFlipValueChanged;
                isFlipped.Dispose();
                isFlipped = null;
            }

            animator = null;
            rigid = null;
            spriteLibrary = null;
            spriteRender = null;
        }
    }
}