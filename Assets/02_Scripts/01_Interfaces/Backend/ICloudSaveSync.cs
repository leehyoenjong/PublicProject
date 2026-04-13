using System;

namespace PublicFramework
{
    /// <summary>
    /// 세이브 슬롯 클라우드 동기화.
    /// </summary>
    public interface ICloudSaveSync : IService
    {
        void UploadSlot(int slotIndex, Action<bool, BackendError, string> callback);
        void DownloadSlot(int slotIndex, Action<bool, BackendError, string> callback);
        void GetRemoteTimestamp(int slotIndex, Action<bool, DateTime, BackendError> callback);

        /// <summary>
        /// 원격 슬롯을 Delete + Insert 로 강제 덮어쓴다.
        /// 뒤끝 SDK 콜백 버전 <c>Backend.GameData.DeleteV2(table, inDate, ownerInDate, Action&lt;BRO&gt;)</c> 를
        /// 내부에서 <c>TaskCompletionSource&lt;bool&gt;</c> 로 래핑해 await 가능한 Task 로 변환한다 —
        /// **외부 <see cref="Action{T1,T2,T3}"/> 콜백 시그니처는 불변**, 최종 결과는 메인 스레드 디스패처 경유.
        ///
        /// 모든 기존 row Delete 가 성공해야 Insert 를 시도하며,
        /// **하나라도 실패 시 Insert 없이 실패 콜백을 전달**한다(부분 상태 방지).
        /// </summary>
        void OverwriteRemoteSlot(int slotIndex, Action<bool, BackendError, string> onComplete);

        CloudSaveConflictStrategy ConflictStrategy { get; set; }
    }
}
