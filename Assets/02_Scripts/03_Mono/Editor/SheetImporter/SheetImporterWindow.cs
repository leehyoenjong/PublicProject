#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using PublicFramework.Core.DataPipeline;
using UnityEditor;
using UnityEngine;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// Sheet Importer 에디터 창 (통합).
    /// Config SO 드래그/생성 → 엔트리 편집(SerializedObject 직접) → 필드 분석/헤더 프리뷰 → 임포트 실행/로그 표시.
    /// Inspector(CustomEditor) 는 제거되고 본 창이 편집 UI를 전담한다.
    /// UI 라벨은 한국어, C# 식별자/메뉴 경로/로그 프리픽스는 영어 유지.
    /// </summary>
    public class SheetImporterWindow : EditorWindow
    {
        // -------- 메뉴 / 식별자 (영어 유지) --------
        private const string MENU_PATH_OPEN = "PublicFramework/Tools/Sheet Importer";
        private const string MENU_PATH_CREATE_CONFIG = "PublicFramework/Tools/Create Sheet Importer Config";
        private const string LOG_PREFIX = "[SheetImporter]";

        // -------- SerializedProperty 이름 (영어 유지) --------
        private const string PROP_ENTRIES = "_entries";
        private const string PROP_ENTRY_NAME = "_entryName";
        private const string PROP_MODE = "_mode";
        private const string PROP_SOURCE = "_source";
        private const string PROP_TARGET_SCRIPT = "_targetScript";
        private const string PROP_OUTPUT_FOLDER = "_outputFolder";
        private const string PROP_HEADER_ROW = "_headerRow";
        private const string PROP_FIRST_DATA_COLUMN = "_firstDataColumn";
        private const string PROP_LAST_DATA_COLUMN = "_lastDataColumn";
        private const string PROP_ALIASES = "_aliases";
        private const string PROP_SHEET_HEADER = "_sheetHeader";
        private const string PROP_FIELD_NAME = "_fieldName";
        private const string PROP_KEY_HEADER = "_keyHeader";
        private const string PROP_COLLECTION_SCRIPT = "_collectionScript";
        private const string PROP_CHILD_TABLES = "_childTables";

        // -------- UI 문자열 (한국어) --------
        private const string WINDOW_TITLE = "시트 임포터";
        private const string PROGRESS_TITLE = "시트 임포트";
        private const string DIALOG_EXCEPTION_TITLE = "시트 임포터";

        private const string LBL_CONFIG_SECTION = "설정";
        private const string LBL_CONFIG_FIELD = "임포터 설정";
        private const string LBL_SUMMARY_SECTION = "마지막 임포트 결과";
        private const string LBL_ENTRIES_SECTION = "엔트리";
        private const string LBL_ADVANCED = "고급 설정";

        private const string LBL_DATA_NAME = "데이터 이름";
        private const string LBL_MODE = "모드";
        private const string LBL_SOURCE = "소스";
        private const string LBL_TARGET_SCRIPT = "대상 SO 스크립트";
        private const string LBL_OUTPUT_FOLDER = "출력 폴더";
        private const string LBL_HEADER_ROW = "헤더 행";
        private const string LBL_FIRST_DATA_COLUMN = "첫 데이터 열";
        private const string LBL_LAST_DATA_COLUMN = "마지막 데이터 열";
        private const string LBL_ALIASES = "타입별 별칭";
        private const string LBL_SHEET_HEADER = "시트 헤더";
        private const string LBL_FIELD_NAME = "실제 필드명";
        private const string LBL_KEY_HEADER = "키 헤더";
        private const string LBL_COLLECTION_SCRIPT = "컬렉션 스크립트";
        private const string LBL_CHILD_TABLES = "자식 테이블";

        private const string LBL_PREVIEW_HEADER_COL = "시트 헤더";
        private const string LBL_PREVIEW_RESOLVED_COL = "→ SO 필드";
        private const string LBL_PREVIEW_STATUS_COL = "상태";
        private const string LBL_PREVIEW_ACTION_COL = "액션";

        private const string STATUS_AUTO = "자동 매칭";
        private const string STATUS_ALIAS = "별칭 적용";
        private const string STATUS_NEEDS_ALIAS = "별칭 필요";
        private const string STATUS_UNKNOWN = "무시됨";
        private const string DROPDOWN_NONE = "(미선택)";
        private const string VALUE_NONE = "(매칭 없음)";

        // -------- 툴팁 --------
        private const string TIP_DATA_NAME = "창/로그에 표시되는 이름. 비우면 대상 SO 스크립트 이름을 자동 사용합니다.";
        private const string TIP_MODE = "GenericSO: 시트 → ScriptableObject 배열. Localization: 시트 → Resources/Localization/{Lang}.csv 덤프.";
        private const string TIP_SOURCE = "http(s):// 로 시작하는 URL 또는 'Assets/...' 로 시작하는 로컬 CSV 경로(절대경로/Packages/ 도 지원).";
        private const string TIP_TARGET_SCRIPT = "ScriptableObject 를 상속한 대상 타입의 스크립트.";
        private const string TIP_OUTPUT_FOLDER = "SO 가 저장될 폴더. 비우면 'Assets/09_ScriptableObjects/{타입명}/' 을 자동 사용합니다. Localization 모드에선 CSV 출력 폴더(기본 'Assets/Resources/Localization').";
        private const string TIP_HEADER_ROW = "CSV 에서 헤더가 적힌 행 번호. 1부터 시작.";
        private const string TIP_FIRST_DATA_COLUMN = "실제 데이터가 시작되는 열 번호. 왼쪽 메타 컬럼을 건너뛸 때 사용. 0 또는 1이면 첫 열부터.";
        private const string TIP_LAST_DATA_COLUMN = "CSV 에서 읽을 마지막 열 번호. 0 이면 모든 열 사용.";
        private const string TIP_ALIASES = "이 엔트리(타입)에만 적용되는 헤더→필드 매핑.";
        private const string TIP_SHEET_HEADER = "시트 1행에 적힌 헤더 이름.";
        private const string TIP_FIELD_NAME = "SO 의 실제 SerializeField 이름(예: _questId).";
        private const string TIP_KEY_HEADER = "Localization 모드에서 정수 키가 들어있는 컬럼의 헤더 이름. 기본 'MID'.";
        private const string TIP_COLLECTION_SCRIPT = "선택 — 임포트 후 생성된 SO들을 모아 등록할 DataCollection<T> 타입 스크립트. 비우면 컬렉션 갱신을 건너뜁니다.";
        private const string TIP_CHILD_TABLES = "부모 import 후 이 리스트의 각 링크 시트를 읽어 부모 SO 의 배열/리스트 필드에 요소로 주입합니다 (1:N 정규화).";

        // -------- 버튼 --------
        private const string BTN_IMPORT_ALL = "전체 가져오기";
        private const string BTN_IMPORT_SINGLE = "가져오기";
        private const string BTN_CLEAR_RESULT = "결과 지우기";
        private const string BTN_NEW_CONFIG = "새 Config 생성...";
        private const string BTN_ADD_ENTRY = "+ 엔트리 추가";
        private const string BTN_ADD_ALIAS = "+ 별칭 추가";
        private const string BTN_DELETE = "삭제";
        private const string BTN_REMOVE = "-";
        private const string BTN_ANALYZE_FIELDS = "필드 분석";
        private const string BTN_LOAD_HEADERS = "헤더 읽어오기";
        private const string BTN_APPLY_SELECTED = "선택 자동 등록";
        private const string BTN_CLEAR_PREVIEW = "프리뷰 초기화";
        private const string BTN_DIALOG_CONFIRM = "확인";

        // -------- 안내 / HelpBox --------
        private const string HINT_NO_CONFIG = "임포터 설정(SheetImporterConfig) 에셋을 위 슬롯에 드래그&드롭 하거나, '새 Config 생성...' 버튼으로 새로 만드세요.";
        private const string HINT_NO_ENTRIES = "엔트리가 없습니다. '+ 엔트리 추가' 버튼으로 시작하세요.";
        private const string HINT_NO_ALIASES = "별칭이 없습니다.";
        private const string HINT_ANALYZE_NO_TARGET = "필드 분석을 하려면 대상 SO 스크립트를 지정하세요.";
        private const string HINT_LOAD_NO_SOURCE = "헤더 읽어오기는 소스와 대상 SO 가 모두 지정된 경우만 가능합니다.";
        private const string HINT_APPLY_DONE = "선택된 매핑 {0}건을 타입별 별칭에 등록했습니다.";
        private const string HINT_APPLY_NONE = "등록할 선택된 매핑이 없습니다.";

        private const string ANALYSIS_OK = "문제 없음 — 자동 매핑으로 충분합니다.";
        private const string ANALYSIS_INVALID_HEADER = "잘못된 별칭 (SO 에 없는 필드):";
        private const string ANALYSIS_UNUSED_HEADER = "사용 안 됨 (자동 매핑으로 이미 해결됨):";
        private const string WARN_RANGE_INVALID = "첫 데이터 열({0})이 마지막 데이터 열({1})보다 큽니다. 프리뷰 결과가 비어있을 수 있습니다.";
        private const string DIALOG_EXCEPTION_FORMAT = "가져오기 중 예외가 발생했습니다.\n\n{0}";
        private const string TYPE_NONE_PLACEHOLDER = "(없음)";

        private const string HDR_ENTRY_WITH_NAME = "엔트리 {0}: {1}";
        private const string HDR_ENTRY_UNNAMED = "엔트리 {0}";

        // -------- Config 생성 --------
        private const string CREATE_DIALOG_TITLE = "Sheet Importer 설정 생성";
        private const string CREATE_DIALOG_HINT = "저장할 폴더와 파일 이름을 선택하세요.";
        private const string CREATE_DEFAULT_NAME = "SheetImporterConfig";
        private const string CREATE_DEFAULT_FOLDER = "Assets/09_ScriptableObjects";
        private const string CREATE_EXT = "asset";

        // -------- 레이아웃 --------
        private const float MIN_WIDTH = 820f;
        private const float MIN_HEIGHT = 640f;
        private const float LABEL_WIDTH = 180f;

        private const float BUTTON_BIG_HEIGHT = 28f;
        private const float BUTTON_CLEAR_RESULT_WIDTH = 110f;
        private const float BUTTON_ADD_ENTRY_WIDTH = 130f;
        private const float BUTTON_NEW_CONFIG_WIDTH = 160f;
        private const float BUTTON_NEW_CONFIG_HEIGHT = 22f;
        private const float BUTTON_DELETE_WIDTH = 60f;
        private const float BUTTON_ADD_ALIAS_WIDTH = 120f;
        private const float BUTTON_REMOVE_ALIAS_WIDTH = 24f;
        private const float BUTTON_ANALYZE_WIDTH = 110f;
        private const float BUTTON_LOAD_WIDTH = 130f;
        private const float BUTTON_CLEAR_PREVIEW_WIDTH = 110f;
        private const float BUTTON_IMPORT_SINGLE_WIDTH = 140f;
        private const float BUTTON_APPLY_WIDTH = 140f;

        private const float COL_HEADER_WIDTH = 160f;
        private const float COL_RESOLVED_WIDTH = 180f;
        private const float COL_STATUS_WIDTH = 90f;
        private const float COL_ACTION_MIN_WIDTH = 180f;

        [SerializeField] private SheetImporterConfig _config;

        // SerializedObject 캐시 — _config 변경 감지 시 재생성
        private SerializedObject _serializedConfig;
        private SerializedProperty _entriesProp;

        private Vector2 _mainScroll;
        private readonly Dictionary<int, EntryResult> _lastResults = new Dictionary<int, EntryResult>();
        private readonly Dictionary<int, bool> _advancedExpanded = new Dictionary<int, bool>();
        private readonly Dictionary<int, EntryPreview> _previews = new Dictionary<int, EntryPreview>();

        private int _createdTotal;
        private int _updatedTotal;
        private int _failedTotal;
        private int _totalLogCount;
        private bool _summaryVisible;

        // -------- 메뉴 진입점 --------

        [MenuItem(MENU_PATH_OPEN)]
        private static void OpenWindow()
        {
            var win = GetWindow<SheetImporterWindow>(WINDOW_TITLE);
            win.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
            win.Show();
        }

        [MenuItem(MENU_PATH_CREATE_CONFIG)]
        private static void CreateConfigFromMenu()
        {
            var config = CreateConfigInteractive();
            if (config == null) return;

            if (HasOpenInstances<SheetImporterWindow>())
            {
                var win = GetWindow<SheetImporterWindow>(WINDOW_TITLE);
                win.AssignConfig(config);
            }
        }

        /// <summary>외부(메뉴 진입점 등)에서 생성한 Config 를 현재 창 슬롯에 배정한다.</summary>
        public void AssignConfig(SheetImporterConfig config)
        {
            _config = config;
            ResetSerializedObject();
            Repaint();
        }

        private static SheetImporterConfig CreateConfigInteractive()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                CREATE_DIALOG_TITLE,
                CREATE_DEFAULT_NAME,
                CREATE_EXT,
                CREATE_DIALOG_HINT,
                CREATE_DEFAULT_FOLDER);
            if (string.IsNullOrEmpty(path)) return null;

            var config = ScriptableObject.CreateInstance<SheetImporterConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            return config;
        }

        // -------- SerializedObject 관리 --------

        private void EnsureSerializedObject()
        {
            if (_config == null)
            {
                _serializedConfig = null;
                _entriesProp = null;
                return;
            }
            if (_serializedConfig == null || _serializedConfig.targetObject != _config)
            {
                _serializedConfig = new SerializedObject(_config);
                _entriesProp = _serializedConfig.FindProperty(PROP_ENTRIES);
                _previews.Clear();
                _advancedExpanded.Clear();
            }
        }

        private void ResetSerializedObject()
        {
            _serializedConfig = null;
            _entriesProp = null;
            _previews.Clear();
            _advancedExpanded.Clear();
        }

        private void MarkConfigDirty()
        {
            if (_config != null) EditorUtility.SetDirty(_config);
        }

        // -------- OnGUI --------

        private void OnGUI()
        {
            EnsureSerializedObject();
            _serializedConfig?.Update();

            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = LABEL_WIDTH;
            try
            {
                _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);

                DrawConfigSlot();
                EditorGUILayout.Space();
                DrawToolbar();
                EditorGUILayout.Space();
                DrawEntryList();
                EditorGUILayout.Space();
                DrawSummary();

                EditorGUILayout.EndScrollView();
            }
            finally
            {
                EditorGUIUtility.labelWidth = prevLabelWidth;
            }

            if (_serializedConfig != null)
            {
                bool changed = _serializedConfig.ApplyModifiedProperties();
                if (changed) MarkConfigDirty();
            }
        }

        // -------- 상단 UI --------

        private void DrawConfigSlot()
        {
            EditorGUILayout.LabelField(LBL_CONFIG_SECTION, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var newConfig = (SheetImporterConfig)EditorGUILayout.ObjectField(
                LBL_CONFIG_FIELD, _config, typeof(SheetImporterConfig), false);
            if (EditorGUI.EndChangeCheck())
            {
                _config = newConfig;
                ResetSerializedObject();
            }

            if (_config == null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(BTN_NEW_CONFIG, GUILayout.Width(BUTTON_NEW_CONFIG_WIDTH), GUILayout.Height(BUTTON_NEW_CONFIG_HEIGHT)))
                    {
                        var created = CreateConfigInteractive();
                        if (created != null) AssignConfig(created);
                        GUIUtility.ExitGUI();
                    }
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.HelpBox(HINT_NO_CONFIG, MessageType.Info);
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_entriesProp == null))
                {
                    if (GUILayout.Button(BTN_ADD_ENTRY, GUILayout.Width(BUTTON_ADD_ENTRY_WIDTH), GUILayout.Height(BUTTON_BIG_HEIGHT)))
                    {
                        _entriesProp.arraySize++;
                        MarkConfigDirty();
                    }
                }

                bool canImport = _entriesProp != null && _entriesProp.arraySize > 0;
                using (new EditorGUI.DisabledScope(!canImport))
                {
                    if (GUILayout.Button(BTN_IMPORT_ALL, GUILayout.Height(BUTTON_BIG_HEIGHT)))
                    {
                        RunImportAll();
                        GUIUtility.ExitGUI();
                    }
                }

                if (GUILayout.Button(BTN_CLEAR_RESULT, GUILayout.Width(BUTTON_CLEAR_RESULT_WIDTH), GUILayout.Height(BUTTON_BIG_HEIGHT)))
                {
                    ClearResults();
                }
            }
        }

        // -------- 엔트리 리스트 --------

        private void DrawEntryList()
        {
            EditorGUILayout.LabelField(LBL_ENTRIES_SECTION, EditorStyles.boldLabel);

            if (_entriesProp == null) return;
            if (_entriesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox(HINT_NO_ENTRIES, MessageType.Info);
                return;
            }

            for (int i = 0; i < _entriesProp.arraySize; i++)
            {
                var entryProp = _entriesProp.GetArrayElementAtIndex(i);
                if (!DrawEntry(entryProp, i))
                {
                    DeleteArrayElement(_entriesProp, i);
                    _previews.Remove(i);
                    _advancedExpanded.Remove(i);
                    _lastResults.Remove(i);
                    MarkConfigDirty();
                    return;
                }
                EditorGUILayout.Space();
            }
        }

        /// <returns>false 반환 시 호출자가 이 엔트리를 삭제한다.</returns>
        private bool DrawEntry(SerializedProperty entryProp, int index)
        {
            var nameProp = entryProp.FindPropertyRelative(PROP_ENTRY_NAME);
            string nameVal = nameProp != null ? nameProp.stringValue : string.Empty;
            string header = string.IsNullOrEmpty(nameVal)
                ? string.Format(HDR_ENTRY_UNNAMED, index)
                : string.Format(HDR_ENTRY_WITH_NAME, index, nameVal);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    entryProp.isExpanded = EditorGUILayout.Foldout(entryProp.isExpanded, header, true);

                    string topSource = GetString(entryProp, PROP_SOURCE);
                    Type topTargetType = GetTargetType(entryProp);
                    bool topIsLocalization = GetMode(entryProp) == SheetEntryMode.Localization;
                    bool topCanImport = !string.IsNullOrEmpty(topSource) && (topIsLocalization || topTargetType != null);
                    using (new EditorGUI.DisabledScope(!topCanImport))
                    {
                        if (GUILayout.Button(BTN_IMPORT_SINGLE, GUILayout.Width(BUTTON_IMPORT_SINGLE_WIDTH)))
                        {
                            int captured = index;
                            RunImportSingle(captured);
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                if (!entryProp.isExpanded) return true;

                // 기본 섹션
                DrawPropertyField(entryProp, PROP_ENTRY_NAME, LBL_DATA_NAME, TIP_DATA_NAME);
                DrawPropertyField(entryProp, PROP_MODE, LBL_MODE, TIP_MODE);

                SheetEntryMode mode = GetMode(entryProp);
                bool isLocalization = mode == SheetEntryMode.Localization;

                DrawPropertyField(entryProp, PROP_SOURCE, LBL_SOURCE, TIP_SOURCE);

                if (isLocalization)
                {
                    DrawPropertyField(entryProp, PROP_KEY_HEADER, LBL_KEY_HEADER, TIP_KEY_HEADER);
                }
                else
                {
                    DrawPropertyField(entryProp, PROP_TARGET_SCRIPT, LBL_TARGET_SCRIPT, TIP_TARGET_SCRIPT);
                }

                EditorGUILayout.Space();

                // 고급 섹션
                bool advOpen = _advancedExpanded.TryGetValue(index, out var v) && v;
                advOpen = EditorGUILayout.Foldout(advOpen, LBL_ADVANCED, true, EditorStyles.foldoutHeader);
                _advancedExpanded[index] = advOpen;

                if (advOpen)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        DrawPropertyField(entryProp, PROP_HEADER_ROW, LBL_HEADER_ROW, TIP_HEADER_ROW);
                        DrawPropertyField(entryProp, PROP_FIRST_DATA_COLUMN, LBL_FIRST_DATA_COLUMN, TIP_FIRST_DATA_COLUMN);
                        DrawPropertyField(entryProp, PROP_LAST_DATA_COLUMN, LBL_LAST_DATA_COLUMN, TIP_LAST_DATA_COLUMN);

                        int first = GetInt(entryProp, PROP_FIRST_DATA_COLUMN);
                        int last = GetInt(entryProp, PROP_LAST_DATA_COLUMN);
                        if (first > 0 && last > 0 && first > last)
                        {
                            EditorGUILayout.HelpBox(string.Format(WARN_RANGE_INVALID, first, last), MessageType.Warning);
                        }

                        DrawPropertyField(entryProp, PROP_OUTPUT_FOLDER, LBL_OUTPUT_FOLDER, TIP_OUTPUT_FOLDER);

                        if (!isLocalization)
                        {
                            DrawPropertyField(entryProp, PROP_COLLECTION_SCRIPT, LBL_COLLECTION_SCRIPT, TIP_COLLECTION_SCRIPT);
                        }

                        if (!isLocalization)
                        {
                            EditorGUILayout.Space();
                            var aliasesProp = entryProp.FindPropertyRelative(PROP_ALIASES);
                            if (aliasesProp != null)
                            {
                                DrawAliasList(aliasesProp, LBL_ALIASES, TIP_ALIASES);
                            }

                            var childTablesProp = entryProp.FindPropertyRelative(PROP_CHILD_TABLES);
                            if (childTablesProp != null)
                            {
                                EditorGUILayout.Space();
                                DrawChildTableList(childTablesProp, LBL_CHILD_TABLES, TIP_CHILD_TABLES);
                            }
                        }
                    }
                }

                EditorGUILayout.Space();
                if (DrawEntryActions(entryProp, index))
                {
                    return false;
                }
                if (!isLocalization)
                {
                    DrawAnalysisResult(index);
                    DrawHeaderPreview(entryProp, index);
                }

                if (_lastResults.TryGetValue(index, out var r) && !string.IsNullOrEmpty(r.Summary))
                {
                    EditorGUILayout.HelpBox($"마지막 결과: {r.Summary}", r.Failed == 0 ? MessageType.Info : MessageType.Warning);
                }
            }
            return true;
        }

        private bool DrawEntryActions(SerializedProperty entryProp, int index)
        {
            Type targetType = GetTargetType(entryProp);
            string source = GetString(entryProp, PROP_SOURCE);
            SheetEntryMode mode = GetMode(entryProp);
            bool isLocalization = mode == SheetEntryMode.Localization;
            bool deleteRequested = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (!isLocalization)
                {
                    using (new EditorGUI.DisabledScope(targetType == null))
                    {
                        if (GUILayout.Button(BTN_ANALYZE_FIELDS, GUILayout.Width(BUTTON_ANALYZE_WIDTH)))
                        {
                            RunAnalysis(entryProp, index, targetType);
                        }
                    }
                    using (new EditorGUI.DisabledScope(targetType == null || string.IsNullOrEmpty(source)))
                    {
                        if (GUILayout.Button(BTN_LOAD_HEADERS, GUILayout.Width(BUTTON_LOAD_WIDTH)))
                        {
                            RunHeaderPreview(entryProp, index, targetType, source);
                        }
                    }
                    if (_previews.TryGetValue(index, out var pv) && (pv.Analysis.HasValue || pv.Matches != null))
                    {
                        if (GUILayout.Button(BTN_CLEAR_PREVIEW, GUILayout.Width(BUTTON_CLEAR_PREVIEW_WIDTH)))
                        {
                            _previews.Remove(index);
                        }
                    }
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(BTN_DELETE, GUILayout.Width(BUTTON_DELETE_WIDTH)))
                {
                    deleteRequested = true;
                }
            }

            if (!isLocalization && targetType == null)
                EditorGUILayout.HelpBox(HINT_ANALYZE_NO_TARGET, MessageType.None);
            else if (string.IsNullOrEmpty(source))
                EditorGUILayout.HelpBox(HINT_LOAD_NO_SOURCE, MessageType.None);

            return deleteRequested;
        }

        // -------- 분석/프리뷰 결과 --------

        private void DrawAnalysisResult(int index)
        {
            if (!_previews.TryGetValue(index, out var pv) || !pv.Analysis.HasValue) return;
            var a = pv.Analysis.Value;

            if (a.InvalidAliases.Count == 0 && a.UnusedAliases.Count == 0)
            {
                EditorGUILayout.HelpBox(ANALYSIS_OK, MessageType.Info);
                return;
            }
            if (a.InvalidAliases.Count > 0)
            {
                EditorGUILayout.HelpBox(ANALYSIS_INVALID_HEADER + "\n" + FormatAliasRefs(a.InvalidAliases), MessageType.Error);
            }
            if (a.UnusedAliases.Count > 0)
            {
                EditorGUILayout.HelpBox(ANALYSIS_UNUSED_HEADER + "\n" + FormatAliasRefs(a.UnusedAliases), MessageType.Warning);
            }
        }

        private static string FormatAliasRefs(List<SheetImporterReflectionHelper.AliasEntryRef> list)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                sb.Append(r.SheetHeader).Append(" → ").Append(r.FieldName);
                if (i < list.Count - 1) sb.Append('\n');
            }
            return sb.ToString();
        }

        private void DrawHeaderPreview(SerializedProperty entryProp, int index)
        {
            if (!_previews.TryGetValue(index, out var pv) || pv.Matches == null) return;

            if (!string.IsNullOrEmpty(pv.LastError))
            {
                EditorGUILayout.HelpBox(pv.LastError, MessageType.Error);
                return;
            }

            var fieldNames = pv.FieldNames ?? new List<string>();
            var fieldOptions = BuildDropdownOptions(fieldNames);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(LBL_PREVIEW_HEADER_COL, EditorStyles.miniBoldLabel, GUILayout.Width(COL_HEADER_WIDTH));
                GUILayout.Label(LBL_PREVIEW_RESOLVED_COL, EditorStyles.miniBoldLabel, GUILayout.Width(COL_RESOLVED_WIDTH));
                GUILayout.Label(LBL_PREVIEW_STATUS_COL, EditorStyles.miniBoldLabel, GUILayout.Width(COL_STATUS_WIDTH));
                GUILayout.Label(LBL_PREVIEW_ACTION_COL, EditorStyles.miniBoldLabel, GUILayout.MinWidth(COL_ACTION_MIN_WIDTH));
            }

            for (int i = 0; i < pv.Matches.Count; i++)
            {
                var m = pv.Matches[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(m.SheetHeader, GUILayout.Width(COL_HEADER_WIDTH));
                    GUILayout.Label(m.ResolvedFieldName ?? VALUE_NONE, EditorStyles.miniLabel, GUILayout.Width(COL_RESOLVED_WIDTH));
                    GUILayout.Label(StatusLabel(m.Status), EditorStyles.miniLabel, GUILayout.Width(COL_STATUS_WIDTH));

                    if (m.Status == SheetImporterReflectionHelper.HeaderMatchStatus.NeedsAlias
                        || m.Status == SheetImporterReflectionHelper.HeaderMatchStatus.Unknown)
                    {
                        int curIndex = 0;
                        if (pv.Selections.TryGetValue(m.SheetHeader, out string selected))
                        {
                            int found = fieldNames.IndexOf(selected);
                            if (found >= 0) curIndex = found + 1;
                        }
                        int newIndex = EditorGUILayout.Popup(curIndex, fieldOptions, GUILayout.MinWidth(COL_ACTION_MIN_WIDTH));
                        if (newIndex == 0) pv.Selections.Remove(m.SheetHeader);
                        else pv.Selections[m.SheetHeader] = fieldNames[newIndex - 1];
                    }
                    else
                    {
                        GUILayout.Label("—", EditorStyles.miniLabel, GUILayout.MinWidth(COL_ACTION_MIN_WIDTH));
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(pv.Selections.Count == 0))
                {
                    if (GUILayout.Button(BTN_APPLY_SELECTED, GUILayout.Width(BUTTON_APPLY_WIDTH)))
                    {
                        ApplySelectedMappings(entryProp, index);
                    }
                }
                GUILayout.FlexibleSpace();
            }

            if (!string.IsNullOrEmpty(pv.LastApplyMessage))
            {
                EditorGUILayout.HelpBox(pv.LastApplyMessage, MessageType.Info);
            }
        }

        private static string[] BuildDropdownOptions(List<string> fieldNames)
        {
            var arr = new string[fieldNames.Count + 1];
            arr[0] = DROPDOWN_NONE;
            for (int i = 0; i < fieldNames.Count; i++) arr[i + 1] = fieldNames[i];
            return arr;
        }

        private static string StatusLabel(SheetImporterReflectionHelper.HeaderMatchStatus s)
        {
            switch (s)
            {
                case SheetImporterReflectionHelper.HeaderMatchStatus.AutoMatched: return STATUS_AUTO;
                case SheetImporterReflectionHelper.HeaderMatchStatus.AliasMatched: return STATUS_ALIAS;
                case SheetImporterReflectionHelper.HeaderMatchStatus.NeedsAlias: return STATUS_NEEDS_ALIAS;
                default: return STATUS_UNKNOWN;
            }
        }

        // -------- 별칭 리스트 --------

        private void DrawChildTableList(SerializedProperty listProp, string label, string tooltip)
        {
            EditorGUILayout.LabelField(new GUIContent(label, tooltip), EditorStyles.boldLabel);

            if (listProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("자식 테이블 링크가 없습니다.", MessageType.None);
            }

            for (int i = 0; i < listProp.arraySize; i++)
            {
                var linkProp = listProp.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var fieldNameProp = linkProp.FindPropertyRelative("_parentFieldName");
                        string title = (fieldNameProp != null && !string.IsNullOrEmpty(fieldNameProp.stringValue))
                            ? fieldNameProp.stringValue
                            : $"#{i}";
                        linkProp.isExpanded = EditorGUILayout.Foldout(linkProp.isExpanded, title, true);
                        if (GUILayout.Button(BTN_DELETE, GUILayout.Width(60)))
                        {
                            DeleteArrayElement(listProp, i);
                            return;
                        }
                    }

                    if (!linkProp.isExpanded) continue;

                    DrawPropertyField(linkProp, "_parentFieldName", "부모 필드 이름",
                        "부모 SO 의 배열/리스트 필드 이름 (예: '_steps').");
                    DrawPropertyField(linkProp, "_source", "자식 소스",
                        "자식 CSV 소스. http(s):// URL 또는 Assets/... 로컬 경로.");
                    DrawPropertyField(linkProp, "_headerRow", LBL_HEADER_ROW, TIP_HEADER_ROW);
                    DrawPropertyField(linkProp, "_firstDataColumn", LBL_FIRST_DATA_COLUMN, TIP_FIRST_DATA_COLUMN);
                    DrawPropertyField(linkProp, "_lastDataColumn", LBL_LAST_DATA_COLUMN, TIP_LAST_DATA_COLUMN);
                    DrawPropertyField(linkProp, "_parentIdHeader", "부모 ID 컬럼(생략 가능)",
                        "자식 시트에서 부모 MID 가 담긴 컬럼 헤더. 비우면 기본 'parentId'.");
                    DrawPropertyField(linkProp, "_orderByHeader", "정렬 컬럼(생략 가능)",
                        "같은 부모 내 요소 정렬 기준 컬럼 헤더. 비우면 시트 등장 순서 및 기본 'order'.");

                    var aliasesP = linkProp.FindPropertyRelative(PROP_ALIASES);
                    if (aliasesP != null)
                    {
                        EditorGUILayout.Space();
                        DrawAliasList(aliasesP, LBL_ALIASES, TIP_ALIASES);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ 자식 테이블 추가", GUILayout.Width(160)))
                {
                    listProp.arraySize++;
                }
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawAliasList(SerializedProperty listProp, string label, string tooltip)
        {
            EditorGUILayout.LabelField(new GUIContent(label, tooltip), EditorStyles.boldLabel);

            if (listProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox(HINT_NO_ALIASES, MessageType.None);
            }

            for (int i = 0; i < listProp.arraySize; i++)
            {
                var aliasProp = listProp.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.HorizontalScope())
                {
                    var sh = aliasProp.FindPropertyRelative(PROP_SHEET_HEADER);
                    var fn = aliasProp.FindPropertyRelative(PROP_FIELD_NAME);
                    if (sh != null) EditorGUILayout.PropertyField(sh, new GUIContent(LBL_SHEET_HEADER, TIP_SHEET_HEADER));
                    if (fn != null) EditorGUILayout.PropertyField(fn, new GUIContent(LBL_FIELD_NAME, TIP_FIELD_NAME));
                    if (GUILayout.Button(BTN_REMOVE, GUILayout.Width(BUTTON_REMOVE_ALIAS_WIDTH)))
                    {
                        DeleteArrayElement(listProp, i);
                        return;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(BTN_ADD_ALIAS, GUILayout.Width(BUTTON_ADD_ALIAS_WIDTH)))
                {
                    listProp.arraySize++;
                }
                GUILayout.FlexibleSpace();
            }
        }

        // -------- 요약 --------

        private void DrawSummary()
        {
            if (!_summaryVisible) return;
            EditorGUILayout.LabelField(LBL_SUMMARY_SECTION, EditorStyles.boldLabel);
            string summary = $"생성 {_createdTotal} / 갱신 {_updatedTotal} / 실패 {_failedTotal} (에러 {_totalLogCount}건)";
            EditorGUILayout.HelpBox(summary, _failedTotal == 0 ? MessageType.Info : MessageType.Warning);
        }

        // -------- 분석/프리뷰 실행 --------

        private void RunAnalysis(SerializedProperty entryProp, int index, Type targetType)
        {
            var pv = GetOrCreatePreview(index);
            var entryAliases = CollectAliases(entryProp.FindPropertyRelative(PROP_ALIASES));

            pv.FieldNames = SheetImporterReflectionHelper.CollectSerializedFieldNames(targetType);
            pv.Analysis = SheetImporterReflectionHelper.AnalyzeAliases(targetType, entryAliases);
            pv.LastApplyMessage = null;
        }

        private void RunHeaderPreview(SerializedProperty entryProp, int index, Type targetType, string source)
        {
            var pv = GetOrCreatePreview(index);
            pv.LastApplyMessage = null;
            pv.Selections.Clear();
            pv.Matches = null;
            pv.LastError = null;

            try
            {
                var downloaders = BuildDownloaders();
                ISheetDownloader picked = PickDownloader(downloaders, source);
                if (picked == null)
                {
                    pv.LastError = $"처리 가능한 Downloader 가 없습니다. Source='{source}'";
                    pv.Matches = new List<SheetImporterReflectionHelper.HeaderMatch>();
                    return;
                }
                if (!picked.TryDownload(source, out string csvText, out string error))
                {
                    pv.LastError = $"다운로드 실패: {error}";
                    pv.Matches = new List<SheetImporterReflectionHelper.HeaderMatch>();
                    return;
                }

                if (SheetImporterReflectionHelper.LooksLikeHtml(csvText))
                {
                    pv.LastError = "CSV가 아닌 HTML 응답을 받았습니다. 시트 공유가 '링크 보유자 뷰어/편집자'인지, URL이 '/export?format=csv&gid=' 형식인지 확인하세요.";
                    Debug.LogError($"{LOG_PREFIX} 헤더 프리뷰 중단 — {pv.LastError}");
                    pv.Matches = new List<SheetImporterReflectionHelper.HeaderMatch>();
                    return;
                }

                var raw = CsvParser.ParseRaw(csvText);
                int headerRow = Math.Max(1, GetInt(entryProp, PROP_HEADER_ROW));
                int firstCol = GetInt(entryProp, PROP_FIRST_DATA_COLUMN);
                int lastCol = GetInt(entryProp, PROP_LAST_DATA_COLUMN);

                if (raw == null || raw.Count < headerRow)
                {
                    pv.LastError = $"헤더 행을 찾을 수 없습니다(행 {headerRow}).";
                    pv.Matches = new List<SheetImporterReflectionHelper.HeaderMatch>();
                    return;
                }

                var rawHeader = raw[headerRow - 1];
                var headers = SliceHeaders(rawHeader, firstCol, lastCol);

                var entryAliases = CollectAliases(entryProp.FindPropertyRelative(PROP_ALIASES));
                var aliasMap = SheetImporterReflectionHelper.BuildAliasMap(entryAliases);

                pv.FieldNames = SheetImporterReflectionHelper.CollectSerializedFieldNames(targetType);
                pv.Matches = SheetImporterReflectionHelper.BuildHeaderMatches(targetType, headers, aliasMap);
            }
            catch (Exception ex)
            {
                pv.LastError = $"프리뷰 예외: {ex.Message}";
                pv.Matches = new List<SheetImporterReflectionHelper.HeaderMatch>();
                Debug.LogError($"{LOG_PREFIX} 헤더 프리뷰 예외: {ex}");
            }
        }

        private static List<string> SliceHeaders(List<string> rawHeader, int firstCol, int lastCol)
        {
            var result = new List<string>();
            int firstIdx = Math.Max(0, firstCol - 1);
            int limit = lastCol > 0 ? Math.Min(lastCol, rawHeader.Count) : rawHeader.Count;
            for (int i = firstIdx; i < limit; i++)
            {
                string h = rawHeader[i];
                if (string.IsNullOrEmpty(h)) continue;
                if (h.StartsWith("#")) continue;
                result.Add(h);
            }
            return result;
        }

        private void ApplySelectedMappings(SerializedProperty entryProp, int index)
        {
            if (!_previews.TryGetValue(index, out var pv) || pv.Selections.Count == 0) return;

            var aliasesProp = entryProp.FindPropertyRelative(PROP_ALIASES);
            if (aliasesProp == null) return;

            int added = 0;
            foreach (var kv in pv.Selections)
            {
                string header = kv.Key;
                string field = kv.Value;
                if (string.IsNullOrEmpty(header) || string.IsNullOrEmpty(field)) continue;
                if (AliasAlreadyExists(aliasesProp, header)) continue;

                int insertAt = aliasesProp.arraySize;
                aliasesProp.arraySize++;
                var newItem = aliasesProp.GetArrayElementAtIndex(insertAt);
                var sh = newItem.FindPropertyRelative(PROP_SHEET_HEADER);
                var fn = newItem.FindPropertyRelative(PROP_FIELD_NAME);
                if (sh != null) sh.stringValue = header;
                if (fn != null) fn.stringValue = field;
                added++;
            }

            pv.Selections.Clear();
            pv.LastApplyMessage = added > 0 ? string.Format(HINT_APPLY_DONE, added) : HINT_APPLY_NONE;

            _serializedConfig.ApplyModifiedProperties();
            MarkConfigDirty();
            _serializedConfig.Update();
        }

        private static bool AliasAlreadyExists(SerializedProperty aliasesProp, string sheetHeader)
        {
            for (int i = 0; i < aliasesProp.arraySize; i++)
            {
                var item = aliasesProp.GetArrayElementAtIndex(i);
                var sh = item.FindPropertyRelative(PROP_SHEET_HEADER);
                if (sh != null && string.Equals(sh.stringValue, sheetHeader, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        // -------- 임포트 실행 --------

        private void RunImportAll()
        {
            if (_config == null || _config.Entries == null || _config.Entries.Count == 0) return;

            BeginRun();
            var downloaders = BuildDownloaders();
            try
            {
                var order = EntryDependencyResolver.ResolveOrder(_config.Entries);
                int total = order.Count;
                for (int ordinal = 0; ordinal < total; ordinal++)
                {
                    int i = order[ordinal];
                    var entry = _config.Entries[i];
                    if (entry == null) continue;

                    string label = GetEntryLabel(entry, i);
                    float progress = total > 0 ? (float)ordinal / total : 0f;
                    if (EditorUtility.DisplayCancelableProgressBar(PROGRESS_TITLE, $"({ordinal + 1}/{total}) {label}", progress))
                    {
                        Debug.Log($"{LOG_PREFIX} 사용자 취소");
                        break;
                    }

                    ImportOne(i, entry, downloaders, ordinal, total);
                }
                _summaryVisible = true;
                Debug.Log($"{LOG_PREFIX} 전체 가져오기 완료 — 생성 {_createdTotal} / 갱신 {_updatedTotal} / 실패 {_failedTotal} (에러 {_totalLogCount}건)");
            }
            catch (Exception ex)
            {
                HandleImportException(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private void RunImportSingle(int index)
        {
            if (_config == null || _config.Entries == null) return;
            if (index < 0 || index >= _config.Entries.Count) return;
            var entry = _config.Entries[index];
            if (entry == null) return;

            BeginRun();
            var downloaders = BuildDownloaders();
            try
            {
                var chain = EntryDependencyResolver.ResolveChain(index, _config.Entries);
                int total = chain.Count;
                for (int ordinal = 0; ordinal < total; ordinal++)
                {
                    int i = chain[ordinal];
                    var e = _config.Entries[i];
                    if (e == null) continue;

                    string label = GetEntryLabel(e, i);
                    float progress = total > 0 ? (float)ordinal / total : 0f;
                    if (EditorUtility.DisplayCancelableProgressBar(PROGRESS_TITLE, $"({ordinal + 1}/{total}) {label}", progress))
                    {
                        Debug.Log($"{LOG_PREFIX} 사용자 취소");
                        break;
                    }

                    ImportOne(i, e, downloaders, ordinal, total);
                }
                _summaryVisible = true;
                string targetLabel = GetEntryLabel(entry, index);
                Debug.Log($"{LOG_PREFIX} [{targetLabel}] 연관 가져오기 완료 (체인 {total}건) — 생성 {_createdTotal} / 갱신 {_updatedTotal} / 실패 {_failedTotal} (에러 {_totalLogCount}건)");
            }
            catch (Exception ex)
            {
                HandleImportException(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private void HandleImportException(Exception ex)
        {
            Debug.LogError($"{LOG_PREFIX} 가져오기 중 예외: {ex}");
            EditorUtility.DisplayDialog(DIALOG_EXCEPTION_TITLE, string.Format(DIALOG_EXCEPTION_FORMAT, ex.Message), BTN_DIALOG_CONFIRM);
        }

        private void ImportOne(int index, SheetImporterEntry entry, List<ISheetDownloader> downloaders, int entryOrdinal, int totalEntries)
        {
            string label = GetEntryLabel(entry, index);
            Debug.Log($"{LOG_PREFIX} [{label}] 가져오기 시작");

            if (string.IsNullOrEmpty(entry.Source))
            {
                RecordFailure(index, "실패: 소스 비어있음", $"{LOG_PREFIX} [{label}] Source 가 비어있습니다.");
                return;
            }

            ISheetDownloader picked = PickDownloader(downloaders, entry.Source);
            if (picked == null)
            {
                RecordFailure(index, "실패: 지원되지 않는 소스",
                    $"{LOG_PREFIX} [{label}] 처리 가능한 Downloader 가 없습니다. Source='{entry.Source}'");
                return;
            }

            if (!picked.TryDownload(entry.Source, out string csvText, out string downloadError))
            {
                RecordFailure(index, "실패: 다운로드", $"{LOG_PREFIX} [{label}] 다운로드 실패: {downloadError}");
                return;
            }

            int charCount = csvText != null ? csvText.Length : 0;
            Debug.Log($"{LOG_PREFIX} [{label}] 다운로드 완료 ({charCount}자)");

            Action<float, string> progress = (p, stage) =>
            {
                float clamped = Mathf.Clamp01(p);
                float overall = totalEntries > 0 ? ((float)entryOrdinal + clamped) / totalEntries : clamped;
                string stageLabel = string.IsNullOrEmpty(stage) ? label : $"{label} — {stage}";
                EditorUtility.DisplayProgressBar(PROGRESS_TITLE, stageLabel, overall);
            };

            GenericSoImporter.ImportResult r;
            try
            {
                switch (entry.Mode)
                {
                    case SheetEntryMode.Localization:
                        r = LocalizationTableExporter.ExportEntry(_config, entry, csvText, progress);
                        break;
                    default:
                        r = GenericSoImporter.ImportEntry(_config, entry, csvText, progress);
                        break;
                }
            }
            catch (Exception ex)
            {
                RecordFailure(index, "실패: 예외", $"{LOG_PREFIX} [{label}] 예외: {ex.Message}");
                Debug.LogError($"{LOG_PREFIX} [{label}] 예외: {ex}");
                return;
            }

            _createdTotal += r.Created;
            _updatedTotal += r.Updated;
            _failedTotal += r.Failed;
            _totalLogCount += r.ErrorLogs;

            if (entry.Mode == SheetEntryMode.GenericSO && r.Failed == 0 && entry.ChildTables != null)
            {
                for (int ci = 0; ci < entry.ChildTables.Count; ci++)
                {
                    var link = entry.ChildTables[ci];
                    if (link == null || string.IsNullOrEmpty(link.Source)) continue;

                    string linkLabel = string.IsNullOrEmpty(link.ParentFieldName) ? $"#{ci}" : link.ParentFieldName;

                    ISheetDownloader childPicked = PickDownloader(downloaders, link.Source);
                    if (childPicked == null)
                    {
                        Debug.LogError($"{LOG_PREFIX} [{label}.{linkLabel}] 처리 가능한 Downloader 가 없습니다. Source='{link.Source}'");
                        _failedTotal++;
                        continue;
                    }
                    if (!childPicked.TryDownload(link.Source, out string childCsv, out string childErr))
                    {
                        Debug.LogError($"{LOG_PREFIX} [{label}.{linkLabel}] 다운로드 실패: {childErr}");
                        _failedTotal++;
                        continue;
                    }
                    Debug.Log($"{LOG_PREFIX} [{label}.{linkLabel}] 자식 다운로드 완료 ({(childCsv != null ? childCsv.Length : 0)}자)");

                    GenericSoImporter.ImportResult cr;
                    try
                    {
                        cr = ChildTableImporter.ImportLink(entry, link, childCsv, progress);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{LOG_PREFIX} [{label}.{linkLabel}] 자식 예외: {ex}");
                        _failedTotal++;
                        continue;
                    }

                    _createdTotal += cr.Created;
                    _updatedTotal += cr.Updated;
                    _failedTotal += cr.Failed;
                    _totalLogCount += cr.ErrorLogs;
                }
            }

            string summary = $"생성 {r.Created} / 갱신 {r.Updated} / 실패 {r.Failed}";
            if (r.UnusedExisting > 0) summary += $" / 미사용 {r.UnusedExisting}";
            if (r.ErrorLogs > 0) summary += $" (에러 {r.ErrorLogs}건)";

            _lastResults[index] = new EntryResult
            {
                Created = r.Created,
                Updated = r.Updated,
                Failed = r.Failed,
                Summary = summary,
                FirstError = r.FirstErrorOrNull,
            };
            Debug.Log($"{LOG_PREFIX} [{label}] 완료 — {summary}");
            if (!string.IsNullOrEmpty(r.FirstErrorOrNull))
            {
                Debug.LogError($"{LOG_PREFIX} [{label}] 첫 에러: {r.FirstErrorOrNull}");
            }
        }

        private void RecordFailure(int index, string summary, string logLine)
        {
            _lastResults[index] = new EntryResult
            {
                Created = 0,
                Updated = 0,
                Failed = 1,
                Summary = summary,
                FirstError = summary,
            };
            _failedTotal++;
            _totalLogCount++;
            Debug.LogError(logLine);
        }

        // -------- Downloaders / 유틸 --------

        private static List<ISheetDownloader> BuildDownloaders()
        {
            return new List<ISheetDownloader>
            {
                new LocalCsvDownloader(),
                new UrlSheetDownloader(),
            };
        }

        private static ISheetDownloader PickDownloader(List<ISheetDownloader> downloaders, string source)
        {
            for (int i = 0; i < downloaders.Count; i++)
            {
                if (downloaders[i].CanHandle(source)) return downloaders[i];
            }
            return null;
        }

        private void BeginRun()
        {
            _createdTotal = 0;
            _updatedTotal = 0;
            _failedTotal = 0;
            _totalLogCount = 0;
            _summaryVisible = false;
        }

        private void ClearResults()
        {
            _lastResults.Clear();
            _totalLogCount = 0;
            _createdTotal = 0;
            _updatedTotal = 0;
            _failedTotal = 0;
            _summaryVisible = false;
        }

        private EntryPreview GetOrCreatePreview(int index)
        {
            if (!_previews.TryGetValue(index, out var pv))
            {
                pv = new EntryPreview();
                _previews[index] = pv;
            }
            return pv;
        }

        private static List<(string sheetHeader, string fieldName)> CollectAliases(SerializedProperty listProp)
        {
            var result = new List<(string, string)>();
            if (listProp == null) return result;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var item = listProp.GetArrayElementAtIndex(i);
                var sh = item.FindPropertyRelative(PROP_SHEET_HEADER);
                var fn = item.FindPropertyRelative(PROP_FIELD_NAME);
                if (sh == null || fn == null) continue;
                result.Add((sh.stringValue, fn.stringValue));
            }
            return result;
        }

        private static void DrawPropertyField(SerializedProperty parent, string name, string label, string tooltip)
        {
            var prop = parent.FindPropertyRelative(name);
            if (prop == null) return;
            EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip));
        }

        private static string GetString(SerializedProperty parent, string name)
        {
            var p = parent.FindPropertyRelative(name);
            return p != null ? p.stringValue : null;
        }

        private static int GetInt(SerializedProperty parent, string name)
        {
            var p = parent.FindPropertyRelative(name);
            return p != null ? p.intValue : 0;
        }

        private static Type GetTargetType(SerializedProperty entryProp)
        {
            var p = entryProp.FindPropertyRelative(PROP_TARGET_SCRIPT);
            if (p == null) return null;
            var mono = p.objectReferenceValue as MonoScript;
            return mono != null ? mono.GetClass() : null;
        }

        private static SheetEntryMode GetMode(SerializedProperty entryProp)
        {
            var p = entryProp.FindPropertyRelative(PROP_MODE);
            return p != null ? (SheetEntryMode)p.enumValueIndex : SheetEntryMode.GenericSO;
        }

        private static string GetEntryLabel(SheetImporterEntry entry, int index)
        {
            if (!string.IsNullOrEmpty(entry.EntryName)) return entry.EntryName;
            if (entry.TargetType != null) return entry.TargetType.Name;
            return $"#{index}";
        }

        /// <summary>
        /// Object 참조 슬롯 포함 배열 요소 안전 삭제. Unity 관례: 첫 Delete 가 null 화만 하면 1회 더 호출.
        /// </summary>
        private static void DeleteArrayElement(SerializedProperty listProp, int index)
        {
            int before = listProp.arraySize;
            listProp.DeleteArrayElementAtIndex(index);
            if (listProp.arraySize == before)
            {
                listProp.DeleteArrayElementAtIndex(index);
            }
        }

        // -------- 내부 클래스 --------

        private class EntryResult
        {
            public int Created { get; set; }
            public int Updated { get; set; }
            public int Failed { get; set; }
            public string Summary { get; set; }
            public string FirstError { get; set; }
        }

        private class EntryPreview
        {
            public SheetImporterReflectionHelper.AliasAnalysis? Analysis;
            public List<string> FieldNames;
            public List<SheetImporterReflectionHelper.HeaderMatch> Matches;
            public readonly Dictionary<string, string> Selections = new Dictionary<string, string>();
            public string LastError;
            public string LastApplyMessage;
        }
    }
}
#endif
