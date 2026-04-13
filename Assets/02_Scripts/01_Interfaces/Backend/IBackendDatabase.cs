using System;
using System.Collections.Generic;
using BACKND.Database;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 데이터베이스 (별도 제품). 유저 데이터 저장/로드, 유연 테이블 쿼리(제네릭), 차트 다운로드.
    /// 유연 테이블 쿼리는 <see cref="BaseModel"/> 상속 POCO 타입 <c>T</c> 를 통해 수행한다.
    /// `BACKND.Database` UPM 확정으로 Phase 11 부터 해당 네임스페이스 참조를 허용한다.
    /// </summary>
    public interface IBackendDatabase : IService
    {
        void SaveUserData<T>(T data, Action<bool, BackendError, string> callback) where T : class;
        void LoadUserData<T>(Action<bool, T, BackendError> callback) where T : class;

        /// <summary>
        /// BACKND.Database 유연 테이블 쿼리 (제네릭 POCO 기반).
        /// 필터 조건은 <see cref="IFlexibleTableFilter"/> 로 누적하여 전달되고, 구현체가 Expression tree 로 변환해 `QueryBuilder.Where` 에 적용한다.
        /// Expression 구성 불가한 조건은 스킵되며, 그 경우 더 많은 row 를 반환할 수 있다(회복 가능).
        /// </summary>
        void QueryFlexibleTable<T>(
            IFlexibleTableFilter filter,
            Action<bool, IReadOnlyList<T>, BackendError> onComplete)
            where T : BaseModel, new();

        /// <summary>
        /// 차트 테이블 전체 payload 다운로드.
        /// NOTE: 현 Phase 에서는 <paramref name="chartName"/> 파라미터는 로그 용도로만 전달되며,
        /// 실제 다운로드는 <c>Backend.CDN.Content.Table.Get()</c> 의 **전체 테이블 JSON** 을 반환한다.
        /// `ContentTableItem` FQN 미해결로 2단계 호출은 Phase 12+ 이관.
        /// </summary>
        void DownloadChart(string chartName, Action<bool, string, BackendError> callback);
    }
}
