using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 우편함 세이브 데이터. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class MailboxSaveData
    {
        [SerializeField] private List<MailData> _mails = new List<MailData>();
        [SerializeField] private string _lastFetchTime;

        public List<MailData> Mails { get => _mails; set => _mails = value; }
        public string LastFetchTime { get => _lastFetchTime; set => _lastFetchTime = value; }
    }
}
