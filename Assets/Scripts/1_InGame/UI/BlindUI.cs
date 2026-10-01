using StockGame.Scripts.Manager;
using StockGame.Scripts.Players;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI
{
    public class BlindUI : UIBase
    {
        [SerializeField] private Image blindImage;
        private Material _material;

        private static readonly int PlayerScreenPos = Shader.PropertyToID("_PlayerScreenPos");
        private static readonly int Radius = Shader.PropertyToID("_Radius");

        private PlayerNetwork _targetTransform;
        private float _radius;
        private Camera mainCam;

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);

            foreach(var arg in args)
            {
                if (arg is PlayerNetwork obj) _targetTransform = obj;
                else if(arg is float radius) _radius = radius;
            }
            _material = blindImage.material;
            mainCam = Managers.Camera.GetCurrentCamera;
            blindImage.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_targetTransform == null) return;

            var screenPos = mainCam.WorldToViewportPoint(_targetTransform.Root.position + new Vector3(0, _targetTransform.Height * 0.5f));
            _material.SetVector(PlayerScreenPos, screenPos);
            _material.SetFloat(Radius, _radius);
        }
    }
}