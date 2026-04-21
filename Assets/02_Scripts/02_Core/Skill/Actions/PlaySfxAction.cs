using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// PlaySfx — param1=sfxId(ISoundManager 에 등록된 SFX ID).
    /// </summary>
    public class PlaySfxAction : ISkillAction
    {
        public SkillActionType Type => SkillActionType.PlaySfx;

        public void Execute(SkillContext context, SkillActionEntry entry)
        {
            if (context == null || entry == null) return;
            if (context.SoundManager == null) return;

            string sfxId = entry.Param1;
            if (string.IsNullOrEmpty(sfxId)) return;

            context.SoundManager.PlaySFX(sfxId);
        }
    }
}
