using Cinemachine;
using Denba.Common;
using UnityEngine;

namespace StockGame.Scripts.Manager
{
    public class CameraManager : MonoSingleton<CameraManager>
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private CinemachineConfiner2D confiner2D; 
        [SerializeField] private LayerMask interactableLayer;
        
        public Camera GetCurrentCamera => _camera;

        public override void Initialize()
        {
            base.Initialize();
        }
        public void SetLookAt(Transform target)
        {
            virtualCamera.LookAt = target;
        }

        public void SetFollow(Transform target)
        {
            virtualCamera.Follow = target; 
        }

        public void SetConfiner(Collider2D confiner)
        {
            confiner2D.m_BoundingShape2D = confiner;
        }
    }
}