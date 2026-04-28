using System.Windows.Controls;
using VraiPseudoSae.data.AudioPlayer;

namespace VraiPseudoSae.data.GoalExplosion
{
    public class GoalExplosionBase : UserControl
    {
        public GoalExplosionBase() { }
        public GoalExplosionBase(Canvas gameCanvas, JsonPakAudioService audio)
        {
            SetDependencies(gameCanvas, audio);
        }

        public void SetDependencies(Canvas gameCanvas, JsonPakAudioService audio)
        {
            _gameCanvas = gameCanvas;
            _audio = audio;
        }

        protected Canvas _gameCanvas;
        protected JsonPakAudioService _audio;
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