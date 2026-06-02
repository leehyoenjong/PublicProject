using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Monster
{
    /// <summary>
    /// 프리셋 확장 BT 노드(KeepDistance/Patrol/AvoidCrowding/TargetOutOfRange) 단위 검증.
    /// SelfStats 는 null 로 두어 DEFAULT_MOVE_SPEED 경로를 탄다(스탯 의존 분리).
    /// </summary>
    public class PresetBehaviorNodesTests
    {
        private class FakeMonster : IMonsterInstance
        {
            public string InstanceId { get; private set; }
            public Vector3 Position { get; private set; }
            public void SetPosition(Vector3 p) => Position = p;
            public IMonsterInfo Info => null;
            public IUnit Unit => null;
            public int Level => 1;
            public int Experience => 0;
            public IStatContainer Stats => null;
            public bool IsAlive => true;
            public FakeMonster(string id, Vector3 pos) { InstanceId = id; Position = pos; }
        }

        private class FakeTarget : IUnit
        {
            public string UnitId => "t";
            public string BaseStatMID => null;
            public StatGroupData BaseStatGroup => null;
        }

        // ---- KeepDistance ----
        [Test]
        public void KeepDistance_WhenTooClose_RetreatsAwayFromTarget()
        {
            var self = new FakeMonster("a", Vector3.zero);
            var ctx = new BehaviorContext { Self = self, Target = new FakeTarget(), TargetPosition = new Vector3(1, 0, 0), DeltaTime = 1f };
            var status = new KeepDistanceAction().Tick(ctx, "3", "1", null);
            Assert.AreEqual(BehaviorNodeStatus.Running, status);
            Assert.Less(self.Position.x, 0f); // 타겟(+x) 반대로 후퇴
        }

        [Test]
        public void KeepDistance_WhenFarEnough_SuccessAndStays()
        {
            var self = new FakeMonster("a", Vector3.zero);
            var ctx = new BehaviorContext { Self = self, Target = new FakeTarget(), TargetPosition = new Vector3(5, 0, 0), DeltaTime = 1f };
            var status = new KeepDistanceAction().Tick(ctx, "3", "1", null);
            Assert.AreEqual(BehaviorNodeStatus.Success, status);
            Assert.AreEqual(0f, self.Position.x, 1e-4f);
        }

        [Test]
        public void KeepDistance_NoTarget_Failure()
        {
            var self = new FakeMonster("a", Vector3.zero);
            var ctx = new BehaviorContext { Self = self, Target = null, DeltaTime = 1f };
            Assert.AreEqual(BehaviorNodeStatus.Failure, new KeepDistanceAction().Tick(ctx, "3", null, null));
        }

        // ---- Patrol ----
        [Test]
        public void Patrol_FirstTick_StoresOriginAndMoves()
        {
            var self = new FakeMonster("a", Vector3.zero);
            var ctx = new BehaviorContext { Self = self, DeltaTime = 0.1f, NowSeconds = 0f };
            var status = new PatrolAction().Tick(ctx, "2", "1", null);
            Assert.AreEqual(BehaviorNodeStatus.Running, status);
            Assert.AreNotEqual(Vector3.zero, self.Position); // 원주점으로 이동 시작
            Assert.AreEqual(Vector3.zero, ctx.GetBlackboard<Vector3>("patrol_origin", new Vector3(9, 9, 9)));
        }

        [Test]
        public void Patrol_NoSelf_Failure()
        {
            var ctx = new BehaviorContext { Self = null };
            Assert.AreEqual(BehaviorNodeStatus.Failure, new PatrolAction().Tick(ctx, "2", null, null));
        }

        // ---- AvoidCrowding ----
        [Test]
        public void AvoidCrowding_WhenNeighborClose_PushesAway()
        {
            var self = new FakeMonster("a", Vector3.zero);
            var other = new FakeMonster("b", new Vector3(0.2f, 0, 0));
            var ctx = new BehaviorContext { Self = self, Neighbors = new IMonsterInstance[] { self, other }, DeltaTime = 1f };
            var status = new AvoidCrowdingAction().Tick(ctx, "0.5", "0.5", null);
            Assert.AreEqual(BehaviorNodeStatus.Running, status);
            Assert.Less(self.Position.x, 0f); // 동족(+x) 반대로 밀림
        }

        [Test]
        public void AvoidCrowding_WhenNeighborFar_SuccessAndStays()
        {
            var self = new FakeMonster("a", Vector3.zero);
            var other = new FakeMonster("b", new Vector3(5, 0, 0));
            var ctx = new BehaviorContext { Self = self, Neighbors = new IMonsterInstance[] { self, other }, DeltaTime = 1f };
            var status = new AvoidCrowdingAction().Tick(ctx, "0.5", "0.5", null);
            Assert.AreEqual(BehaviorNodeStatus.Success, status);
            Assert.AreEqual(Vector3.zero, self.Position);
        }

        [Test]
        public void AvoidCrowding_OnlySelfInNeighbors_Success()
        {
            var self = new FakeMonster("a", Vector3.zero);
            var ctx = new BehaviorContext { Self = self, Neighbors = new IMonsterInstance[] { self }, DeltaTime = 1f };
            Assert.AreEqual(BehaviorNodeStatus.Success, new AvoidCrowdingAction().Tick(ctx, "0.5", "0.5", null));
        }

        [Test]
        public void AvoidCrowding_NullNeighbors_SuccessNoOp()
        {
            var self = new FakeMonster("a", Vector3.zero);
            var ctx = new BehaviorContext { Self = self, Neighbors = null, DeltaTime = 1f };
            Assert.AreEqual(BehaviorNodeStatus.Success, new AvoidCrowdingAction().Tick(ctx, "0.5", "0.5", null));
            Assert.AreEqual(Vector3.zero, self.Position);
        }

        // ---- TargetOutOfRange ----
        [Test]
        public void TargetOutOfRange_WhenFar_Success()
        {
            var self = new FakeMonster("a", Vector3.zero);
            var ctx = new BehaviorContext { Self = self, Target = new FakeTarget(), TargetPosition = new Vector3(5, 0, 0) };
            Assert.AreEqual(BehaviorNodeStatus.Success, new TargetOutOfRangeCondition().Tick(ctx, "3", null, null));
        }

        [Test]
        public void TargetOutOfRange_WhenClose_Failure()
        {
            var self = new FakeMonster("a", Vector3.zero);
            var ctx = new BehaviorContext { Self = self, Target = new FakeTarget(), TargetPosition = new Vector3(1, 0, 0) };
            Assert.AreEqual(BehaviorNodeStatus.Failure, new TargetOutOfRangeCondition().Tick(ctx, "3", null, null));
        }
    }
}
