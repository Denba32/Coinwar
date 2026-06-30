using StockGame.Scripts.Manager;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.MissionDefine;

namespace StockGame.Scripts.Missions
{
    /// <summary>
    /// 미션 액션 이벤트 데이터
    /// </summary>
    public class MissionActionEvent
    {
        public MissionActionType ActionType { get; }
        public MissionTargetType TargetType { get; }

        /// <summary>행동의 대상 (플레이어 ID, 오브젝트 ID 등)</summary>
        public ulong TargetId { get; }
        public float Value { get; }
        public Vector3 Position { get; }  // ← 추가


        public MissionActionEvent(MissionActionType actionType, MissionTargetType targetType = MissionTargetType.None, 
            ulong targetId = 0, int value = 1, Vector3 position = default)
        {
            ActionType = actionType;
            TargetType = targetType;
            TargetId = targetId;
            Value = value;
            Position = position;
        }
    }

    /// <summary>
    /// 직업 미션의 진행 조건을 판별하고 AddProgress를 호출하는 리졸버
    /// </summary>
    public class MissionResolver : IDisposable
    {
        private static float startMissionTime;
        // 전역 미션 액션 스트림 — 게임 내 어디서든 Push
        private static readonly Subject<MissionActionEvent> _actionStream = new();
        public static IObservable<MissionActionEvent> ActionStream => _actionStream;

        /// <summary>
        /// 게임 내에서 미션 관련 행동이 발생했을 때 호출
        /// </summary>
        public static void Notify(MissionActionEvent evt) => _actionStream.OnNext(evt);

        public static void StartMission() => startMissionTime = Time.time;

        // ──────────────────────────────────────────────
        // 인스턴스: 특정 JobMission 하나를 구독·관리
        // ──────────────────────────────────────────────
        private readonly JobMission _mission;
        private readonly MissionCondition _condition;
        private readonly CompositeDisposable _disposables = new();

        /// <summary>WithinTime 필터용: 시간 윈도우 내 이벤트 기록</summary>
        private readonly List<float> _eventTimestamps = new();

        public MissionResolver(JobMission mission)
        {
            _mission = mission;
            _condition = mission.Condition;

            if (_condition == null)
            {
                Debug.LogWarning($"[MissionResolver] MissionId={mission.MissionId} 에 Condition이 없습니다.");
                return;
            }

            ActionStream
                .Where(Filter)
                .Subscribe(OnAction)
                .AddTo(_disposables);
        }

        // ──────────────────────────────────────────────
        // 필터: ActionType → FilterType → TargetType 순서로 걸러냄
        // ──────────────────────────────────────────────
        private bool Filter(MissionActionEvent evt)
        {
            if (_mission.IsCompleted) return false;

            if (evt.ActionType != _condition.MissionActionType) return false;

            if (_condition.MissionTargetType != MissionTargetType.None &&
                evt.TargetType != _condition.MissionTargetType) return false;
            return true;
        }

        // ──────────────────────────────────────────────
        // 실제 진행 처리
        // ──────────────────────────────────────────────
        private ulong? _firstTargetId = null; // SameTarget 필터용

        private void OnAction(MissionActionEvent evt)
        {
            var filter = _condition.MissionFilterType;

            if (filter.HasFlag(MissionFilterType.SameTarget))
            {
                if (_firstTargetId == null)
                    _firstTargetId = evt.TargetId;
                else if (_firstTargetId != evt.TargetId)
                    return;
            }

            if (filter.HasFlag(MissionFilterType.WithinTime) && _condition.TimeLimit > 0)
            {
                float now = Time.time;

                _eventTimestamps.Add(now);

                _eventTimestamps.RemoveAll(t => now - t > _condition.TimeLimit);

                int progress = Mathf.Min(_eventTimestamps.Count, _mission.RequireCount);

                int delta = progress - _mission.CurrentCount;

                if (delta > 0)
                    _mission.AddProgress(delta);

                return;
            }

            if (filter.HasFlag(MissionFilterType.WithinStartTime) && _condition.TimeLimit > 0)
            {
                if (Time.time - startMissionTime > _condition.TimeLimit)
                    return;

                _eventTimestamps.Add(Time.time);

                int progress = Mathf.Min(_eventTimestamps.Count, _mission.RequireCount);

                int delta = progress - _mission.CurrentCount;

                if (delta > 0)
                    _mission.AddProgress(delta);

                return;
            }

            if(filter.HasFlag(MissionFilterType.MissionZone))
            {
                if(ZoneManager.Instance.CheckMissionPoint(_mission, evt.Position))
                    _mission.AddProgress(1);

                return;
            }

            // ── 일반: 이벤트 1회당 카운트 1 증가
            _mission.AddProgress(evt.Value > 0 ? (int)evt.Value : 1);
        }

        public void Dispose() => _disposables.Dispose();
    }

    /// <summary>
    /// MissionManager에서 직업 미션 목록을 받아 리졸버를 일괄 생성·관리
    /// </summary>
    public class MissionResolverGroup : IDisposable
    {
        private readonly List<MissionResolver> _resolvers = new();

        public MissionResolverGroup(List<Mission> jobMissions)
        {
            foreach (var mission in jobMissions)
            {
                if (mission is JobMission jm)
                    _resolvers.Add(new MissionResolver(jm));
            }
        }

        public void Dispose()
        {
            foreach (var r in _resolvers) r.Dispose();
            _resolvers.Clear();
        }
    }
}