using StockGame.Scripts.Manager;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.MissionDefine;

namespace StockGame.Scripts.Missions
{
    public class MissionActionEvent
    {
        public MissionActionType ActionType { get; }
        public MissionTargetType TargetType { get; }

        public ulong TargetId { get; }
        public float Value { get; }
        public Vector3 Position { get; }


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

    public class MissionResolver : IDisposable
    {
        private static float startMissionTime;
        private static readonly Subject<MissionActionEvent> _actionStream = new();
        public static IObservable<MissionActionEvent> ActionStream => _actionStream;

        public static void Notify(MissionActionEvent evt) => _actionStream.OnNext(evt);

        public static void StartMission() => startMissionTime = Time.time;


        private readonly JobMission _mission;
        private readonly MissionCondition _condition;
        private readonly CompositeDisposable _disposables = new();

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


        private bool Filter(MissionActionEvent evt)
        {
            if (_mission.IsCompleted) return false;

            if (evt.ActionType != _condition.MissionActionType) return false;

            if (_condition.MissionTargetType != MissionTargetType.None &&
                evt.TargetType != _condition.MissionTargetType) return false;

            return true;
        }


        private ulong? _firstTargetId = null;

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

            if (filter.HasFlag(MissionFilterType.WithinTime) || _condition.TimeLimit > 0)
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

            if (filter.HasFlag(MissionFilterType.WithinStartTime) || _condition.TimeLimit > 0)
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

            _mission.AddProgress(evt.Value > 0 ? (int)evt.Value : 1);
        }

        public void Dispose() => _disposables.Dispose();
    }

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