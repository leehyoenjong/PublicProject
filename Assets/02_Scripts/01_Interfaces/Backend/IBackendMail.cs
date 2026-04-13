using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 서버 우편 (관리자 발송 우편) 조회/수령.
    /// 명시적 "삭제" API 는 제공하지 않는다 — 뒤끝 정책상 수령 시 자동 제거되며,
    /// 수령 전 거부는 기획상 만료 처리로 대체한다.
    /// </summary>
    public interface IBackendMail : IService
    {
        void FetchServerMails(Action<List<MailData>> onSuccess, Action<string> onFail);
        void ClaimServerMail(string mailId, Action<bool, BackendError, string> callback);
    }
}
