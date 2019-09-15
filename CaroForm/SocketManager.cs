using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CaroForm
{
    class SocketManager
    {
        #region Client
        Socket client;
        public bool ConnectSever()
        {
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(IP), Port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                client.Connect(ipe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Sever
        Socket sever;
        public void CreateSever()
        {
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(IP), Port);
            sever = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sever.Bind(ipe);
            sever.Listen(19);
            Thread acceptClient = new Thread(()=> {
                client = sever.Accept();
            });
            Thread Timer = new Thread(() => {
                int i = 0;
                while (true)
                {
                    Thread.Sleep(500);
                    i++;
                    if(i>=40)
                    {
                        acceptClient.Abort();
                    }
                }
            });
            acceptClient.IsBackground = true;
            acceptClient.Start();
            Timer.IsBackground = true;
            Timer.Start();
        }
        public void CloseSever()
        {
            sever.Close();
        }
        #endregion

        #region Cả Hai
        public string IP = "192.168.1.1";
        public int Port = 511;
        public bool isSever = false;
        public bool isConnected = false;
        public const int BUFFER = 9999;

        public bool Send(object data)
        {
            byte[] Data = SerializeData(data);
            return SendData(client, Data);
        }

        public Object Receive()
        {
            byte[] receive = new byte[BUFFER];
            bool isOk = ReceiveData(client, receive);
            return DeserializeData(receive);
        }

        public bool SendData(Socket target, byte[] data)
        {
            return target.Send(data) == 1 ? true : false;
        }
        public bool ReceiveData(Socket target,byte[] data)
        {
            return target.Receive(data) == 1 ? true : false;
        }

        /// <summary>
        /// Nén đối tượng thành mảng byte[]
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public byte[] SerializeData(Object o)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf1 = new BinaryFormatter();
            bf1.Serialize(ms, o);
            return ms.ToArray();
        }

        /// <summary>
        /// Giải nén mảng byte[] thành đối tượng object
        /// </summary>
        /// <param name="theByteArray"></param>
        /// <returns></returns>
        public object DeserializeData(byte[] theByteArray)
        {
            MemoryStream ms = new MemoryStream(theByteArray);
            BinaryFormatter bf1 = new BinaryFormatter();
            ms.Position = 0;
            return bf1.Deserialize(ms);
        }

        /// <summary>
        /// Lấy ra IP V4 của card mạng đang dùng
        /// </summary>
        /// <param name="_type"></param>
        /// <returns></returns>
        public string GetLocalIPv4(NetworkInterfaceType _type)
        {
            string output = "";
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            output = ip.Address.ToString();
                        }
                    }
                }
            }
            return output;
        }

        #endregion

    }
}
