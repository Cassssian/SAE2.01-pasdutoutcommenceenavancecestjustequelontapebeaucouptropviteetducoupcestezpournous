using IUTGame;

namespace VraiPseudoSae.view.RLS_Pages
{
    public class RLSBall : GameItem
    {
        public double VX, VY;
        public const double Size = 30;

        public RLSBall(double x, double y, Game game)
            : base(x, y, game, "rls_ball.png", 12)
        {
            Collidable = false;
        }

        public override string TypeName => "RLSBall";

        public void PutPosition(double x, double y)
        {
            PutXY(x, y);
        }

        public override void CollideEffect(GameItem other) { }
    }
}