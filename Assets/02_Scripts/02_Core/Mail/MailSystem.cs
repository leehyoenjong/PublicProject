using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IMailSystem 구현체.
    /// IMailProvider + IEventBus + ISaveSystem DI.
    /// 만료 처리, 최대 보관, 중복 수령 방지.
    /// </summary>
    public class MailSystem : IMailSystem
    {
        private readonly IMailProvider _mailProvider;
        private readonly IEventBus _eventBus;
        private readonly ISaveSystem _saveSystem;
        private readonly MailConfig _config;

        private readonly List<MailData> _mails = new List<MailData>();

        private const int SAVE_SLOT = 0;
        private const string SAVE_KEY = "mail_data";
        private const int MAIL_ID_LENGTH = 12;

        public MailSystem(IMailProvider mailProvider, IEventBus eventBus,
            ISaveSystem saveSystem, MailConfig config)
        {
            _mailProvider = mailProvider;
            _eventBus = eventBus;
            _saveSystem = saveSystem;
            _config = config;

            LoadMailbox();
            ProcessExpired();

            Debug.Log("[우편] 초기화 시작.");
        }

        public void SendMail(MailData mail)
        {
            if (mail == null)
            {
                Debug.LogError("[우편] MailData가 null임.");
                return;
            }

            if (string.IsNullOrEmpty(mail.MailId))
            {
                mail.MailId = Guid.NewGuid().ToString("N").Substring(0, MAIL_ID_LENGTH);
            }

            if (string.IsNullOrEmpty(mail.SentTime))
            {
                mail.SentTime = DateTime.UtcNow.ToString("o");
            }

            if (string.IsNullOrEmpty(mail.ExpiryTime) && _config != null)
            {
                DateTime expiry = DateTime.UtcNow.AddDays(_config.DefaultExpiryDays);
                mail.ExpiryTime = expiry.ToString("o");
            }

            mail.State = MailState.Unread;

            if (_config != null && _mails.Count >= _config.MaxMailCount)
            {
                if (!RemoveOldestDeletable())
                {
                    Debug.LogWarning("[우편] 우편함 가득 참 — 모든 우편에 미수령 보상 있음. 전송 거부됨.");
                    return;
                }
            }

            _mails.Add(mail);
            SaveMailbox();

            _eventBus?.Publish(new MailReceivedEvent
            {
                MailId = mail.MailId,
                MailType = mail.MailType,
                Title = mail.Title,
                HasRewards = mail.HasRewards
            });

            CheckNearFull();
            Debug.Log($"[우편] 우편 전송됨: {mail.MailId} ({mail.Title})");
        }

        public IReadOnlyList<MailData> GetAllMails()
        {
            return _mails.AsReadOnly();
        }

        public IReadOnlyList<MailData> GetMailsByType(MailType type)
        {
            var filtered = new List<MailData>();
            foreach (MailData mail in _mails)
            {
                if (mail.MailType == type && mail.State != MailState.Expired)
                {
                    filtered.Add(mail);
                }
            }
            return filtered.AsReadOnly();
        }

        public MailData GetMail(string mailId)
        {
            foreach (MailData mail in _mails)
            {
                if (mail.MailId == mailId) return mail;
            }
            return null;
        }

        public int GetUnreadCount()
        {
            int count = 0;
            foreach (MailData mail in _mails)
            {
                if (mail.State == MailState.Unread) count++;
            }
            return count;
        }

        public int GetClaimableCount()
        {
            int count = 0;
            foreach (MailData mail in _mails)
            {
                if (mail.HasRewards && (mail.State == MailState.Unread || mail.State == MailState.Read))
                {
                    count++;
                }
            }
            return count;
        }

        public void ReadMail(string mailId)
        {
            MailData mail = GetMail(mailId);
            if (mail == null) return;

            if (mail.State == MailState.Unread)
            {
                mail.State = MailState.Read;
                SaveMailbox();

                _eventBus?.Publish(new MailReadEvent { MailId = mailId });
                Debug.Log($"[우편] 우편 읽음: {mailId}");
            }
        }

        public bool ClaimMail(string mailId)
        {
            MailData mail = GetMail(mailId);
            if (mail == null)
            {
                Debug.LogWarning($"[우편] 우편을 찾을 수 없음: {mailId}");
                return false;
            }

            if (mail.State == MailState.Claimed)
            {
                Debug.LogWarning($"[우편] 이미 수령됨: {mailId}");
                return false;
            }

            if (mail.State == MailState.Expired)
            {
                Debug.LogWarning($"[우편] 우편 만료됨: {mailId}");
                return false;
            }

            mail.State = MailState.Claimed;

            if (mail.HasRewards)
            {
                GrantMailRewards(mail);
            }

            SaveMailbox();
            _mailProvider?.ReportClaimed(mailId);

            _eventBus?.Publish(new MailClaimedEvent
            {
                MailId = mailId,
                RewardCount = mail.Rewards?.Count ?? 0
            });

            Debug.Log($"[우편] 우편 수령됨: {mailId}");
            return true;
        }

        public int ClaimAll(MailType? typeFilter = null)
        {
            int claimed = 0;
            int totalRewards = 0;

            foreach (MailData mail in _mails)
            {
                if (mail.State == MailState.Claimed || mail.State == MailState.Expired) continue;
                if (!mail.HasRewards) continue;
                if (typeFilter.HasValue && mail.MailType != typeFilter.Value) continue;

                mail.State = MailState.Claimed;
                GrantMailRewards(mail);
                _mailProvider?.ReportClaimed(mail.MailId);

                totalRewards += mail.Rewards?.Count ?? 0;
                claimed++;
            }

            if (claimed > 0)
            {
                SaveMailbox();

                _eventBus?.Publish(new MailClaimAllEvent
                {
                    ClaimedCount = claimed,
                    TotalRewards = totalRewards
                });

                Debug.Log($"[우편] 전체 수령: {claimed}개 우편, {totalRewards}개 보상.");
            }

            return claimed;
        }

        public bool DeleteMail(string mailId)
        {
            for (int i = _mails.Count - 1; i >= 0; i--)
            {
                if (_mails[i].MailId == mailId)
                {
                    _mails.RemoveAt(i);
                    SaveMailbox();
                    _mailProvider?.ReportDeleted(mailId);

                    _eventBus?.Publish(new MailDeletedEvent
                    {
                        MailId = mailId,
                        Reason = MailDeleteReason.Manual
                    });

                    Debug.Log($"[우편] 우편 삭제됨: {mailId}");
                    return true;
                }
            }
            return false;
        }

        public int DeleteClaimedMails()
        {
            int deleted = 0;

            for (int i = _mails.Count - 1; i >= 0; i--)
            {
                if (_mails[i].State == MailState.Claimed)
                {
                    string mailId = _mails[i].MailId;
                    _mails.RemoveAt(i);
                    _mailProvider?.ReportDeleted(mailId);

                    _eventBus?.Publish(new MailDeletedEvent
                    {
                        MailId = mailId,
                        Reason = MailDeleteReason.Manual
                    });

                    deleted++;
                }
            }

            if (deleted > 0)
            {
                SaveMailbox();
                Debug.Log($"[우편] 수령 완료 우편 {deleted}개 삭제됨.");
            }

            return deleted;
        }

        public void FetchFromServer(Action<int> onComplete)
        {
            if (_mailProvider == null || !_mailProvider.IsAvailable)
            {
                Debug.LogWarning("[우편] 우편 제공자 사용 불가.");
                onComplete?.Invoke(0);
                return;
            }

            _mailProvider.FetchMails(
                newMails =>
                {
                    int added = 0;
                    foreach (MailData mail in newMails)
                    {
                        if (GetMail(mail.MailId) != null) continue;

                        if (_config != null && _mails.Count >= _config.MaxMailCount)
                        {
                            if (!RemoveOldestDeletable())
                            {
                                Debug.LogWarning("[우편] 조회 중 우편함 가득 참 — 나머지 건너뜀.");
                                break;
                            }
                        }

                        _mails.Add(mail);

                        _eventBus?.Publish(new MailReceivedEvent
                        {
                            MailId = mail.MailId,
                            MailType = mail.MailType,
                            Title = mail.Title,
                            HasRewards = mail.HasRewards
                        });

                        added++;
                    }

                    if (added > 0)
                    {
                        SaveMailbox();
                        CheckNearFull();
                    }

                    Debug.Log($"[우편] {added}개 새 우편 조회됨.");
                    onComplete?.Invoke(added);
                },
                error =>
                {
                    Debug.LogError($"[우편] 조회 실패: {error}");
                    onComplete?.Invoke(0);
                });
        }

        public void ProcessExpired()
        {
            int expired = 0;

            foreach (MailData mail in _mails)
            {
                if (mail.State != MailState.Expired && mail.IsExpired)
                {
                    mail.State = MailState.Expired;

                    _eventBus?.Publish(new MailExpiredEvent
                    {
                        MailId = mail.MailId,
                        Title = mail.Title
                    });

                    expired++;
                }
            }

            if (_config != null && _config.ExpiredAutoDeleteDays > 0)
            {
                for (int i = _mails.Count - 1; i >= 0; i--)
                {
                    if (_mails[i].State != MailState.Expired) continue;

                    if (DateTime.TryParse(_mails[i].ExpiryTime, CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out DateTime expiry))
                    {
                        if ((DateTime.UtcNow - expiry).TotalDays >= _config.ExpiredAutoDeleteDays)
                        {
                            string mailId = _mails[i].MailId;
                            _mails.RemoveAt(i);

                            _eventBus?.Publish(new MailDeletedEvent
                            {
                                MailId = mailId,
                                Reason = MailDeleteReason.Expired
                            });
                        }
                    }
                }
            }

            if (expired > 0)
            {
                SaveMailbox();
                Debug.Log($"[우편] 만료 우편 {expired}개 처리됨.");
            }
        }

        private void GrantMailRewards(MailData mail)
        {
            if (mail.Rewards == null) return;

            foreach (MailRewardEntry reward in mail.Rewards)
            {
                _eventBus?.Publish(new RewardGrantedEvent
                {
                    ProductId = mail.MailId,
                    RewardId = reward.RewardId,
                    RewardType = reward.RewardType,
                    Amount = reward.Amount,
                    Source = "Mail"
                });
            }
        }

        private bool RemoveOldestDeletable()
        {
            // 1순위: Claimed 상태 우편
            for (int i = 0; i < _mails.Count; i++)
            {
                if (_mails[i].State == MailState.Claimed)
                {
                    _mails.RemoveAt(i);
                    return true;
                }
            }

            // 2순위: 보상 없는 Read 상태 우편
            for (int i = 0; i < _mails.Count; i++)
            {
                if (_mails[i].State == MailState.Read && !_mails[i].HasRewards)
                {
                    _mails.RemoveAt(i);
                    return true;
                }
            }

            // 3순위: Expired 상태 우편
            for (int i = 0; i < _mails.Count; i++)
            {
                if (_mails[i].State == MailState.Expired)
                {
                    _mails.RemoveAt(i);
                    return true;
                }
            }

            // 모두 미수령 보상이 있는 우편 → 삭제 불가
            return false;
        }

        private void CheckNearFull()
        {
            if (_config == null) return;

            if (_mails.Count >= _config.MaxMailCount * _config.NearFullThreshold)
            {
                _eventBus?.Publish(new MailNearFullEvent
                {
                    CurrentCount = _mails.Count,
                    MaxCount = _config.MaxMailCount
                });
            }
        }

        private void LoadMailbox()
        {
            if (_saveSystem == null) return;

            if (_saveSystem.HasKey(SAVE_SLOT, SAVE_KEY))
            {
                var data = _saveSystem.Load<MailboxSaveData>(SAVE_SLOT, SAVE_KEY);
                if (data?.Mails != null)
                {
                    _mails.AddRange(data.Mails);
                }
            }

            Debug.Log($"[우편] {_mails.Count}개 우편 로드됨.");
        }

        private void SaveMailbox()
        {
            if (_saveSystem == null) return;

            var data = new MailboxSaveData
            {
                Mails = new List<MailData>(_mails),
                LastFetchTime = DateTime.UtcNow.ToString("o")
            };

            _saveSystem.Save(SAVE_SLOT, SAVE_KEY, data);
            _saveSystem.WriteToDisk(SAVE_SLOT);
        }
    }
}
