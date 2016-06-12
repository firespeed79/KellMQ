using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace KellMQ
{
    [Serializable]
    public class ClonableConsumer : IDisposable, ISubscribe
    {
        public event EventHandler<ClonableMessageArgs> Submited;
        ClonableCommunicationClient send;

        public ClonableConsumer(int port, string serviceName)
        {
            send = new ClonableCommunicationClient(ClientType.Consumer, port, serviceName);
            send.Subscribe += new ClonableSubscribeHandler(send_Subscribe);
        }

        void send_Subscribe(IPEndPoint client, ClonableObject msg)
        {
            IPEndPoint ep = client as IPEndPoint;
            if (ep != null)
            {
                if (msg != null)
                {
                    Logs.Create("消息[" + msg.GetHashCode() + "]已从消息队列[" + ep.Address + "]中消费掉！");
                    if (Submited != null)
                        Submited(this, new ClonableMessageArgs(msg.GetHashCode()));
                }
                else
                {
                    Logs.Create("无法确定消息是否已从消息队列[" + ep.Address + "]中消费掉，返回的值为空！");
                    if (Submited != null)
                        Submited(this, new ClonableMessageArgs(0));
                }
            }
        }

        public bool Query(string clientId)
        {
            return send.Query(clientId);
        }

        public void SubscrMessage()
        {
            send.SubscrMessage();
        }

        public void CancelSubscribeMessage()
        {
            send.CancelSubscribeMessage();
        }

        public void Dispose()
        {
            send.Dispose();
        }
    }
}
