using System;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using LitJson;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 서버 우편. IMailProvider 로 MailSystem 에 주입되고,
    /// IBackendMail 로 세부 서버 우편 API 를 직접 노출한다.
    /// 뒤끝 정책: 우편은 수령(ReceivePostItem) 시 서버에서 자동 제거되며, 명시적 삭제 API 는 제공하지 않는다.
    /// </summary>
    public class BackendMailProvider : IMailProvider, IBackendMail
    {
        private const string ACTION_FETCH = "FetchServerMails";
        private const string ACTION_CLAIM = "ClaimServerMail";

        private readonly IEventBus _eventBus;

        public bool IsAvailable { get; private set; } = true;

        public BackendMailProvider(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void FetchMails(Action<List<MailData>> onSuccess, Action<string> onFail)
        {
            FetchServerMails(onSuccess, onFail);
        }

        public void ReportClaimed(string mailId)
        {
            ClaimServerMail(mailId, null);
        }

        public void ReportDeleted(string mailId)
        {
            // 뒤끝 서버 우편은 수령 시 자동 제거. 수령 전 "삭제(거부)" 는 지원하지 않는다.
            // 로컬 측 리포트만 남기고 서버 호출은 생략한다.
            Debug.Log($"[BackendMail] ReportDeleted: 서버 삭제 API 미지원 (no-op) id={mailId}");
        }

        public void FetchServerMails(Action<List<MailData>> onSuccess, Action<string> onFail)
        {
            try
            {
                var bro = Backend.UPost.GetPostList(PostType.Admin);
                if (!bro.IsSuccess())
                {
                    var err = BackendErrorMapper.Map(bro);
                    Debug.LogWarning($"[BackendMail] 서버 우편 조회 실패: code={bro.GetStatusCode()}");
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_FETCH, err, bro.GetMessage());
                    onFail?.Invoke(bro.GetMessage());
                    return;
                }

                BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                var list = ParseMails(bro);
                Debug.Log($"[BackendMail] 서버 우편 {list.Count}건 수신");
                _eventBus?.Publish(new BackendMailFetchedEvent { Count = list.Count });
                onSuccess?.Invoke(list);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendMail] 우편 조회 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_FETCH, BackendError.NetworkError, e.Message);
                onFail?.Invoke(e.Message);
            }
        }

        public void ClaimServerMail(string mailId, Action<bool, BackendError, string> callback)
        {
            if (string.IsNullOrEmpty(mailId))
            {
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_CLAIM, BackendError.InvalidRequest, "mailId empty");
                callback?.Invoke(false, BackendError.InvalidRequest, "mailId empty");
                return;
            }

            try
            {
                var bro = Backend.UPost.ReceivePostItem(PostType.Admin, mailId);
                var ok = bro.IsSuccess();
                Debug.Log($"[BackendMail] 우편 수령: id={mailId}, ok={ok}");

                var err = BackendErrorMapper.Map(bro);
                if (ok) BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                else BackendEventDispatcher.PublishFailed(_eventBus, ACTION_CLAIM, err, bro.GetMessage());

                callback?.Invoke(ok, err, bro.GetMessage());
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendMail] 수령 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_CLAIM, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, BackendError.NetworkError, e.Message);
            }
        }

        private static List<MailData> ParseMails(BackendReturnObject bro)
        {
            var list = new List<MailData>();
            try
            {
                JsonData json = bro.GetReturnValuetoJSON();
                if (json == null || !json.ContainsKey("postItems")) return list;

                JsonData items = json["postItems"];
                if (items == null || !items.IsArray) return list;

                int count = items.Count;
                for (int i = 0; i < count; i++)
                {
                    var row = items[i];
                    var mail = new MailData
                    {
                        MailId = ReadString(row, "inDate"),
                        SenderName = ReadString(row, "sender"),
                        Title = ReadString(row, "title"),
                        Body = ReadString(row, "content"),
                        SentTime = ReadString(row, "sentDate"),
                        ExpiryTime = ReadString(row, "expirationDate"),
                        State = MailState.Unread,
                        MailType = MailType.System,
                    };
                    list.Add(mail);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendMail] 응답 파싱 실패: {e.Message}");
            }
            return list;
        }

        private static string ReadString(JsonData row, string field)
        {
            if (row == null || !row.ContainsKey(field)) return string.Empty;
            var v = row[field];
            return v == null ? string.Empty : v.ToString();
        }
    }
}
