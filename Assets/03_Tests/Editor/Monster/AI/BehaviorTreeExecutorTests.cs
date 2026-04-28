using NUnit.Framework;

namespace PublicFramework.Tests.Monster.AI
{
    public class BehaviorTreeExecutorTests
    {
        private BehaviorActionRegistry _registry;
        private BehaviorTreeExecutor _executor;
        private BehaviorContext _ctx;

        [SetUp]
        public void SetUp()
        {
            _registry = new BehaviorActionRegistry();
            _registry.Register(new ConstAction("Success", BehaviorNodeStatus.Success));
            _registry.Register(new ConstAction("Failure", BehaviorNodeStatus.Failure));
            _registry.Register(new ConstAction("Running", BehaviorNodeStatus.Running));
            _executor = new BehaviorTreeExecutor(_registry);
            _ctx = new BehaviorContext { NowSeconds = 0f };
        }

        // --- Action / Composite ---

        [Test]
        public void Sequence_AllSuccess_ReturnsSuccess()
        {
            BehaviorTreePreset preset = TestHelpers.MakeBehaviorTreePreset("p", 0, new[]
            {
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Sequence, new[] { 1, 2 }),
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "Success"),
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "Success"),
            });

            Assert.AreEqual(BehaviorNodeStatus.Success, _executor.Tick(preset, _ctx));
        }

        [Test]
        public void Sequence_FailureInMiddle_ReturnsFailure()
        {
            BehaviorTreePreset preset = TestHelpers.MakeBehaviorTreePreset("p", 0, new[]
            {
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Sequence, new[] { 1, 2, 3 }),
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "Success"),
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "Failure"),
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "Success"),
            });

            Assert.AreEqual(BehaviorNodeStatus.Failure, _executor.Tick(preset, _ctx));
        }

        [Test]
        public void Selector_FirstSuccess_ReturnsSuccess()
        {
            BehaviorTreePreset preset = TestHelpers.MakeBehaviorTreePreset("p", 0, new[]
            {
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Selector, new[] { 1, 2 }),
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "Failure"),
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "Success"),
            });

            Assert.AreEqual(BehaviorNodeStatus.Success, _executor.Tick(preset, _ctx));
        }

        [Test]
        public void Selector_AllFailure_ReturnsFailure()
        {
            BehaviorTreePreset preset = TestHelpers.MakeBehaviorTreePreset("p", 0, new[]
            {
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Selector, new[] { 1, 2 }),
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "Failure"),
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "Failure"),
            });

            Assert.AreEqual(BehaviorNodeStatus.Failure, _executor.Tick(preset, _ctx));
        }

        // --- Decorator ---

        [Test]
        public void Inverter_Success_BecomesFailure()
        {
            BehaviorTreePreset preset = TestHelpers.MakeBehaviorTreePreset("p", 0, new[]
            {
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Inverter, new[] { 1 }),
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "Success"),
            });

            Assert.AreEqual(BehaviorNodeStatus.Failure, _executor.Tick(preset, _ctx));
        }

        [Test]
        public void Inverter_Failure_BecomesSuccess()
        {
            BehaviorTreePreset preset = TestHelpers.MakeBehaviorTreePreset("p", 0, new[]
            {
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Inverter, new[] { 1 }),
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "Failure"),
            });

            Assert.AreEqual(BehaviorNodeStatus.Success, _executor.Tick(preset, _ctx));
        }

        [Test]
        public void Cooldown_FirstSuccess_ThenBlocked_ThenAvailable()
        {
            BehaviorTreePreset preset = TestHelpers.MakeBehaviorTreePreset("p", 0, new[]
            {
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Cooldown, new[] { 1 }, param1: "5"),
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "Success"),
            });

            _ctx.NowSeconds = 0f;
            Assert.AreEqual(BehaviorNodeStatus.Success, _executor.Tick(preset, _ctx));

            _ctx.NowSeconds = 3f;
            Assert.AreEqual(BehaviorNodeStatus.Failure, _executor.Tick(preset, _ctx));

            _ctx.NowSeconds = 6f;
            Assert.AreEqual(BehaviorNodeStatus.Success, _executor.Tick(preset, _ctx));
        }

        [Test]
        public void Repeat_RunsUntilMaxCount()
        {
            BehaviorTreePreset preset = TestHelpers.MakeBehaviorTreePreset("p", 0, new[]
            {
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Repeat, new[] { 1 }, param1: "3"),
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "Success"),
            });

            Assert.AreEqual(BehaviorNodeStatus.Running, _executor.Tick(preset, _ctx));
            Assert.AreEqual(BehaviorNodeStatus.Running, _executor.Tick(preset, _ctx));
            Assert.AreEqual(BehaviorNodeStatus.Success, _executor.Tick(preset, _ctx));
        }

        // --- Action lookup ---

        [Test]
        public void Action_UnknownKey_ReturnsFailure()
        {
            BehaviorTreePreset preset = TestHelpers.MakeBehaviorTreePreset("p", 0, new[]
            {
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "DoesNotExist"),
            });

            Assert.AreEqual(BehaviorNodeStatus.Failure, _executor.Tick(preset, _ctx));
        }

        [Test]
        public void Tick_NullPreset_ReturnsFailure()
        {
            Assert.AreEqual(BehaviorNodeStatus.Failure, _executor.Tick(null, _ctx));
        }

        [Test]
        public void Tick_NullContext_ReturnsFailure()
        {
            BehaviorTreePreset preset = TestHelpers.MakeBehaviorTreePreset("p", 0, new[]
            {
                TestHelpers.MakeBehaviorNode(BehaviorNodeType.Action, actionKey: "Success"),
            });

            Assert.AreEqual(BehaviorNodeStatus.Failure, _executor.Tick(preset, null));
        }

        // 테스트용 상수 액션
        private class ConstAction : IBehaviorAction
        {
            private readonly string _key;
            private readonly BehaviorNodeStatus _result;
            public ConstAction(string key, BehaviorNodeStatus result) { _key = key; _result = result; }
            public string ActionKey => _key;
            public BehaviorNodeStatus Tick(BehaviorContext c, string p1, string p2, string p3) => _result;
        }
    }
}
