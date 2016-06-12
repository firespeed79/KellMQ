using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace KellMQ.Sender
{
    public class Sender
    {
        public event ReceiveHandler Reply;
        Client client;
        ClientType clientType;

        public ClientType ClientType
        {
            get { return clientType; }
        }

        public Sender(ClientType clientType)
        {
            this.clientType = clientType;
            client = new Client();
            client.Receiving += new ReceiveHandler(client_Receiving);
        }
        /// <summary>
        /// 往指定的队列插入消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool Send<T>(T msg, string ip) where T : BaseMessage
        {
            try
            {
                bool isHostName;
                if (Common.IsValidHostOrAddress(ip, out isHostName))
                {
                    if (isHostName)
                    {
                        client.Send<T>(clientType, msg, ip);
                    }
                    else
                    {
                        client.Send<T>(clientType, msg, IPAddress.Parse(ip));
                    }
                    return true;
                }
                return false;
            }
            catch (Exception e) { throw new Exception("发送消息失败(也许服务器[" + ip + "]尚未监听)：" + e.Message); }
        }
        /// <summary>
        /// 广播插入消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Send<T>(T msg) where T : BaseMessage
        {
            try
            {
                client.Send<T>(clientType, msg);
                return true;
            }
            catch (Exception e) { throw e; }
        }
        /// <summary>
        /// 往指定的队列查询
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool Query(Guid id, string ip)
        {
            try
            {
                bool isHostName;
                if (Common.IsValidHostOrAddress(ip, out isHostName))
                {
                    if (isHostName)
                    {
                        client.Query(id, ip);
                    }
                    else
                    {
                        client.Query(id, IPAddress.Parse(ip));
                    }
                    return true;
                }
                return false;
            }
            catch (Exception e) { throw new Exception("发送消息失败(也许客户端[" + ip + "]尚未监听)：" + e.Message); }
        }
        /// <summary>
        /// 广播查询
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Query(Guid id)
        {
            try
            {
                client.Query(id);
                return true;
            }
            catch (Exception e) { throw e; }
        }
        /// <summary>
        /// 往指定的队列查询
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool Query(string clientId, string ip)
        {
            try
            {
                bool isHostName;
                if (Common.IsValidHostOrAddress(ip, out isHostName))
                {
                    if (isHostName)
                    {
                        client.Query(clientId, ip);
                    }
                    else
                    {
                        client.Query(clientId, IPAddress.Parse(ip));
                    }
                    return true;
                }
                return false;
            }
            catch (Exception e) { throw new Exception("发送消息失败(也许客户端[" + ip + "]尚未监听)：" + e.Message); }
        }
        /// <summary>
        /// 广播查询
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public bool Query(string clientId)
        {
            try
            {
                client.Query(clientId);
                return true;
            }
            catch (Exception e) { throw e; }
        }

        void client_Receiving(EndPoint client, byte[] data)
        {
            if (Reply != null)
                Reply(client, data);
        }
    }
}
