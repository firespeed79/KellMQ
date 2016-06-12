using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Media;
using System.Configuration;

namespace KellMQ.Receiver
{
    public class Receiver
    {
        static Server server;
        public static event OfflineHandler ClientOffline;
        public static event ReceiveHandler Received;

        public static bool IsRunning
        {
            get
            {
                if (server != null)
                    return server.Listening;
                return false;
            }
        }
        public static bool Start()
        {
            try
            {
                //string broadcast = ConfigurationManager.AppSettings["broadcast"];
                if (server == null)
                {
                    server = new Server();
                    server.Receiving += new ReceiveHandler(server_Receiving);
                    //if (string.IsNullOrEmpty(broadcast) || broadcast == "0")
                        server.Offline += new OfflineHandler(server_Offline);
                }
                //if (!string.IsNullOrEmpty(broadcast) && broadcast == "1")
                //    server.StartBind();
                //else
                    server.StartListen();
                return true;
            }
            catch (Exception e) { Logs.Create("开始服务监听失败：" + e.Message); return false; }
        }

        static void server_Offline(object sender, System.Net.EndPoint client)
        {
            IPEndPoint ep = client as IPEndPoint;
            if (ep != null)
                Logs.Create("与客户端[" + ep.Address + "]的连接已断开！");
            if (ClientOffline != null)
                ClientOffline(sender, client);
        }

        static void server_Receiving(System.Net.EndPoint client, byte[] data)
        {
            if (Received != null)
                Received(client, data);
            if (data != null && data.Length > 0)
            {
                ClientType ct = (ClientType)Enum.ToObject(typeof(ClientType), data[0]);
                if (ct == ClientType.Producer)
                {
                    //将收到的消息保存到本地消息队列，并回复生产者已经插入消息的ID
                    //Type type;
                    object obj = Common.GetMsg(data);//, out type);
                    try
                    {
                        BaseMessage msg = Newtonsoft.Json.JsonConvert.DeserializeObject<BaseMessage>(obj.ToString());//不能反序列化为抽象类或者接口！！！
                        if (msg != null)
                        {
                            KellMQ.MessageQueueQuery.GlobalQueue.Add(msg.ID, msg);
                            byte[] idBytes = msg.ID.ToByteArray();
                            Send(client, idBytes);
                        }
                        else
                        {
                            Send(client, Common.GetBytes(Common.Received, Encoding.UTF8));
                        }
                    }
                    catch (Exception e)
                    {
                        Logs.Create("将收到的消息保存到本地消息队列，并回复生产者[" + ((IPEndPoint)client).ToString() + "]将消息插入消息队列时出错：" + e.Message);
                        Send(client, Common.GetBytes(Common.Error + e.Message, Encoding.UTF8));
                    }
                }
                else if (ct == ClientType.Consumer)
                {
                    byte[] buffer = new byte[data.Length - 1];
                    Array.Copy(data, 1, buffer, 0, buffer.Length);
                    //收到获取消息的命令，到本地消息队列找出对应的消息，并返回给消费者 
                    Guid id = Common.GetGuid(buffer);
                    if (id != Guid.Empty)
                    {
                        try
                        {
                            BaseMessage msg = KellMQ.MessageQueueQuery.GlobalQueue.Query(id);
                            if (msg != null)
                            {
                                byte[] msgBytes = Common.Serialize<BaseMessage>(msg);
                                Send(client, msgBytes);
                            }
                            else
                            {
                                Send(client, Common.GetBytes(Common.Received + id, Encoding.UTF8));
                            }
                        }
                        catch (Exception e)
                        {
                            Logs.Create("收到查询消息的命令[id=" + id + "]，到本地消息队列找出对应的消息，并返回给消费者[" + ((IPEndPoint)client).ToString() + "]时出错：" + e.Message);
                            Send(client, Common.GetBytes(Common.Error + e.Message, Encoding.UTF8));
                        }
                    }
                    else
                    {
                        string clientId = Common.GetString(data, 1, Encoding.UTF8);
                        try
                        {
                            List<BaseMessage> msgs = KellMQ.MessageQueueQuery.GlobalQueue.Query(clientId);
                            if (msgs.Count > 0)
                            {
                                foreach (BaseMessage msg in msgs)
                                {
                                    byte[] msgBytes = Common.Serialize<BaseMessage>(msg);
                                    Send(client, msgBytes);
                                }
                            }
                            else
                            {
                                Send(client, Common.GetBytes(Common.Received + clientId, Encoding.UTF8));
                            }
                        }
                        catch (Exception e)
                        {
                            Logs.Create("收到查询消息的命令[clientId=" + clientId + "]，到本地消息队列找出对应的消息，并返回给消费者[" + ((IPEndPoint)client).ToString() + "]时出错：" + e.Message);
                            Send(client, Common.GetBytes(Common.Error + e.Message, Encoding.UTF8));
                        }
                    }
                }
            }
        }

        public static bool Stop()
        {
            if (server != null)
            {
                try
                {
                    //string broadcast = ConfigurationManager.AppSettings["broadcast"];
                    //if (!string.IsNullOrEmpty(broadcast) && broadcast == "1")
                    //    server.StopBind();
                    //else
                        server.StopListen();
                        KellMQ.MessageQueueQuery.GlobalQueue.Dispose();
                    return true;
                }
                catch (Exception e) { Logs.Create("停止服务监听失败：" + e.Message); }
            }
            return false;
        }

        internal static void Send(EndPoint client, byte[] data)
        {
            if (server != null)
            {
                try
                {
                    server.SendTo(client, data);
                }
                catch (Exception e) { Logs.Create("发送数据失败：" + e.Message); }
            }
        }

        internal static void Send(EndPoint client, string msg)
        {
            if (server != null)
            {
                try
                {
                    byte[] data = Common.GetBytes(msg, Encoding.UTF8);
                    server.SendTo(client, data);
                }
                catch (Exception e) { Logs.Create("发送回复信息失败：" + e.Message); }
            }
        }
    }
}
