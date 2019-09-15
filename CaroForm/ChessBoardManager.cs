using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CaroForm
{
    public class ChessBoardManager
    {
        #region Biến
        Panel chessBoard;
        public bool isWon =false;

        public Panel ChessBoard { get => chessBoard; set => chessBoard = value; }

        public static List<Player> players;
        public List<Player> Players { get => players; set => players = value; }

        public static int currentPlayer;
        public int CurrentPlayer { get => currentPlayer; set => currentPlayer = value; }

        private TextBox playerName;
        public TextBox PlayerName { get => playerName; set => playerName = value; }

        private PictureBox playerMark;
        public PictureBox PlayerMark { get => playerMark; set => playerMark = value; }

        private List<List<Button>> matrix;
        public List<List<Button>> Matrix { get => matrix; set => matrix = value; }

        private event EventHandler<ButtonClickEvent> playerMarked;
        public event EventHandler<ButtonClickEvent> PlayerMarked
        {
            add
            {
                playerMarked += value;
            }
            remove
            {
                playerMarked -= value;
            }
        }

        private event EventHandler endedGame;
        public event EventHandler EndedGame
        {
            add
            {
                endedGame += value;
            }
            remove
            {
                endedGame -= value;
            }
        }

        private Stack<Point> PlayerMove;

        #endregion
        
        #region Khởi Tạo
        public ChessBoardManager(Panel chessBoard,TextBox playerName,PictureBox playerMark)
        {
            this.ChessBoard = chessBoard;
            this.Players = new List<Player>()
            {
                new Player("Player 1", CaroForm.Properties.Resources.X),
                new Player("Player 2", CaroForm.Properties.Resources.O)
            };
            this.PlayerName = playerName;
            this.PlayerMark = playerMark;

        }
        #endregion

        #region Hàm
        public void DrawChessBoard()
        {
            PlayerMove = new Stack<Point>(); 
            ChangePlayer();
            ChessBoard.Controls.Clear();
            chessBoard.Enabled = true;
            Matrix = new List<List<Button>>();
            Button olBtn = new Button() { Width = 0, Height = 0, Location = new Point(0, 0) };
            int y = olBtn.Location.Y;
            int x = olBtn.Location.X;
            for (int i = 0; i < Cons.row; i++)
            {
                Matrix.Add(new List<Button>());
                for (int j = 0; j < Cons.col; j++)
                {
                    Button btn = new Button()
                    {
                        Width = Cons.CHESS_WIDTH,
                        Height = Cons.CHESS_HEIGHT,
                        Location = new Point(x, y),
                        BackgroundImageLayout = ImageLayout.Stretch,
                        Tag = i.ToString()
                    };
                    Matrix[i].Add(btn);
                    btn.Click += btn_Click;
                    x += Cons.CHESS_WIDTH;
                    chessBoard.Controls.Add(btn);
                    olBtn = btn;
                }
                x = 0;
                y += Cons.CHESS_HEIGHT;
            }
        }


        void btn_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;

            if (btn.BackgroundImage != null)
                return;

            Mark(btn);
            PlayerMove.Push(GetChessPoint(btn));
            ChangePlayer();

            if (playerMarked != null)
                playerMarked(this, new ButtonClickEvent(GetChessPoint(btn)));

            if (isEndGame(btn))
            {
                isWon = true;
                EndGame();
            }
        }
        public void OtherPlayerMark(Point point)
        {
            Button btn = Matrix[point.Y][point.X];

            if (btn.BackgroundImage != null)
            {
                return;
            }

            Mark(btn);
            PlayerMove.Push(GetChessPoint(btn));
            ChangePlayer();

            if (isEndGame(btn))
            {
                isWon = true;
                EndGame();
            }
        }
        public bool Undo()
        {
            if(PlayerMove.Count <= 0)
            {
                return false;
            }
            Point oldPoint = PlayerMove.Pop();
            Button btn = Matrix[oldPoint.Y][oldPoint.X];
            CurrentPlayer = CurrentPlayer == 0 ? 1 : 0;
            ChangePlayer();
            btn.BackgroundImage = null;
            return true;
        }
        public void EndGame()
        {
            if (endedGame != null)
                endedGame(this, new EventArgs());
        }

        private bool isEndGame(Button btn)
        {
            return isEndHorizontal(btn) || isEndVertical(btn) || isEndPrimary(btn) || isEndSub(btn);
        }

        private Point GetChessPoint(Button btn)
        {
            int vertical = Convert.ToInt32(btn.Tag);
            int horizontal = Matrix[vertical].IndexOf(btn);

            Point point = new Point(horizontal, vertical);

            return point;
        }

        private bool isEndHorizontal(Button btn)
        {
            Point point = GetChessPoint(btn);

            int countLeft = 0;
            Image dau = null,dit = null;
            for (int i = point.X; i >= 0; i--)
            {
                if (Matrix[point.Y][i].BackgroundImage == btn.BackgroundImage)
                {
                    countLeft++;
                }
                else
                {
                    dau = Matrix[point.Y][i].BackgroundImage;
                    break;
                }
            }

            int countRight = 0;
            for (int i = point.X + 1; i < Cons.col; i++)
            {
                if (Matrix[point.Y][i].BackgroundImage == btn.BackgroundImage)
                {
                    countRight++;
                }
                else
                {
                    dit = Matrix[point.Y][i].BackgroundImage;
                    break;
                }
            }
            if(dau!=null && dau == dit)
            {
                return false;
            }
            return countLeft + countRight == 5;
        }
        private bool isEndVertical(Button btn)
        {
            Image dau = null, dit = null;
            Point point = GetChessPoint(btn);

            int countTop = 0;
            for (int i = point.Y; i >= 0; i--)
            {
                if (Matrix[i][point.X].BackgroundImage == btn.BackgroundImage)
                {
                    countTop++;
                }
                else
                {
                    dau = Matrix[i][point.X].BackgroundImage;
                    break;
                }
            }

            int countBottom = 0;
            for (int i = point.Y + 1; i < Cons.col; i++)
            {
                if (Matrix[i][point.X].BackgroundImage == btn.BackgroundImage)
                {
                    countBottom++;
                }
                else
                {
                    dit = Matrix[i][point.X].BackgroundImage;
                    break;
                }
            }
            if (dau != null && dau == dit)
            {
                return false;
            }
            return countTop + countBottom == 5;
        }
        private bool isEndPrimary(Button btn)
        {
            Image dau = null, dit = null;
            Point point = GetChessPoint(btn);

            int countTop = 0;
            for (int i = 0; i <= point.X; i++)
            {
                if (point.X - i < 0 || point.Y - i < 0)
                    break;

                if (Matrix[point.Y - i][point.X - i].BackgroundImage == btn.BackgroundImage)
                {
                    countTop++;
                }
                else
                {
                    dau = Matrix[point.Y - i][point.X - i].BackgroundImage;
                    break;
                }
            }

            int countBottom = 0;
            for (int i = 1; i <= Cons.col - point.X; i++)
            {
                if (point.Y + i >= Cons.row || point.X + i >= Cons.row)
                    break;

                if (Matrix[point.Y + i][point.X + i].BackgroundImage == btn.BackgroundImage)
                {
                    countBottom++;
                }
                else
                {
                    dit = Matrix[point.Y + i][point.X + i].BackgroundImage;
                    break;
                }
            }
            if (dau != null && dau == dit)
            {
                return false;
            }
            return countTop + countBottom == 5;
        }
        private bool isEndSub(Button btn)
        {
            Image dau = null, dit = null;
            Point point = GetChessPoint(btn);

            int countTop = 0;
            for (int i = 0; i <= point.X; i++)
            {
                if (point.X + i > Cons.row || point.Y - i < 0)
                    break;

                if (Matrix[point.Y - i][point.X + i].BackgroundImage == btn.BackgroundImage)
                {
                    countTop++;
                }
                else
                {
                    dau = Matrix[point.Y - i][point.X + i].BackgroundImage;
                    break;
                }
            }

            int countBottom = 0;
            for (int i = 1; i <= Cons.row - point.X; i++)
            {
                if (point.Y + i >= Cons.col || point.X - i < 0)
                    break;

                if (Matrix[point.Y + i][point.X - i].BackgroundImage == btn.BackgroundImage)
                {
                    countBottom++;
                }
                else
                {
                    dit = Matrix[point.Y + i][point.X - i].BackgroundImage;
                    break;
                }
            }
            if (dau != null && dau == dit)
            {
                return false;
            }
            return countTop + countBottom == 5;
        }

        private void Mark(Button btn)
        {
            btn.BackgroundImage = Players[CurrentPlayer].Mark;

            CurrentPlayer = CurrentPlayer == 1 ? 0 : 1;
        }

        public void ChangePlayer()
        {
            PlayerName.Text = Players[CurrentPlayer].Name;

            PlayerMark.Image = Players[CurrentPlayer].Mark;
        }
        #endregion
    }
    public class ButtonClickEvent : EventArgs
    {
        private Point clickPoint;

        public Point ClickPoint { get => clickPoint; set => clickPoint = value; }
        public ButtonClickEvent(Point clickPoint)
        {
            this.ClickPoint = clickPoint;
        }
    }
}
