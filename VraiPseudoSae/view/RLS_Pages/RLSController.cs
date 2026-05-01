using System;
using System.Windows.Input;
using IUTGame;

namespace VraiPseudoSae.view.RLS_Pages
{
    public class RLSController : GameItem, IAnimable, IKeyboardInteract
    {
        private readonly RLSGame game;

        public RLSController(RLSGame game)
            : base(0, 0, game, "", 0)
        {
            this.game = game;
            Collidable = false;
        }

        public override string TypeName => "RLSController";
        public override void CollideEffect(GameItem other) { }

        public void Animate(TimeSpan interval)
        {
            if (game.GoalPaused)
            {
                game.GoalTimer--;
                if (game.GoalTimer <= 0) game.EndGoalPause();
                return;
            }

            HandleP1Input();
            if (game.VsBot) HandleBotInput(); else HandleP2Input();

            UpdateCar(game.Car1);
            UpdateCar(game.Car2);
            UpdateBall();

            CheckCarBallCollision(game.Car1);
            CheckCarBallCollision(game.Car2);
            CheckCarCarCollision();
            CheckCrossbarCollision();
            CheckGoal();

            game.Car1.ApplyFlip();
            game.Car2.ApplyFlip();

            game.OnHudRefresh?.Invoke();
        }

        public void KeyDown(Key key)
        {
            game.Keys.Add(key);
            if (key == Key.R)      { game.ResetPositions(); game.ResetScore(); }
            if (key == Key.Escape) { game.Pause(); game.OnBackToMenu?.Invoke(); }
        }

        public void KeyUp(Key key) => game.Keys.Remove(key);

        private void ProcessCarInput(RLSCar c, bool left, bool right, bool jump, bool boost)
        {
            if (left)  { c.VX -= 0.8; c.FacingDir = -1; }
            if (right) { c.VX += 0.8; c.FacingDir =  1; }

            if (jump && !c.JumpKeyPrev)
            {
                if (c.OnGround)
                {
                    c.VY = -12; c.OnGround = false; c.JumpsLeft = 1;
                    game.Audio.Play("first_jump");
                }
                else if (c.JumpsLeft > 0 && (left || right))
                {
                    c.VX += c.FacingDir * 20; c.VY = -11; c.JumpsLeft--;
                    game.Audio.Play("second_jump_movement");
                }
                else if (c.JumpsLeft > 0)
                {
                    c.VY = -11; c.JumpsLeft--;
                    game.Audio.Play("second_jump");
                }
            }
            c.JumpKeyPrev = jump;

            bool usingBoost = boost && c.Boost > 0;

            if (jump && usingBoost && !c.OnGround)
            {
                c.VY -= 0.9; c.Boost -= 1.5;
            }
            else if (usingBoost)
            {
                double dir = Math.Abs(c.VX) < 0.1 ? c.FacingDir : (c.VX >= 0 ? 1 : -1);
                c.VX += dir * 1.2; c.Boost -= 1.5;
            }

            c.Boost = Math.Min(100, Math.Max(0, c.Boost) + 0.2);
            c.IsBoosting = usingBoost;
        }

        private void HandleP1Input() =>
            ProcessCarInput(game.Car1,
                left:  game.Keys.Contains(Key.Q),
                right: game.Keys.Contains(Key.D),
                jump:  game.Keys.Contains(Key.Z),
                boost: game.Keys.Contains(Key.Space));

        private void HandleP2Input() =>
            ProcessCarInput(game.Car2,
                left:  game.Keys.Contains(Key.K),
                right: game.Keys.Contains(Key.M),
                jump:  game.Keys.Contains(Key.O),
                boost: game.Keys.Contains(Key.Enter));

        private void HandleBotInput()
        {
            double botCX  = game.Car2.PhysMidX;
            double botCY  = game.Car2.PhysMidY;
            double ballCX = game.Ball.Left + RLSBall.Size / 2.0;
            double ballCY = game.Ball.Top  + RLSBall.Size / 2.0;

            double predictedBallX = Math.Max(20, Math.Min(RLSGame.FieldWidth - 20, ballCX + game.Ball.VX * 15));
            double predictedBallY = Math.Max(0,  Math.Min(RLSGame.GroundY, ballCY + game.Ball.VY * 15 + 0.5 * RLSGame.Gravity * 225));

            double targetX;
            if (Math.Abs(game.Ball.VX) < 0.1 && Math.Abs(ballCX - 500) < 10)
                targetX = ballCX + 35;
            else if (ballCX < 400)
                targetX = Math.Min(850, ballCX + 100);
            else if (ballCX > 700)
                targetX = ballCX + 20;
            else
                targetX = botCX < ballCX - 10 ? ballCX + 80 : predictedBallX + 30;

            if (ballCX > botCX + 100 && targetX < botCX) targetX = botCX + 10;

            double diff  = targetX - botCX;
            bool   bleft = diff < -15, bright = diff > 15, jump = false, boost = false;

            double dist = Math.Sqrt(Math.Pow(ballCX - botCX, 2) + Math.Pow(ballCY - botCY, 2));
            if (game.Car2.OnGround)      { if (dist < 120 && ballCY < game.Car2.PhysTop - 20) jump = true; }
            else if (game.Car2.JumpsLeft > 0) { if (ballCY < game.Car2.PhysTop - 50 && dist < 100) jump = true; }
            if (Math.Abs(diff) > 100 && game.Car2.Boost > 10) boost = true;
            if (!game.Car2.OnGround && ballCY < game.Car2.PhysTop && dist < 150 && game.Car2.Boost > 0) boost = true;

            ProcessCarInput(game.Car2, bleft, bright, jump, boost);
        }

        private void UpdateCar(RLSCar c)
        {
            c.VY += RLSGame.Gravity;
            double nextX = c.X + c.VX;
            double nextY = c.Y + c.VY;
            c.VX *= RLSGame.Friction;

            if (nextY + RLSCar.BaseHeight >= RLSGame.GroundY)
            { nextY = RLSGame.GroundY - RLSCar.BaseHeight; c.VY = 0; if (!c.OnGround) c.JumpsLeft = 2; c.OnGround = true; }
            else c.OnGround = false;

            if (nextX < 0)                                     { nextX = 0;                                     c.VX = 0; }
            if (nextX + RLSCar.BaseWidth > RLSGame.FieldWidth) { nextX = RLSGame.FieldWidth - RLSCar.BaseWidth; c.VX = 0; }
            if (nextY < 0) { nextY = 0; c.VY = 0.1; }

            c.VX = Math.Max(-15, Math.Min(15, c.VX));
            c.VY = Math.Max(-18, Math.Min(20, c.VY));
            c.PutPosition(nextX, nextY);
        }

        //TODO: Rajouter les sons de but/arrêt
        // Attention pour l'arrêt, vérifier si la balle est tirée par le joueur adverse, et sauvée par le joueur où y'a
        // la cage, et vérifier aussi avant si la balle passe de l'autre moitié de terrain avant de rejouer un son sous
        // peine de spam de son
        private void UpdateBall()
        {
            var b = game.Ball;
            b.VY += RLSGame.Gravity * 0.5;
            double nextX = b.Left + b.VX, nextY = b.Top + b.VY;
            b.VX *= RLSGame.BallFriction;

            if (nextY + RLSBall.Size >= RLSGame.GroundY)
            { nextY = RLSGame.GroundY - RLSBall.Size; b.VY = -b.VY * RLSGame.BallBounce; b.VX *= 0.95; if (Math.Abs(b.VY) < 1) b.VY = 0; }

            if (nextX < 0 && (nextY + RLSBall.Size < 370 || nextY > 520))
            { nextX = 0; b.VX = -b.VX * RLSGame.BallBounce; }
            if (nextX + RLSBall.Size > RLSGame.FieldWidth && (nextY + RLSBall.Size < 370 || nextY > 520))
            { nextX = RLSGame.FieldWidth - RLSBall.Size; b.VX = -b.VX * RLSGame.BallBounce; }
            if (nextY < 0) { nextY = 0; b.VY = -b.VY * RLSGame.BallBounce; }

            b.PutPosition(nextX, nextY);
        }

        private void CheckCrossbarCollision()
        {
            CheckBallRect(0,   365, 26, 8);
            CheckBallRect(974, 365, 26, 8);
        }

        private void CheckBallRect(double rx, double ry, double rw, double rh)
        {
            var b = game.Ball; double r = RLSBall.Size / 2.0;
            double bcx = b.Left + r, bcy = b.Top + r;
            double cx = Math.Max(rx, Math.Min(bcx, rx + rw)), cy = Math.Max(ry, Math.Min(bcy, ry + rh));
            double dx = bcx - cx, dy = bcy - cy, dist = Math.Sqrt(dx*dx + dy*dy);
            if (dist < r && dist > 0)
            {
                double nx = dx/dist, ny = dy/dist;
                b.PutPosition(b.Left + nx*(r-dist), b.Top + ny*(r-dist));
                double dot = b.VX*nx + b.VY*ny;
                b.VX = (b.VX - 2*dot*nx) * RLSGame.BallBounce;
                b.VY = (b.VY - 2*dot*ny) * RLSGame.BallBounce;
            }
        }

        private void CheckCarBallCollision(RLSCar c)
        {
            var b = game.Ball; double r = RLSBall.Size / 2.0;
            double bcx = b.Left + r, bcy = b.Top + r;
            double cx = Math.Max(c.PhysLeft, Math.Min(bcx, c.PhysRight));
            double cy = Math.Max(c.PhysTop,  Math.Min(bcy, c.PhysBottom));
            double dx = bcx - cx, dy = bcy - cy, dist = Math.Sqrt(dx*dx + dy*dy);
            if (dist < r)
            {
                if (dist == 0) { dx = bcx-c.PhysMidX; dy = bcy-c.PhysMidY; dist = Math.Sqrt(dx*dx+dy*dy); if (dist==0){dx=0;dy=-1;dist=1;} }
                double nx = dx/dist, ny = dy/dist;
                double newBX = b.Left + nx*(r-dist), newBY = b.Top + ny*(r-dist);
                if (newBY + RLSBall.Size > RLSGame.GroundY) { newBY = RLSGame.GroundY - RLSBall.Size; if (Math.Abs(b.VX)<2) b.VX += nx>=0?5:-5; }
                b.PutPosition(newBX, newBY);
                double impact = 8 + Math.Sqrt(c.VX*c.VX + c.VY*c.VY)*0.6;
                b.VX = nx*impact + c.VX*0.5; b.VY = ny*impact + c.VY*0.5;
                if (ny > 0.5) b.VY += 3;
            }
        }

        private void CheckCarCarCollision()
        {
            var p1 = game.Car1; var p2 = game.Car2;
            if (p1.PhysLeft < p2.PhysRight && p1.PhysRight > p2.PhysLeft && p1.PhysTop < p2.PhysBottom && p1.PhysBottom > p2.PhysTop)
            {
                double tmpVX = p1.VX; p1.VX = p2.VX*0.8; p2.VX = tmpVX*0.8;
                if (p1.PhysLeft < p2.PhysLeft) { p1.PutPosition(p1.X-2,p1.Y); p2.PutPosition(p2.X+2,p2.Y); }
                else                           { p1.PutPosition(p1.X+2,p1.Y); p2.PutPosition(p2.X-2,p2.Y); }
            }
        }

        private void CheckGoal()
        {
            var b = game.Ball;
            if (b.Left <= 20 && b.Top > 373 && b.Top < 520)       { game.Score2++; game.ShowGoal("GOAL P2!"); }
            else if (b.Right >= 980 && b.Top > 373 && b.Top < 520) { game.Score1++; game.ShowGoal("GOAL P1!"); }
        }
    }
}
