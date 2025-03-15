using MoreMountains.CorgiEngine;
using MoreMountains.Tools;

namespace PathBerserker2d.Corgi
{
    /// <summary>
    /// Makes the character flip to face the target. Not PathBerserker2d specific.
    /// </summary>
    public class AIActionLookAtTarget : AIAction
    {
        Character character;

        public override void Initialization()
        {
            base.Initialization();
            character = GetComponentInParent<Character>();
        }

        public override void PerformAction()
        {
            if (_brain.Target == null)
                return;

            if ((_brain.Target.position.x - character.transform.position.x > 0) !=
                character.IsFacingRight)
                character.Flip();
        }
    }
}
