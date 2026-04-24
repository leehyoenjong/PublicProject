using System;

namespace PublicFramework
{
    /// <summary>
    /// 서버 시간 공급자. 상점 갱신/기간제 아이템 만료 등 시간 기준이 필요한 시스템이 공통으로 사용한다.
    /// 프로젝트는 뒤끝 SDK 또는 자체 서버 시간 API 를 래핑해 주입한다.
    /// 주입 안 하면 기본 DateTime.UtcNow 로 동작하도록 구현체에서 처리.
    /// </summary>
    public interface ITimeProvider
    {
        DateTime NowUtc { get; }
        long NowUnixSeconds { get; }
    }
}
