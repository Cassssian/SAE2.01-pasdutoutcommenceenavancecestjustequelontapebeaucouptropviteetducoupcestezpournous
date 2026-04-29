using System.Reflection;
using IUTGame;

namespace VraiPseudoSae.view.RLS_Pages
{
    public class RLSCar : GameItem
    {
        private static readonly FieldInfo  FX   = typeof(GameItem).GetField("x",   BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly FieldInfo  FY   = typeof(GameItem).GetField("y",   BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly MethodInfo MPut = typeof(GameItem).GetMethod("Put", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public double X;
        public double Y;

        public double VX, VY;
        public bool   OnGround;
        public double Boost      = 100;
        public int    JumpsLeft  = 2;
        public bool   JumpKeyPrev;
        public bool   IsBoosting;        // ← vrai quand la flamme doit s'afficher

        public const double BaseWidth  = 60;
        public const double BaseHeight = 38;

        private int _facingDir = 1;
        public int FacingDir
        {
            get => _facingDir;
            set
            {
                if (_facingDir == value) return;
                _facingDir = value;
                ChangeScale(_facingDir >= 0 ? 1.0 : -1.0, 1.0);
                SetRawPosition(X, Y);
            }
        }

        public RLSCar(double x, double y, Game game, string spriteName)
            : base(x, y, game, spriteName, 11)
        {
            X = x; Y = y;
            Collidable = false;
        }

        public override string TypeName => "RLSCar";
        public override void CollideEffect(GameItem other) { }

        public void PutPosition(double x, double y)
        {
            X = x; Y = y;
            SetRawPosition(x, y);
        }

        public void ApplyFlip()
        {
            ChangeScale(_facingDir >= 0 ? 1.0 : -1.0, 1.0);
            SetRawPosition(X, Y);
        }

        public double PhysLeft   => X;
        public double PhysRight  => X + BaseWidth;
        public double PhysTop    => Y;
        public double PhysBottom => Y + BaseHeight;
        public double PhysMidX   => X + BaseWidth  / 2.0;
        public double PhysMidY   => Y + BaseHeight / 2.0;

        private void SetRawPosition(double x, double y)
        {
            FX.SetValue(this, x);
            FY.SetValue(this, y);
            MPut.Invoke(this, null);
        }
    }
}
