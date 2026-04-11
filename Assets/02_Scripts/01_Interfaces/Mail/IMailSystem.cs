using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 우편함 서비스 인터페이스.
    /// 우편 송수신, 보상 수령, 만료 처리.
    /// </summary>
    public interface IMailSystem : IService
    {
        void SendMail(MailData mail);
        IReadOnlyList<MailData> GetAllMails();
        IReadOnlyList<MailData> GetMailsByType(MailType type);
        MailData GetMail(string mailId);
        int GetUnreadCount();
        int GetClaimableCount();
        void ReadMail(string mailId);
        bool ClaimMail(string mailId);
        int ClaimAll(MailType? typeFilter = null);
        bool DeleteMail(string mailId);
        int DeleteClaimedMails();
        void FetchFromServer(Action<int> onComplete);
        void ProcessExpired();
    }
}
