using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Character
{
    public class NullPositionResolverTests
    {
        [Test]
        public void TryResolve_AnyId_ReturnsZeroVectorAndTrue()
        {
            var r = new NullPositionResolver();
            Assert.IsTrue(r.TryResolve("front_left", out var pos));
            Assert.AreEqual(Vector2.zero, pos);
            Assert.IsTrue(r.TryResolve("", out var pos2));
            Assert.AreEqual(Vector2.zero, pos2);
        }

        [Test]
        public void IsValid_AlwaysTrue()
        {
            var r = new NullPositionResolver();
            Assert.IsTrue(r.IsValid(""));
            Assert.IsTrue(r.IsValid("abc"));
            Assert.IsTrue(r.IsValid(null));
        }
    }
}
