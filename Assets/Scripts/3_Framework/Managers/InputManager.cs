using Cysharp.Threading.Tasks;
using Denba.Common;
using Steamworks;
using System;
using System.Threading;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using static StockGame.Scripts.Define.GameDefine.InputDefine;

namespace StockGame.Scripts.Manager
{
    public class InputManager : MonoSingleton<InputManager>
    {
        private readonly InputBindingStack BindingStack = new();
        #region Events
        /* PLAYER */
        private Subject<Vector2> onMove = new Subject<Vector2>();
        private Subject<Unit> onInteract = new Subject<Unit>();
        private Subject<Unit> onSkill = new Subject<Unit>();
        private Subject<Unit> onClickInteract = new Subject<Unit>();
        public IObservable<Vector2> OnMove => onMove;
        public IObservable<Unit> OnInteract => onInteract;
        public IObservable<Unit> OnSkill => onSkill;
        public IObservable<Unit> OnClickInteract => onClickInteract;
        /* UI */
        private Subject<Vector2> onPointer = new Subject<Vector2>();
        private Subject<Unit> onSubmit = new Subject<Unit>();
        //private Subject<Unit> onMap = new Subject<Unit>();
        //private Subject<Unit> onEscape = new Subject<Unit>();
        public IObservable<Unit> OnSubmit => onSubmit;
        //public IObservable<Unit> OnMap => onMap;
        //public IObservable<Unit> OnEscape => onEscape;
        #endregion

        private Input_Action input;

        public override void Initialize()
        {
            base.Initialize();
            isInitialed = true;
            if (input == null) input = new Input_Action();

            /* Player */
            AddBind(input.Player.Move, InputMove, started: true, performed: true, canceled: true);
            AddBind(input.Player.Interact, InputInteract, started: true);
            AddBind(input.Player.Skill, InputSkill, started: true);
            AddBind(input.Player.ClickInteract, InputClickInteract, started: true);

            /* UI */
            AddBind(input.UI.Submit, InputSubmit, started: true, performed: false, canceled: false);
            AddBind(input.UI.Pointer, InputPointer, started: false, performed: true, canceled: false);
            AddBind(input.UI.Escape, InputEscape, started: true, performed: false, canceled: false);
            AddBind(input.UI.Map, InputMap, started: true, performed: false, canceled: false);
            input.Enable();
        }

        public void PushBindingContext(InputBindingContext bindingContext) => BindingStack.Push(bindingContext);
        public void PopBindingContext(InputBindingContext bindingContext ) => BindingStack.Pop(bindingContext);
        private void AddBind(InputAction action, Action<InputAction.CallbackContext> callback, bool started = false, bool performed = false, bool canceled = false)
        {
            if (started) action.started += callback;
            if (performed) action.performed += callback;
            if (canceled) action.canceled += callback;
        }

        private void RemoveBind(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            action.started -= callback;
            action.performed -= callback;
            action.canceled -= callback;
        }

        #region PLAYER INPUT
        private void InputMove(InputAction.CallbackContext context)
        {
            Vector2 dir = context.ReadValue<Vector2>();
            onMove?.OnNext(dir);
        }
        private void InputInteract(InputAction.CallbackContext context)
        {
            onInteract?.OnNext(Unit.Default);
        }
        private void InputSkill(InputAction.CallbackContext context)
        {
            onSkill?.OnNext(Unit.Default);
        }
        private void InputClickInteract(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Started) return;
            onClickInteract?.OnNext(Unit.Default);
        }
        #endregion

        #region UI INPUT

        private void InputSubmit(InputAction.CallbackContext context) => onSubmit?.OnNext(Unit.Default);

        private void InputPointer(InputAction.CallbackContext context)
        {
            Vector2 pointer = context.ReadValue<Vector2>();
            onPointer?.OnNext(pointer);
        }

        private void InputEscape(InputAction.CallbackContext context)
        {
            BindingStack.Handle("Escape");
        }

        private void InputMap(InputAction.CallbackContext context)
        {
            BindingStack.Handle("Map");
        }

        #endregion

        #region INPUT_SETTING
        public void StartAllInput() => input.Enable();
        public void StopAllInput() => input.Disable();
        public void StartPlayerInput() => input.Player.Enable();
        public void StopPlayerInput() => input.Player.Disable();
        public void StartUIInput() => input.UI.Enable();
        public void StopUIInput() => input.UI.Disable();
        #endregion INPUT_SETTING

        void OnDestroy()
        {
            BindingStack?.Dispose();
            input?.Dispose();
            onMove?.Dispose();
            onInteract?.Dispose();
            onSkill?.Dispose();
            onClickInteract?.Dispose();

            onPointer?.Dispose();
            onSubmit?.Dispose();

            input = null;
            onMove = null;
            onInteract = null;
            onClickInteract = null;
            onPointer = null;
            onSubmit = null;
        }
    }
}