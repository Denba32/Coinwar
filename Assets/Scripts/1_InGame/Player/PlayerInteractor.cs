using StockGame.Scripts.Manager;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StockGame.Scripts.Players
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private float scanRadius;

        private PlayerNetwork player;
        private Vector2 ScanCenterPivot => new Vector2(transform.position.x, transform.position.y + 0.5f);

        private readonly Collider2D[] _scanBuffer = new Collider2D[4];

        public IInteractable ClosestInteractable { get; private set; }

        // Scan 중심점 
        public void Initialize(PlayerNetwork player)
        {
            this.player = player;
        }

        private void Update()
        {
            UpdateClosest();
        }

        private int Scan()
        {
            return Physics2D.OverlapCircleNonAlloc(
                ScanCenterPivot,
                scanRadius,
                _scanBuffer,
                _layerMask
            );
        }

        private void UpdateClosest()
        {
            IInteractable best = null;

            int count = Scan();
            if (count <= 0)
            {
                SetClosestInteractable(null);
                return;
            }

            float minDist = float.MaxValue;
            var ctx = new InteractorContext
            {
                PlayerObject = player
            };

            for (int i = 0; i < count; i++)
            {
                var col = _scanBuffer[i];
                if (!col) continue;

                if (!col.TryGetComponent<IInteractable>(out var interactable))
                    continue;

                if (!interactable.CanInteract(ctx))
                    continue;

                float dist = ((Vector2)col.transform.position - ScanCenterPivot).sqrMagnitude;

                if (dist < minDist)
                {
                    minDist = dist;
                    best = interactable;
                }
            }

            SetClosestInteractable(best);
        }
        private void SetClosestInteractable(IInteractable interactable)
        {
            if (!player.IsOwner) return;
            if (ReferenceEquals(ClosestInteractable, interactable))
                return;

            if (IsAlive(ClosestInteractable) && ClosestInteractable is IHighlightable prev)
            {
                prev.SetHighlight(false);
            }

            ClosestInteractable = interactable;

            if (ClosestInteractable is IHighlightable current)
            {
                current.SetHighlight(true);
            }
        }

        public void Interact(Unit _)
        {
            if (!player.IsOwner) return;
            if (ClosestInteractable == null) return;
            var ctx = new InteractorContext(player.NetData, player);
            if (!ClosestInteractable.CanInteract(ctx)) return;
            player.IsInteracted.Value = true;
            ClosestInteractable?.Interact(ctx);
        }

        public void TryInteractByMouse()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var worldPos = CameraManager.Instance.GetCurrentCamera
                               .ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.Raycast(worldPos, Vector2.zero, Mathf.Infinity, _layerMask);
            if (hit.collider == null) return;

            var interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null || !ReferenceEquals(ClosestInteractable, interactable)) return;

            var ctx = new InteractorContext(player.NetData, player);
            if (!interactable.CanInteract(ctx)) return;

            interactable.Interact(ctx);
            player.IsInteracted.Value = true;
        }

        private bool IsAlive(IInteractable interactable)
        {
            if (interactable == null) return false;
            if (interactable is Object unityObj) return unityObj != null; // Fake Null 비교 명시적 적용
            return true;
        }

        public void ResetData()
        {
            if (IsAlive(ClosestInteractable) && ClosestInteractable is IHighlightable prev)
            {
                prev.SetHighlight(false);
            }

            ClosestInteractable = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(
                Application.isPlaying ? ScanCenterPivot : (Vector2)transform.position,
                scanRadius
            );
        }
#endif
    }
}