using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    [Serializable]
    public class LeaderboardBinding
    {
        public LeaderboardKey Key;
        public string Uuid;
    }

    [Serializable]
    public class FlexibleTableBinding
    {
        public FlexibleTableKey Key;
        public string TableName;
    }

    /// <summary>
    /// 뒤끝 네트워크 시스템 설정. 인증키/AppSecret 은 뒤끝 SDK Settings 에서 관리한다.
    /// 여기에는 앱 메타데이터 + 런타임 옵션 + 논리키↔실제식별자 매핑만 둔다.
    /// </summary>
    [CreateAssetMenu(menuName = "PublicFramework/Backend/BackendConfig", fileName = "BackendConfig")]
    public class BackendConfig : ScriptableObject
    {
        [Header("앱 메타")]
        [SerializeField] private string _appVersion = "1.0.0";
        [SerializeField] private BackendEnvironment _environment = BackendEnvironment.Dev;

        [Header("자동 동작")]
        [SerializeField] private bool _autoGuestLogin = true;
        [SerializeField] private bool _sendQueueEnabled = true;
        [SerializeField] private bool _autoCloudSaveOnLogin = false;

        [Header("논리키 → 실제 식별자 매핑")]
        [SerializeField] private LeaderboardBinding[] _leaderboardUuids = new LeaderboardBinding[0];
        [SerializeField] private FlexibleTableBinding[] _flexibleTableNames = new FlexibleTableBinding[0];

        [Header("네트워크")]
        [SerializeField, Min(1)] private int _defaultTimeoutSec = 10;

        [Header("뒤끝 데이터베이스")]
        [SerializeField] private string _databaseUuid = string.Empty;

        [Header("분석")]
        [SerializeField] private bool _analyticsEnabled = false;
        [SerializeField] private bool _analyticsSessionAutoTrack = false;

        [Header("동의 및 크래시")]
        [SerializeField, Min(1)] private int _consentVersion = 1;
        [SerializeField] private bool _crashReporterEnabled = false;
        [SerializeField] private bool _crashIncludeErrors = false;
        [SerializeField] private bool _crashIncludeFullStackInDebugOnly = false;

        public string AppVersion => _appVersion;
        public BackendEnvironment Environment => _environment;
        public bool AutoGuestLogin => _autoGuestLogin;
        public bool SendQueueEnabled => _sendQueueEnabled;
        public bool AutoCloudSaveOnLogin => _autoCloudSaveOnLogin;
        public IReadOnlyList<LeaderboardBinding> LeaderboardUuids => _leaderboardUuids;
        public IReadOnlyList<FlexibleTableBinding> FlexibleTableNames => _flexibleTableNames;
        public int DefaultTimeoutSec => _defaultTimeoutSec;
        public string DatabaseUuid => _databaseUuid;
        public bool AnalyticsEnabled => _analyticsEnabled;
        public bool AnalyticsSessionAutoTrack => _analyticsSessionAutoTrack;
        public int ConsentVersion => _consentVersion;
        public bool CrashReporterEnabled => _crashReporterEnabled;
        public bool CrashIncludeErrors => _crashIncludeErrors;
        public bool CrashIncludeFullStackInDebugOnly => _crashIncludeFullStackInDebugOnly;

        public string GetLeaderboardUuid(LeaderboardKey key)
        {
            if (_leaderboardUuids == null) return string.Empty;
            for (int i = 0; i < _leaderboardUuids.Length; i++)
            {
                var binding = _leaderboardUuids[i];
                if (binding != null && binding.Key == key)
                    return binding.Uuid ?? string.Empty;
            }
            return string.Empty;
        }

        public string GetFlexibleTableName(FlexibleTableKey key)
        {
            if (_flexibleTableNames == null) return string.Empty;
            for (int i = 0; i < _flexibleTableNames.Length; i++)
            {
                var binding = _flexibleTableNames[i];
                if (binding != null && binding.Key == key)
                    return binding.TableName ?? string.Empty;
            }
            return string.Empty;
        }
    }
}
