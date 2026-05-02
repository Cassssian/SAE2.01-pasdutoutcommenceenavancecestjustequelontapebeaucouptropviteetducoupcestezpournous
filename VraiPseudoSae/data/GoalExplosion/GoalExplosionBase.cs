using System.Windows.Controls;
using VraiPseudoSae.Utils.AudioPlayer;

namespace VraiPseudoSae.data.GoalExplosion
{
    public abstract class GoalExplosionBase : UserControl
    {
        protected GoalExplosionBase(Canvas gameCanvas, JsonPakAudioService audio)
        {
            SetDependencies(gameCanvas, audio);
        }

        protected void SetDependencies(Canvas gameCanvas, JsonPakAudioService audio)
        {
            _gameCanvas = gameCanvas;
            _audio = audio;
        }

        protected Canvas? _gameCanvas;
        protected JsonPakAudioService? _audio;
        /// <summary>
        /// Explosion quand le ballon rentre dans le but gauche (but pour P2).
        /// </summary>
        public virtual void PlayLeftGoal()
        {
            return;
        }

        /// <summary>
        /// Explosion quand le ballon rentre dans le but droit (but pour P1).
        /// </summary>
        public virtual void PlayRightGoal()
        {
            return;
        }
    }
}