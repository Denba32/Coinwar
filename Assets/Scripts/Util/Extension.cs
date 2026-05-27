using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using StockGame.Scripts.Define;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static StockGame.Scripts.Define.GameDefine.ProbabilityDefine;
namespace StockGame.Utility
{
    public static class Extension
    {
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            return Util.GetOrAddComponent<T>(go);
        }

        public static async UniTask Await(this AsyncOperation operation)
        {
            while (!operation.isDone)
            {
                await UniTask.Yield();
            }
        }

        #region Vector2_Extension

        public static Vector2 ConvertWorldToUIPosition(this Vector3 worldPos, RectTransform target, Canvas canvas)
        {
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(target, screenPoint, cam, out Vector2 localPoint);

            return localPoint;
        }

        #endregion

        #region Animator_Extension
        public static float GetAnimationLength(this Animator animator, int layer, string clipName)
        {
            var clips = animator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                if (clip.name == clipName)
                    return clip.length;
            }
            Debug.LogWarning($"AnimationClip not found: {clipName}");
            return 0f;
        }
        #endregion Animator_Extension

        #region RectTransform_Extension

        public static void PlaceRandomlyInRect(this RectTransform item, RectTransform randomlyRect)
        {
            var itemSize = item.rect.size;
            Rect boundsRect = randomlyRect.rect;

            float randomX = UnityEngine.Random.Range(
                boundsRect.xMin + itemSize.x * randomlyRect.pivot.x,
                boundsRect.xMax - itemSize.x * (1 - randomlyRect.pivot.x)
            );

            float randomY = UnityEngine.Random.Range(
                boundsRect.yMin + itemSize.y * randomlyRect.pivot.y,
                boundsRect.yMax - itemSize.y * (1 - randomlyRect.pivot.y)
            );

            Vector2 localPoint = new Vector2(randomX, randomY);

            Vector3 worldPoint = randomlyRect.TransformPoint(localPoint);
            Vector3 parentLocalPoint =
                item.parent.InverseTransformPoint(worldPoint);

            item.anchoredPosition = parentLocalPoint;
        }

        #endregion RectTransform_Extension

        #region Array
        public static void Sort(this string[] array)
        {
            System.Array.Sort(array);
        }
        public static string[] Remove(this string[] array, string value)
        {
            return array.Where(x => !x.Equals(value)).ToArray();
        }
        #endregion

        #region  Data / Json Extension

        public static void ToJson<T>(this T data, string path, bool isPretty = false) where T : new()
        {
            if (data == null)
            {
                Debug.LogWarning($"ToJson: data is null, path={path}");
                return;

            }
            var _path = Path.Combine(Application.persistentDataPath, path);

#if UNITY_EDITOR
            Debug.Log(_path);
#endif

            var directory = Path.GetDirectoryName(_path);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var settings = isPretty ? Formatting.Indented : Formatting.None;
            var jObject = JsonConvert.SerializeObject(data, settings);

            Debug.Log(jObject);
            File.WriteAllText(_path, jObject);
        }

        public static T FromJson<T>(this string json, bool isPretty = false) where T : new()
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static byte GetHash(this string str, bool isPretty = false)
        {
            return byte.MaxValue;
        }

        #endregion

        #region BUTTON_EXTENSION
        /// <summary>
        /// 버튼 연속 클릭 방지 Observable
        /// </summary>
        public static IObservable<Unit> OnClickAsObservableFirst(
            this Button button,
            float throttleSeconds = 0.5f)
        {
            return button
                .OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromSeconds(throttleSeconds));
        }

        public static IObservable<Unit> OnClickAsObservableWithThrottle(this Button button, float intervalSeconds, Action onWaitAction = null)
        {
            var throttle = TimeSpan.FromSeconds(intervalSeconds);
            var lastClickTime = DateTimeOffset.MinValue;

            return button.OnClickAsObservable()
                .Select(_ =>
                {
                    var now = DateTimeOffset.UtcNow;
                    var canClick = (now - lastClickTime) >= throttle;

                    if (canClick)
                    {
                        lastClickTime = now;
                    }
                    return canClick;
                })
                .Do(canClick =>
                {
                    if (!canClick)
                        onWaitAction?.Invoke();
                })
                .Where(canClick => canClick)
                .Select(_ => Unit.Default);
        }

        #endregion BUTTON_EXTENSION

        #region ENUMERATOR_EXTENSION
        public static void Shuffle<T>(this IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        #endregion ENUMERATOR_EXTENSION

        #region COLLIDER_EXTENSION
        public static bool ValidateDropArea(this Collider2D dragItem, Collider2D slot)
        {
            return slot.OverlapPoint(dragItem.bounds.min) &&
                   slot.OverlapPoint(dragItem.bounds.max) &&
                   slot.OverlapPoint(dragItem.bounds.center);
        }

        public static bool Overlaps(this RectTransform drag, RectTransform slot, float threshold = 0.9f)
        {
            // 월드 좌표 기준 Rect 계산
            Vector3[] dragCorners = new Vector3[4];
            Vector3[] slotCorners = new Vector3[4];

            drag.GetWorldCorners(dragCorners);
            slot.GetWorldCorners(slotCorners);

            // Drag의 AABB
            float dragMinX = dragCorners[0].x;
            float dragMinY = dragCorners[0].y;
            float dragMaxX = dragCorners[2].x;
            float dragMaxY = dragCorners[2].y;

            // Slot의 AABB
            float slotMinX = slotCorners[0].x;
            float slotMinY = slotCorners[0].y;
            float slotMaxX = slotCorners[2].x;
            float slotMaxY = slotCorners[2].y;

            // 겹치는 영역 계산
            float xMin = Mathf.Max(dragMinX, slotMinX);
            float xMax = Mathf.Min(dragMaxX, slotMaxX);
            float yMin = Mathf.Max(dragMinY, slotMinY);
            float yMax = Mathf.Min(dragMaxY, slotMaxY);

            float overlapWidth = Mathf.Max(0f, xMax - xMin);
            float overlapHeight = Mathf.Max(0f, yMax - yMin);
            float overlapArea = overlapWidth * overlapHeight;

            float dragArea = (dragMaxX - dragMinX) * (dragMaxY - dragMinY);
            if (dragArea <= 0f) return false;

            float ratio = overlapArea / dragArea;
            return ratio >= threshold;
        }

        public static Rect GetScreenRect(this RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

            return new Rect(min, max - min);
        }

        #endregion COLLIDER_EXTENSION

        #region SCENE
        public static string GetSceneName(this SceneEnum scene)
        {
            return Enum.GetName(typeof(SceneEnum), scene);
        }

        #endregion SCENE

        #region PROBABILITY
        public static float GetRandomByJobProbability(this List<JobProbability> list)
        {
            if (list == null || list.Count <= 0) return 0;
            var sum = list.Sum(p => p.Probability);
            var randomValue = UnityEngine.Random.Range(0f, sum);
            foreach(var li in list)
            {
                randomValue -= li.Probability;
                if (randomValue < 0)
                {
                    return li.Reward;
                }
            }
            return 0f;
        }

        #endregion PROBABILITY
    }
}