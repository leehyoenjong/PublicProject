using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 데이터베이스 (별도 제품). 유저 데이터 저장/로드, 유연 테이블 쿼리, 차트 다운로드.
    /// namespace BACKND.Database 는 구현체 내부에서만 사용.
    /// 유연 테이블 쿼리는 <see cref="FlexibleTableKey"/> 로 논리키 지정, <see cref="IFlexibleTableFilter"/> 로 필터 조건 전달.
    /// </summary>
    public interface IBackendDatabase : IService
    {
        void SaveUserData<T>(T data, Action<bool, BackendError, string> callback) where T : class;
        void LoadUserData<T>(Action<bool, T, BackendError> callback) where T : class;

        void QueryFlexibleTable(
            FlexibleTableKey key,
            IFlexibleTableFilter filter,
            Action<bool, IReadOnlyList<Dictionary<string, object>>, BackendError> onComplete);

        /// <summary>
        /// 차트 테이블 전체 payload 다운로드.
        /// NOTE: 현 Phase 에서는 <paramref name="chartName"/> 파라미터는 로그 용도로만 전달되며,
        /// 실제 다운로드는 <c>Backend.CDN.Content.Table.Get()</c> 의 **전체 테이블 JSON** 을 반환한다.
        /// `ContentTableItem` FQN 이 프레임워크 참조 범위에서 해결되지 않아(CS0246) 2단계 호출
        /// (`Backend.CDN.Content.Get(List&lt;ContentTableItem&gt;)`) 은 Phase 11+ 이관.
        /// 호출부는 반환된 payload 에서 chartName 기반 파싱을 직접 수행해야 한다.
        /// </summary>
        void DownloadChart(string chartName, Action<bool, string, BackendError> callback);
    }
}
