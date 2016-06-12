using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;
using System.Configuration;

namespace KellMQ.Sender
{
    internal class Client
    {
        public Client()
        {
            string p = ConfigurationManager.AppSettings["port"];
            if (!string.IsNullOrEmpty(p))
            {
                int RET;
                if (int.TryParse(p, out RET))
                    port = RET;
            }
        }

        ~Client()
        {
            Disconnect();
        }

        private int port = 8888;
        private Thread threadclient;//客户端用于接收数据的线程
        private Socket socketClient;//客户端用于接收数据的套接字
        private IPEndPoint server;
        private volatile bool connect;
        public static string HeartBeat = Common.HeartBeat;
        public static int BufferSize = Common.BufferSize;
        public static int ReceiveTimeout = Common.ReceiveTimeout;
        public event ReceiveHandler Receiving;

        private void Connect(IPAddress ip)
        {
            //if (socketClient == null)
            //{
                try
                {
                    connect = false;
                    server = new IPEndPoint(ip, port);
                    socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socketClient.ReceiveTimeout = ReceiveTimeout;
                    socketClient.Connect(server);
                    connect = true;

                    //要采用以上同步方法来连接，是因为担心同时有多个客户端发送连接请求的时候，保证每个连接的独立，因为socketClient是共用的对象，若是采用异步方法，那么如果后者连接的速度较前者快，则可能会造成“脏连接”，故不可采用异步的方法
                    //SocketAsyncEventArgs ConnectAsyncEvent = new SocketAsyncEventArgs();
                    //ConnectAsyncEvent.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectAsyncEvent_Completed);
                    //ConnectAsyncEvent.UserToken = socketClient;
                    //ConnectAsyncEvent.AcceptSocket = socketClient;
                    //ConnectAsyncEvent.RemoteEndPoint = server;
                    //socketClient.ConnectAsync(ConnectAsyncEvent);
                }
                catch (Exception ex)
                {
                    throw new Exception("服务器[" + ip.ToString() + "]不在线，或者启用了防火墙：" + ex.Message);
                }
            //}
            //else
            //{
            //    IPEndPoint ipep = (IPEndPoint)socketClient.RemoteEndPoint;
            //    if (ipep.Address == server.Address && ipep.Port == server.Port)
            //    {
            //        try
            //        {
            //            socketClient.Disconnect(true);
            //            socketClient.Connect(server);
            //            connect = true;
            //        }
            //        catch (Exception ex)
            //        {
            //            throw new Exception("套接字重连服务器[" + server + "]时出错：" + ex.Message);
            //        }
            //    }
            //    else
            //    {
            //        try
            //        {
            //            socketClient.Disconnect(true);
            //            socketClient.Connect(ip, port);
            //            server = new IPEndPoint(ip, port);
            //            connect = true;
            //        }
            //        catch (Exception ex)
            //        {
            //            throw new Exception("套接字再次连接服务器[" + server + "]时出错：" + ex.Message);
            //        }
            //    }
            //}
        }

        private void Connect(string host)
        {
            //if (socketClient == null)
            //{
                try
                {
                    connect = false;
                    socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socketClient.ReceiveTimeout = ReceiveTimeout;
                    socketClient.Connect(host, port);
                    server = new IPEndPoint(((IPEndPoint)socketClient.RemoteEndPoint).Address, port);
                    connect = true;

                    //要采用以上同步方法来连接，是因为担心同时有多个客户端发送连接请求的时候，保证每个连接的独立，因为socketClient是共用的对象，若是采用异步方法，那么如果后者连接的速度较前者快，则可能会造成“脏连接”，故不可采用异步的方法
                    //SocketAsyncEventArgs ConnectAsyncEvent = new SocketAsyncEventArgs();
                    //ConnectAsyncEvent.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectAsyncEvent_Completed);
                    //ConnectAsyncEvent.UserToken = socketClient;
                    //ConnectAsyncEvent.AcceptSocket = socketClient;
                    //ConnectAsyncEvent.RemoteEndPoint = server;
                    //socketClient.ConnectAsync(ConnectAsyncEvent);
                }
                catch (Exception ex)
                {
                    throw new Exception("服务器[" + host + "]不在线，或者启用了防火墙：" + ex.Message);
                }
            //}
            //else
            //{
            //    IPEndPoint ipep = (IPEndPoint)socketClient.RemoteEndPoint;
            //    if (ipep.Address == server.Address && ipep.Port == server.Port)
            //    {
            //        if (!socketClient.Connected)
            //        {
            //            try
            //            {
            //                socketClient.Disconnect(true);
            //                socketClient.Connect(host, port);
            //                connect = true;
            //            }
            //            catch (Exception ex)
            //            {
            //                throw new Exception("套接字重连服务器[" + host + ":" + port + "]时出错：" + ex.Message);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        try
            //        {
            //            socketClient.Disconnect(true);
            //            socketClient.Connect(host, port);
            //            server = new IPEndPoint(((IPEndPoint)socketClient.RemoteEndPoint).Address, port);
            //            connect = true;
            //        }
            //        catch (Exception ex)
            //        {
            //            throw new Exception("套接字再次连接服务器[" + host + ":" + port + "]时出错：" + ex.Message);
            //        }
            //    }
            //}
        }

        private void CreateBroadcast()
        {
            try
            {
                connect = false;
                server = new IPEndPoint(IPAddress.Broadcast, port);
                socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socketClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                //socketClient.ReceiveTimeout = ReceiveTimeout;
                connect = true;
            }
            catch (Exception ex)
            {
                throw new Exception("构造广播对象失败：" + ex.Message);
            }
        }

        private void ConnectAsyncEvent_Completed(object sender, SocketAsyncEventArgs e)
        {
            connect = true;
        }

        private void NewThread()
        {
            CloseThread();
            //if (threadclient == null)
            //{
                //开启一个线程实例
                threadclient = new Thread(Receive);
                //设为后台线程
                threadclient.IsBackground = true;
                //启动线程
                threadclient.Start();
            //}
        }

        public void Disconnect()
        {
            if (socketClient != null && socketClient.Connected)
            {
                CloseThread();
            }
        }

        private void CloseThread()
        {
            connect = false;
            if (threadclient != null && threadclient.ThreadState != ThreadState.Aborted)
            {
                try
                {
                    threadclient.Abort();
                    threadclient.Join();
                }
                catch (Exception e)
                {
                    //ShowMsg(e.Message);
                }
            }
        }

        //线程中用于接收数据的方法
        private void Receive()
        {
            while (connect)
            {
                if (!connect)
                {
                    break;
                }
                if (socketClient.Available > 0)
                {
                    try
                    {
                        //接收数据
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
                        byte[] data = append.ToArray();
                        //bool crc = CommonLib.Common.CRC16(raw, append.Count);
                        //if (!crc)
                        //    continue;
                        if (Receiving != null)
                            Receiving(socketClient.RemoteEndPoint, data);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("接收服务器的答复时出错：" + e.Message);
                        //connect = false;
                        //ShowMsg("-----服务器已经断开连接！-----");
                        //socketClient.Close();
                        ////结束当前线程
                        //Thread.CurrentThread.Abort();
                    }
                    finally
                    {
                        //socketClient.Close();
                        //结束当前线程
                        //Thread.CurrentThread.Abort();
                    }
                }
            }
        }

        /// <summary>
        /// 往客户端发送数据
        /// </summary>
        /// <param name="clientType"></param>
        /// <param name="msg"></param>
        /// <param name="ip"></param>
        public void Send<T>(ClientType clientType, T msg, IPAddress ip) where T : BaseMessage
        {
            try
            {
                Connect(ip);
                NewThread();
                byte[] arrMsg = Common.GetData<T>(clientType, msg);
                socketClient.Send(arrMsg);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 往客户端发送数据
        /// </summary>
        /// <param name="clientType"></param>
        /// <param name="msg"></param>
        /// <param name="host"></param>
        public void Send<T>(ClientType clientType, T msg, string host) where T : BaseMessage
        {
            try
            {
                Connect(host);
                NewThread();
                byte[] arrMsg = Common.GetData<T>(clientType, msg);
                socketClient.Send(arrMsg);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// 广播消息数据
        /// </summary>
        /// <param name="clientType"></param>
        /// <param name="msg"></param>
        public void Send<T>(ClientType clientType, T msg) where T : BaseMessage
        {
            try
            {
                CreateBroadcast();
                byte[] arrMsg = Common.GetData<T>(clientType, msg);
                socketClient.SendTo(arrMsg, server);
                socketClient.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 往客户端发送数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ip"></param>
        public void Query(Guid id, IPAddress ip)
        {
            try
            {
                Connect(ip);
                NewThread();
                byte[] data = Common.GetData(id);
                byte[] buffer = new byte[data.Length + 1];
                buffer[0] = (byte)ClientType.Consumer;
                Array.Copy(data, 0, buffer, 1, data.Length);
                socketClient.Send(buffer);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 往客户端发送数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="host"></param>
        public void Query(Guid id, string host)
        {
            try
            {
                Connect(host);
                NewThread();
                byte[] data = Common.GetData(id);
                byte[] buffer = new byte[data.Length + 1];
                buffer[0] = (byte)ClientType.Consumer;
                Array.Copy(data, 0, buffer, 1, data.Length);
                socketClient.Send(buffer);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// 广播消息数据
        /// </summary>
        /// <param name="id"></param>
        public void Query(Guid id)
        {
            try
            {
                CreateBroadcast();
                byte[] data = Common.GetData(id);
                byte[] buffer = new byte[data.Length + 1];
                buffer[0] = (byte)ClientType.Consumer;
                Array.Copy(data, 0, buffer, 1, data.Length);
                socketClient.Send(buffer);
                socketClient.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 往客户端发送数据
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="ip"></param>
        public void Query(string clientId, IPAddress ip)
        {
            try
            {
                Connect(ip);
                NewThread();
                byte[] data = Common.GetBytes(clientId, Encoding.UTF8);
                byte[] buffer = new byte[data.Length + 1];
                buffer[0] = (byte)ClientType.Consumer;
                Array.Copy(data, 0, buffer, 1, data.Length);
                socketClient.Send(buffer);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 往客户端发送数据
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="host"></param>
        public void Query(string clientId, string host)
        {
            try
            {
                Connect(host);
                NewThread();
                byte[] data = Common.GetBytes(clientId, Encoding.UTF8);
                byte[] buffer = new byte[data.Length + 1];
                buffer[0] = (byte)ClientType.Consumer;
                Array.Copy(data, 0, buffer, 1, data.Length);
                socketClient.Send(buffer);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// 广播消息数据
        /// </summary>
        /// <param name="clientId"></param>
        public void Query(string clientId)
        {
            try
            {
                CreateBroadcast();
                byte[] data = Common.GetBytes(clientId, Encoding.UTF8);
                byte[] buffer = new byte[data.Length + 1];
                buffer[0] = (byte)ClientType.Consumer;
                Array.Copy(data, 0, buffer, 1, data.Length);
                socketClient.Send(buffer);
                socketClient.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 判断接收方是否在线
        /// </summary>
        /// <returns></returns>
        public bool IsTheServerOnline()
        {
            try
            {
                byte[] hb = Array.ConvertAll<char, byte>(HeartBeat.ToCharArray(), a => (byte)a);
                socketClient.Send(hb);
                return true;
            }
            catch (SocketException e)
            {
                //ShowMsg(e.Message);
            }
            return false;
        }
    }
}
