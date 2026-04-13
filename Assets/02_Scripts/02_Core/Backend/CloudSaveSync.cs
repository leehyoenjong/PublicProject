using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using BackEnd;
using LitJson;

namespace PublicFramework
{
    /// <summary>
    /// 세이브 슬롯 클라우드 동기화.
    /// 직렬화된 슬롯 바이트를 Base64 문자열로 변환해 뒤끝 GameData 에 저장한다.
    /// - 테이블: CLOUD_SAVE
    /// - 컬럼: slot(int), payload(base64 string), savedAt(utc iso string)
    /// 저장 정책: Upsert (slot 당 단일 row). 기존 row 존재 시 UpdateV2, 없으면 Insert.
    /// 구 Insert-only 구현 이전 버전 데이터는 Download 시 복수 row 감지 + LogWarning 으로 안내.
    /// </summary>
    public class CloudSaveSync : ICloudSaveSync
    {
        private const string ACTION_UPLOAD = "UploadSlot";
        private const string ACTION_DOWNLOAD = "DownloadSlot";
        private const string ACTION_REMOTE_TIME = "GetRemoteTimestamp";
        private const string ACTION_OVERWRITE = "OverwriteRemoteSlot";

        private const string TABLE_CLOUD_SAVE = "CLOUD_SAVE";
        private const string COLUMN_SLOT = "slot";
        private const string COLUMN_PAYLOAD = "payload";
        private const string COLUMN_SAVED_AT = "savedAt";

        private readonly ISaveSystem _saveSystem;
        private readonly IEventBus _eventBus;

        public CloudSaveConflictStrategy ConflictStrategy { get; set; } = CloudSaveConflictStrategy.PreferNewest;

        public CloudSaveSync(ISaveSystem saveSystem, IEventBus eventBus)
        {
            _saveSystem = saveSystem;
            _eventBus = eventBus;
        }

        /// <summary>
        /// 로컬 슬롯을 즉시 디스크에 flush 후, 바이트를 Base64 로 변환해 서버에 Upsert 업로드한다.
        /// 호출 규약: 호출자는 별도로 <see cref="ISaveSystem.WriteToDisk"/> 를 먼저 부를 필요가 없다 — 내부에서 자동으로 수행한다.
        /// </summary>
        public void UploadSlot(int slotIndex, Action<bool, BackendError, string> callback)
        {
            if (_saveSystem == null)
            {
                Debug.LogWarning("[CloudSaveSync] 업로드 중단: saveSystem null");
                PublishUploadResult(slotIndex, false);
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_UPLOAD, BackendError.NotInitialized, "saveSystem null");
                callback?.Invoke(false, BackendError.NotInitialized, "saveSystem null");
                return;
            }

            try
            {
                _saveSystem.WriteToDisk(slotIndex);
                string path = SavePathHelper.GetSlotPath(slotIndex);
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[CloudSaveSync] 업로드 중단: slot 파일 없음 {path}");
                    PublishUploadResult(slotIndex, false);
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_UPLOAD, BackendError.NotFound, "slot file missing");
                    callback?.Invoke(false, BackendError.NotFound, "slot file missing");
                    return;
                }
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes == null || bytes.Length == 0)
                {
                    Debug.LogWarning($"[CloudSaveSync] 업로드 중단: slot {slotIndex} 비어있음");
                    PublishUploadResult(slotIndex, false);
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_UPLOAD, BackendError.NotFound, "slot empty");
                    callback?.Invoke(false, BackendError.NotFound, "slot empty");
                    return;
                }

                string payload = Convert.ToBase64String(bytes);
                string savedAt = DateTime.UtcNow.ToString("o");

                var param = new Param();
                param.Add(COLUMN_SLOT, slotIndex);
                param.Add(COLUMN_PAYLOAD, payload);
                param.Add(COLUMN_SAVED_AT, savedAt);

                // 1) 기존 row 조회
                var whereLookup = new Where();
                whereLookup.Equal(COLUMN_SLOT, slotIndex);
                var lookupBro = Backend.GameData.GetMyData(TABLE_CLOUD_SAVE, whereLookup);
                if (!lookupBro.IsSuccess())
                {
                    var err = BackendErrorMapper.Map(lookupBro);
                    Debug.LogWarning($"[CloudSaveSync] Upsert 조회 실패: code={lookupBro.GetStatusCode()}");
                    PublishUploadResult(slotIndex, false);
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_UPLOAD, err, lookupBro.GetMessage());
                    callback?.Invoke(false, err, lookupBro.GetMessage());
                    return;
                }

                string existingInDate = ExtractFirstInDate(lookupBro);

                // 2) 분기: 기존 있으면 UpdateV2, 없으면 Insert
                // 뒤끝 SDK 실제 UpdateV2 시그니처: `UpdateV2(string table, string rowInDate, string owner_inDate, Param param)`.
                // 3번째 인자는 Where 가 아닌 소유자 inDate (=Backend.UserInDate).
                BackendReturnObject writeBro;
                if (!string.IsNullOrEmpty(existingInDate))
                {
                    string ownerInDate = Backend.UserInDate ?? string.Empty;
                    writeBro = Backend.GameData.UpdateV2(TABLE_CLOUD_SAVE, existingInDate, ownerInDate, param);
                    Debug.Log($"[CloudSaveSync] 슬롯 Upsert(Update): slot={slotIndex}, inDate={existingInDate}, size={bytes.Length}B");
                }
                else
                {
                    writeBro = Backend.GameData.Insert(TABLE_CLOUD_SAVE, param);
                    Debug.Log($"[CloudSaveSync] 슬롯 Upsert(Insert): slot={slotIndex}, size={bytes.Length}B");
                }

                var ok = writeBro.IsSuccess();
                var writeErr = BackendErrorMapper.Map(writeBro);
                PublishUploadResult(slotIndex, ok);
                if (ok) BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                else BackendEventDispatcher.PublishFailed(_eventBus, ACTION_UPLOAD, writeErr, writeBro.GetMessage());

                callback?.Invoke(ok, writeErr, writeBro.GetMessage());
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveSync] 업로드 예외: slot={slotIndex}, msg={e.Message}");
                PublishUploadResult(slotIndex, false);
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_UPLOAD, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, BackendError.NetworkError, e.Message);
            }
        }

        public void DownloadSlot(int slotIndex, Action<bool, BackendError, string> callback)
        {
            if (_saveSystem == null)
            {
                Debug.LogWarning("[CloudSaveSync] 다운로드 중단: saveSystem null");
                PublishDownloadResult(slotIndex, false);
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_DOWNLOAD, BackendError.NotInitialized, "saveSystem null");
                callback?.Invoke(false, BackendError.NotInitialized, "saveSystem null");
                return;
            }

            try
            {
                var where = new Where();
                where.Equal(COLUMN_SLOT, slotIndex);
                var bro = Backend.GameData.GetMyData(TABLE_CLOUD_SAVE, where);
                if (!bro.IsSuccess())
                {
                    var err = BackendErrorMapper.Map(bro);
                    Debug.LogWarning($"[CloudSaveSync] 다운로드 실패: code={bro.GetStatusCode()}");
                    PublishDownloadResult(slotIndex, false);
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_DOWNLOAD, err, bro.GetMessage());
                    callback?.Invoke(false, err, bro.GetMessage());
                    return;
                }

                BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);

                var selected = SelectSingleRow(bro, slotIndex);
                if (selected == null)
                {
                    Debug.LogWarning($"[CloudSaveSync] 원격 슬롯 없음: slot={slotIndex}");
                    PublishDownloadResult(slotIndex, false);
                    callback?.Invoke(false, BackendError.NotFound, "no remote slot");
                    return;
                }

                string payload = ExtractCellString(selected, COLUMN_PAYLOAD);
                if (string.IsNullOrEmpty(payload))
                {
                    Debug.LogWarning($"[CloudSaveSync] payload 비어있음: slot={slotIndex}");
                    PublishDownloadResult(slotIndex, false);
                    callback?.Invoke(false, BackendError.NotFound, "empty payload");
                    return;
                }

                byte[] bytes = Convert.FromBase64String(payload);
                string path = SavePathHelper.GetSlotPath(slotIndex);
                File.WriteAllBytes(path, bytes);
                _saveSystem.ReadFromDisk(slotIndex);

                Debug.Log($"[CloudSaveSync] 슬롯 다운로드 완료: slot={slotIndex}, size={bytes.Length}B");
                PublishDownloadResult(slotIndex, true);
                callback?.Invoke(true, BackendError.None, string.Empty);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveSync] 다운로드 예외: slot={slotIndex}, msg={e.Message}");
                PublishDownloadResult(slotIndex, false);
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_DOWNLOAD, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, BackendError.NetworkError, e.Message);
            }
        }

        /// <summary>
        /// 원격 슬롯을 Delete + Insert 로 강제 덮어쓴다.
        /// 뒤끝 SDK `Backend.GameData.DeleteV2(table, inDate, ownerInDate, Action<BRO>)` 콜백 버전을
        /// 내부에서 <see cref="TaskCompletionSource{TResult}"/> 로 래핑해 await 가능한 Task 로 변환한다.
        /// 외부 Action 콜백 시그니처는 변경 없음 — 최종 결과만 <see cref="BackendMainThreadDispatcher"/> 경유 통지.
        /// 모든 Delete 가 성공해야 Insert 를 시도하며, 하나라도 실패 시 Insert 없이 실패 콜백을 전달한다.
        /// </summary>
        public async void OverwriteRemoteSlot(int slotIndex, Action<bool, BackendError, string> onComplete)
        {
            if (_saveSystem == null)
            {
                Debug.LogWarning("[CloudSaveSync] Overwrite 중단: saveSystem null");
                PublishUploadResult(slotIndex, false);
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_OVERWRITE, BackendError.NotInitialized, "saveSystem null");
                DispatchCallback(onComplete, false, BackendError.NotInitialized, "saveSystem null");
                return;
            }

            byte[] bytes;
            try
            {
                _saveSystem.WriteToDisk(slotIndex);
                string path = SavePathHelper.GetSlotPath(slotIndex);
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[CloudSaveSync] Overwrite 중단: slot 파일 없음 {path}");
                    PublishUploadResult(slotIndex, false);
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_OVERWRITE, BackendError.NotFound, "slot file missing");
                    DispatchCallback(onComplete, false, BackendError.NotFound, "slot file missing");
                    return;
                }
                bytes = File.ReadAllBytes(path);
                if (bytes == null || bytes.Length == 0)
                {
                    Debug.LogWarning($"[CloudSaveSync] Overwrite 중단: slot {slotIndex} 비어있음");
                    PublishUploadResult(slotIndex, false);
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_OVERWRITE, BackendError.NotFound, "slot empty");
                    DispatchCallback(onComplete, false, BackendError.NotFound, "slot empty");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveSync] Overwrite 로컬 단계 예외: {e.Message}");
                PublishUploadResult(slotIndex, false);
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_OVERWRITE, BackendError.NetworkError, e.Message);
                DispatchCallback(onComplete, false, BackendError.NetworkError, e.Message);
                return;
            }

            // 1) 기존 row 조회
            BackendReturnObject lookupBro;
            try
            {
                var whereLookup = new Where();
                whereLookup.Equal(COLUMN_SLOT, slotIndex);
                lookupBro = Backend.GameData.GetMyData(TABLE_CLOUD_SAVE, whereLookup);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveSync] Overwrite 조회 예외: {e.Message}");
                PublishUploadResult(slotIndex, false);
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_OVERWRITE, BackendError.NetworkError, e.Message);
                DispatchCallback(onComplete, false, BackendError.NetworkError, e.Message);
                return;
            }

            if (!lookupBro.IsSuccess())
            {
                var err = BackendErrorMapper.Map(lookupBro);
                Debug.LogWarning($"[CloudSaveSync] Overwrite 조회 실패: code={lookupBro.GetStatusCode()}");
                PublishUploadResult(slotIndex, false);
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_OVERWRITE, err, lookupBro.GetMessage());
                DispatchCallback(onComplete, false, err, lookupBro.GetMessage());
                return;
            }

            var inDates = ExtractAllInDates(lookupBro);
            string ownerInDate = Backend.UserInDate ?? string.Empty;

            // 2) 모든 기존 row Delete — 콜백 버전을 Task 로 래핑.
            if (inDates.Count > 0)
            {
                var deleteTasks = new Task<bool>[inDates.Count];
                for (int i = 0; i < inDates.Count; i++)
                    deleteTasks[i] = DeleteRowAsync(inDates[i], ownerInDate);

                bool[] results;
                try
                {
                    results = await Task.WhenAll(deleteTasks);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CloudSaveSync] Overwrite Delete.WhenAll 예외: {e.Message}");
                    PublishUploadResult(slotIndex, false);
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_OVERWRITE, BackendError.NetworkError, e.Message);
                    DispatchCallback(onComplete, false, BackendError.NetworkError, e.Message);
                    return;
                }

                int success = 0;
                for (int i = 0; i < results.Length; i++)
                    if (results[i]) success++;

                Debug.Log($"[CloudSaveSync] Overwrite Delete: slot={slotIndex}, {success}/{results.Length} 성공");
                if (success != results.Length)
                {
                    Debug.LogWarning($"[CloudSaveSync] Overwrite 중단: Delete 일부 실패({results.Length - success}/{results.Length}) — Insert 시도 안함");
                    PublishUploadResult(slotIndex, false);
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_OVERWRITE, BackendError.ServerError, "delete partial failure");
                    DispatchCallback(onComplete, false, BackendError.ServerError, "delete partial failure");
                    return;
                }
            }

            // 3) 새 row Insert
            try
            {
                string payload = Convert.ToBase64String(bytes);
                string savedAt = DateTime.UtcNow.ToString("o");
                var param = new Param();
                param.Add(COLUMN_SLOT, slotIndex);
                param.Add(COLUMN_PAYLOAD, payload);
                param.Add(COLUMN_SAVED_AT, savedAt);

                var insertBro = Backend.GameData.Insert(TABLE_CLOUD_SAVE, param);
                var ok = insertBro.IsSuccess();
                var writeErr = BackendErrorMapper.Map(insertBro);
                Debug.Log($"[CloudSaveSync] Overwrite(Insert): slot={slotIndex}, size={bytes.Length}B, ok={ok}");

                PublishUploadResult(slotIndex, ok);
                if (ok) BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                else BackendEventDispatcher.PublishFailed(_eventBus, ACTION_OVERWRITE, writeErr, insertBro.GetMessage());

                DispatchCallback(onComplete, ok, writeErr, insertBro.GetMessage());
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveSync] Overwrite Insert 예외: slot={slotIndex}, msg={e.Message}");
                PublishUploadResult(slotIndex, false);
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_OVERWRITE, BackendError.NetworkError, e.Message);
                DispatchCallback(onComplete, false, BackendError.NetworkError, e.Message);
            }
        }

        /// <summary>
        /// 뒤끝 SDK `GameData.DeleteV2(table, inDate, ownerInDate, Action<BRO>)` 콜백 버전을
        /// Task&lt;bool&gt; 로 래핑. SDK 호출 자체가 예외를 던지면 false 로 완료한다.
        /// </summary>
        private static Task<bool> DeleteRowAsync(string inDate, string ownerInDate)
        {
            var tcs = new TaskCompletionSource<bool>();
            try
            {
                Backend.GameData.DeleteV2(TABLE_CLOUD_SAVE, inDate, ownerInDate, bro =>
                {
                    bool ok = bro != null && bro.IsSuccess();
                    if (!ok && bro != null)
                        Debug.LogWarning($"[CloudSaveSync] DeleteV2 실패: inDate={inDate}, code={bro.GetStatusCode()}");
                    tcs.TrySetResult(ok);
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveSync] DeleteV2 호출 예외: inDate={inDate}, msg={e.Message}");
                tcs.TrySetResult(false);
            }
            return tcs.Task;
        }

        private static List<string> ExtractAllInDates(BackendReturnObject bro)
        {
            var result = new List<string>();
            try
            {
                JsonData json = bro.GetReturnValuetoJSON();
                if (json == null || !json.ContainsKey("rows")) return result;
                JsonData rows = json["rows"];
                if (rows == null || !rows.IsArray) return result;
                for (int i = 0; i < rows.Count; i++)
                {
                    string inDate = ExtractCellString(rows[i], "inDate");
                    if (!string.IsNullOrEmpty(inDate)) result.Add(inDate);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveSync] inDate 수집 실패: {e.Message}");
            }
            return result;
        }

        private static void DispatchCallback(
            Action<bool, BackendError, string> onComplete, bool ok, BackendError err, string msg)
        {
            if (onComplete == null) return;
            if (BackendMainThreadDispatcher.Instance != null)
                BackendMainThreadDispatcher.Instance.Enqueue(() => onComplete.Invoke(ok, err, msg));
            else
                onComplete.Invoke(ok, err, msg);
        }

        public void GetRemoteTimestamp(int slotIndex, Action<bool, DateTime, BackendError> callback)
        {
            try
            {
                var where = new Where();
                where.Equal(COLUMN_SLOT, slotIndex);
                var bro = Backend.GameData.GetMyData(TABLE_CLOUD_SAVE, where);
                if (!bro.IsSuccess())
                {
                    var err = BackendErrorMapper.Map(bro);
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_REMOTE_TIME, err, bro.GetMessage());
                    callback?.Invoke(false, DateTime.MinValue, err);
                    return;
                }

                BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                var selected = SelectSingleRow(bro, slotIndex);
                if (selected == null)
                {
                    callback?.Invoke(false, DateTime.MinValue, BackendError.NotFound);
                    return;
                }

                string iso = ExtractCellString(selected, COLUMN_SAVED_AT);
                if (string.IsNullOrEmpty(iso) || !DateTime.TryParse(iso, out DateTime remote))
                {
                    callback?.Invoke(false, DateTime.MinValue, BackendError.NotFound);
                    return;
                }

                callback?.Invoke(true, remote, BackendError.None);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveSync] 타임스탬프 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_REMOTE_TIME, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, DateTime.MinValue, BackendError.NetworkError);
            }
        }

        private void PublishUploadResult(int slotIndex, bool success)
        {
            _eventBus?.Publish(new BackendCloudSaveSyncedEvent
            {
                Slot = slotIndex,
                IsUpload = true,
                Success = success,
            });
        }

        private void PublishDownloadResult(int slotIndex, bool success)
        {
            _eventBus?.Publish(new BackendCloudSaveSyncedEvent
            {
                Slot = slotIndex,
                IsUpload = false,
                Success = success,
            });
        }

        /// <summary>
        /// Upsert 분기용 — 기존 row 의 inDate 를 추출 (없으면 빈 문자열).
        /// 신 규약에서는 slot 당 1 row 가 보장되므로 rows[0] 선택.
        /// </summary>
        private static string ExtractFirstInDate(BackendReturnObject bro)
        {
            try
            {
                JsonData json = bro.GetReturnValuetoJSON();
                if (json == null || !json.ContainsKey("rows")) return string.Empty;
                JsonData rows = json["rows"];
                if (rows == null || !rows.IsArray || rows.Count == 0) return string.Empty;
                return ExtractCellString(rows[0], "inDate");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveSync] inDate 파싱 실패: {e.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// slot 당 단일 row 규약 하에서 rows[0] 를 선택한다.
        /// 복수 row 감지 시 구버전 Insert-only 데이터 잔존으로 간주하고 LogWarning + savedAt 기준 최신 row 선택 (마이그레이션 안내).
        /// </summary>
        private static JsonData SelectSingleRow(BackendReturnObject bro, int slotIndex)
        {
            try
            {
                JsonData json = bro.GetReturnValuetoJSON();
                if (json == null || !json.ContainsKey("rows")) return null;
                JsonData rows = json["rows"];
                if (rows == null || !rows.IsArray || rows.Count == 0) return null;

                if (rows.Count == 1)
                    return rows[0];

                Debug.LogWarning($"[CloudSaveSync] 복수 row 감지({rows.Count}) — 구버전 Insert-only 데이터로 추정. slot={slotIndex}. 관리자에서 정리 필요.");
                // 최신 savedAt 선택
                JsonData newest = null;
                DateTime newestAt = DateTime.MinValue;
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    string iso = ExtractCellString(row, COLUMN_SAVED_AT);
                    if (DateTime.TryParse(iso, out DateTime at) && at >= newestAt)
                    {
                        newestAt = at;
                        newest = row;
                    }
                }
                return newest ?? rows[0];
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveSync] row 선택 실패: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 뒤끝 GameData 의 셀 포맷은 현재 raw 문자열과 {"S":"..."} 래퍼 두 가지가 혼재할 수 있다.
        /// payload/savedAt/inDate 모두 문자열 컬럼 예정이므로 문자열 추출에 한해 안전하게 양쪽을 처리한다.
        /// </summary>
        private static string ExtractCellString(JsonData row, string field)
        {
            if (row == null || !row.ContainsKey(field)) return string.Empty;
            JsonData cell = row[field];
            if (cell == null) return string.Empty;
            if (cell.IsString) return cell.ToString();
            if (cell.IsObject && cell.ContainsKey("S"))
            {
                var inner = cell["S"];
                return inner != null ? inner.ToString() : string.Empty;
            }
            return cell.ToString();
        }
    }
}
