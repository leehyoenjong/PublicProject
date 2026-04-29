using NUnit.Framework;

namespace PublicFramework.Tests.Shop
{
    public class ShopRuntimeContextTests
    {
        [Test]
        public void PlayerLevel_InitializedFromCtor_AndUpdatedBySetter()
        {
            var ctx = new ShopRuntimeContext(initialPlayerLevel: 5);

            Assert.AreEqual(5, ctx.PlayerLevel);

            ctx.SetPlayerLevel(12);
            Assert.AreEqual(12, ctx.PlayerLevel);
        }

        [Test]
        public void IsQuestCleared_NullResolver_ReturnsFalse()
        {
            var ctx = new ShopRuntimeContext();

            Assert.IsFalse(ctx.IsQuestCleared(101));
        }

        [Test]
        public void IsQuestCleared_DelegatesToResolver()
        {
            var ctx = new ShopRuntimeContext(questResolver: id => id == 42);

            Assert.IsTrue(ctx.IsQuestCleared(42));
            Assert.IsFalse(ctx.IsQuestCleared(43));
        }

        [Test]
        public void SetQuestResolver_ReplacesPreviousResolver()
        {
            var ctx = new ShopRuntimeContext(questResolver: id => id == 1);
            Assert.IsTrue(ctx.IsQuestCleared(1));

            ctx.SetQuestResolver(id => id == 2);
            Assert.IsFalse(ctx.IsQuestCleared(1));
            Assert.IsTrue(ctx.IsQuestCleared(2));
        }

        [Test]
        public void SetQuestResolver_Null_ResetsToFalse()
        {
            var ctx = new ShopRuntimeContext(questResolver: id => true);
            Assert.IsTrue(ctx.IsQuestCleared(99));

            ctx.SetQuestResolver(null);
            Assert.IsFalse(ctx.IsQuestCleared(99));
        }
    }
}
