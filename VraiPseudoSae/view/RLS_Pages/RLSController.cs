using System;
using System.Windows.Input;
using IUTGame;

namespace VraiPseudoSae.view.RLS_Pages
{
    public class RLSController : GameItem, IAnimable, IKeyboardInteract
    {
        private readonly RLSGame game;
        private readonly Random rng = new();

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
                if (game.GoalTimer <= 0)
                    game.EndGoalPause();
                return;
            }

            HandleP1Input();

            if (game.VsBot)
                HandleBotInput();
            else
                HandleP2Input();

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

            if (key == Key.R)
            {
                game.ResetPositions();
                game.ResetScore();
            }

            if (key == Key.Escape)
            {
                game.Pause();
                game.OnBackToMenu?.Invoke();
            }
        }

        public void KeyUp(Key key)
        {
            game.Keys.Remove(key);
        }

        private void ProcessCarInput(RLSCar c, bool left, bool right, bool jump, bool boost, bool isP1)
        {
            if (left)
            {
                c.VX -= 0.8;
                c.FacingDir = -1;
            }

            if (right)
            {
                c.VX += 0.8;
                c.FacingDir = 1;
            }

            if (jump && !c.JumpKeyPrev)
            {
                if (c.OnGround)
                {
                    c.VY = -12;
                    c.OnGround = false;
                    c.JumpsLeft = 1;
                    game.Audio.Play("first_jump");
                }
                else if (c.JumpsLeft > 0 && (left || right))
                {
                    c.VX += c.FacingDir * 20;
                    c.VY = -11;
                    c.JumpsLeft--;
                    game.Audio.Play("second_jump_movement");
                }
                else if (c.JumpsLeft > 0)
                {
                    c.VY = -11;
                    c.JumpsLeft--;
                    game.Audio.Play("second_jump");
                }
            }

            c.JumpKeyPrev = jump;

            if (jump && boost && c.Boost > 0 && !c.OnGround)
            {
                c.VY -= 0.9;
                c.Boost -= 1.5;
            }
            else if (boost && c.Boost > 0)
            {
                double dir = c.VX >= 0 ? 1 : -1;
                if (Math.Abs(c.VX) < 0.1)
                    dir = c.FacingDir;

                c.VX += dir * 1.2;
                c.Boost -= 1.5;
            }

            if (c.Boost < 0)
                c.Boost = 0;

            c.Boost = Math.Min(100, c.Boost + 0.2);
        }

        private void HandleP1Input()
        {
            ProcessCarInput(
                game.Car1,
                left: game.Keys.Contains(Key.Q),
                right: game.Keys.Contains(Key.D),
                jump: game.Keys.Contains(Key.Z),
                boost: game.Keys.Contains(Key.Space),
                isP1: true);
        }

        private void HandleP2Input()
        {
            ProcessCarInput(
                game.Car2,
                left: game.Keys.Contains(Key.K),
                right: game.Keys.Contains(Key.M),
                jump: game.Keys.Contains(Key.O),
                boost: game.Keys.Contains(Key.Enter),
                isP1: false);
        }

        private void HandleBotInput()
        {
            double botCX = game.Car2.MiddleX;
            double botCY = game.Car2.MiddleY;
            double ballCX = game.Ball.MiddleX;
            double ballCY = game.Ball.MiddleY;

            double predictedBallX = ballCX + game.Ball.VX * 15;
            double predictedBallY = ballCY + game.Ball.VY * 15 + 0.5 * RLSGame.Gravity * 15 * 15;

            predictedBallX = Math.Max(20, Math.Min(RLSGame.FieldWidth - 20, predictedBallX));
            predictedBallY = Math.Max(0, Math.Min(RLSGame.GroundY, predictedBallY));

            double targetX;

            if (Math.Abs(game.Ball.VX) < 0.1 && Math.Abs(ballCX - 500) < 10)
                targetX = ballCX + 35;
            else if (ballCX < 400)
                targetX = Math.Min(850, ballCX + 100);
            else if (ballCX > 700)
                targetX = ballCX + 20;
            else
                targetX = botCX < ballCX - 10 ? ballCX + 80 : predictedBallX + 30;

            if (ballCX > botCX + 100 && targetX < botCX)
                targetX = botCX + 10;

            double diff = targetX - botCX;
            bool left = diff < -15;
            bool right = diff > 15;
            bool jump = false;
            bool boost = false;

            double distToBall = Math.Sqrt(Math.Pow(ballCX - botCX, 2) + Math.Pow(ballCY - botCY, 2));

            if (game.Car2.OnGround)
            {
                if (distToBall < 120 && ballCY < game.Car2.Top - 20)
                    jump = true;
            }
            else if (game.Car2.JumpsLeft > 0)
            {
                if (ballCY < game.Car2.Top - 50 && distToBall < 100)
                    jump = true;
            }

            if (Math.Abs(diff) > 100 && game.Car2.Boost > 10)
                boost = true;

            if (!game.Car2.OnGround && ballCY < game.Car2.Top && distToBall < 150 && game.Car2.Boost > 0)
                boost = true;

            ProcessCarInput(game.Car2, left, right, jump, boost, false);
        }

        private void UpdateCar(RLSCar c)
        {
            c.VY += RLSGame.Gravity;

            double nextX = c.Left + c.VX;
            double nextY = c.Top + c.VY;

            c.PutPosition(nextX, nextY);

            c.VX *= RLSGame.Friction;

            if (c.Bottom >= RLSGame.GroundY)
            {
                c.PutPosition(c.Left, RLSGame.GroundY - c.Height);
                c.VY = 0;

                if (!c.OnGround)
                    c.JumpsLeft = 2;

                c.OnGround = true;
            }
            else
            {
                c.OnGround = false;
            }

            if (c.Left < 0)
            {
                c.PutPosition(0, c.Top);
                c.VX = 0;
            }

            if (c.Right > RLSGame.FieldWidth)
            {
                c.PutPosition(RLSGame.FieldWidth - c.Width, c.Top);
                c.VX = 0;
            }

            if (c.Top < 0)
            {
                c.PutPosition(c.Left, 0);
                c.VY = 0.1;
            }

            c.VX = Math.Max(-15, Math.Min(15, c.VX));
            c.VY = Math.Max(-18, Math.Min(20, c.VY));
        }

        private void UpdateBall()
        {
            var b = game.Ball;

            b.VY += RLSGame.Gravity * 0.5;

            b.PutPosition(b.Left + b.VX, b.Top + b.VY);

            b.VX *= RLSGame.BallFriction;

            if (b.Bottom >= RLSGame.GroundY)
            {
                b.PutPosition(b.Left, RLSGame.GroundY - b.Height);
                b.VY = -b.VY * RLSGame.BallBounce;
                b.VX *= 0.95;

                if (Math.Abs(b.VY) < 1)
                    b.VY = 0;
            }

            if (b.Left < 0)
            {
                if (b.Bottom < 370 || b.Top > 520)
                {
                    b.PutPosition(0, b.Top);
                    b.VX = -b.VX * RLSGame.BallBounce;
                }
            }

            if (b.Right > RLSGame.FieldWidth)
            {
                if (b.Bottom < 370 || b.Top > 520)
                {
                    b.PutPosition(RLSGame.FieldWidth - b.Width, b.Top);
                    b.VX = -b.VX * RLSGame.BallBounce;
                }
            }

            if (b.Top < 0)
            {
                b.PutPosition(b.Left, 0);
                b.VY = -b.VY * RLSGame.BallBounce;
            }
        }

        private void CheckCrossbarCollision()
        {
            CheckBallRectCollision(0, 365, 26, 8);
            CheckBallRectCollision(974, 365, 26, 8);
        }

        private void CheckBallRectCollision(double rx, double ry, double rw, double rh)
        {
            var b = game.Ball;

            double bcx = b.MiddleX;
            double bcy = b.MiddleY;
            double closestX = Math.Max(rx, Math.Min(bcx, rx + rw));
            double closestY = Math.Max(ry, Math.Min(bcy, ry + rh));
            double dx = bcx - closestX;
            double dy = bcy - closestY;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist < b.Width / 2 && dist > 0)
            {
                double nx = dx / dist;
                double ny = dy / dist;
                double overlap = b.Width / 2 - dist;

                b.PutPosition(b.Left + nx * overlap, b.Top + ny * overlap);

                double dot = b.VX * nx + b.VY * ny;
                b.VX = (b.VX - 2 * dot * nx) * RLSGame.BallBounce;
                b.VY = (b.VY - 2 * dot * ny) * RLSGame.BallBounce;
            }
        }

        private void CheckCarBallCollision(RLSCar c)
        {
            var b = game.Ball;

            double bcx = b.MiddleX;
            double bcy = b.MiddleY;
            double closestX = Math.Max(c.Left, Math.Min(bcx, c.Right));
            double closestY = Math.Max(c.Top, Math.Min(bcy, c.Bottom));
            double dx = bcx - closestX;
            double dy = bcy - closestY;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            double radius = b.Width / 2;

            if (dist < radius)
            {
                if (dist == 0)
                {
                    dx = bcx - c.MiddleX;
                    dy = bcy - c.MiddleY;
                    dist = Math.Sqrt(dx * dx + dy * dy);

                    if (dist == 0)
                    {
                        dx = 0;
                        dy = -1;
                        dist = 1;
                    }
                }

                double nx = dx / dist;
                double ny = dy / dist;
                double overlap = radius - dist;

                b.PutPosition(b.Left + nx * overlap, b.Top + ny * overlap);

                if (b.Bottom > RLSGame.GroundY)
                {
                    double correction = b.Bottom - RLSGame.GroundY;
                    b.PutPosition(b.Left, b.Top - correction);

                    if (Math.Abs(b.VX) < 2)
                        b.VX += (nx >= 0 ? 5 : -5);
                }

                double impact = 8 + Math.Sqrt(c.VX * c.VX + c.VY * c.VY) * 0.6;
                b.VX = nx * impact + c.VX * 0.5;
                b.VY = ny * impact + c.VY * 0.5;

                if (ny > 0.5)
                    b.VY += 3;
            }
        }

        private void CheckCarCarCollision()
        {
            var p1 = game.Car1;
            var p2 = game.Car2;

            if (p1.Left < p2.Right && p1.Right > p2.Left &&
                p1.Top < p2.Bottom && p1.Bottom > p2.Top)
            {
                double tmpVX = p1.VX;
                p1.VX = p2.VX * 0.8;
                p2.VX = tmpVX * 0.8;

                if (p1.Left < p2.Left)
                {
                    p1.PutPosition(p1.Left - 2, p1.Top);
                    p2.PutPosition(p2.Left + 2, p2.Top);
                }
                else
                {
                    p1.PutPosition(p1.Left + 2, p1.Top);
                    p2.PutPosition(p2.Left - 2, p2.Top);
                }
            }
        }

        private void CheckGoal()
        {
            var b = game.Ball;

            if (b.Left <= 20 && b.Top > 373 && b.Top < 520)
            {
                game.Score2++;
                game.ShowGoal("GOAL P2!");
                return;
            }

            if (b.Right >= 980 && b.Top > 373 && b.Top < 520)
            {
                game.Score1++;
                game.ShowGoal("GOAL P1!");
            }
        }
    }
}