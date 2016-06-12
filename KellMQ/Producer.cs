using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace KellMQ
{
    [Serializable]
    public class Producer : IDisposable
    {
        //Sender.Sender send;
        //public event EventHandler<MessageArgs> Submited;
        CommunicationClient send;

        public Producer(int port, string serviceName)
        {
            //send = new Sender.Sender(ClientType.Producer);
            //send.Reply += new ReceiveHandler(Sender_Reply);
            send = new CommunicationClient(ClientType.Producer, port, serviceName);
        }

        public Producer(IPAddress ip, int port, string serviceName)
        {
            //send = new Sender.Sender(ClientType.Producer);
            //send.Reply += new ReceiveHandler(Sender_Reply);
            send = new CommunicationClient(ClientType.Producer, ip, port, serviceName);
        }

        public bool SendMsg<T>(T msg) where T : BaseMessage
        {
            return send.Send<T>(msg);
        }

        //public bool SendMsg<T>(T msg, string ip) where T : BaseMessage
        //{
        //    return send.Send<T>(msg, ip);
        //}

        //void Sender_Reply(EndPoint client, byte[] data)
        //{
        //    IPEndPoint ep = client as IPEndPoint;
        //    if (ep != null)
        //    {
        //        Guid id = Common.GetGuid(Common.GetBytes(data, 0, Common.MsgIDSize));
        //        if (id != Guid.Empty)
        //        {
        //            Logs.Create("消息[" + id + "]已插入消息队列[" + ep.Address + "]中！");
        //            if (Submited != null)
        //                Submited(this, new MessageArgs(id));
        //        }
        //        else
        //        {
        //            Logs.Create("无法确定消息插入消息队列[" + ep.Address + "]中是否成功，返回的值为：[" + Common.GetString(data, 0, Encoding.UTF8) + "]");
        //            if (Submited != null)
        //                Submited(this, new MessageArgs(Guid.Empty));
        //        }
        //    }
        //}

        public void Dispose()
        {
            send.Dispose();
        }
    }
}
