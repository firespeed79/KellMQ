using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Configuration;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Lifetime;
using System.Collections;
using System.IO;

namespace KellMQ
{
    /// <summary>
    /// 广播事件参数类
    /// </summary>
    [Serializable]
    public class BroadcastEventArgs : EventArgs
    {
        IPEndPoint client;
        /// <summary>
        /// 消息来源
        /// </summary>
        public IPEndPoint Client
        {
            get { return client; }
            set { client = value; }
        }
        byte[] data;
        /// <summary>
        /// 消息数据
        /// </summary>
        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }
        IChannel channel;
        /// <summary>
        /// Remoting通道
        /// </summary>
        public IChannel Channel
        {
            get { return channel; }
            set { channel = value; }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        public BroadcastEventArgs(IPEndPoint client, byte[] data)
        {
            this.client = client;
            this.data = data;
        }
    }

    /// <summary>
    /// 远程对象接口
    /// </summary>
    public interface IRemoteObject
    {
        /// <summary>
        /// 广播事件
        /// </summary>
        event EventHandler<BroadcastEventArgs> Broadcasting;
        /// <summary>
        /// 发送广播
        /// </summary>
        /// <param name="arg"></param>
        void Broadcast(BroadcastEventArgs arg);
    }

    /// <summary>
    /// 订阅消息的客户端接口
    /// </summary>
    public interface ISubscribe
    {
        void SubscrMessage();
        void CancelSubscribeMessage();
    }

    /// <summary>
    /// 广播事件类(终极类，即不可被继承)
    /// </summary>
    [Serializable]
    public sealed class BroadcastEvent : MarshalByRefObject, IRemoteObject
    {
        #region IRemoteObject 成员
        /// <summary>
        /// 广播事件
        /// </summary>
        public event EventHandler<BroadcastEventArgs> Broadcasting;

        /// <summary>
        /// 发送广播
        /// </summary>
        /// <param name="arg"></param>
        public void Broadcast(BroadcastEventArgs arg)
        {
            if (Broadcasting != null)
            {
                EventHandler<BroadcastEventArgs> tmpEvent = null;
                IEnumerator enumerator = Broadcasting.GetInvocationList().GetEnumerator() ;
                while(enumerator.MoveNext())
                {
                    Delegate handler = (Delegate)enumerator.Current;
                    try
                    {
                        tmpEvent = (EventHandler<BroadcastEventArgs>)handler;
                        arg.Channel = this.channel;
                        tmpEvent(this, arg);
                    }
                    catch (Exception e)
                    {
                        Logs.Create("触发Broadcast时出错：" + e.Message);//错误信息：在分析完成之前就遇到流结尾。
                        //注册的事件处理程序出错，删除
                        Broadcasting -= tmpEvent;
                    }
                }
            }
        }

        #endregion

        private IChannel channel;

        public IChannel Channel
        {
            get { return channel; }
            set { channel = value; }
        }

        /// <summary>
         /// 重载InitializeLifetimeService
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
            //ILease aLease = (ILease)base.InitializeLifetimeService();
            //if (aLease.CurrentState == LeaseState.Initial)
            //{
            //    aLease.InitialLeaseTime = TimeSpan.Zero;// 不过期
            //}
            //return aLease;
        }
        /*
         /// <summary>
         /// 事件字典
         /// </summary>
         private Dictionary<string, EventHandler<BroadcastEventArgs>> Handlers = new Dictionary<string, EventHandler<BroadcastEventArgs>>();
 
         /// <summary>
         /// 添加事件
         /// </summary>
         /// <param name="eventName"></param>
         /// <param name="handler"></param>
         public void AddEvent(string eventName, EventHandler<BroadcastEventArgs> handler)
         {
             if (Handlers.ContainsKey(eventName))
             {
                 Handlers[eventName] += handler;
             }
             else
             {
                 Handlers.Add(eventName, handler);
             }
         }
 
         /// <summary>
         /// 清除事件
         /// </summary>
         /// <param name="eventName"></param>
         /// <param name="handler"></param>
         public void RemoveEvent(string eventName, EventHandler<BroadcastEventArgs> handler)
         {
             if (Handlers.ContainsKey(eventName))
             {
                 Handlers[eventName] -= handler;
             }
         }
 
         /// <summary>
         /// 立即广播
         /// </summary>
         /// <param name="eventName"></param>
         /// <param name="arg"></param>
         public void Broadcast(string eventName, BroadcastEventArgs arg)
         {
             if (!string.IsNullOrEmpty(eventName))
             {
                 // 从字典中获取事件
                 EventHandler<BroadcastEventArgs> eh = null;
                 Handlers.TryGetValue(eventName, out eh);
                 // 执行事件
                 if (eh != null)
                 {
                     eh(this, arg);
                 }
             }
         }
         */
    }
    /// <summary>
    /// 远程对象包装类
    /// </summary>
    [Serializable]
    public class RemoteObjectWrapper : MarshalByRefObject
    {
        /// <summary>
        /// 远程对象包装对象的广播消息事件
        /// </summary>
        public event EventHandler<BroadcastEventArgs> WrapperBroadcastMessageEvent;
        /// <summary>
        /// 发送广播消息的包装函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        public void WrapperBroadMessage(object sender, BroadcastEventArgs arg)
        {
            if (WrapperBroadcastMessageEvent != null)
            {
                WrapperBroadcastMessageEvent(this, arg);
            }
        }

        /// <summary>
        /// 重载生命周期函数，使之无限长
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }

    /// <summary>
    /// 服务注册器
    /// </summary>
    public class RegisterServer<T> where T : MarshalByRefObject
    {
        List<Uri> uris;
        /// <summary>
        /// 现有通道中的资源
        /// </summary>
        public List<Uri> Uris
        {
            get { return uris; }
        }
        List<IChannel> channels;
        /// <summary>
        /// 已经注册的通道
        /// </summary>
        public List<IChannel> Channels
        {
            get
            {
                return channels;
            }
        }
        Dictionary<string, ObjRef> regObjects;
        /// <summary>
        /// 当前服务注册器中是否存在正在运行的服务
        /// </summary>
        public bool IsStarted
        {
            get
            {
                return regObjects.Count > 0;
            }
        }

        /// <summary>
        /// 是否已经注册指定的服务
        /// </summary>
        public bool IsRegister(string uri)
        {
            if (regObjects.Count > 0)
            {
                return regObjects.ContainsKey(uri);
            }
            return false;
        }

        /// <summary>
        /// 构造服务的字典容器
        /// </summary>
        public RegisterServer()
        {
            regObjects = new Dictionary<string, ObjRef>();
            uris = new List<Uri>();
            channels = new List<IChannel>();
        }

        /// <summary>
        /// 注销当前服务注册器所有已经正在运行的服务
        /// </summary>
        ~RegisterServer()
        {
            if (regObjects.Count > 0)
            {
                foreach (string key in regObjects.Keys)
                {
                    Unregister(key);
                }
                regObjects.Clear();
            }
            uris.Clear();
            channels.Clear();
        }

        /// <summary>
        /// 往当前服务注册器中注册默认的服务
        /// </summary>
        /// <param name="sharedObject"></param>
        public void Register(T sharedObject)
        {
            if (sharedObject != null)
            {
                ObjRef objRef = RemotingServices.Marshal(sharedObject, typeof(T).Name, typeof(T));
                regObjects.Add(typeof(T).Name, objRef);
            }
        }

        /// <summary>
        /// 往当前服务注册器中注册指定的服务
        /// </summary>
        /// <param name="sharedObject"></param>
        /// <param name="ObjURI"></param>
        public void Register(T sharedObject, string ObjURI)
        {
            if (sharedObject != null)
            {
                ObjRef objRef = RemotingServices.Marshal(sharedObject, ObjURI, typeof(T));
                regObjects.Add(ObjURI, objRef);
            }
        }

        /// <summary>
        /// 在当前服务注册器中注销指定的服务
        /// </summary>
        /// <param name="ObjURI"></param>
        public void Unregister(string ObjURI)
        {
            if (regObjects.ContainsKey(ObjURI))
                RemotingServices.Unmarshal(regObjects[ObjURI]);
        }
    }

    /// <summary>
    /// TCP协议客户端泛型类(服务器是单件模式的发送端)
    /// </summary>
    [Serializable]
    public class Client<T> where T : MarshalByRefObject
    {
        IPAddress server;
        /// <summary>
        /// 事件中心服务器IP地址
        /// </summary>
        public IPAddress Server
        {
            get { return server; }
        }

        T sharedObject;
        /// <summary>
        /// 远程共享的对象
        /// </summary>
        public T SharedObject
        {
            get { return sharedObject; }
        }

        string uri;
        /// <summary>
        /// 当前通讯通道资源
        /// </summary>
        public string Uri
        {
            get { return uri; }
        }
        IPEndPoint ipep;
        /// <summary>
        /// 当前终结点
        /// </summary>
        IPEndPoint IPEP
        {
            get
            {
                return ipep;
            }
        }

        /// <summary>
        /// 默认的构造函数
        /// </summary>
        public Client(IPEndPoint ipep, string serviceName = null)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                string svr = ConfigurationManager.AppSettings["service"];
                if (!string.IsNullOrEmpty(svr))
                    serviceName = svr;
                else
                    serviceName = typeof(T).Name;
            }

            this.ipep = ipep;

            //BinaryServerFormatterSinkProvider serverProv = new BinaryServerFormatterSinkProvider();
            //serverProv.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
            BinaryClientFormatterSinkProvider clientProv = new BinaryClientFormatterSinkProvider();
            //IDictionary props = new Hashtable();
            //props["port"] = 0;
            TcpClientChannel channel = new TcpClientChannel(serviceName + DateTime.Now.Ticks, clientProv);
            //channel = new TcpClientChannel();
            try
            {
                ChannelServices.RegisterChannel(channel, false);

                string fullUri = "tcp://" + ipep.Address.ToString() + ":" + ipep.Port + "/" + serviceName;
                Type t = typeof(T);
                if (RemotingConfiguration.IsWellKnownClientType(t) == null)
                    RemotingConfiguration.RegisterWellKnownClientType(t, fullUri);
                sharedObject = (T)Activator.GetObject(t, fullUri);
                this.uri = fullUri;
                //RemotingConfiguration.RegisterActivatedClientType(typeof(T), fullUri);//按值传送
                //sharedObject = Activator.CreateInstance(typeof(T)) as T;//按值传送
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 指定服务的构造函数
        /// </summary>
        public Client(string serviceName = null)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                string svr = ConfigurationManager.AppSettings["service"];
                if (!string.IsNullOrEmpty(svr))
                    serviceName = svr;
                else
                    serviceName = typeof(T).Name;
            }
            //BinaryServerFormatterSinkProvider serverProv = new BinaryServerFormatterSinkProvider();
            //serverProv.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
            BinaryClientFormatterSinkProvider clientProv = new BinaryClientFormatterSinkProvider();
            //IDictionary props = new Hashtable();
            //props["port"] = 0;
            TcpClientChannel channel = new TcpClientChannel(serviceName + DateTime.Now.Ticks, clientProv);
            //channel = new TcpClientChannel();
            try
            {
                ChannelServices.RegisterChannel(channel, true);

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

                int port = 8888;
                string portStr = ConfigurationManager.AppSettings["port"];
                int RET;
                if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out RET))
                {
                    port = RET;
                }

                string fullUri = "tcp://" + server.ToString() + ":" + port + "/" + serviceName;
                Type t = typeof(T);
                if (RemotingConfiguration.IsWellKnownClientType(t) == null)
                    RemotingConfiguration.RegisterWellKnownClientType(t, fullUri);
                sharedObject = (T)Activator.GetObject(t, fullUri);
                this.uri = fullUri;
                //RemotingConfiguration.RegisterActivatedClientType(typeof(T), fullUri);//按值传送
                //sharedObject = Activator.CreateInstance(typeof(T)) as T;//按值传送
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// 注销信道
        /// </summary>
        ~Client()
        {
            
        }
    }

    /// <summary>
    /// 消息服务类
    /// </summary>
    public static class MessageServer<T> where T : MarshalByRefObject
    {
        /// <summary>
        /// 根据指定的配置文档(ServerCfg.config)启动TCP服务，找不到该配置文件就找app.config
        /// </summary>
        /// <param name="msg"></param>
        public static RegisterServer<T> Init(T msg)
        {
            RegisterServer<T> reg = new RegisterServer<T>();
            try
            {
                if (File.Exists("ServerCfg.config"))
                    RemotingConfiguration.Configure("ServerCfg.config", false);
                else
                    RemotingConfiguration.Configure(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile, false);
                WellKnownServiceTypeEntry swte = new WellKnownServiceTypeEntry(typeof(T), typeof(T).Name, WellKnownObjectMode.Singleton);
                RemotingConfiguration.ApplicationName = typeof(T).Name;
                RemotingConfiguration.RegisterWellKnownServiceType(swte);
                reg.Register(msg);
                IChannel ch = ChannelServices.GetChannel(RemotingConfiguration.ApplicationName);
                TcpChannel tch = ch as TcpChannel;
                if (tch != null)
                {
                    if (!reg.Channels.Contains(tch))
                        reg.Channels.Add(tch);
                    string[] ss = tch.GetUrlsForUri(swte.ObjectUri);
                    foreach (string uri in ss)
                    {
                        if (!reg.Uris.Exists(a => a.AbsoluteUri == uri))
                            reg.Uris.Add(new Uri(uri, UriKind.RelativeOrAbsolute));
                    }
                }
                return reg;
            }
            catch (Exception e)
            {
                Logs.Create("启动消息服务失败：" + e.Message);
            }
            return reg;
        }
        /// <summary>
        /// 根据默认的配置文档(app.config)启动TCP服务
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="port"></param>
        public static RegisterServer<T> Intialize(T msg, string serviceName, int port = 8888)
        {
            RegisterServer<T> reg = new RegisterServer<T>();
            try
            {
                BinaryServerFormatterSinkProvider serverProv = new BinaryServerFormatterSinkProvider();
                serverProv.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
                //BinaryClientFormatterSinkProvider clientProv = new BinaryClientFormatterSinkProvider();
                //IDictionary props = new Hashtable();
                //props["port"] = port;
                TcpServerChannel servChannel = new TcpServerChannel(serviceName, port, serverProv);
                ChannelServices.RegisterChannel(servChannel, false);
                if (!reg.Channels.Contains(servChannel))
                    reg.Channels.Add(servChannel);
                WellKnownServiceTypeEntry swte = new WellKnownServiceTypeEntry(typeof(T), serviceName, WellKnownObjectMode.Singleton);
                //RemotingConfiguration.ApplicationName = serviceName;
                RemotingConfiguration.RegisterWellKnownServiceType(swte);
                reg.Register(msg, serviceName);
                string[] ss = servChannel.GetUrlsForUri(serviceName);
                foreach (string uri in ss)
                {
                    if (!reg.Uris.Exists(a => a.AbsoluteUri == uri))
                        reg.Uris.Add(new Uri(uri, UriKind.RelativeOrAbsolute));
                }
                return reg;
            }
            catch (Exception e)
            {
                Logs.Create("启动服务时出错：" + e.Message);
            }
            return reg;
        }
        /// <summary>
        /// 注销指定的服务
        /// </summary>
        /// <param name="reg"></param>
        public static void Dispose(RegisterServer<T> reg)
        {
            try
            {
                reg.Unregister(typeof(T).Name);
            }
            catch (Exception e)
            {
                Logs.Create("停止消息服务失败：" + e.Message);
            }
        }
    }
}
