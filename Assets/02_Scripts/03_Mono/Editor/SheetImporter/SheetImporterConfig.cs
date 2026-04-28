#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// 엔트리 처리 모드.
    /// GenericSO: 시트 → SO 배열 생성. 자식 테이블은 엔트리 내부 _childTables 링크 목록으로 함께 주입.
    /// Localization: 시트 → 언어별 LocalizationTable SO.
    /// </summary>
    public enum SheetEntryMode
    {
        GenericSO = 0,
        Localization = 1,
    }

    /// <summary>
    /// Google Sheets / 로컬 CSV → ScriptableObject 임포트 설정.
    /// 에디터 타임 전용(MonoScript 사용). 런타임 진입점 없음.
    /// 엔트리 하나당 시트 1개 ↔ SO 타입 1개 매핑. 자식 테이블은 ChildTableLink 로 내부화.
    /// </summary>
    [CreateAssetMenu(fileName = "SheetImporterConfig", menuName = "PublicFramework/Sheet Importer/Config")]
    public class SheetImporterConfig : ScriptableObject
    {
        private const string ASSETS_ROOT = "Assets/";
        private const string PACKAGES_ROOT = "Packages/";
        private const string HTTP_PREFIX = "http://";
        private const string HTTPS_PREFIX = "https://";

        [SerializeField] private List<SheetImporterEntry> _entries = new List<SheetImporterEntry>();

        public IReadOnlyList<SheetImporterEntry> Entries => _entries;

        private void OnValidate()
        {
            if (_entries == null) return;

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry == null) continue;

                string label = string.IsNullOrEmpty(entry.EntryName) ? $"#{i}" : entry.EntryName;

                if (string.IsNullOrEmpty(entry.Source))
                    Debug.LogWarning($"[SheetImporter] 엔트리 '{label}' Source 가 비어있습니다.");
                else if (!IsValidSource(entry.Source))
                    Debug.LogWarning($"[SheetImporter] 엔트리 '{label}' Source 형식을 인식할 수 없습니다(http(s):// 또는 Assets/... 또는 절대경로).");

                if (entry.Mode == SheetEntryMode.GenericSO)
                {
                    if (entry.TargetScript == null)
                        Debug.LogWarning($"[SheetImporter] 엔트리 '{label}' TargetScript 가 비어있습니다.");
                    else if (entry.TargetType == null)
                        Debug.LogWarning($"[SheetImporter] 엔트리 '{label}' TargetScript 에서 Type 을 얻지 못했습니다(컴파일 오류 가능).");
                    else if (!typeof(ScriptableObject).IsAssignableFrom(entry.TargetType))
                        Debug.LogWarning($"[SheetImporter] 엔트리 '{label}' TargetScript '{entry.TargetType.Name}' 는 ScriptableObject 가 아닙니다.");
                }

                if (!string.IsNullOrEmpty(entry.OutputFolder) && !entry.OutputFolder.StartsWith(ASSETS_ROOT))
                    Debug.LogWarning($"[SheetImporter] 엔트리 '{label}' OutputFolder 는 '{ASSETS_ROOT}' 로 시작해야 합니다.");
            }
        }

        private static bool IsValidSource(string source)
        {
            return source.StartsWith(ASSETS_ROOT)
                || source.StartsWith(PACKAGES_ROOT)
                || source.StartsWith(HTTP_PREFIX)
                || source.StartsWith(HTTPS_PREFIX)
                || System.IO.Path.IsPathRooted(source);
        }
    }

    /// <summary>
    /// 임포트 엔트리 한 건. 시트 1개 ↔ SO 타입 1개 매핑.
    /// 자식 시트(1:N 정규화)는 _childTables 로 내부화해 부모 import 시 함께 처리된다.
    /// </summary>
    [Serializable]
    public class SheetImporterEntry
    {
        [Tooltip("UI/로그에 표시되는 이름. 비우면 대상 SO 스크립트 이름을 자동 사용.")]
        [SerializeField] private string _entryName;

        [Tooltip("엔트리 처리 모드. GenericSO: 시트 → SO 배열. Localization: 시트 → 언어별 Resources CSV 덤프.")]
        [SerializeField] private SheetEntryMode _mode = SheetEntryMode.GenericSO;

        [Tooltip("CSV 소스. http(s):// URL 또는 Assets/... 로컬 경로 또는 절대 경로.")]
        [SerializeField] private string _source;

        [Tooltip("대상 ScriptableObject 스크립트.")]
        [SerializeField] private MonoScript _targetScript;

        [Tooltip("생성/갱신 대상 폴더. 비우면 Assets/09_ScriptableObjects/{타입명}/ 을 자동 사용.")]
        [SerializeField] private string _outputFolder;

        [Tooltip("헤더가 시작되는 행 번호(1부터 시작). 비우거나 0이면 1로 간주.")]
        [SerializeField] private int _headerRow = 1;

        [Tooltip("첫 데이터 열 번호(1부터 시작). 0 또는 1이면 첫 열부터.")]
        [SerializeField] private int _firstDataColumn = 1;

        [Tooltip("마지막 데이터 열 번호(1부터 시작). 0이면 전체 열 사용.")]
        [SerializeField] private int _lastDataColumn = 0;

        [Tooltip("이 엔트리(타입) 전용 별칭 매핑. 헤더 자동 매핑으로 해결 안 되는 필드에만 등록.")]
        [SerializeField] private List<AliasMapping> _aliases = new List<AliasMapping>();

        [Tooltip("Localization 모드에서 정수 키 컬럼의 헤더 이름. 기본 'MID'.")]
        [SerializeField] private string _keyHeader = "MID";

        [Tooltip("선택 — 임포트 후 생성된 SO들을 모아 등록할 DataCollection<T> 타입 스크립트. 비우면 컬렉션 갱신을 건너뜀.")]
        [SerializeField] private MonoScript _collectionScript;

        [Tooltip("자식 시트 링크. 부모 import 후 여기에 등록된 각 링크의 시트를 읽어 부모 SO 의 배열/리스트 필드에 요소로 주입한다.")]
        [SerializeField] private List<ChildTableLink> _childTables = new List<ChildTableLink>();

        public string EntryName => _entryName;
        public SheetEntryMode Mode => _mode;
        public string Source => _source;
        public MonoScript TargetScript => _targetScript;
        public string OutputFolder => _outputFolder;
        public int HeaderRow => _headerRow;
        public int FirstDataColumn => _firstDataColumn;
        public int LastDataColumn => _lastDataColumn;
        public IReadOnlyList<AliasMapping> Aliases => _aliases;
        public string KeyHeader => _keyHeader;
        public MonoScript CollectionScript => _collectionScript;
        public IReadOnlyList<ChildTableLink> ChildTables => _childTables;

        /// <summary>TargetScript 에서 얻은 실제 Type. null 가능.</summary>
        public Type TargetType => _targetScript != null ? _targetScript.GetClass() : null;

        /// <summary>CollectionScript 에서 얻은 실제 Type. null 가능.</summary>
        public Type CollectionType => _collectionScript != null ? _collectionScript.GetClass() : null;
    }

    /// <summary>
    /// 자식 테이블 링크. 부모 GenericSO 엔트리의 배열/리스트 필드에 시트 행을 주입한다.
    /// 자식 시트의 parentIdHeader 컬럼은 부모 SO 와 매칭에 사용된다.
    /// 매칭 키는 기본적으로 부모 SO 의 파일명(MID)이며, _parentLookupField 가 설정된 경우
    /// 부모 SO 의 해당 필드 값과 매칭한다 (예: 다대일 stat 묶음 공유).
    /// </summary>
    [Serializable]
    public class ChildTableLink
    {
        [Tooltip("부모 SO 에서 주입 대상이 될 배열/리스트 필드 이름 (예: '_steps').")]
        [SerializeField] private string _parentFieldName;

        [Tooltip("자식 CSV 소스. http(s):// URL 또는 Assets/... 로컬 경로 또는 절대 경로.")]
        [SerializeField] private string _source;

        [Tooltip("자식 시트 헤더가 시작되는 행 번호(1부터 시작). 비우거나 0이면 1로 간주.")]
        [SerializeField] private int _headerRow = 1;

        [Tooltip("자식 시트 첫 데이터 열 번호(1부터 시작). 0 또는 1이면 첫 열부터.")]
        [SerializeField] private int _firstDataColumn = 1;

        [Tooltip("자식 시트 마지막 데이터 열 번호(1부터 시작). 0이면 전체 열 사용.")]
        [SerializeField] private int _lastDataColumn = 0;

        [Tooltip("자식 시트에서 부모 MID 가 담긴 컬럼 헤더. 기본 'parentId'.")]
        [SerializeField] private string _parentIdHeader = "parentId";

        [Tooltip("같은 부모 내 요소 정렬 기준 컬럼 헤더. 비우면 시트 등장 순서. 기본 'order'.")]
        [SerializeField] private string _orderByHeader = "order";

        [Tooltip("자식 parentId 와 매칭할 부모 SO 의 필드 이름 (예: '_baseStatMID'). 비우면 부모 파일명(MID) 매칭.")]
        [SerializeField] private string _parentLookupField;

        [Tooltip("자식 요소 타입 전용 별칭 매핑. 헤더 자동 매핑으로 해결 안 되는 필드에만 등록.")]
        [SerializeField] private List<AliasMapping> _aliases = new List<AliasMapping>();

        public string ParentFieldName => _parentFieldName;
        public string Source => _source;
        public int HeaderRow => _headerRow;
        public int FirstDataColumn => _firstDataColumn;
        public int LastDataColumn => _lastDataColumn;
        public string ParentIdHeader => _parentIdHeader;
        public string OrderByHeader => _orderByHeader;
        public string ParentLookupField => _parentLookupField;
        public IReadOnlyList<AliasMapping> Aliases => _aliases;
    }

    /// <summary>
    /// 시트 헤더 → SO 필드명 별칭 매핑. 열린 구조(추가 메타는 추후 확장).
    /// </summary>
    [Serializable]
    public class AliasMapping
    {
        [Tooltip("시트 1행 헤더 이름(기획자 표기).")]
        [SerializeField] private string _sheetHeader;

        [Tooltip("SO 의 실제 SerializeField 이름.")]
        [SerializeField] private string _fieldName;

        public string SheetHeader => _sheetHeader;
        public string FieldName => _fieldName;
    }
}
#endif

