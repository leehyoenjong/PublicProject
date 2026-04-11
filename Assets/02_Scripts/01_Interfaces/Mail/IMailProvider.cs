using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 서버 우편 연동 추상화 인터페이스.
    /// </summary>
    public interface IMailProvider
    {
        void FetchMails(Action<List<MailData>> onSuccess, Action<string> onFail);
        void ReportClaimed(string mailId);
        void ReportDeleted(string mailId);
        bool IsAvailable { get; }
    }
}
