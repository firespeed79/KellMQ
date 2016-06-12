using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Configuration;

namespace KellMQ
{
    [Serializable]
    public class CommunicationClient : IDisposable, ISubscribe
    {
        Client<BroadcastEvent> send;
        int port = 8888;
        ClientType ct;

        public event SubscribeHandler Subscribe;
        private IPAddress server;

        public CommunicationClient(ClientType ct, string serviceName)
        {
            this.ct = ct;
            string p = ConfigurationManager.AppSettings["port"];
            if (!string.IsNullOrEmpty(p))
            {
                int RET;
                if (int.TryParse(p, out RET))
                    port = RET;
            }
            send = new Client<BroadcastEvent>(IPEP, serviceName);
        }

        public CommunicationClient(ClientType ct, int port, string serviceName)
        {
            this.ct = ct;
            this.port = port;
            send = new Client<BroadcastEvent>(IPEP, serviceName);
        }

        public CommunicationClient(ClientType ct, IPAddress server, int port, string serviceName)
        {
            this.ct = ct;
            this.server = server;
            this.port = port;
            send = new Client<BroadcastEvent>(IPEP, serviceName);
        }

        public IPEndPoint IPEP
        {
            get
            {
                if (server == null)
                {
                    string serverStr = ConfigurationManager.AppSettings["server"];
                    IPAddress RETIP;
                    if (!string.IsNullOrEmpty(serverStr) && IPAddress.TryParse(serverStr, out RETIP))
                    {
                        server = RETIP;
                    }
                    else
                    {
                        IPAddress ip = IPAddress.Loopback;
                        List<IPAddress> ips = KellMQ.Common.GetIPsV4();
                        if (ips.Count > 0)
                        {
                            ip = ips[0];
                        }
                        server = ip;
                    }
                }
                IPEndPoint ipep = new IPEndPoint(server, port);
                return ipep;
            }
        }

        public bool Query(Guid id)
        {
            if (send.SharedObject != null)
            {
                List<IPAddress> ips = KellMQ.Common.GetIPsV4();
                if (ips.Count > 0)
                {
                    send.SharedObject.Broadcast(new KellMQ.BroadcastEventArgs(new IPEndPoint(ips[0], port), KellMQ.Common.GetData(id)));
                    return true;
                }
                else
                {
                    Logs.Create("当前客户端机器找不到网卡，无法发送消息到远程节点！");
                }
            }
            return false;
        }

        public bool Query(string clientId)
        {
            if (send.SharedObject != null)
            {
                List<IPAddress> ips = KellMQ.Common.GetIPsV4();
                if (ips.Count > 0)
                {
                    send.SharedObject.Broadcast(new KellMQ.BroadcastEventArgs(new IPEndPoint(ips[0], port), KellMQ.Common.GetBytes(clientId, Encoding.UTF8)));
                    return true;
                }
                else
                {
                    Logs.Create("当前客户端机器找不到网卡，无法发送消息到远程节点！");
                }
            }
            return false;
        }

        public bool Send<T>(T msg) where T : BaseMessage
        {
            if (send.SharedObject != null)
            {
                List<IPAddress> ips = KellMQ.Common.GetIPsV4();
                if (ips.Count > 0)
                {
                    send.SharedObject.Broadcast(new KellMQ.BroadcastEventArgs(new IPEndPoint(ips[0], port), KellMQ.Common.GetData(ct, msg)));
                    return true;
                }
                else
                {
                    Logs.Create("当前客户端机器找不到网卡，无法发送消息到远程节点！");
                }
            }
            return false;
        }

        public void Dispose()
        {
            if (this.send.SharedObject.Channel != null)
            {
                try
                {
                    ChannelServices.UnregisterChannel(this.send.SharedObject.Channel);
                }
                catch (Exception e)
                {
                    Logs.Create("释放客户端通讯对象失败:" + e.Message);
                }
            }
        }

        public void SubscrMessage()
        {
            this.send.SharedObject.Broadcasting += SubscribeHanler;
        }

        public void CancelSubscribeMessage()
        {
            this.send.SharedObject.Broadcasting -= SubscribeHanler;
        }

        public void SubscribeHanler(object sender, BroadcastEventArgs e)
        {
            if (Subscribe != null)
            {
                BaseMessage msg = Common.GetMsg<BaseMessage>(e.Data);
                Subscribe(e.Client, msg);
            }
        }
    }
}
