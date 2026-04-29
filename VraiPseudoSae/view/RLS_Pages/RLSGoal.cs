using IUTGame;

namespace VraiPseudoSae.view.RLS_Pages
{
    public class RLSGoal : GameItem
    {
        public RLSGoal(double x, double y, Game game, bool flipForRight)
            : base(x, y, game, "rls_goal.png", 2)
        {
            Collidable = false;

            if (flipForRight)
                ChangeScale(-1, 1);
        }

        public override string TypeName => "RLSGoal";

        public override void CollideEffect(GameItem other) { }
    }
}