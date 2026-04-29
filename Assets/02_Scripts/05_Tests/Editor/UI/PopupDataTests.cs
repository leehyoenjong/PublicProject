using NUnit.Framework;

namespace PublicFramework.Tests.UI
{
    /// <summary>
    /// EditMode 부분 커버 — UIManager / PopupManager 의 Push/Show 흐름은 BaseScreen / BasePopup
    /// MonoBehaviour 의존이라 PlayMode 로 위임. 본 테스트는 데이터 클래스(getter/setter) 와
    /// PopupResult enum 값 안정성만 검증.
    /// </summary>
    public class PopupDataTests
    {
        // ---------- AlertPopupData ----------

        [Test]
        public void AlertPopupData_Default_TitleAndMessageAreNull()
        {
            var data = new AlertPopupData();

            Assert.IsNull(data.Title);
            Assert.IsNull(data.Message);
        }

        [Test]
        public void AlertPopupData_SetTitle_GetReturnsSame()
        {
            var data = new AlertPopupData { Title = "안내" };

            Assert.AreEqual("안내", data.Title);
        }

        [Test]
        public void AlertPopupData_SetMessage_GetReturnsSame()
        {
            var data = new AlertPopupData { Message = "저장 완료" };

            Assert.AreEqual("저장 완료", data.Message);
        }

        // ---------- ConfirmPopupData ----------

        [Test]
        public void ConfirmPopupData_Default_TitleAndMessageAreNull()
        {
            var data = new ConfirmPopupData();

            Assert.IsNull(data.Title);
            Assert.IsNull(data.Message);
        }

        [Test]
        public void ConfirmPopupData_SetTitleAndMessage_GetReturnsSame()
        {
            var data = new ConfirmPopupData
            {
                Title = "확인",
                Message = "정말 종료하시겠습니까?"
            };

            Assert.AreEqual("확인", data.Title);
            Assert.AreEqual("정말 종료하시겠습니까?", data.Message);
        }

        // ---------- PopupResult ----------

        [Test]
        public void PopupResult_HasExpectedOrdinalValues()
        {
            Assert.AreEqual(0, (int)PopupResult.Confirm);
            Assert.AreEqual(1, (int)PopupResult.Cancel);
            Assert.AreEqual(2, (int)PopupResult.Close);
        }
    }
}
