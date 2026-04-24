using System;
using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IMailProvider. Fetch 결과 사전 지정 + Report 호출 기록.</summary>
    public class FakeMailProvider : IMailProvider
    {
        public bool IsAvailable { get; set; } = true;
        public List<MailData> NextFetch { get; } = new List<MailData>();
        public string FailReason { get; set; }
        public List<string> ClaimedReports { get; } = new List<string>();
        public List<string> DeletedReports { get; } = new List<string>();
        public int FetchCallCount { get; private set; }

        public void FetchMails(Action<List<MailData>> onSuccess, Action<string> onFail)
        {
            FetchCallCount++;
            if (FailReason != null)
            {
                onFail?.Invoke(FailReason);
                return;
            }
            onSuccess?.Invoke(new List<MailData>(NextFetch));
        }

        public void ReportClaimed(string mailId) => ClaimedReports.Add(mailId);
        public void ReportDeleted(string mailId) => DeletedReports.Add(mailId);
    }
}
