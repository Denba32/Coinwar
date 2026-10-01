using Cysharp.Threading.Tasks;
using StockGame.Common.Interfaces;
using StockGame.Scripts.Datas;
using StockGame.Scripts.Manager;
using StockGame.Scripts.MasterDatas;
using StockGame.Scripts.Missions;
using StockGame.Scripts.Players;
using StockGame.Scripts.Scenes;
using StockGame.Scripts.Skills;
using StockGame.Scripts.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace StockGame.Scripts.Define
{
    public static class GameDefine
    {
        #region PLAYER_ASSET_PATH
        public static string CharacterLibraryAssetPath = "Characters/{0}/CHA{1}{2}";
        public static string CharacterProfilePath = "Profile/{0}/CHA{1}{2}";
        #endregion PLAYER_ASSET_PATH

        public struct NetworkPlayerJoinInfo : INetworkSerializable, IEquatable<NetworkPlayerJoinInfo>
        {
            public int Index;
            public ulong ClientId;
            public FixedString128Bytes Nickname;
            public CharacterType CharacterType;
            public JobDefine.NetworkJobInfo JobInfo;
            public long Money;
            public int Coin;
            public int Score;

            #region Default Method
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Index);
                serializer.SerializeValue(ref ClientId);
                serializer.SerializeValue(ref Nickname);
                serializer.SerializeValue(ref CharacterType);
                serializer.SerializeValue(ref JobInfo);
                serializer.SerializeValue(ref Money);
                serializer.SerializeValue(ref Coin);
                serializer.SerializeValue(ref Score);
                JobInfo.NetworkSerialize(serializer);
            }

            public bool Equals(NetworkPlayerJoinInfo other)
            {
                return Index == other.Index
                    && ClientId == other.ClientId
                    && Nickname.Equals(other.Nickname)
                    && JobInfo.Equals(other.JobInfo)
                    && CharacterType.Equals(other.CharacterType);
            }

            public override bool Equals(object obj)
            {
                return obj is NetworkPlayerJoinInfo other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Index, ClientId, Nickname, JobInfo);
            }

            public static bool operator ==(NetworkPlayerJoinInfo left, NetworkPlayerJoinInfo right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(NetworkPlayerJoinInfo left, NetworkPlayerJoinInfo right)
            {
                return !left.Equals(right);
            }
            #endregion Default Method

            public string GetName => Nickname.ToString();
            public void ResetData()
            {
                Money = 0;
                Coin = 0;
            }
        }

        #region STOCK
        public static class StockDefine
        {
            public const long CoinValueInWon = 1000000;
            /// <summary>
            /// 동기화 주식 정보
            /// </summary>
            public struct NetworkStockInfo : INetworkSerializable, IEquatable<NetworkStockInfo>
            {
                public int StockId;
                public FixedString32Bytes StockName;
                public long BeforePrice;
                public long AfterPrice;
                public float GainProbability; // 상승 확률
                public float LossProbability; // 하락 확률
                public float MinGainRate; // 최소 가격 상승 비율
                public float MaxGainRate; // 최대 가격 상승 비율
                public float MinLossRate; // 최소 가격 하락 비율
                public float MaxLossRate; // 최대 가격 하락 비율
                public int PurchasedCount; // 판매된 주식 개수

                public NetworkStockInfo(int stockId, FixedString32Bytes stockName, long beforePrice, long afterPrice, float gainProbability, float lossProbability, float minGainRate, float maxGainRate, float minLossRate, float maxLossRate)
                {
                    StockId = stockId;
                    StockName = stockName;
                    BeforePrice = beforePrice;
                    AfterPrice = afterPrice;
                    GainProbability = gainProbability;
                    LossProbability = lossProbability;
                    MinGainRate = minGainRate;
                    MaxGainRate = maxGainRate;
                    MinLossRate = minLossRate;
                    MaxLossRate = maxLossRate;
                    PurchasedCount = 0;
                }

                public void UpdateDiff()
                {
                    BeforePrice = AfterPrice;
                }

                public float CalculateStockDelta()
                {
                    if (AfterPrice <= 0 || BeforePrice <= 0) return 0;
                    return ((float)(AfterPrice - BeforePrice) / BeforePrice) * 100f;
                }

                public Sprite GetStockProfile()
                {
                    string path = $"Stock/STK{StockId}";
                    Debug.Log($"NetworkStockInfo: {path}");
                    return Managers.Resource.Load<Sprite>(path, ResourceDirectory.Images);
                }

                public string GetStockName()
                {
                    return StockName.ToString();
                }

                public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
                {
                    serializer.SerializeValue(ref StockId);
                    serializer.SerializeValue(ref StockName);
                    serializer.SerializeValue(ref BeforePrice);
                    serializer.SerializeValue(ref AfterPrice);
                    serializer.SerializeValue(ref MaxGainRate);
                    serializer.SerializeValue(ref MaxLossRate);
                    serializer.SerializeValue(ref GainProbability);
                    serializer.SerializeValue(ref LossProbability);
                    serializer.SerializeValue(ref PurchasedCount);
                }

                public bool Equals(NetworkStockInfo other)
                {
                    return StockId == other.StockId
                        && StockName.Equals(other.StockName);
                }

                public override bool Equals(object obj)
                {
                    return obj is NetworkStockInfo other && Equals(other);
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(StockId, StockName);
                }

                public static bool operator ==(NetworkStockInfo left, NetworkStockInfo right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(NetworkStockInfo left, NetworkStockInfo right)
                {
                    return !left.Equals(right);
                }

                public NetworkStockInfo Clone()
                {
                    return new NetworkStockInfo(StockId, StockName, BeforePrice, AfterPrice,
                        GainProbability, LossProbability, MinGainRate, MaxGainRate, MinLossRate, MaxLossRate);
                }
            }

            public struct NetworkPurchasedStock : INetworkSerializable, IEquatable<NetworkPurchasedStock>
            {
                public int StockId;
                public ulong OwnerId;
                public FixedString32Bytes StockName;
                public long PurchasePrice;
                public int PurchaseCount;

                public bool IsValid => !string.IsNullOrEmpty(StockName.Value);

                public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
                {
                    serializer.SerializeValue(ref StockId);
                    serializer.SerializeValue(ref OwnerId);
                    serializer.SerializeValue(ref StockName);
                    serializer.SerializeValue(ref PurchasePrice);
                    serializer.SerializeValue(ref PurchaseCount);
                }

                public Sprite GetStockProfile()
                {
                    string path = $"Stock/STK{StockId}";
                    return Managers.Resource.Load<Sprite>(path, ResourceDirectory.Images);
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(
                        StockId,
                        OwnerId,
                        StockName,
                        PurchasePrice,
                        PurchaseCount
                    );
                }

                public bool Equals(NetworkPurchasedStock other)
                {
                    return StockId == other.StockId &&
                           OwnerId == other.OwnerId &&
                           PurchasePrice == other.PurchasePrice;
                }

                public override bool Equals(object obj)
                {
                    return obj is NetworkPurchasedStock other && Equals(other);
                }
                public static bool operator ==(NetworkPurchasedStock left, NetworkPurchasedStock right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(NetworkPurchasedStock left, NetworkPurchasedStock right)
                {
                    return !(left == right);
                }
            }

            public readonly static List<long> DefaultStockPrice = new List<long>
            {
                1000000, 800000, 650000, 500000, 350000, 200000, 100000, 50000
            };

            public class StockInfo
            {
                public int StockNameId;
                public string StockKrName;
                public string StockEnName;

                public StockInfo(MasterDataRow row)
                {
                    StockNameId = row.Get<int>(nameof(StockNameId));
                    StockKrName = row.Get<string>(nameof(StockKrName));
                    StockEnName = row.Get<string>(nameof(StockEnName));
                }
            }

            /// <summary>
            /// 마스터 데이터를 활용한 실질적인 주식 정보
            /// </summary>
            [Serializable]
            public class StockPriceInfo
            {
                public int StockPriceId;
                public long[] StockPrice;
                public float GainProbability;
                public float LossProbability;
                public float GainRate;
                public float LossRate;
                public StockPriceInfo(MasterDataRow row)
                {
                    StockPriceId = row.Get<int>(nameof(StockPriceId));
                    StockPrice = row.Get<long[]>(nameof(StockPrice));
                    GainProbability = row.Get<float>(nameof(GainProbability));
                    LossProbability = row.Get<float>(nameof(LossProbability));
                    GainRate = row.Get<float>(nameof(GainRate));
                    LossRate = row.Get<float>(nameof(LossRate));
                }
            }

            [System.Serializable]
            public class StockRanking
            {
                public int Ranking;
                public int PlayerIndex;
                public ulong ClientId;
                public string Nickname;
                public long Profit;
                public CharacterType CharacterType;
                public int Score;       // 현재까지 누적 점수 (서버 반영용)

                public Sprite GetRankingProfile()
                {
                    return Managers.Resource.Load<Sprite>($"RankingProfile/{CharacterType.ToString()}/CHA{(int)CharacterType}{PlayerIndex.ToString("D2")}", ResourceDirectory.Images);
                }
            }

            public class ChangeProbability
            {
                public int GainPercentage { get; }
                public int LossPercentage { get; }

                public ChangeProbability(int gainPercentage, int lossPercentage)
                {
                    GainPercentage = gainPercentage;
                    LossPercentage = lossPercentage;
                }
            }

            public class ChangeRateLimit
            {
                public int GainRate { get; }
                public int LossRate { get; }

                public ChangeRateLimit(int gainRate, int lossRate)
                {
                    GainRate = gainRate;
                    LossRate = lossRate;
                }
            }

            public enum StockDirection
            {
                None,
                Increase,
                Decrease
            }
        }

        #endregion STOCK

        public static class InputDefine
        {
            public sealed class InputBindingStack : IDisposable
            {
                private readonly List<InputBindingContext> _stack = new();
                public void Push(InputBindingContext context) => _stack.Add(context);
                public void Pop(InputBindingContext context) => _stack.Remove(context);
                public bool Handle(string action)
                {
                    if (_stack.Count == 0) return false;
                    return _stack[^1].TryHandle(action);
                }

                public void Dispose()
                {
                    _stack?.Clear();
                }
            }
            public class InputBindingContext : IEquatable<InputBindingContext>, IDisposable
            {
                private string Key { get; }

                public InputBindingContext(string key) { Key = key; }
                private readonly Dictionary<string, Action> _bindings = new();

                public InputBindingContext Bind(string action, Action handler)
                {
                    _bindings[action] = handler;
                    return this;
                }

                public override bool Equals(object obj)
                {
                    return Equals(obj as InputBindingContext);
                }

                public bool Equals(InputBindingContext other)
                {
                    if (other is null) return false;
                    if (ReferenceEquals(this, other)) return true;
                    return Key == other.Key;
                }

                public override int GetHashCode()
                {
                    return Key?.GetHashCode() ?? 0;
                }

                public bool TryHandle(string action)
                {
                    if (_bindings.TryGetValue(action, out var handler))
                    {
                        handler?.Invoke();
                        return true;
                    }
                    return false;
                }

                public void Dispose()
                {
                    _bindings?.Clear();
                }
            }
        }
        public static class RoundDefine
        {
            public readonly static Dictionary<int, int> RankingScore = new()
            {
                {1, 3 },
                {2, 2 },
                {3, 2 },
                {4, 1},
                {5, 1},
                {6, 1},
                {7, -1},
                {8, -2}
            };

            public struct RoundInfo : INetworkSerializable, IEquatable<RoundInfo>
            {
                public int RoundIndex;
                public RoundPhase RoundPhase;
                public int RemainingTime;

                public RoundInfo(int roundIndex, RoundPhase roundType, int remainingTime)
                {
                    RoundIndex = roundIndex;
                    RoundPhase = roundType;
                    RemainingTime = remainingTime;
                }

                public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
                {
                    serializer.SerializeValue(ref RoundIndex);
                    serializer.SerializeValue(ref RoundPhase);
                    serializer.SerializeValue(ref RemainingTime);
                }
                public bool Equals(RoundInfo other) => RoundIndex.Equals(other.RoundIndex) && RoundPhase.Equals(other.RoundPhase);
            }

            public enum RoundPhase
            {
                None,
                RoundInfo,
                StockInfo,
                Explore,
                StockPurchaseAndSell,
                ReleaseRanking,
                Finish
            }

            #region SCORE
            public static int GetScoreFromRank(int rank)
            {
                if (RankingScore.TryGetValue(rank, out var score) == false) return -1;
                return score;
            }
            #endregion SCORE
        }
        public static class ResourceDefine
        {
            public const string IMAGE_PATH = "Images";
            public const string IMAGE_FILE_NAME = "IMG";
            public const string PREFAB_PATH = "Prefabs/";
            public const string SOUND_PATH = "Sounds";
            public const string SOUND_FILE_NAME = "CLP";

            public enum IMAGE_INDEX
            {
                BACKGROUND = 1,
                MINIMAP = 2,
                SKILL_ICON = 3,
                STOCK_ICON = 4,
            }

            public enum SOUND_INDEX
            {
                BGM = 1,
                SFX = 2,
                ENV = 3,
            }

            public enum SFX_TYPE
            {
                CHARACTER = 0,
                UI = 2,
                MISSION = 3,
                ROUND = 8
            }
            public enum UI_SOUND_TYPE
            {
                POSITIVE = 0,
                NEGATIVE = 1
            }

            public enum BGM_SOUND_TYPE
            {
                LOBBY = 0,
            }

            public sealed class FMODBus
            {
                public const string MASTER = "bus:/";
                public const string BGM = "bus:/Bgm";
                public const string SFX = "bus:/Sfx";
            }
            public sealed class FMODEvent
            {
                public const string BGM101 = "event:/BGM101";
                public const string BGM102 = "event:/BGM102";
                public const string BGM103 = "event:/BGM103";
                public const string BGM104 = "event:/BGM104";

                public const string FootStep = "event:/FootStep";

                public const string SFX200 = "event:/SFX200";
                public const string SFX202 = "event:/SFX202";
                public const string SFX204 = "event:/SFX204";
                public const string SFX206 = "event:/SFX206";
                public const string SFX208 = "event:/SFX208";
                public const string SFX210 = "event:/SFX210";
                public const string SFX211 = "event:/SFX211";
                public const string SFX212 = "event:/SFX212";
                public const string SFX213 = "event:/SFX213";
                public const string SFX214 = "event:/SFX214";
                public const string SFX215 = "event:/SFX215";
                public const string SFX216 = "event:/SFX216";
                public const string SFX217 = "event:/SFX217";
                public const string SFX218 = "event:/SFX218";
                public const string SFX219 = "event:/SFX219";
                public const string SFX220 = "event:/SFX220";

                public const string SFX221 = "event:/SFX221";
                public const string SFX222 = "event:/SFX222";
                public const string SFX223 = "event:/SFX223";
                public const string SFX224 = "event:/SFX224";
                public const string SFX225 = "event:/SFX225";
                public const string SFX226 = "event:/SFX226";
                public const string SFX227 = "event:/SFX227";
                public const string SFX230 = "event:/SFX230";
                public const string SFX231 = "event:/SFX231";
                public const string SFX232 = "event:/SFX232";
                public const string SFX233 = "event:/SFX233";
                public const string SFX234 = "event:/SFX234";
                public const string SFX235 = "event:/SFX235";
                public const string SFX236 = "event:/SFX236";
                public const string SFX237 = "event:/SFX237";
                public const string SFX238 = "event:/SFX238";
                public const string SFX239 = "event:/SFX239";
                public const string SFX240 = "event:/SFX240";

                public const string SFX241 = "event:/SFX241";
                public const string SFX242 = "event:/SFX242";
                public const string SFX243 = "event:/SFX243";
                public const string SFX244 = "event:/SFX244";
                public const string SFX245 = "event:/SFX245";
                public const string SFX246 = "event:/SFX246";
                public const string SFX247 = "event:/SFX247";
                public const string SFX248 = "event:/SFX248";
                public const string SFX249 = "event:/SFX249";

                public const string SFX250 = "event:/SFX250";
                public const string SFX251 = "event:/SFX251";
                public const string SFX252 = "event:/SFX252";
                public const string SFX253 = "event:/SFX253";
                public const string SFX254 = "event:/SFX254";
                public const string SFX260 = "event:/SFX260";

                public const string SFX261 = "event:/SFX261";
                public const string SFX262 = "event:/SFX262";
                public const string SFX263 = "event:/SFX263";
                public const string SFX264 = "event:/SFX264";
                public const string SFX265 = "event:/SFX265";
                public const string SFX266 = "event:/SFX266";
                public const string SFX267 = "event:/SFX267";
                public const string SFX268 = "event:/SFX268";
                public const string SFX270_1 = "event:/SFX270_1";
                public const string SFX270_2= "event:/SFX270_2";


            }
        }
        public static class ProbabilityDefine
        {
            public class JobProbability
            {
                public int Index { get; }
                public int JobId { get; }
                public float Probability { get; }
                public float Reward { get; }

                public JobProbability(MasterDataRow row)
                {
                    Index = row.Get<int>(nameof(Index));
                    JobId = row.Get<int>(nameof(JobId));
                    Probability = row.Get<float>(nameof(Probability));
                    Reward = row.Get<float>(nameof(Reward));
                }
            }
        }
        public static class JobDefine
        {
            public struct NetworkJobInfo : INetworkSerializable, IEquatable<NetworkJobInfo>
            {
                public int JobIndex;
                public FixedString32Bytes JobName;
                public FixedString512Bytes JobDescription;
                public JobType JobType;
                public bool IsValid => JobType != JobType.None;

                public NetworkJobInfo(int jobIndex, string jobName, string jobDescription, JobType jobType)
                {
                    JobIndex = jobIndex;
                    JobName = jobName;
                    JobDescription = jobDescription;
                    JobType = jobType;
                }

                public NetworkJobInfo(JobInfo jobInfo)
                {
                    JobIndex = jobInfo.JobId;
                    JobName = jobInfo.JobName;
                    JobDescription = jobInfo.JobDescription;
                    JobType = jobInfo.JobType;
                }

                #region Default Method
                public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
                {
                    serializer.SerializeValue(ref JobIndex);
                    serializer.SerializeValue(ref JobName);
                    serializer.SerializeValue(ref JobDescription);
                    serializer.SerializeValue(ref JobType);
                }

                public bool Equals(NetworkJobInfo other)
                {
                    return JobIndex == other.JobIndex
                        && JobName.Equals(other.JobName)
                        && JobDescription.Equals(other.JobDescription)
                        && JobType == other.JobType;
                }

                public override bool Equals(object obj)
                {
                    return obj is NetworkJobInfo other && Equals(other);
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(JobIndex, JobName, JobDescription, JobType);
                }

                public static bool operator ==(NetworkJobInfo left, NetworkJobInfo right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(NetworkJobInfo left, NetworkJobInfo right)
                {
                    return !left.Equals(right);
                }
                #endregion Default Method

                public string GetJobName() => JobName.ToString();
                public string GetJobDescription() => JobDescription.ToString();
                public Sprite GetJobSkillIconEnable() => Managers.Resource.Load<Sprite>($"Skill/SKI{JobIndex}_Enable", ResourceDirectory.Images);
                public Sprite GetJobSkillIconDisable() => Managers.Resource.Load<Sprite>($"Skill/SKI{JobIndex}_Disable", ResourceDirectory.Images);
            }

            public enum SkillTargetType
            {
                None = 0,       // 대상 불필요 (즉시 발동) - GainCoin 등
                Single = 1,     // 단일 대상
                Multi = 2,      // 다수 대상
                Instant = 3,    // 즉각 사용
                Object = 4,     // 오브젝트 대상
            }

            public enum RangeType
            {
                None,
                Box,
                Circle,
                Capsule,
            }

            public class JobInfo
            {
                public int JobId { get; }
                public string JobName { get; }
                public string JobDescription { get; }
                public JobType JobType { get; }
                public JobSkillBase Skill { get; private set; }

                public JobInfo(MasterDataRow row)
                {
                    JobId = row.Get<int>(nameof(JobId));
                    JobName = row.Get<string>(nameof(JobName));
                    JobDescription = row.Get<string>(nameof(JobDescription));
                    JobType = row.Get<JobType>(nameof(JobType));
                }

                public bool CanExecutable => Skill.CanExecutable;
                public void SetSkill(JobSkillBase skill) => Skill = skill;
                public void Execute(SkillContext context) => Skill?.Execute(context);
                public float GetCoolTime() => Skill?.CoolTime ?? 0f;
                public SkillTargetType GetSkillTargetType() => Skill?.SkillTargetType ?? SkillTargetType.None;
                public float GetRangeX() => (Skill as ScanableJobSkillBase)?.XRange ?? 0f;
                public float GetRangeY() => (Skill as ScanableJobSkillBase)?.YRange ?? 0f;
                public RangeType GetRangeType() => (Skill as ScanableJobSkillBase)?.RangeType ?? RangeType.None;
                public void Cancel() => Skill?.Cancel();
            }

            #region JobSkill

            public enum SkillResult
            {
                Rejected,   // 수신자가 받을 수 없는 상태 (이미 걸림 / 쉴드 / 상호작용 중)
                Accepted,   // 수신자가 접수 → 효과 적용 확정. Executor는 쿨타임 시작
                Deferred    // 수신자/스킬이 자체적으로 성공·쿨타임을 관리 (해커, 은둔자 등)
            }

            public class SkillParameter
            {
                public int JobSkillId { get; }
                public int JobId { get; }
                public string ParameterName { get; }
                public float EffectValue { get; }
                public string Description { get; }

                public SkillParameter(MasterDataRow row)
                {
                    JobSkillId = row.Get<int>(nameof(JobSkillId));
                    JobId = row.Get<int>(nameof(JobId));
                    ParameterName = row.Get<string>(nameof(ParameterName));
                    EffectValue = row.Get<float>(nameof(EffectValue));
                    Description = row.Get<string>(nameof(Description));
                }
            }

            public class SkillParameterSet
            {
                private readonly Dictionary<string, float> _map;
                public int JobSkillId { get; }
                public int JobId { get; }

                public SkillParameterSet(List<SkillParameter> parameters)
                {
                    JobSkillId = parameters[0].JobSkillId;
                    JobId = parameters[0].JobId;
                    _map = parameters.ToDictionary(p => p.ParameterName, p => p.EffectValue);
                }

                public float GetFloat(string key) => _map.TryGetValue(key, out var v) ? v : 0f;
                public int GetInt(string key) => (int)GetFloat(key);
                public bool GetBool(string key) => GetInt(key) == 1;
                public T GetEnum<T>(string key) where T : struct, Enum => (T)(object)GetInt(key);
            }

            public abstract class JobSkillBase
            {
                public int JobId { get; private set; }
                public float CoolTime { get; private set; }
                public SkillTargetType SkillTargetType { get; private set; }

                protected JobSkillBase(SkillParameterSet parameter)
                {
                    JobId = parameter.JobId;
                    CoolTime = parameter.GetFloat(nameof(CoolTime));
                    SkillTargetType = parameter.GetEnum<SkillTargetType>(nameof(SkillTargetType));
                }

                public virtual bool CanExecutable => true;
                public abstract void Execute(SkillContext context);
                public virtual void Initialize() { }

                /// <summary>
                /// 스캔된 receiver를 대상으로 스킬 발동이 가능한지 판단.
                /// NotifyReady()에서 호출되며, 각 스킬 클래스에서 조건을 오버라이드.
                /// </summary>
                public virtual bool CanExecuteOn(ISkillReceiver receiver) => true;
                public virtual void RechargeSkill() { }
                public virtual void Cancel() { Managers.Token.CancelAll(this); }

                /// <summary>
                /// context.Receiver를 T로 캐스팅해 효과를 적용하는 공통 경로.
                /// 실제 적용은 ISkillReceiver.ReceiveSkill을 통해서만 이뤄지므로,
                /// 수신자가 이미 다른 스킬을 처리 중이면 Rejected로 통지되고 effect는 실행되지 않는다.
                /// </summary>
                /// <typeparam name="T">적용할 효과 인터페이스 (IStunnable, IArrestHoldable 등)</typeparam>
                protected static void ApplyToReceiver<T>(SkillContext context, Action<T> effect) where T : class
                {
                    if (context.Receiver is not T target)
                    {
                        context.Complete(SkillResult.Rejected);
                        return;
                    }

                    context.Receiver.ReceiveSkill(context, () => effect(target));
                }
            }

            public abstract class ScanableJobSkillBase : JobSkillBase
            {
                public float XRange { get; private set; }
                public float YRange { get; private set; }
                public RangeType RangeType { get; private set; }

                protected ScanableJobSkillBase(SkillParameterSet parameter) : base(parameter)
                {
                    XRange = parameter.GetFloat(nameof(XRange));
                    YRange = parameter.GetFloat(nameof(YRange));
                    RangeType = parameter.GetEnum<RangeType>(nameof(RangeType));
                }

                // 스캔 스킬 기본값: receiver가 존재하고 수신 가능해야 함
                public override bool CanExecuteOn(ISkillReceiver receiver)
                    => receiver?.CanReceive() ?? false;
            }

            #region JOB_SKILL
            public sealed class FryingPanKillSkill : ScanableJobSkillBase
            {
                public float StunDuration { get; private set; }

                public FryingPanKillSkill(SkillParameterSet paramSet) : base(paramSet)
                {
                    StunDuration = paramSet.GetFloat(nameof(StunDuration));
                }

                public override void Execute(SkillContext context) => ApplyToReceiver<IStunnable>(context, stunnable =>
                {
                    RPCManager.Instance.PlayFryingPanSound(context.Receiver.OwnerId);
                    stunnable.ApplyStun(StunDuration);
                    MissionResolver.Notify(new MissionActionEvent(MissionDefine.MissionActionType.ApplyStatus, targetId: context.Receiver.OwnerId));
                });
            }

            public sealed class InsanePharmacistSkill : JobSkillBase
            {
                public float XRange { get; private set; }
                public float YRange { get; private set; }
                public float DeployDuration { get; private set; }
                public float DeployEffectTime { get; private set; }

                public InsanePharmacistSkill(SkillParameterSet parameter) : base(parameter)
                {
                    XRange = parameter.GetFloat(nameof(XRange));
                    YRange = parameter.GetFloat(nameof(YRange));
                    DeployDuration = parameter.GetFloat(nameof(DeployDuration));
                    DeployEffectTime = parameter.GetFloat(nameof(DeployEffectTime));
                }

                public override void Execute(SkillContext context)
                {
                    if (context.Sender == null)
                    {
                        context.Complete(SkillResult.Rejected);
                        return;
                    }

                    var sender = context.Sender;
                    var senderPosition = sender.LeftHand.position;
                    var senderRotation = sender.Root.rotation;
                    var poison = Managers.Resource.Instantiate<Poison>($"Skills/JSI{JobId}");
                    poison.transform.position = senderPosition;
                    poison.transform.rotation = senderRotation;
                    poison?.Initialize();
                    poison?.SetSkill(this);
                    poison?.Activate();
                    MissionResolver.Notify(new MissionActionEvent(MissionDefine.MissionActionType.Deploy, MissionDefine.MissionTargetType.Poison, context?.Receiver?.OwnerId ?? 0));

                    // 설치 성공 = 스킬 성공. 함정 발동(트리거)은 Poison 쪽에서 별도로 처리한다.
                    context.Complete(SkillResult.Accepted);
                }

                public override void Initialize() { }
            }
            public sealed class GamblerSkill : JobSkillBase
            {
                public int UseCoinCount { get; private set; }

                public override bool CanExecutable => GameManager.Instance.GetLocalPlayerInfo().Coin > 0;

                public GamblerSkill(SkillParameterSet parameter) : base(parameter)
                {
                    UseCoinCount = parameter.GetInt(nameof(UseCoinCount));
                }

                public override void Initialize()
                {
                    base.Initialize();
                }

                public override void Execute(SkillContext context)
                {
                    var localPlayerInfo = GameManager.Instance.GetLocalPlayerInfo();
                    bool isExecutable = localPlayerInfo.Coin >= UseCoinCount;
                    if (!isExecutable)
                    {
                        context.Complete(SkillResult.Rejected);
                        return;
                    }

                    var randomCoin = (int)JobManager.Instance.GetRandomRewardByJobId(JobId);
                    context.SetData(randomCoin);
                    context.Sender.Receiver.GambledCoin(randomCoin);
                    Debug.Log($"획득한 코인 수 : {randomCoin} | 최종 코인 수 : {localPlayerInfo.Coin + randomCoin - UseCoinCount}");
                    GameManager.Instance.UpdateCoinServerRpc(localPlayerInfo.Coin + randomCoin - UseCoinCount);
                    context.Complete(SkillResult.Accepted);
                }
            }
            public sealed class PoliceSkill : ScanableJobSkillBase
            {
                public float DetentionTime { get; private set; }
                public float PrisonInLocationX { get; private set; }
                public float PrisonInLocationY { get; private set; }
                public float PrisonOutLocationX { get; private set; }
                public float PrisonOutLocationY { get; private set; }

                public PoliceSkill(SkillParameterSet parameter) : base(parameter)
                {
                    DetentionTime = parameter.GetFloat(nameof(DetentionTime));
                    PrisonInLocationX = parameter.GetFloat(nameof(PrisonInLocationX));
                    PrisonInLocationY = parameter.GetFloat(nameof(PrisonInLocationY));
                    PrisonOutLocationX = parameter.GetFloat(nameof(PrisonOutLocationX));
                    PrisonOutLocationY = parameter.GetFloat(nameof(PrisonOutLocationY));
                }

                public override void Execute(SkillContext context) => ApplyToReceiver<IArrestHoldable>(context, arrestHoldable =>
                {
                    context.Sender.Receiver.PlayArrest();
                    RPCManager.Instance.PlayArrestSoundRpc(RPCManager.Instance.RpcTarget.Single(context.Receiver.OwnerId, RpcTargetUse.Temp));
                    arrestHoldable.ArrestHold(1f, DetentionTime, new Vector3(PrisonInLocationX, PrisonInLocationY, 0), new Vector3(PrisonOutLocationX, PrisonOutLocationY, 0));
                    MissionResolver.Notify(new MissionActionEvent(MissionDefine.MissionActionType.Arrest, targetId: context.Receiver.OwnerId));
                });
            }
            public sealed class HackerSkill : ScanableJobSkillBase
            {
                public class HackerQuizData
                {
                    public int Index { get; }
                    public string Question { get; }
                    public string Answer { get; }

                    public HackerQuizData(MasterDataRow row)
                    {
                        Index = row.Get<int>(nameof(Index));
                        Question = row.Get<string>(nameof(Question));
                        Answer = row.Get<string>(nameof(Answer));
                    }
                }

                public class HackerAction : IDisposable
                {
                    public Action OnSuccess;
                    public Action OnClose;
                    public void Dispose()
                    {
                        OnSuccess = null;
                        OnClose = null;
                    }
                }

                public class BlindContext
                {
                    public PlayerNetwork Player { get; }
                    public float Duration { get; }
                    public float VisibleRange { get; }

                    public BlindContext(PlayerNetwork player, float duration, float visibleRange)
                    {
                        Player = player;
                        Duration = duration;
                        VisibleRange = visibleRange;
                    }
                }

                public float VisibleRange { get; private set; }
                public float SkillDuration { get; private set; }
                public HackerAction HackerContext { get; private set; }
                private HackerMissionUI _activePopup;
                private PlayerNetwork _activeSender;

                public HackerSkill(SkillParameterSet parameter) : base(parameter)
                {
                    VisibleRange = parameter.GetFloat(nameof(VisibleRange));
                    SkillDuration = parameter.GetFloat(nameof(SkillDuration));
                }

                public override void Initialize()
                {
                    base.Initialize();
                    HackerContext = new HackerAction();
                }

                public override void Execute(SkillContext context)
                {
                    if (!context.IsValid)
                    {
                        context.Complete(SkillResult.Rejected);
                        return;
                    }

                    // 해킹 미션 시작 시점에 쿨타임 진입 (기존 동작 유지).
                    // 이후 흐름은 Condition(Hacking)과 팝업 콜백으로 관리된다.
                    context.Complete(SkillResult.Accepted);
                    ExecuteAsync(context, Managers.Token.GetToken(this)).Forget();
                }

                private async UniTask ExecuteAsync(SkillContext context, CancellationToken token = default)
                {
                    Managers.Input.StopAllInput();
                    context.Sender.ChangeCondition(PlayerConditionType.Hacking);
                    context.Sender.Receiver.PlayHacking();
                    Managers.Sound?.PlaySfx(ResourceDefine.FMODEvent.SFX239);

                    try
                    {
                        await UniTask.WaitForSeconds(1f, cancellationToken: token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    void OnSuccess()
                    {
                        Managers.Input.StartAllInput();
                        _activeSender = null;
                        context.Sender.RequestBlindRpc(context.Sender.OwnerClientId, SkillDuration, VisibleRange);
                    }

                    void OnClose()
                    {
                        context.Sender.ChangeCondition(PlayerConditionType.None);
                        _activePopup = null;
                        _activeSender = null;
                        HackerContext?.Dispose();
                    }

                    HackerContext.OnSuccess = OnSuccess;
                    HackerContext.OnClose = OnClose;
                    _activePopup = await Managers.UI.Open<HackerMissionUI>(UIDefine.UILayer.Popup, path: "Mission/MSS328", HackerContext);

                    if (token.IsCancellationRequested)
                    {
                        _activePopup?.Close();
                        _activeSender = null;
                        HackerContext?.Dispose();
                        return;
                    }

                }
                /// <summary>
                /// 외부(PlayerSkillActionExecutor 등)에서 호출되는 강제 취소 진입점.
                /// 어느 시점에서 불려도(1초 대기 중이든, 팝업이 열려 유저 응답을 기다리는 중이든)
                /// 안전하게 원상복구되도록 한다.
                /// </summary>
                public override void Cancel()
                {
                    base.Cancel(); // Managers.Token.CancelAll(this) — 대기 중인 ExecuteAsync가 있다면 즉시 취소됨

                    // 이미 팝업이 열려서 유저 응답을 기다리고 있던 경우 — 강제로 닫는다.
                    if (_activePopup != null)
                    {
                        _activePopup.Close();
                        _activePopup = null;
                    }

                    // 입력/상태는 성공(OnSuccess)이나 정상 종료(OnClose) 콜백이 처리하지만,
                    // 강제 취소 경로에서는 그 콜백들이 절대 안 불리므로 여기서 직접 복구한다.
                    if (_activeSender != null)
                    {
                        Managers.Input.StartAllInput();
                        _activeSender.ChangeCondition(PlayerConditionType.None);
                        _activeSender = null;
                    }

                    HackerContext?.Dispose();
                }
            }
            public sealed class RecluseSkill : ScanableJobSkillBase
            {
                public float CoinRewardDelay { get; private set; }
                public int RewardCoin { get; private set; }

                public RecluseSkill(SkillParameterSet parameter) : base(parameter)
                {
                    CoinRewardDelay = parameter.GetFloat(nameof(CoinRewardDelay));
                    RewardCoin = parameter.GetInt(nameof(RewardCoin));
                }

                // 은둔자는 범위 안에 아무도 없어야 발동 가능
                public override bool CanExecuteOn(ISkillReceiver receiver) => true;

                public override void Execute(SkillContext context)
                {
                    if (context.Sender == null)
                    {
                        context.Complete(SkillResult.Rejected);
                        return;
                    }

                    context.Sender.StartRecluse(CoinRewardDelay, RewardCoin);
                    Managers.Sound.PlaySfx(ResourceDefine.FMODEvent.SFX241);

                    // 은둔자는 FreezeCooltime/UnfreezeCooltime 로 쿨타임을 직접 관리한다.
                    context.Complete(SkillResult.Deferred);
                }
            }
            public sealed class ThiefSkill : ScanableJobSkillBase
            {
                public float StealSuccessDelay { get; private set; }

                public ThiefSkill(SkillParameterSet parameter) : base(parameter)
                {
                    StealSuccessDelay = parameter.GetFloat(nameof(StealSuccessDelay));
                }

                public override bool CanExecuteOn(ISkillReceiver receiver)
                {
                    if (receiver is not PlayerSkillActionReceiver playerReceiver) return false;
                    if (!playerReceiver.CanReceiveSteal()) return false;
                    return GameManager.Instance.GetPlayerInfoByClientId(playerReceiver.Player.OwnerClientId).Coin > 0;
                }


                public override void Execute(SkillContext context) => ApplyToReceiver<ISteakable>(context, stealable =>
                {
                    var randomCoin = (int)JobManager.Instance.GetRandomRewardByJobId(JobId);
                    stealable.ApplyStealCoin(context.Sender, StealSuccessDelay, randomCoin);
                });
            }
            public sealed class GangsterSkill : JobSkillBase
            {
                public float XRange { get; private set; }
                public float YRange { get; private set; }
                public float DeployDuration { get; private set; }
                public float DeployEffectValue { get; private set; }
                public float PressHoldDuration { get; private set; }

                public GangsterSkill(SkillParameterSet parameter) : base(parameter)
                {
                    XRange = parameter.GetFloat(nameof(XRange));
                    YRange = parameter.GetFloat(nameof(YRange));
                    DeployDuration = parameter.GetFloat(nameof(DeployDuration));
                    DeployEffectValue = parameter.GetFloat(nameof(DeployEffectValue));
                    PressHoldDuration = parameter.GetFloat(nameof(PressHoldDuration));
                }

                public override void Execute(SkillContext context)
                {
                    if (context.Sender == null)
                    {
                        context.Complete(SkillResult.Rejected);
                        return;
                    }

                    var sender = context.Sender;

                    var gum = Managers.Resource.Instantiate<Gum>($"Skills/JSI{JobId}");
                    var gumPosition = sender.Root.position + (sender.Root.localRotation.y > 0 ? Vector3.right : Vector3.left) * 0.5f;
                    gum.transform.position = gumPosition;
                    gum?.Initialize();
                    Managers.Sound?.PlaySfx(ResourceDefine.FMODEvent.SFX242);
                    // 지정된 장소에 설치
                    MissionResolver.Notify(new MissionActionEvent(MissionDefine.MissionActionType.Deploy, MissionDefine.MissionTargetType.Gum, position: gumPosition));

                    gum?.SetSkill(this);
                    gum?.Activate();

                    // 설치 성공 = 스킬 성공. 함정 발동(트리거)은 Gum 쪽에서 별도로 처리한다.
                    context.Complete(SkillResult.Accepted);
                }
            }

            #endregion JOB_SKILL

            #region SKILL_INTERFACE
            // ISkillReceiver 인터페이스 분리
            public interface IStunnable
            {
                void ApplyStun(float duration);
            }

            public interface ISlow
            {
                void ApplySlow(float percent, float duration);
            }

            public interface IArrestHoldable
            {
                void ArrestHold(float holdDuration, float arrestDuration, Vector3 prisonInPos, Vector3 prisonOutPos);
            }

            public interface ITeleportable
            {
                void ApplyTeleport(Vector3 pos);
            }

            public interface IBlindable
            {
                void TryBlind(float duration, float visibleRange);
            }

            public interface IInvertControllable
            {
                void ApplyInvertControls(float duration, ulong sender);
            }

            public interface ISteakable
            {
                void ApplyStealCoin(PlayerNetwork sender, float successDelay, int amount);
            }

            public interface IObjectGeneratable
            {
                UnityEngine.Object GenerateObject();
            }
            #endregion SKILL_INTERFACE

            public enum EffectType
            {
                None = 0,
                Stun = 1,
                Slow = 2,
                Teleport = 3,
                Blind = 4,
                GainCoin = 5,
                InvertControls = 6,
                StealCoin = 7,
                DeployObject = 8,
            }

            public enum JobType
            {
                None = 0,
                FryingPanKiller,
                InsanePharmacist,
                Gambler,
                Police,
                Hacker,
                Recluse,
                Thief,
                Gangster
            }

            #endregion JobSkill

            public class SkillContext
            {
                public PlayerNetwork Sender { get; }
                public ISkillReceiver Receiver { get; }
                public bool IsValid => Receiver != null;
                public object[] Data { get; private set; }
                private Action<SkillResult> _onComplete;
                public SkillContext(PlayerNetwork sender, ISkillReceiver receiver, Action<SkillResult> onComplete = null)
                { 
                    Sender = sender; 
                    Receiver = receiver; 
                    _onComplete = onComplete;
                }

                // 한 번만 통지되도록 보장
                public void Complete(SkillResult result)
                {
                    var cb = _onComplete;
                    _onComplete = null;
                    cb?.Invoke(result);
                }

                public void SetData(params object[] data) => Data = data;
            }

            public interface IJobExecutable
            {
                bool CanExecute(SkillContext skillContext);
                void Execute(SkillContext skillContext);
            }
        }
        public static class UIDefine
        {
            public enum UILayer
            {
                SceneUI = 0,
                Alert = 30,
                Popup = 50,
                Transition = 100
            }
        }
        public static class MissionDefine
        {
            public enum MissionState
            {
                Ready,
                Clear
            }

            public enum MissionActionType
            {
                None = 0,

                // 설치
                Deploy,

                // 함정 발동
                Trigger,

                // 미션형 인터랙션 성공 (ex. 해커, 도박사 등)
                CompleteInteraction,

                // 상태 이상 부여
                ApplyStatus,

                // 체포
                Arrest,

                // 탐지
                Detect,

                // 경제
                Steal,
            }
            public enum MissionTargetType
            {
                None = 0,

                // 설치물
                Gum,
                Poison,

                // 인터랙션
                Laptop,
                NumberGuessGame,
            }

            [Flags]
            public enum MissionFilterType
            {
                None = 0,

                // 특정 위치
                MissionZone = 1 << 0,

                // 같은 대상 반복
                SameTarget = 1 << 1,

                // 시간 제한
                WithinTime = 1 << 2,

                // 라운드 시작을 기준으로 시간 카운팅
                WithinStartTime = 1 << 3,
            }

            public class MissionCondition : IEquatable<MissionCondition>
            {
                public int ConditionId { get; }
                public MissionActionType MissionActionType { get; }
                public MissionFilterType MissionFilterType { get; }
                public MissionTargetType MissionTargetType { get; }
                public float TimeLimit { get; }

                public MissionCondition(MasterDataRow row)
                {
                    ConditionId = row.Get<int>(nameof(ConditionId));
                    MissionActionType = row.Get<MissionActionType>(nameof(MissionActionType));
                    MissionFilterType = row.Get<MissionFilterType>(nameof(MissionFilterType));
                    MissionTargetType = row.Get<MissionTargetType>(nameof(MissionTargetType));
                    TimeLimit = row.Get<float>(nameof(TimeLimit));
                }

                private MissionCondition(MissionCondition origin)
                {
                    ConditionId = origin.ConditionId;
                    MissionActionType = origin.MissionActionType;
                    MissionFilterType = origin.MissionFilterType;
                    MissionTargetType = origin.MissionTargetType;
                    TimeLimit = origin.TimeLimit;
                }

                public bool Equals(MissionCondition other) => ConditionId == other.ConditionId;
                public MissionCondition Clone() => new MissionCondition(this);
            }

            public abstract class Mission : IDisposable, IEquatable<Mission>
            {
                #region MasterData Field
                public int MissionId { get; }
                public string MissionName { get; }
                public string MissionDescription { get; }
                public MissionType MissionType { get; }
                public JobDefine.JobType JobType { get; }
                public int RequireCount { get; }
                public int Reward { get; }
                public MissionCondition Condition { get; }
                #endregion

                public MissionState MissionState { get; protected set; } = default;
                public bool IsCompleted { get; protected set; }

                private Subject<Mission> onCompleted = new();
                private Subject<Mission> onReset = new();
                public IObservable<Mission> OnCompleted => onCompleted;
                public IObservable<Mission> OnReset => onReset;

                protected Mission(MasterDataRow row)
                {
                    MissionId = row.Get<int>(nameof(MissionId));
                    MissionName = row.Get<string>(nameof(MissionName));
                    MissionDescription = row.Get<string>(nameof(MissionDescription));
                    MissionType = row.Get<MissionType>(nameof(MissionType));
                    JobType = row.Get<JobDefine.JobType>(nameof(JobType));
                    RequireCount = row.Get<int>(nameof(RequireCount));
                    Reward = row.Get<int>(nameof(Reward));
                    if(MissionType == MissionType.Job)
                    {
                        var conditionRow = Managers.Master.GetTable("MissionConditionTable").GetDataByIndex(MissionId);
                        if (conditionRow == null)
                        {
                            Condition = null;
                            return;
                        }
                        Condition = new MissionCondition(conditionRow);
                    }
                }

                // Clone용 복사 생성자
                protected Mission(Mission origin)
                {
                    MissionId = origin.MissionId;
                    MissionName = origin.MissionName;
                    MissionDescription = origin.MissionDescription;
                    MissionType = origin.MissionType;
                    JobType = origin.JobType;
                    Reward = origin.Reward;
                    onCompleted = new();
                    onReset = new();
                }

                public virtual void ResetMission()
                {
                    MissionState = MissionState.Ready;
                    onReset = new();
                    IsCompleted = false;
                    onReset?.OnNext(this);
                }

                public virtual void Complete()
                {
                    if (IsCompleted) return;
                    IsCompleted = true;
                    MissionState = MissionState.Clear;
                    onCompleted?.OnNext(this);
                }

                public abstract Mission Clone();

                public virtual void Dispose()
                {
                    onCompleted?.Dispose();
                    onReset?.Dispose();
                }

                public bool Equals(Mission other) => MissionId == other.MissionId;
            }

            // ─────────────────────────────────────────
            // 일반 미션 - 단순 완료형
            // ─────────────────────────────────────────
            public class NormalMission : Mission
            {
                public NormalMission(MasterDataRow row) : base(row) { }
                private NormalMission(NormalMission origin) : base(origin) { }
                public override Mission Clone() => new NormalMission(this);
            }

            // ─────────────────────────────────────────
            // 직업 미션 - 카운트 기반
            // ─────────────────────────────────────────
            public class JobMission : Mission
            {
                public int CurrentCount { get; private set; }

                private Subject<JobMission> onProgressChanged = new();
                public IObservable<JobMission> OnProgressChanged => onProgressChanged;

                public JobMission(MasterDataRow row) : base(row) { }

                private JobMission(JobMission origin) : base(origin)
                {
                    onProgressChanged = new();
                }

                public void AddProgress(int amount = 1)
                {
                    if (IsCompleted) return;
                    CurrentCount += amount;
                    onProgressChanged?.OnNext(this);
                    if (CurrentCount >= RequireCount) Complete();
                }

                public override void ResetMission()
                {
                    onProgressChanged = new();
                    CurrentCount = 0;
                    base.ResetMission();
                }

                public override Mission Clone() => new JobMission(this);

                public override void Dispose()
                {
                    base.Dispose();
                    onProgressChanged?.Dispose();
                }
            }
        }
        public static class SceneDefine
        {
            private readonly static Dictionary<SceneEnum, ISceneController> SCENE_DICT = new Dictionary<SceneEnum, ISceneController>()
            {
                {SceneEnum.BootScene, new BootSceneController() },
                {SceneEnum.TitleScene, new TitleSceneController(ResourceDefine.FMODEvent.BGM104) },
                {SceneEnum.LobbyScene, new LobbySceneController() },
                {SceneEnum.MainScene, new MainSceneController() },
            };

            public static ISceneController CreateSceneController(SceneEnum sceneId)
            {
                if (!SCENE_DICT.TryGetValue(sceneId, out ISceneController controller)) return null;
                return controller;
            }

            public static SceneEnum GetSceneIdByName(string sceneName)
            {
                if (string.IsNullOrEmpty(sceneName))
                    return default;

                if (!Enum.TryParse(sceneName, out SceneEnum sceneId))
                    return default;

                return sceneId;
            }
        }
        public static class CharacterDefine
        {
            public class CharacterData : IDisposable
            {
                private CharacterDataSO dataSO;
                private Sprite thumbnail;
                private SpriteLibraryAsset libraryAsset;

                public Sprite Thumbnail => thumbnail;
                public SpriteLibraryAsset LibraryAsset => libraryAsset;
                public float ScanPivotH { get; set; }

                public CharacterData(CharacterDataSO data)
                {
                    dataSO = data;
                    thumbnail = data.thumbnail;
                    libraryAsset = data.libaryAsset;
                }

                public void Dispose()
                {
                    dataSO = null;
                    thumbnail = null;
                    libraryAsset = null;
                }
            }
        }
        public static class AudioDefine
        {
            public enum BusType
            {
                Master,
                Bgm,
                Sfx
            }
        }
        public static class GraphicsDefine
        {
            public enum DisplayModeOption
            {
                Fullscreen,
                Windowed,
            }
        }
        public static class SteamDefine
        {
            [Serializable]
            public class SteamAuthData
            {
                public ulong steamId;
                public byte[] ticket;
            }
        }
        public enum SupplyBoxEvent
        {
            None,
            Shield,
            SwapPlayer,
            TeleportPlayer,
            ResetSkill,
            SpawnBarricade
        }
    }
}