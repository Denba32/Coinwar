using StockGame.Scripts.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StockGame.Scripts.Players
{
    public interface IState
    {
        void Enter(params object[] args);
        void Exit();
        void LogicUpdate(float deltaTime);
        void PhysicsUpdate(float deltaTime);
    }

    public abstract class PlayerState : IState
    {
        protected PlayerNetwork player;
        protected PlayerFiniteStateMachine stateMachine;
        protected string animationName;
        protected float startTime;
        protected bool isExitState;
        protected PlayerState(PlayerNetwork player, PlayerFiniteStateMachine stateMachine, string animationName)
        {
            this.player = player;
            this.animationName = animationName;
            this.stateMachine = stateMachine;
        }
        public virtual void Enter(params object[] args)
        {
            player.PlayAnimation(animationName);
            startTime = Time.time;
            isExitState = false;
        }

        public virtual void Exit()
        {
            isExitState = true;
        }

        public virtual void LogicUpdate(float deltaTime)
        {

        }

        public virtual void PhysicsUpdate(float deltaTime)
        {

        }
    }

    public sealed class PlayerIdleState : PlayerState
    {
        public PlayerIdleState(PlayerNetwork player, PlayerFiniteStateMachine stateMachine, string animationName) : base(player, stateMachine, animationName)
        {
        }

        public override void Enter(params object[] args)
        {
            base.Enter(args);
            player.StopMove();
            player.StopFootStepSound();
        }

        public override void LogicUpdate(float deltaTime)
        {
            base.LogicUpdate(deltaTime);
            if (isExitState) return;
            if (player.Direction == Vector2.zero) return;
            if (player.Condition.Value == PlayerConditionType.Poisoned)
                stateMachine?.ChangeState(stateMachine.PoisonedState);
            stateMachine?.ChangeState(stateMachine.WalkState);
        }
    }

    public sealed class PlayerWalkState : PlayerState
    {
        public PlayerWalkState(PlayerNetwork player, PlayerFiniteStateMachine stateMachine, string animationName) : base(player, stateMachine, animationName)
        {
        }

        public override void Enter(params object[] args)
        {
            base.Enter(args);
        }

        public override void LogicUpdate(float deltaTime)
        {
            base.LogicUpdate(deltaTime);
            if (isExitState) return; 

            if(player.Condition.Value == PlayerConditionType.Poisoned)
            {
                stateMachine?.ChangeState(stateMachine.PoisonedState);
                return;
            }

            if (player.Direction != Vector2.zero) return;
            stateMachine?.ChangeState(stateMachine.IdleState);
        }

        public override void PhysicsUpdate(float deltaTime)
        {
            base.PhysicsUpdate(deltaTime);
            player?.SetMove();
            player?.Turn();
        }
    }

    public sealed class PlayerPoisonedState : PlayerState
    {
        private readonly List<Vector2> DIR = new List<Vector2>(4) { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        private List<Vector2> dir = new(4);
        public PlayerPoisonedState(PlayerNetwork player, PlayerFiniteStateMachine stateMachine, string animationName) : base(player, stateMachine, animationName) { }

        public override void Enter(params object[] args)
        {
            base.Enter(args);
            dir = DIR.ShuffleDerangement();
        }

        public override void LogicUpdate(float deltaTime)
        {
            base.LogicUpdate(deltaTime);
            if (isExitState) return;
            if (player.Direction == Vector2.zero) stateMachine?.ChangeState(stateMachine.IdleState);
            if (player.Condition.Value == PlayerConditionType.None) stateMachine?.ChangeState(stateMachine.IdleState);
        }

        public override void PhysicsUpdate(float deltaTime)
        {
            base.PhysicsUpdate(deltaTime);
            if (player == null) return;
            var result = player.InvertMove(dir);
            player?.Turn(result);
        }
    }

    public sealed class PlayerStunState : PlayerState
    {
        private float duration;
        private float prevSpeed;
        public PlayerStunState(PlayerNetwork player, PlayerFiniteStateMachine stateMachine, string animationName) : base(player, stateMachine, animationName) { }
        public override void Enter(params object[] args)
        {
            base.Enter(args);

            prevSpeed = player.Speed.Value;
            if(args == null ||  args.Length == 0)
            {
                stateMachine?.ChangeState(stateMachine?.IdleState);
                return;
            }

            foreach(var arg in args)
            {
                if(arg is float value)
                {
                    duration = value;
                }
            }
            player.ChangeCondition(PlayerConditionType.Stun);
            player.StopMove();
            player.SetSpeed(0f);
            player.SkillExecutor.LockSkill(true);
        }

        public override void Exit()
        {
            base.Exit();
            player.ChangeCondition(PlayerConditionType.None);
            player.SkillExecutor.LockSkill(false);
            player.SetSpeed(prevSpeed);
        }

        public override void LogicUpdate(float deltaTime)
        {
            if (isExitState) return;
            base.LogicUpdate(deltaTime);
            if (Time.time - startTime <= duration) return;
            stateMachine?.ChangeState(stateMachine?.IdleState);
        }
    }
}