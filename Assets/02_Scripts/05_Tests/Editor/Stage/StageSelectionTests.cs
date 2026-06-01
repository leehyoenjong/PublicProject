using NUnit.Framework;

namespace PublicFramework.Tests.Stage
{
    /// <summary>
    /// StageSelection 운반자 상태 검증.
    /// </summary>
    public class StageSelectionTests
    {
        [Test]
        public void Default_SelectedStageId_IsNull()
        {
            Assert.IsNull(new StageSelection().SelectedStageId);
        }

        [Test]
        public void Select_SetsSelectedStageId()
        {
            var sel = new StageSelection();
            sel.Select("stage_01_02");
            Assert.AreEqual("stage_01_02", sel.SelectedStageId);
        }

        [Test]
        public void Select_Overwrites_PreviousSelection()
        {
            var sel = new StageSelection();
            sel.Select("stage_01_01");
            sel.Select("stage_01_03");
            Assert.AreEqual("stage_01_03", sel.SelectedStageId);
        }

        [Test]
        public void Clear_NullsSelection()
        {
            var sel = new StageSelection();
            sel.Select("stage_01_02");
            sel.Clear();
            Assert.IsNull(sel.SelectedStageId);
        }
    }
}
