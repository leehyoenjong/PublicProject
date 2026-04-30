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
            Debug.Log("[우편] 로컬 제공자 조회: 빈 목록 반환 (로컬 전용).");
            onSuccess?.Invoke(new List<MailData>());
        }

        public void ReportClaimed(string mailId)
        {
            Debug.Log($"[우편] 수령 보고됨: {mailId}");
        }

        public void ReportDeleted(string mailId)
        {
            Debug.Log($"[우편] 삭제 보고됨: {mailId}");
        }
    }
}
