using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Configuration;
using System.Text;

namespace KellMQ.Receiver
{
    internal class Server
    {
        public Server()
        {
            string p = ConfigurationManager.AppSettings["port"];
            if (!string.IsNullOrEmpty(p))
            {
                int RET;
                if (int.TryParse(p, out RET))
                    port = RET;
            }
            Logs.Create("初始化服务对象，监听端口为[" + port + "]");
        }

        ~Server()
        {
            StopListen();
            Logs.Create("释放服务对象");
        }

        private int port = 8888;
        private Thread threadWatch;//负责监听客户端连接请求的线程
        private Socket socketWatch;//负责监听的套接字
        private Socket socketClient;//接收客户端连接后的套接字
        private volatile bool listen;
        private IPEndPoint server;
        public static string HeartBeat = Common.HeartBeat;
        public static int BufferSize = Common.BufferSize;
        public static int ReceiveTimeout = Common.ReceiveTimeout;
        public event ReceiveHandler Receiving;
        public event OfflineHandler Offline;
        /// <summary>
        /// 停止监听
        /// </summary>
        public void StopListen()
        {
            bool flag = false;
            try
            {
                CloseThread();
                socketWatch.Close();
                if (socketClient != null && socketClient.Connected)
                    socketClient.Close();
                listen = false;
                flag = true;
            }
            catch (Exception e) { Logs.Create("停止监听出错：" + e.Message); }
            Logs.Create("服务停止监听..." + (flag ? "成功！" : "失败！"));
        }
        /// <summary>
        /// 停止绑定
        /// </summary>
        //public void StopBind()
        //{
        //    bool flag = false;
        //    try
        //    {
        //        CloseThread();
        //        if (socketClient != null && socketClient.Connected)
        //            socketClient.Close();
        //        listen = false;
        //        flag = true;
        //    }
        //    catch { }
        //    Logs.Info("停止监听广播..." + (flag ? "成功！" : "失败！"));
        //}

        private void CloseThread()
        {
            if (threadWatch != null && threadWatch.ThreadState != ThreadState.Aborted)
            {
                try
                {
                    threadWatch.Abort();
                    //threadWatch.Join();
                }
                catch (Exception e)
                {
                    //ShowMsg(e.Message);
                }
            }
        }
        /// <summary>
        /// 开始监听TCP连接
        /// </summary>
        public void StartListen()
        {
            try
            {
                listen = true;
                //创建一个网络端点
                server = new IPEndPoint(IPAddress.Any, port);
                listen = Listen();
                Logs.Create("服务开始监听..." + (listen ? "成功！" : "失败！"));
            }
            catch (Exception e)
            {
                Logs.Create("开始监听出错：" + e.Message);
            }
            finally
            {
                
            }
        }
        ///// <summary>
        ///// 绑定监听UDP广播
        ///// </summary>
        //public void StartBind()
        //{
        //    try
        //    {
        //        listen = true;
        //        //创建一个网络端点
        //        server = new IPEndPoint(IPAddress.Any, port);
        //        Bind();
        //        Logs.Info("服务开始监听广播..." + (listen ? "成功！" : "失败！"));
        //    }
        //    catch (Exception e)
        //    {
        //        Logs.LogDataBase(NLog.LogLevel.Error, "ClientMsgService", "Server", "StartBind()", "开始监听广播出错", e.Message);
        //    }
        //    finally
        //    {

        //    }
        //}

        private bool Listen()
        {
            bool listen = true;
            //创建一个基于IPV4的TCP协议的Socket对象
            socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                CloseThread();
                threadWatch = new Thread(WatchConnection);
                threadWatch.IsBackground = true;//设置为后台
                threadWatch.Start();// 启动线程
                socketWatch.Bind(server);
                socketWatch.Listen(0);
            }
            catch (Exception e)
            {
                listen = false;
                Logs.Create("监听TCP连接出错：" + e.Message);
            }
            return listen;
        }

        //private void Bind()
        //{
        //    //创建一个基于IPV4的UDP协议的Socket对象
        //    socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //    try
        //    {
        //        CloseThread();
        //        threadWatch = new Thread(WatchBroadcast);
        //        threadWatch.IsBackground = true;//设置为后台
        //        threadWatch.Start();// 启动线程
        //        socketClient.Bind(server);
        //        listen = true;
        //    }
        //    catch (Exception e)
        //    {
        //        listen = false;
        //        Logs.LogDataBase(NLog.LogLevel.Error, "ClientMsgService", "Server", "Bind()", "监听广播出错", e.Message);
        //    }
        //}

        ///// <summary>
        ///// 监听广播
        ///// </summary>
        //void WatchBroadcast()
        //{
        //    EndPoint ep = server; //获取发送广播主机的确切通信ip
        //    while (true)
        //    {
        //        if (listen)
        //        {
        //            try
        //            {
        //                List<byte> append = new List<byte>();
        //                //创建一个字节数组接收数据
        //                byte[] arrMsgRec = new byte[BufferSize];
        //                int length = socketClient.ReceiveFrom(arrMsgRec, ref ep);//同步调用，此处会被阻塞
        //                for (int i = 0; i < length; i++)
        //                {
        //                    append.Add(arrMsgRec[i]);
        //                }
        //                while (length == BufferSize)
        //                {
        //                    length = socketClient.ReceiveFrom(arrMsgRec, ref ep);//同步调用，此处会被阻塞
        //                    for (int i = 0; i < length; i++)
        //                    {
        //                        append.Add(arrMsgRec[i]);
        //                    }
        //                }
        //                if (append.Count > 0)
        //                {
        //                    byte[] data = append.ToArray();
        //                    if (Receiving != null)
        //                        Receiving(ep, data);
        //                }
        //            }
        //            catch (Exception e)
        //            {
        //                listen = false;
        //                Logs.LogDataBase(NLog.LogLevel.Error, "ClientMsgService", "Server", "Bind()", "接收广播消息出错", e.Message);
        //                //ShowMsg("-----客户端[" + socketClient.RemoteEndPoint.ToString() + "]已经断开！-----");
        //                //关闭套接字
        //                //socketClient.Close();
        //                //结束当前线程
        //                //Thread.CurrentThread.Abort();
        //            }
        //            //finally
        //            //{
        //            //    //关闭套接字
        //            //    socketClient.Close();
        //            //    //结束当前线程
        //            //    //Thread.CurrentThread.Abort();
        //            //}
        //        }
        //    }
        //}

        /// <summary>
        /// 监听客户端连接
        /// </summary>
        void WatchConnection()
        {
            while (true)
            {
                if (listen)
                {
                    if (socketWatch.IsBound)
                    {
                        try
                        {
                            //创建监听的连接套接字
                            Socket sockConnection = socketWatch.Accept();//当执行socketWatch.Close()时这里很容易引发一个这样的异常：“一个封锁操作被对 WSACancelBlockingCall 的调用中断。”
                            sockConnection.ReceiveTimeout = ReceiveTimeout;
                            //为客户端套接字连接开启新的线程用于接收客户端发送的数据
                            Thread t = new Thread(Receive);
                            //设置为后台线程
                            t.IsBackground = true;
                            //为客户端连接开启线程
                            t.Start(sockConnection);
                            //ShowMsg("-----[" + key + "]客户端连接进入-----");
                        }
                        catch (Exception e)
                        {
                            Logs.Create("监听发送端的连接套接字出错：" + e.Message);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 接收客户端信息的线程执行代码
        /// </summary>
        /// <param name="o"></param>
        private void Receive(object o)
        {
            try
            {
                //接收数据
                socketClient = o as Socket;
                List<byte> append = new List<byte>();
                while (socketClient.Available > 0)
                {
                    //创建一个字节数组接收数据
                    byte[] arrMsgRec = new byte[BufferSize];
                    int length = socketClient.Receive(arrMsgRec);
                    for (int i = 0; i < length; i++)
                    {
                        append.Add(arrMsgRec[i]);
                    }
                    //append.AddRange(arrMsgRec.Take<byte>(length).ToArray());
                }
                if (append.Count > 0)
                {
                    byte[] data = append.ToArray();
                    if (Receiving != null)
                        Receiving(socketClient.RemoteEndPoint, data);
                }
            }
            catch (Exception e)
            {
                if (Offline != null)
                    Offline(this, socketClient.RemoteEndPoint);
                Console.WriteLine("客户端[" + socketClient.RemoteEndPoint.ToString() + "]已经断开：" + e.Message);
                //if (Receiving != null)
                //    Receiving(socketClient.RemoteEndPoint, Common.GetBytes(Common.Error + e.Message, Encoding.UTF8));
                //ShowMsg("-----客户端[" + socketClient.RemoteEndPoint.ToString() + "]已经断开！-----");
                //关闭套接字
                //socketClient.Close();
                //结束当前线程
                //Thread.CurrentThread.Abort();                
            }
            //finally
            //{
            //    //关闭套接字
            //    socketClient.Close();
            //    //结束当前线程
            //    //Thread.CurrentThread.Abort();
            //}
        }
        /// <summary>
        /// 给发送方回复数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="msg"></param>
        public void SendTo(EndPoint client, byte[] data)
        {
            if (socketClient != null && socketClient.Connected)
            {
                //发送信息
                try
                {
                    socketClient.SendTo(data, client);
                    Logs.Create("给发送方回复数据：" + Common.GetString(data, 0, Encoding.UTF8));
                }
                catch (Exception e)
                {
                    Logs.Create("给发送方回复数据失败！");
                    //ShowMsg("-----客户端[" + selectkey + "]已经断开！-----");
                }
            }
        }
        /// <summary>
        /// 判断发送方是否在线
        /// </summary>
        /// <returns></returns>
        public bool IsTheClientOnline()
        {
            try
            {
                byte[] hb = Array.ConvertAll<char, byte>(HeartBeat.ToCharArray(), a => (byte)a);
                socketWatch.Send(hb);
                return true;
            }
            catch (SocketException e)
            {
                //ShowMsg(e.Message);
            }
            return false;
        }
        /// <summary>
        /// 是否正在监听
        /// </summary>
        public bool Listening
        {
            get
            {
                return listen;
            }
        }
    }
}
