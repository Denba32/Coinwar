using FMOD.Studio;
using FMODUnity;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace StockGame.Scripts.UI.Missions
{
    public interface IDriver { }

    public sealed class UIDriver : MonoBehaviour, IDragItem, IDriver, IPointerDownHandler
    {
        private const string MOVABLE = "Movable";

        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup group;

        [SerializeField] private RectTransform rect;
        [SerializeField] private RectTransform bounds;
        [SerializeField] private RectTransform draggableBoundary;
        [SerializeField] private SortingGroup sortingGroup;

        [SerializeField] private int id;

        private bool isDragable = true;
        private bool isPicking = false;
        private Transform startParent;

        private Vector3 lastWorldPos;
        private Vector3 worldDelta;

        private EventInstance driverSound = default;

        private HashSet<IDrivenItem> movableList = new HashSet<IDrivenItem>();
        public int Id => id;
        public RectTransform Bounds => bounds;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isDragable) return;
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX223);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDragable) return;
            isPicking = true;
            startParent = transform.parent;
            transform.SetParent(canvas.transform);

            group.blocksRaycasts = false;

            lastWorldPos = rect.anchoredPosition3D;
            worldDelta = Vector3.zero;
            UIManager.Instance.SetDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragable) return;

            rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
            var currentWorldPos = rect.anchoredPosition3D;
            worldDelta = currentWorldPos - lastWorldPos;
            lastWorldPos = currentWorldPos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragable) return;
            isPicking = false;
            group.blocksRaycasts = true;

            if (transform.parent == canvas.transform)
            {
                transform.SetParent(startParent);
            }
            
            if(eventData != null)
                worldDelta = eventData.delta;
            UIManager.Instance.SetDrag(null);
        }

        private void Update()
        {
            if (worldDelta.sqrMagnitude < 0.000001f)
                return;

            if (worldDelta.x > 0f || worldDelta.y < 0f)
                return;

            foreach (var moveable in movableList)
            {
                moveable?.TryMove(worldDelta);
            }

            worldDelta = Vector3.zero;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!isDragable) return;
            if (collision == null) return;
            if (!collision.CompareTag(MOVABLE)) return;
            var drivenItem = collision?.GetComponent<IDrivenItem>();
            if (drivenItem == null) return;
            movableList?.Add(drivenItem);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (!isDragable) return;
            if (collision == null) return;
            if (!collision.CompareTag(MOVABLE)) return;
            if (movableList.Count <= 0) return;
            if (!isPicking) return;
            if(!driverSound.isValid()) driverSound = RuntimeManager.CreateInstance(GameDefine.ResourceDefine.FMODEvent.SFX212);
            driverSound.getPlaybackState(out var state);
            if (state == PLAYBACK_STATE.STOPPED)
            {
                Debug.Log("실행중이 아니다");
                driverSound.start();
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!isDragable) return;
            if (collision == null) return;
            if (!collision.CompareTag(MOVABLE)) return;

            movableList?.RemoveWhere(movable => movable.Collider == collision);
        }

        public void ForceDragEnd()
        {
            OnEndDrag(null);
            isDragable = false;
        }

        private void OnDestroy()
        {
            isPicking = false;
            movableList?.Clear();
            movableList = null;
            driverSound.release();
            driverSound.clearHandle();
        }
    }
}
