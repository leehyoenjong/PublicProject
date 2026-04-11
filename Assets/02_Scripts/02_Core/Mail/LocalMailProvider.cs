using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 기본 로컬 전용 우편 제공자. 서버 연동 없음.
    /// </summary>
    public class LocalMailProvider : IMailProvider
    {
        public bool IsAvailable => true;

        public void FetchMails(Action<List<MailData>> onSuccess, Action<string> onFail)
        {
            Debug.Log("[LocalMailProvider] Fetch: returning empty (local only)");
            onSuccess?.Invoke(new List<MailData>());
        }

        public void ReportClaimed(string mailId)
        {
            Debug.Log($"[LocalMailProvider] Claimed reported: {mailId}");
        }

        public void ReportDeleted(string mailId)
        {
            Debug.Log($"[LocalMailProvider] Deleted reported: {mailId}");
        }
    }
}
