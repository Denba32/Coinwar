using System;
using System.Collections.Generic;
using UnityEngine;
using StockGame.Utility;

namespace StockGame.Scripts.MasterDatas
{
    /// <summary>
    /// TSV 파일 한 장을 나타내는 마스터 데이터 테이블
    /// </summary>
    public class MasterDataTable
    {
        public string TableName { get; }
        public IReadOnlyList<string> FieldNames => _fieldNames;
        public IReadOnlyList<string> FieldTypes => _fieldTypes;
        public int RowCount => _rows.Count;

        private readonly List<string> _fieldNames = new();
        private readonly List<string> _fieldTypes = new();

        // 전체 행 목록
        private readonly List<Dictionary<string, object>> _rows = new();

        // Index(유니크키) 기준 캐시: Index값(int) → 행
        private readonly Dictionary<int, Dictionary<string, object>> _indexCache = new();

        // Index 컬럼명 (첫 번째 컬럼으로 고정)
        private string _indexFieldName;


        public MasterDataTable(string tableName, string tsvText)
        {
            TableName = tableName;
            Parse(tsvText);
        }


        private void Parse(string tsvText)
        {
            var lines = TsvParser.ParseLines(tsvText);

            if (lines.Length < 2)
            {
                Debug.LogError($"[MasterDataTable] '{TableName}': TSV 라인이 부족합니다 (최소 헤더+타입 2줄 필요).");
                return;
            }

            // 라인 0 : 필드명
            foreach (var name in lines[0])
                _fieldNames.Add(name.Trim());

            // 라인 1 : 타입
            foreach (var type in lines[1])
                _fieldTypes.Add(type.Trim());

            _indexFieldName = _fieldNames.Count > 0 ? _fieldNames[0] : null;

            // 라인 2 : 데이터
            for (int r = 2; r < lines.Length; r++)
            {
                var cols = lines[r];
                var row = new Dictionary<string, object>(_fieldNames.Count);

                for (int c = 0; c < _fieldNames.Count; c++)
                {
                    string raw = c < cols.Length ? cols[c] : string.Empty;
                    row[_fieldNames[c]] = TsvParser.ParseValue(_fieldTypes[c], raw);
                }

                _rows.Add(row);

                // Index 캐싱
                if (_indexFieldName != null && row.TryGetValue(_indexFieldName, out var idxObj) && idxObj is int idx)
                    _indexCache[idx] = row;
            }

            Debug.Log($"[MasterDataTable] '{TableName}' 로드 완료 — {_rows.Count}행, {_fieldNames.Count}열");
        }

        public MasterDataRow GetDataByIndex(int index)
        {
            if (_indexCache.TryGetValue(index, out var row))
                return new MasterDataRow(row);

            Debug.LogWarning($"[MasterDataTable] '{TableName}': Index {index} 를 찾을 수 없습니다.");
            return null;
        }

        public MasterDataRow GetData(Predicate<MasterDataRow> predicate)
        {
            foreach (var raw in _rows)
            {
                var row = new MasterDataRow(raw);
                if (predicate(row))
                    return row;
            }
            return null;
        }

        public List<MasterDataRow> GetAllData(Predicate<MasterDataRow> predicate)
        {
            var result = new List<MasterDataRow>();
            foreach (var raw in _rows)
            {
                var row = new MasterDataRow(raw);
                if (predicate(row))
                    result.Add(row);
            }
            return result;
        }

        public List<MasterDataRow> GetAllData()
        {
            var result = new List<MasterDataRow>(_rows.Count);
            foreach (var raw in _rows)
                result.Add(new MasterDataRow(raw));
            return result;
        }
    }

    public class MasterDataRow
    {
        private readonly Dictionary<string, object> _data;

        public MasterDataRow(Dictionary<string, object> data)
        {
            _data = data;
        }

        public T Get<T>(string fieldName)
        {
            if (_data.TryGetValue(fieldName, out var value))
                return TsvParser.Cast<T>(value);

            Debug.LogWarning($"[MasterDataRow] 필드 '{fieldName}' 를 찾을 수 없습니다.");
            return default;
        }

        public bool HasField(string fieldName) => _data.ContainsKey(fieldName);

        public object GetRaw(string fieldName) =>
            _data.TryGetValue(fieldName, out var v) ? v : null;
    }
}