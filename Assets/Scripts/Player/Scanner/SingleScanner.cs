using System.Collections.Generic;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.JobDefine;

namespace StockGame.Scripts.Players
{
    public interface ISkillScanner
    {
        void Scan(GameObject sender, Vector2 center, float x, float y, LayerMask targetLayer, RangeType rangeType);
        ISkillReceiver GetPrimary();
        List<ISkillReceiver> GetAll();
    }

    #region BASE_SCANNER
    public abstract class SingleScanner<T> where T : class
    {
        private readonly Collider2D[] _buffer;

        protected SingleScanner(int bufferSize = 8)
        {
            _buffer = new Collider2D[bufferSize];
        }

        protected Collider2D GetClosest(GameObject sender, Vector2 center, float x, float y, LayerMask targetLayer, RangeType rangeType)
        {            
            int count = ScanByRange((Vector2)sender.transform.position + center, x, y, targetLayer, rangeType);
            if (count == 0) return null;

            Collider2D closest = null;
            float minDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var hit = _buffer[i];
                if (!hit || hit.gameObject == sender) continue;
                float dist = Vector2.Distance(center, hit.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = hit;
                }
            }
            return closest;
        }

        private int ScanByRange(Vector2 center, float x, float y, LayerMask targetLayer, RangeType rangeType)
        {
            var origin = center;
            return rangeType switch
            {
                RangeType.Box => Physics2D.OverlapBoxNonAlloc(origin, new Vector2(x, y), 0f, _buffer, targetLayer),
                RangeType.Circle => Physics2D.OverlapCircleNonAlloc(origin, x, _buffer, targetLayer),
                RangeType.Capsule => Physics2D.OverlapCapsuleNonAlloc(origin, new Vector2(x, y), CapsuleDirection2D.Vertical, 0f, _buffer, targetLayer),
                _ => 0
            };
        }
    }

    public abstract class MultiScanner<T> where T : class
    {
        private readonly Collider2D[] _buffer;

        protected MultiScanner(int bufferSize = 8)
        {
            _buffer = new Collider2D[bufferSize];
        }

        protected int GetAll(GameObject sender, Vector2 center, float x, float y, LayerMask targetLayer, RangeType rangeType)
        {
            var origin = (Vector2)sender.transform.position + center;
            return rangeType switch
            {
                RangeType.Box => Physics2D.OverlapBoxNonAlloc(origin, new Vector2(x, y), 0f, _buffer, targetLayer),
                RangeType.Circle => Physics2D.OverlapCircleNonAlloc(origin, x, _buffer, targetLayer),
                RangeType.Capsule => Physics2D.OverlapCapsuleNonAlloc(origin, new Vector2(x, y), CapsuleDirection2D.Vertical, 0f, _buffer, targetLayer),
                _ => 0
            };
        }

        protected Collider2D GetBuffer(int index) => _buffer[index];
    }
    #endregion BASE_SCANNER
    public sealed class SingleSkillScanner : SingleScanner<ISkillReceiver>, ISkillScanner
    {
        private ISkillReceiver _cached;

        public SingleSkillScanner(int bufferSize = 8) : base(bufferSize) { }

        public void Scan(GameObject sender, Vector2 center, float x, float y, LayerMask targetLayer, RangeType rangeType)
        {
            var target = GetClosest(sender, center, x, y, targetLayer, rangeType);
            _cached = target != null ? target.GetComponent<ISkillReceiver>() : null;
        }

        public ISkillReceiver GetPrimary() => _cached;
        public List<ISkillReceiver> GetAll() => _cached != null ? new List<ISkillReceiver> { _cached } : new();
    }
    public sealed class MultiSkillScanner : MultiScanner<ISkillReceiver>, ISkillScanner
    {
        private readonly List<ISkillReceiver> _cached = new();

        public MultiSkillScanner(int bufferSize = 8) : base(bufferSize) { }

        public void Scan(GameObject sender, Vector2 center, float x, float y, LayerMask targetLayer, RangeType rangeType)
        {
            _cached.Clear();
            int count = GetAll(sender, center, x, y, targetLayer, rangeType);
            for (int i = 0; i < count; i++)
            {
                var hit = GetBuffer(i);
                if (!hit || hit.gameObject == sender) continue;
                if (hit.TryGetComponent<ISkillReceiver>(out var receiver))
                    _cached.Add(receiver);
            }
        }

        public ISkillReceiver GetPrimary() => _cached.Count > 0 ? _cached[0] : null;
        public List<ISkillReceiver> GetAll() => _cached;
    }
    public sealed class ObjectSkillScanner : SingleScanner<ObjectSkillScanner>, ISkillScanner
    {
        private ISkillReceiver _cached;
        public ObjectSkillScanner(int bufferSize = 8) : base(bufferSize) { }
        public void Scan(GameObject sender, Vector2 center, float x, float y, LayerMask targetLayer, RangeType rangeType)
        {
            var target = GetClosest(sender, center, x, y, targetLayer, rangeType);
            var newReceiver = target != null ? target.GetComponent<ISkillReceiver>() : null;

            if (!ReferenceEquals(_cached, newReceiver))
            {
                if (_cached is IHighlightable prev)
                    prev.SetHighlight(false);

                _cached = newReceiver;

                if (_cached is IHighlightable current)
                    current.SetHighlight(true);
            }
        }

        public ISkillReceiver GetPrimary() => _cached;
        public List<ISkillReceiver> GetAll() => _cached != null ? new List<ISkillReceiver> { _cached } : new();
    }
}