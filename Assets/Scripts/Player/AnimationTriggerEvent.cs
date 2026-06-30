using FMOD.Studio;
using FMODUnity;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using UnityEngine;

namespace StockGame.Scripts.Players
{
    public enum FootstepSurfaceType
    {
        Default = 0,
        Bunker = 1,
        Wood = 2,
        Grass = 3,
        Snow = 4,
    }

    public class AnimationTriggerEvent : MonoBehaviour
    {
        [SerializeField] private PlayerNetwork playerNetwork;
        [SerializeField] private PlayerPositionTracker tracker;

        private FootstepSurfaceType? _currentSurface = null;
        private EventInstance footStepInstance;
        private float index = 0;
        private bool _isStopped = false;

        private void Start()
        {
            footStepInstance = RuntimeManager.CreateInstance(GameDefine.ResourceDefine.FMODEvent.FootStep);
        }

        public void PlayFootStepSound()
        {
            if (!playerNetwork.IsOwner) return;
            if (_isStopped) return;

            var surfaceType = tracker.CurrentSurface;
            if (_currentSurface != surfaceType) index = 0;
            _currentSurface = surfaceType;
            footStepInstance.setParameterByName("FootStep", SurfaceTypeToFloat(_currentSurface.Value));
            footStepInstance.setParameterByName("Step", index);
            index += 1.0f;
            footStepInstance.start();
        }

        public void StopFootStep()
        {
            _isStopped = true;
            footStepInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }

        private float SurfaceTypeToFloat(FootstepSurfaceType surfaceType) => _currentSurface switch
        {
            FootstepSurfaceType.Default => 0f,
            FootstepSurfaceType.Bunker => 1f,
            FootstepSurfaceType.Wood => 2f,
            FootstepSurfaceType.Grass => 3f,
            FootstepSurfaceType.Snow => 4f,
            _ => 0f
        };

        public void ResumeFootStep()
        {
            _isStopped = false;
        }

        private void OnDestroy()
        {
            footStepInstance.release();
        }
    }
}