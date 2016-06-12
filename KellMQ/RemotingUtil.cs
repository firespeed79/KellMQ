using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Net;

namespace KellMQ
{
    /// <summary>
    /// Remoteing公共类
    /// </summary>
    public static class RemotingCommon
    {
        /// <summary>
        /// 将指定的对象设置为远程共享对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sharedObject"></param>
        /// <param name="ObjURI"></param>
        /// <returns></returns>
        public static ObjRef SetRemoteSharedObject<T>(T sharedObject, string ObjURI) where T : MarshalByRefObject
        {
            ObjRef objRef = RemotingServices.Marshal(sharedObject, ObjURI, typeof(T));
            return objRef;
        }
        /// <summary>
        /// 将远程代理取消远程共享
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objRef"></param>
        public static void UnsetRemoteSharedObject<T>(ObjRef objRef)
        {
            RemotingServices.Unmarshal(objRef);
        }
    }
    /// <summary>
    /// Tcp通道的Remoting
    /// </summary>
    public static class RemotingTcp
    {
        /// <summary>
        /// 服务器端注册指定的服务
        /// </summary>
        /// <param name="port"></param>
        /// <param name="ensureSecurity"></param>
        public static void RegSvr(int port, bool ensureSecurity)
        {
            //创建通信侦听通道对象
            TcpServerChannel channel = new TcpServerChannel(port);
            //注册通信侦听通道
            ChannelServices.RegisterChannel(channel, ensureSecurity);
        }
        /// <summary>
        /// 客户端注册通信通道
        /// </summary>
        /// <param name="ensureSecurity"></param>
        public static void RegCli(bool ensureSecurity)
        {
            //注册客户端通信通道
            ChannelServices.RegisterChannel(new TcpClientChannel(), ensureSecurity);
        }
        /// <summary>
        /// 服务器端注册远程对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri"></param>
        /// <param name="wkom"></param>
        public static void RegSvrObj<T>(string uri, WellKnownObjectMode wkom)
        {
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(T), uri, wkom);
        }
        /// <summary>
        /// 客户端注册远程对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri"></param>
        public static void RegCliObj<T>(string uri)
        {
            RemotingConfiguration.RegisterWellKnownClientType(typeof(T), uri);
        }
        /// <summary>
        /// 获取远程对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static T GetRemoteObject<T>(IPAddress ip, int port, string uri)
        {
            T t = (T)Activator.GetObject(typeof(T), "tcp://" + ip.ToString() + ":" + port + "/" + uri);
            return t;
        }
    }
    /// <summary>
    /// Http通道的Remoting
    /// </summary>
    public static class RemotingHttp
    {
        /// <summary>
        /// 服务器端注册指定的服务
        /// </summary>
        /// <param name="port"></param>
        public static void RegSvr(int port)
        {
            //创建通信侦听通道对象
            HttpServerChannel channel = new HttpServerChannel(port);
            //注册通信侦听通道
            ChannelServices.RegisterChannel(channel, false);
        }
        /// <summary>
        /// 客户端注册通信通道
        /// </summary>
        public static void RegCli()
        {
            //注册客户端通信通道
            ChannelServices.RegisterChannel(new HttpClientChannel(), false);
        }
        /// <summary>
        /// 服务器端注册远程对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri"></param>
        /// <param name="wkom"></param>
        public static void RegSvrObj<T>(string uri, WellKnownObjectMode wkom)
        {
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(T), uri, wkom);
        }
        /// <summary>
        /// 客户端注册远程对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri"></param>
        public static void RegCliObj<T>(string uri)
        {
            RemotingConfiguration.RegisterWellKnownClientType(typeof(T), uri);
        }
        /// <summary>
        /// 获取远程对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static T GetRemoteObject<T>(IPAddress ip, int port, string uri)
        {
            T t = (T)Activator.GetObject(typeof(T), "http://" + ip.ToString() + ":" + port + "/" + uri);
            return t;
        }
    }
    /// <summary>
    /// Ipc通道的Remoting
    /// </summary>
    public static class RemotingIpc
    {
        /// <summary>
        /// 服务器端注册指定的服务
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="ensureSecurity"></param>
        public static void RegSvr(string portName, bool ensureSecurity)
        {
            //创建通信侦听通道对象
            IpcServerChannel channel = new IpcServerChannel(portName);
            //注册通信侦听通道
            ChannelServices.RegisterChannel(channel, ensureSecurity);
        }
        /// <summary>
        /// 客户端注册通信通道
        /// </summary>
        /// <param name="ensureSecurity"></param>
        public static void RegCli(bool ensureSecurity)
        {
            //注册客户端通信通道
            ChannelServices.RegisterChannel(new IpcClientChannel(), ensureSecurity);
        }
        /// <summary>
        /// 服务器端注册指定的服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri"></param>
        /// <param name="wkom"></param>
        public static void RegSvrObj<T>(string uri, WellKnownObjectMode wkom)
        {
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(T), uri, wkom);
        }
        /// <summary>
        /// 客户端注册指定的服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri"></param>
        public static void RegCliObj<T>(string uri)
        {
            RemotingConfiguration.RegisterWellKnownClientType(typeof(T), uri);
        }
        /// <summary>
        /// 获取远程对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="portName"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static T GetRemoteObject<T>(string portName, string uri)
        {
            T t = (T)Activator.GetObject(typeof(T), "ipc://" + portName + "/" + uri);
            return t;
        }
    }
}
