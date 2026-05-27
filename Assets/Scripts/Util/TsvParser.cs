using System;
using System.Collections.Generic;
using UnityEngine;

namespace StockGame.Utility
{
    /// <summary>
    /// TSV 데이터의 타입 파싱을 담당
    /// </summary>
    public static class TsvParser
    {
        private const char ArraySeparator = ',';

        /// <summary>
        /// TSV 원본 텍스트를 파싱하여 행/열 2차원 배열로 반환
        /// </summary>
        public static string[][] ParseLines(string tsvText)
        {
            var lines = tsvText.Split('\n');
            var result = new List<string[]>();

            foreach (var line in lines)
            {
                var trimmed = line.TrimEnd('\r');
                if (string.IsNullOrEmpty(trimmed)) continue;
                result.Add(trimmed.Split('\t'));
            }

            return result.ToArray();
        }

        /// <summary>
        /// 타입 문자열과 원시 값 문자열을 받아 object로 변환
        /// 지원 타입: int, float, string, bool, int[], float[], string[]
        /// </summary>
        public static object ParseValue(string typeStr, string rawValue)
        {
            try
            {
                switch (typeStr.Trim().ToLower())
                {
                    case "int":
                        return int.Parse(rawValue);
                    case "float":
                        return float.Parse(rawValue);
                    case "string":
                        return rawValue;
                    case "bool":
                        return bool.Parse(rawValue);
                    case "long":
                        return long.Parse(rawValue);
                    case "int[]":
                        return ParseArray(rawValue, s => (object)int.Parse(s));
                    case "float[]":
                        return ParseArray(rawValue, s => (object)float.Parse(s));
                    case "long[]":
                        return ParseArray(rawValue, s=> (object)long.Parse(s));
                    case "string[]":
                        return ParseArray(rawValue, s => (object)s);
                    default:
                        Debug.LogWarning($"[TsvParser] 알 수 없는 타입 '{typeStr}', string으로 처리합니다.");
                        return rawValue;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TsvParser] 파싱 실패 - 타입: {typeStr}, 값: '{rawValue}'\n{e.Message}");
                return null;
            }
        }

        private static object[] ParseArray(string rawValue, Func<string, object> elementParser)
        {
            if (string.IsNullOrEmpty(rawValue)) return Array.Empty<object>();
            var parts = rawValue.Split(ArraySeparator);
            var result = new object[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                result[i] = elementParser(parts[i].Trim());
            return result;
        }

        /// <summary>
        /// object를 T로 캐스팅, 배열 타입의 경우 object[]에서 T[]로 변환
        /// </summary>
        public static T Cast<T>(object value)
        {
            if (value is T direct)
                return direct;

            // object[] → T[] 변환 (int[], float[], string[] 등)
            if (value is object[] objArray && typeof(T).IsArray)
            {
                var elementType = typeof(T).GetElementType();
                var typed = Array.CreateInstance(elementType, objArray.Length);
                for (int i = 0; i < objArray.Length; i++)
                    typed.SetValue(Convert.ChangeType(objArray[i], elementType), i);
                return (T)(object)typed;
            }
            // Enum의 경우 처리
            if(typeof(T).BaseType == typeof(Enum))
            {
                return (T)value;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
    }

}