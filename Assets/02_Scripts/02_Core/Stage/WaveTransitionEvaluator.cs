using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// Wave 전환 조건 평가기. 4종 WaveTransitionCondition 을 순수 함수로 판정한다.
    /// MonoBehaviour 분리 — StageBattleHost 가 매 Update 호출, EditMode 테스트로 직접 검증.
    /// 살아있는 몬스터 카운트는 monsterMID → count dict 로 외부에서 추적해 전달.
    /// </summary>
    public static class WaveTransitionEvaluator
    {
        /// <summary>
        /// 현재 wave 가 다음 단계로 넘어가야 하는지 판정.
        /// </summary>
        /// <param name="aliveMonsterCountByMID">살아있는 몬스터의 MID 별 카운트. 죽으면 호출자가 dict 에서 감산/제거.</param>
        /// <param name="elapsedSecondsInWave">현재 wave 가 시작된 후 경과 초.</param>
        public static bool ShouldTransition(
            WaveTransitionCondition condition,
            string transitionTargetMonsterMID,
            float transitionTimer,
            IReadOnlyDictionary<string, int> aliveMonsterCountByMID,
            float elapsedSecondsInWave)
        {
            switch (condition)
            {
                case WaveTransitionCondition.AllKill:
                    return TotalAlive(aliveMonsterCountByMID) <= 0;

                case WaveTransitionCondition.BossKill:
                case WaveTransitionCondition.SpecificKill:
                    if (string.IsNullOrEmpty(transitionTargetMonsterMID)) return false;
                    if (aliveMonsterCountByMID == null) return true;
                    return !aliveMonsterCountByMID.TryGetValue(transitionTargetMonsterMID, out int count) || count <= 0;

                case WaveTransitionCondition.Timer:
                    return transitionTimer > 0f && elapsedSecondsInWave >= transitionTimer;

                default:
                    return false;
            }
        }

        private static int TotalAlive(IReadOnlyDictionary<string, int> dict)
        {
            if (dict == null) return 0;
            int sum = 0;
            foreach (KeyValuePair<string, int> kv in dict)
            {
                if (kv.Value > 0) sum += kv.Value;
            }
            return sum;
        }
    }
}
