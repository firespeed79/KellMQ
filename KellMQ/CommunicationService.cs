using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Media;
using System.Configuration;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections;

namespace KellMQ
{
    public class CommunicationService
    {
        static BroadcastEvent broadcast;
        static BroadcastEvent broadcast2;
        static RemoteObjectWrapper wrapperRemoteObject;
        static RemoteObjectWrapper wrapperRemoteObject2;
        static ClonableMessageQueueEvent ev;
        static ClonableMessageTrunkQuery mq;
        /// <summary>
        /// 订阅型事件
        /// </summary>
        public static ReceiveHandler Received;
        /// <summary>
        /// 消费型事件
        /// </summary>
        public static ReceiveHandler Received2;

        public static bool Start()
        {
            try
            {
                ev = ClonableMessageQueueEvent.GlobalQueue;
                ev.Init();
                mq = ClonableMessageTrunkQuery.GlobalQueue;
                string fullUri = ev.Uri.AbsoluteUri;//"tcp://" + serv.ToString() + ":" + localPort + "/" + service;
                wrapperRemoteObject = new RemoteObjectWrapper();
                wrapperRemoteObject.WrapperBroadcastMessageEvent += new EventHandler<BroadcastEventArgs>(BroadcastEvent_Received);
                broadcast = (BroadcastEvent)Activator.GetObject(typeof(BroadcastEvent), fullUri);
                broadcast.Broadcasting += new EventHandler<BroadcastEventArgs>(wrapperRemoteObject.WrapperBroadMessage);
                Console.WriteLine("初始化订阅型消息服务成功！");
                string fullUri2 = mq.Uri.AbsoluteUri;//"tcp://" + serv.ToString() + ":" + localPort + "/" + service;
                wrapperRemoteObject2 = new RemoteObjectWrapper();
                wrapperRemoteObject2.WrapperBroadcastMessageEvent += new EventHandler<BroadcastEventArgs>(BroadcastEvent2_Received);
                broadcast2 = (BroadcastEvent)Activator.GetObject(typeof(BroadcastEvent), fullUri2);
                broadcast2.Broadcasting += new EventHandler<BroadcastEventArgs>(wrapperRemoteObject2.WrapperBroadMessage);
                Console.WriteLine("初始化消费型消息服务成功！");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("开始服务监听失败：" + e.Message);
                Logs.Create("开始服务监听失败：" + e.Message);
                return false;
            }
        }

        static void BroadcastEvent_Received(object sender, BroadcastEventArgs arg)
        {
            if (Received != null)
                Received(arg.Client, arg.Data);
            if (arg.Data != null)
            {
                ClonableObject msg = Common.GetMsgClonable<ClonableObject>(arg.Data);
                if (msg != null)
                {
                    ev.Enqueue(msg);
                    Console.WriteLine("接收到来自客户端[" + arg.Client + "]的订阅型消息：" + msg.GetHashCode() + "，并插入队列成功！");
                }
                else
                {
                    string clientId = Common.GetString(arg.Data, 0, Encoding.UTF8);
                    if (clientId != null)
                    {
                        Console.WriteLine("接收到客户端[" + clientId + "]的订阅型消息的订阅请求！");
                    }
                }
            }
        }

        static void BroadcastEvent2_Received(object sender, BroadcastEventArgs arg)
        {
            if (Received2 != null)
                Received2(arg.Client, arg.Data);
            if (arg.Data != null)
            {
                ClonableObject msg = Common.GetMsgClonable<ClonableObject>(arg.Data);
                if (msg != null)
                {
                    if (mq.Insert(msg))
                        Console.WriteLine("接收到来自客户端[" + arg.Client + "]的消费型消息：" + msg.GetHashCode() + "，并插入队列成功！");
                    else
                        Console.WriteLine("接收到来自客户端[" + arg.Client + "]的消费型消息：" + msg.GetHashCode() + "，但插入队列失败！");
                }
                else
                {
                    string clientId = Common.GetString(arg.Data, 0, Encoding.UTF8);
                    if (clientId != null)
                    {
                        Console.WriteLine("接收到来自客户端[" + clientId + "]的消费型消息的消费请求！");
                        //List<KellPersistence.Data<ClonableObject>> msgs = mq.Query(clientId);
                        //if (mq.Reply(arg, msgs))
                        //    Console.WriteLine("回复客户端[" + clientId + "]消费请求成功！");
                        //else
                        //    Console.WriteLine("回复客户端[" + clientId + "]消费请求失败！");
                    }
                }
            }
        }

        public static bool Stop()
        {
            try
            {
                if (broadcast != null)
                {
                    wrapperRemoteObject.WrapperBroadcastMessageEvent -= new EventHandler<BroadcastEventArgs>(BroadcastEvent_Received);
                    broadcast.Broadcasting -= new EventHandler<BroadcastEventArgs>(wrapperRemoteObject.WrapperBroadMessage);
                    if (ev.Register.Channels.Count > 0)
                    {
                        foreach (IChannel ch in ev.Register.Channels)
                        {
                            ChannelServices.UnregisterChannel(ch);
                        }
                    }
                    ev.Dispose();
                    Console.WriteLine("停止订阅型消息服务成功！");
                }
                if (broadcast2 != null)
                {
                    wrapperRemoteObject2.WrapperBroadcastMessageEvent -= new EventHandler<BroadcastEventArgs>(BroadcastEvent2_Received);
                    broadcast2.Broadcasting -= new EventHandler<BroadcastEventArgs>(wrapperRemoteObject2.WrapperBroadMessage);
                    if (mq.Register.Channels.Count > 0)
                    {
                        foreach (IChannel ch in mq.Register.Channels)
                        {
                            ChannelServices.UnregisterChannel(ch);
                        }
                    }
                    mq.Dispose();
                    Console.WriteLine("停止消费型消息服务成功！");
                }
                return true;
            }
            catch (Exception e) { Logs.Create("停止服务监听失败：" + e.Message); Console.WriteLine("停止服务出错：" + e.Message); }
            return false;
        }
    }
}
