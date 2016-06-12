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
    public class Consumer : IDisposable, ISubscribe
    {
        public event EventHandler<MessageArgs> Submited;
        CommunicationClient send;

        public Consumer(string serviceName)
        {
            send = new CommunicationClient(ClientType.Consumer, serviceName);
            send.Subscribe += new SubscribeHandler(send_Subscribe);
        }

        void send_Subscribe(IPEndPoint client, BaseMessage msg)
        {
            IPEndPoint ep = client as IPEndPoint;
            if (ep != null)
            {
                if (msg != null)
                {
                    Logs.Create("消息[" + msg.ID + "]已从消息队列[" + ep.Address + "]中消费掉！");
                    if (Submited != null)
                        Submited(this, new MessageArgs(msg.ID));
                }
                else
                {
                    Logs.Create("无法确定消息是否已从消息队列[" + ep.Address + "]中消费掉，返回的值为空！");
                    if (Submited != null)
                        Submited(this, new MessageArgs(Guid.Empty));
                }
            }
        }

        public bool SendMsg(string clientId)
        {
            return send.Query(clientId);
        }

        public bool SendMsg(Guid id)
        {
            return send.Query(id);
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
