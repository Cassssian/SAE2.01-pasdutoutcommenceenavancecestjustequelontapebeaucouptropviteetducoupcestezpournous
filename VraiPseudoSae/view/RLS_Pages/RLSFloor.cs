using IUTGame;

namespace VraiPseudoSae.view.RLS_Pages
{
    public class RLSFloor : GameItem
    {
        public RLSFloor(double x, double y, Game game, bool flipForRight)
            : base(x, y, game, "rls_floor.png", 1)
        {
            Collidable = false;

            if (flipForRight)
                ChangeScale(-1, 1);
        }

        public override string TypeName => "RLSFloor";

        public override void CollideEffect(GameItem other) { }
    }
}