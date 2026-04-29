using IUTGame;

namespace VraiPseudoSae.view.RLS_Pages
{
    public class RLSCar : GameItem
    {
        public double VX, VY;
        public bool OnGround;
        public double Boost = 100;
        public int FacingDir = 1;
        public int JumpsLeft = 2;
        public bool JumpKeyPrev;

        public const double BaseWidth = 60;
        public const double BaseHeight = 38;

        public RLSCar(double x, double y, Game game, string spriteName)
            : base(x, y, game, spriteName, 11)
        {
            Collidable = false;
        }

        public override string TypeName => "RLSCar";

        public void PutPosition(double x, double y)
        {
            PutXY(x, y);
        }

        public void ApplyFlip()
        {
            double sx = FacingDir >= 0 ? 1 : -1;
            ChangeScale(sx, 1);
        }

        public override void CollideEffect(GameItem other) { }
    }
}