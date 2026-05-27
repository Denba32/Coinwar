using Denba.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StockGame.Scripts.Manager
{
    using StockGame.Scripts.MasterDatas;

    /// <summary>
    /// 모든 마스터 데이터 테이블을 관리
    ///
    /// [초기화]
    ///   Initialize();
    ///   → Resources 폴더 아래 모든 .tsv 파일을 자동으로 로드합니다.
    /// </summary>
    public class MasterDataManager : Singleton<MasterDataManager>
    {
        // tsv 파일들이 위치한 Resources 하위 경로
        private const string ResourcePath = "MasterDatas";

        // 테이블명(파일명 without 확장자) → MasterDataTable
        private readonly Dictionary<string, MasterDataTable> _tables = new();

        private bool _initialized;


        /// <summary>
        /// Resources/{ResourcePath} 하위의 모든 TSV 파일을 로드합니다.
        /// 게임 시작 시 한 번만 호출하세요.
        /// </summary>
        public override void Initialize()
        {
            if (_initialized)
            {
                Debug.LogWarning("[MasterDataManager] 이미 초기화되어 있습니다.");
                return;
            }

            _tables.Clear();

            var assets = Managers.Resource.GetByParentFolder<TextAsset>(ResourcePath);

            if (assets == null || assets.Count == 0)
            {
                Debug.LogWarning($"[MasterDataManager] Resources/{ResourcePath} 에서 TSV 파일을 찾을 수 없습니다.");
                return;
            }

            foreach (var asset in assets)
            {
                // .tsv 파일 처리
                var table = new MasterDataTable(asset.name, asset.text);
                _tables[asset.name] = table;
            }

            _initialized = true;
            Debug.Log($"[MasterDataManager] 초기화 완료 — {_tables.Count}개 테이블 로드");
        }

        /// <summary>
        /// 테이블명으로 MasterDataTable을 반환
        /// </summary>
        public MasterDataTable GetTable(string tableName)
        {
            if (_tables.TryGetValue(tableName, out var table))
                return table;

            Debug.LogError($"[MasterDataManager] 테이블 '{tableName}' 을 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 테이블이 존재여부 확인
        /// </summary>
        public bool HasTable(string tableName) => _tables.ContainsKey(tableName);

        /// <summary>
        /// 로드된 모든 테이블명 전부 반환
        /// </summary>
        public IEnumerable<string> GetAllTableNames() => _tables.Keys;


        /// <summary>
        /// 테이블명 + Index(유니크키)로 단일 행 조회합니다.
        /// </summary>
        public MasterDataRow GetDataByIndex(string tableName, int index)
        {
            return GetTable(tableName)?.GetDataByIndex(index);
        }


        /// <summary>
        /// 첫 번째 일치 행을 반환합니다.
        /// </summary>
        public MasterDataRow GetData(string tableName, Predicate<MasterDataRow> predicate)
        {
            return GetTable(tableName)?.GetData(predicate);
        }

        /// <summary>
        /// 모든 일치 행을 반환합니다.
        /// </summary>
        public List<MasterDataRow> GetAllData(string tableName, Predicate<MasterDataRow> predicate)
        {
            return GetTable(tableName)?.GetAllData(predicate) ?? new List<MasterDataRow>();
        }

        /// <summary>
        /// 테이블의 전체 행을 반환
        /// </summary>
        public List<MasterDataRow> GetAllData(string tableName)
        {
            return GetTable(tableName)?.GetAllData() ?? new List<MasterDataRow>();
        }

    }
}