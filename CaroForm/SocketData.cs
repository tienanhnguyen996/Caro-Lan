using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaroForm
{
    [Serializable]
    public class SocketData
    {
        private int command;

        public int Command { get => command; set => command = value; }

        private string message;
        public string Message { get => message; set => message = value; }

        private Point point;
        public Point Point { get => point; set => point = value; }
        public SocketData(int command,string message, Point point)
        {
            this.Command = command;
            this.Message = message;
            this.Point = point;
        }
    }
    public enum SocketCommand
    {
        SEND_POINT,
        MESSAGE,
        NOTIFY,
        NEW_GAME,
        UNDO,
        EXIT,
        CONNECTED,
        ACCEPT_UNDO,
        DENY_UNDO,
        DENY_NEWGAME,
        ACCEPT_NEWGAME,
        RECEIVE_NAME,
        TIME_OUT
    };
}
