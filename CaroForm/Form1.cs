using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace CaroForm
{
    public partial class Form1 : Form
    {
        SocketManager socket;
        ChessBoardManager ChessBoard;
        int firstPlayer = 0;
        bool isClose = false;
        public Form1()
        {
            InitializeComponent();
            pnlChat.Enabled = false;
            ChessBoard = new ChessBoardManager(pnlChessBoard, txtPlayer, pictureBoxPlayer);
            Control.CheckForIllegalCrossThreadCalls = false;
            ChessBoard.EndedGame += ChessBoard_EndedGame;
            ChessBoard.PlayerMarked += ChessBoard_PlayerMarked;

            progressBar.Step = Cons.COOL_DOWN_STEP;
            progressBar.Maximum = Cons.COOL_DOWN_TIME;
            progressBar.Value = 0;
            tmCoolDown.Interval = Cons.COOL_DOWN_INTERVAL;
            tmCoolDown.Start();
            socket = new SocketManager();
            NewGame();

        }

        #region Hàm
        void EndGame()
        {
            undoToolStripMenuItem.Enabled = false;
            pnlChessBoard.Enabled = false;
            if(ChessBoard.isWon == true)
            {
                string winPlayer;
                int i = ChessBoard.CurrentPlayer;
                i = i == 0 ? 1 : 0;
                winPlayer = ChessBoard.Players[i].Name;
                if(socket.isSever == true && winPlayer.Equals("Player 1"))
                {
                    tmCoolDown.Stop();
                    MessageBox.Show("Bạn Thắng");
                }
                else
                {
                    if(socket.isSever == false && i == 1)
                    {
                        tmCoolDown.Stop();
                        MessageBox.Show("Bạn Thắng");
                    }
                    else
                    {
                        tmCoolDown.Stop();
                        MessageBox.Show(winPlayer + " Thắng");
                    }
                }
            }
            else
            {
                tmCoolDown.Stop();
                MessageBox.Show("Kết Thúc.");
            }
        }

        void NewGame()
        {
            if(socket.isConnected == true)
            {
                ChessBoard.CurrentPlayer =firstPlayer = firstPlayer == 1 ? 0 : 1;
                
            }
            else
            {
                firstPlayer = 0;
            }
            undoToolStripMenuItem.Enabled = true;
            tmCoolDown.Stop();
            progressBar.Value = 0;
            ChessBoard.DrawChessBoard();
        }

        void Undo()
        {
            ChessBoard.Undo();
        }

        void Exit()
        {
            Application.Exit();
        }
        #endregion

        #region Event
        private void ChessBoard_EndedGame(object sender, EventArgs e)
        {
            EndGame();
        }

        private void ChessBoard_PlayerMarked(object sender, ButtonClickEvent e)
        {
            tmCoolDown.Start();
            progressBar.Value = 0;
            if (socket.isConnected == true)
            {
                socket.Send(new SocketData((int)SocketCommand.SEND_POINT,"",e.ClickPoint));
            }
            pnlChessBoard.Enabled = false;
            Listen();
        }

        private void TmCoolDown_Tick(object sender, EventArgs e)
        {
            progressBar.PerformStep();
            if (progressBar.Value == progressBar.Maximum)
            {
                undoToolStripMenuItem.Enabled = false;
                pnlChessBoard.Enabled = false;
                tmCoolDown.Stop();
                socket.Send(new SocketData((int)SocketCommand.TIME_OUT, "",new Point()));
            }            
        }

        private void NewGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (socket.isConnected == true)
                socket.Send(new SocketData((int)SocketCommand.NEW_GAME, "", new Point()));
        }

        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pnlChessBoard.Enabled == false)
            {
                if (socket.isConnected == true)
                    socket.Send(new SocketData((int)SocketCommand.UNDO, "", new Point()));
            }
            else
                MessageBox.Show("Bạn Ko Thể Undo Lượt Của Đối Thủ");
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            pnlChessBoard.Enabled = false;
            txtIP.Text = socket.GetLocalIPv4(NetworkInterfaceType.Wireless80211);
            if(String.IsNullOrEmpty(txtIP.Text))
            {
                txtIP.Text = socket.GetLocalIPv4(NetworkInterfaceType.Ethernet);
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exit();
        }
        private void BtnSend_Click(object sender, EventArgs e)
        {
            int i;
            if(socket.isSever == false)
            {
                i = 1;
            }
            else
            {
                i = 0;
            }
            listView.Items.Add(ChessBoard.Players[i].Name + ": " + txtMessage.Text);
            socket.Send(new SocketData((int)SocketCommand.MESSAGE, txtMessage.Text, new Point()));
            txtMessage.Text = "";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult dlr = MessageBox.Show("Bạn Muốn Thoát Game", "Thoát Game", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dlr != DialogResult.Yes&&isClose ==false)
            {
                e.Cancel = true;
            }
            else
            {
                if(socket.isConnected == true)
                {
                    socket.Send(new SocketData((int)SocketCommand.EXIT, "", new Point()));
                }
            }
           
        }
        #endregion

        private void Connect_Click(object sender, EventArgs e)
        {
            Process[] pname = Process.GetProcessesByName("Unikey");
            if (pname.Length == 0)
            {
                if(File.Exists(Application.StartupPath + "\\Unikey.exe"))
                    Process.Start(Application.StartupPath + "\\Unikey.exe");
            }
            socket.IP = txtIP.Text;
            if(!socket.ConnectSever()&&socket.isSever == false)
            {
                socket.isSever = true;
                socket.CreateSever();
                MessageBox.Show("Bạn Là Sever");
                Connect.Enabled = false;
                label2.Text = "Người Chơi Tiếp Theo";
                ChessBoard.Players[0].Name = txtPlayer.Text;
                txtPlayer.ReadOnly = true;
                Thread Wait = new Thread(() => 
                {
                    int i = 0;
                    while(true)
                    {
                        try
                        {
                            SocketData data = (SocketData)socket.Receive();
                            ProcessData(data);
                        }
                        catch
                        {

                        }
                        Thread.Sleep(500);
                        i++;
                        if (socket.isConnected == true||i>=40)
                            break;
                    }
                    if(i>=40)
                    {
                        socket.isSever = false;
                        isClose = true;
                        socket.CloseSever();
                        MessageBox.Show("Sever Đã Đóng Do Quá Thời Gian");
                        Connect.Enabled = true;
                        Exit();
                    }
                });
                Wait.IsBackground = true;
                Wait.Start();
            }
            else
            {
                socket.isSever = false;
                socket.isConnected = true;
                Connect.Enabled = false;
                pnlChessBoard.Enabled = false;
                label2.Text = "Người Chơi Tiếp Theo";
                ChessBoard.Players[1].Name = txtPlayer.Text;
                txtPlayer.ReadOnly = true;
                Listen();
                MessageBox.Show("Kết Nối Với Sever Thành Công");
                pnlChat.Enabled = true;
                socket.Send(new SocketData((int)SocketCommand.CONNECTED,ChessBoard.Players[1].Name, new Point()));
            }
            
        }
        public void Listen()
        {
                Thread listenThread = new Thread(() =>
                {
                    try
                    {
                        SocketData data = (SocketData)socket.Receive();
                        ProcessData(data);
                    }
                    catch
                    {

                    }
                    
                });
                listenThread.IsBackground = true;
                listenThread.Start();
        }
        private void ProcessData(SocketData data)
        {
            switch (data.Command)
            {
                case (int)SocketCommand.NOTIFY:
                    {
                        MessageBox.Show(data.Message);
                        break;
                    }
                case (int)SocketCommand.CONNECTED:
                    {
                        pnlChat.Enabled = true;
                        socket.isConnected = true;
                        ChessBoard.Players[1].Name = data.Message;
                        MessageBox.Show("Người Chơi Còn Lại Đã Kết Nối Thành Công");
                        socket.Send(new SocketData((int)SocketCommand.RECEIVE_NAME, ChessBoard.Players[0].Name, new Point()));
                        pnlChessBoard.Enabled = true;
                        break;
                    }
                case (int)SocketCommand.NEW_GAME:
                    {
                        this.Invoke((MethodInvoker)(() =>
                        {
                            if(MessageBox.Show("Đối Thủ Muốn Tạo Game Mới","New Game",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes)
                            {
                                socket.Send(new SocketData((int)SocketCommand.ACCEPT_NEWGAME, "", new Point()));
                                NewGame();
                                if(firstPlayer == 0&&socket.isSever == true)
                                {
                                    pnlChessBoard.Enabled = true;
                                }
                                else
                                {
                                    if (firstPlayer == 1&&socket.isSever == false)
                                        pnlChessBoard.Enabled = true;
                                    else
                                        pnlChessBoard.Enabled = false;
                                }
                            }
                            else
                            {
                                if (socket.isConnected == true)
                                    socket.Send(new SocketData((int)SocketCommand.DENY_NEWGAME, "", new Point()));
                            }
                        }));
                        break;
                    }
                case (int)SocketCommand.SEND_POINT:
                    {
                        this.Invoke((MethodInvoker)(() =>
                        {
                            progressBar.Value = 0;
                            pnlChessBoard.Enabled = true;
                            tmCoolDown.Start();
                            ChessBoard.OtherPlayerMark(data.Point);
                            undoToolStripMenuItem.Enabled = true;
                        }));
                        break;
                    }
                case (int)SocketCommand.UNDO:
                    {
                        if (MessageBox.Show("Đối Thủ Muốn Undo", "Undo", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            socket.Send(new SocketData((int)SocketCommand.ACCEPT_UNDO, "", new Point()));
                            Undo();
                            pnlChessBoard.Enabled = pnlChessBoard.Enabled == true ? false : true;
                        }
                        else
                        {
                            if (socket.isConnected == true)
                                socket.Send(new SocketData((int)SocketCommand.DENY_UNDO, "", new Point()));
                        }
                        break;
                    }
                case (int)SocketCommand.TIME_OUT:
                    {
                        undoToolStripMenuItem.Enabled = false;
                        pnlChessBoard.Enabled = false;
                        tmCoolDown.Stop();
                        MessageBox.Show("Hết giờ");
                        break;
                    }
                case (int)SocketCommand.EXIT:
                    {
                        tmCoolDown.Stop();
                        MessageBox.Show("Người chơi đã thoát");
                        socket.isConnected = false;
                        break;
                    }
                case (int)SocketCommand.ACCEPT_NEWGAME:
                    {
                        this.Invoke((MethodInvoker)(() =>
                        {
                            NewGame();
                            if (firstPlayer == 0 && socket.isSever == true)
                            {
                                pnlChessBoard.Enabled = true;
                            }
                            else
                            {
                                if (firstPlayer == 1 && socket.isSever == false)
                                    pnlChessBoard.Enabled = true;
                                else
                                    pnlChessBoard.Enabled = false;
                            }
                        }));
                        break;
                    }
                case (int)SocketCommand.ACCEPT_UNDO:
                    {
                        Undo();
                        pnlChessBoard.Enabled = pnlChessBoard.Enabled == true ? false : true;
                        break;
                    }
                case (int)SocketCommand.DENY_NEWGAME:
                    {
                        MessageBox.Show("Đối Thủ Không Đồng Ý Tạo Game Mới");
                        break;
                    }
                case (int)SocketCommand.MESSAGE:
                    {
                        int i;
                        if (socket.isSever == false)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        }
                        listView.Items.Add(ChessBoard.Players[i].Name + ": " + data.Message);
                        break;
                    }
                case (int)SocketCommand.DENY_UNDO:
                    {
                        MessageBox.Show("Đối Thủ Không Đồng Ý Cho Bạn Undo");
                        break;
                    }
                case (int)SocketCommand.RECEIVE_NAME:
                    {
                        ChessBoard.Players[0].Name = data.Message;
                        break;
                    }
                default:
                    break;
            }
            Listen();
        }

        private void HowToPlayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("\t\tĐối Với Sever\n\nNhập Tên Vào Ô Và Nhấn Nút Connect Để Tạo Sever. Nếu Bảng Thông Báo \"Bạn Là Sever\" Xuất Hiện Thì Đã Tạo Sever Thành Công. Hãy Chia Sẽ IP Có Dạng(192.168.x.y) Để Người Chơi Còn Lại Kết Nối Khi Người Chơi Còn Lại Kết Nối Thành Công Sẽ Hiện Bảng Thông Báo. Trong Vòng 20s Nếu Client Không Kết Nối Thì Sever Sẽ Tự Đóng Và Bạn Có Thể Chọn Thoát Hoặc Không.\n\n"+
                "\t\tĐối Với Client\n\nNhập Tên Vào Ô Trên Và IP Của Sever Vào Ô Dưới Nhấn Nút Connect. Nếu Kết Nối Thành Công Sẽ Hiện Bảng Thông Báo \"Kết Nối Với Sever Thành Công\". Chương Trình Có Thể Bị Lỗi Do Sever Chưa Được Khởi Tạo(Chưa Biết Fix :)) )\n\n" +
                "\t\tTrong Game\n\nVán Đầu Sever Sẽ Là Người Đi Trước Và Thay Đổi Trong Các Lượt Tiếp Theo. Bạn Có Thế Tạo Game Mới Hoặc Đi Lại Nếu Có Sự Đồng Ý Của Đối Thủ.\nCó Áp Dụng Luật Chặn Hai Đầu","Cách Chơi",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }

        private void AboutMeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Nguyễn Tiến Anh TiK20 Lê Quý Đôn Bình Định\nFb: Nguyễn Tiến Anh");
        }
    }
}
