using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace StockGame.Scripts.Utility
{
    public readonly struct ResolutionOption
    {
        public readonly int Width;
        public readonly int Height;

        public ResolutionOption(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public override string ToString() => $"{Width}x{Height}";
    }

    public static class ResolutionUtility
    {
        public static List<ResolutionOption> GetResolutionsMatchingNativeAspect()
        {
            var native = Screen.currentResolution;
            float nativeAspect = (float)native.width / native.height;

            return Screen.resolutions
                .Select(r => new ResolutionOption(r.width, r.height))
                .Distinct()
                .Where(r => Mathf.Abs((float)r.Width / r.Height - nativeAspect) < 0.01f)
                .OrderByDescending(r => r.Width)
                .ToList();
        }

        /// <summary>
        /// 특정 해상도(width, height)가 지원하는 주사율 목록 (높은 Hz 먼저).
        /// </summary>
        public static List<int> GetSupportedRefreshRates(int width, int height)
        {
            return Screen.resolutions
                .Where(r => r.width == width && r.height == height)
                .Select(r => Mathf.RoundToInt((float)r.refreshRateRatio.value))
                .Distinct()
                .OrderByDescending(hz => hz)
                .ToList();
        }

        /// <summary>
        /// 모니터의 네이티브 해상도 + 주사율. 최초 실행 시 기본값으로 사용.
        /// </summary>
        public static (int width, int height, int refreshRate) GetNativeResolution()
        {
            var native = Screen.currentResolution;
            return (native.width, native.height, Mathf.RoundToInt((float)native.refreshRateRatio.value));
        }
    }
}
