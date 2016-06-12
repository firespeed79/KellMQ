using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Runtime.Remoting.Channels;
using System.Configuration;

namespace KellMQ
{
    public delegate void ClonableConsumeEventHandler(ClonableObject message);
    public delegate void ClonableConsumingEventHandler(IEnumerable<ClonableObject> messages);

    /// <summary>
    /// 以轮询的方式实现事件驱动的分布式事件队列引擎(且同一个事件处理程序只订阅一次)
    /// </summary>
    public class ClonableMessageQueueEvent : Queue<ClonableObject>, IDisposable
    {
        //bool newCreate;
        //System.Threading.Mutex sync = new System.Threading.Mutex(true, "ClonableMessageQueueEvent", out newCreate);//Mutex用于跨应用程序域，即进程间也可以保持同步
        static ClonableMessageQueueEvent mq;
        /// <summary>
        /// 全局公共消息队列
        /// </summary>
        public static ClonableMessageQueueEvent GlobalQueue
        {
            get
            {
                if (mq == null)
                    mq = new ClonableMessageQueueEvent();

                return mq;
            }
        }
        private Dictionary<string, ClonableConsumeEventHandler> messagesNotifyEvent;
        private Timer timer = new Timer();
        RegisterServer<BroadcastEvent> reg;
        /// <summary>
        /// 通道注册器
        /// </summary>
        public RegisterServer<BroadcastEvent> Register
        {
            get { return reg; }
        }
        IChannel channel;
        Uri uri;
        
        /// <summary>
        /// 私人消息队列
        /// </summary>
        public ClonableMessageQueueEvent()
        {
            BroadcastEvent be = new BroadcastEvent();
            reg = MessageServer<BroadcastEvent>.Intialize(be, ConfigurationManager.AppSettings["service"], Convert.ToInt32(ConfigurationManager.AppSettings["port"]));
            if (reg.Channels.Count > 0)
                this.channel = reg.Channels[0];//.Find(a => a.ChannelName == ConfigurationManager.AppSettings["service"]);
            be.Channel = this.channel;
            if (reg.Uris.Count > 0)
                this.uri = reg.Uris.Find(a => a.Segments[a.Segments.Length - 1] == ConfigurationManager.AppSettings["service"]);
        }
        /// <summary>
        /// 通信通道
        /// </summary>
        public IChannel Channel
        {
            get
            {
                return channel;
            }
        }
        /// <summary>
        /// 远程共享资源
        /// </summary>
        public Uri Uri
        {
            get
            {
                return uri;
            }
        }

        public void Init(double notifyInterval = 1000)
        {
            this.messagesNotifyEvent = new Dictionary<string, ClonableConsumeEventHandler>();
            if (notifyInterval < 20)
                notifyInterval = 20;
            timer.Interval = notifyInterval;
            timer.Elapsed += Notify;
            timer.Start();
        }

        public void SubscrMessage(string clientId, ClonableConsumeEventHandler eventConsume)
        {
            if (eventConsume != null)
            {
                if (this.messagesNotifyEvent != null)
                {
                    if (!this.messagesNotifyEvent[clientId].GetInvocationList().Contains(eventConsume))
                        this.messagesNotifyEvent[clientId] += eventConsume;
                }
                else
                {
                    this.messagesNotifyEvent.Add(clientId, eventConsume);
                }
            }
        }

        public void CancelSubscriMessage(string clientId, ClonableConsumeEventHandler eventConsume)
        {
            if (eventConsume != null)
            {
                if (this.messagesNotifyEvent != null)
                {
                    if (this.messagesNotifyEvent[clientId].GetInvocationList().Contains(eventConsume))
                        this.messagesNotifyEvent[clientId] -= eventConsume;
                }
            }
        }

        private void Notify(object sender, ElapsedEventArgs e)
        {
            lock (this)
            {
                //sync.WaitOne();
                if (messagesNotifyEvent != null)
                {
                    while (this.Count > 0)
                    {
                        var message = this.Dequeue();
                        foreach (string clientId in messagesNotifyEvent.Keys)
                        {
                            if (messagesNotifyEvent.ContainsKey(clientId))
                                messagesNotifyEvent[clientId](message);
                        }
                    }
                }
                //sync.ReleaseMutex();
            }
        }

        public void Dispose()
        {
            if (timer.Enabled)
                timer.Stop();
            if (this.Count > 0)
            {
                Enumerator en = this.GetEnumerator();
                while (en.MoveNext())
                {
                    en.Dispose();
                }
                this.Clear();
            }
        }

        public bool IsDispose
        {
            get
            {
                return !timer.Enabled;
            }
        }
    }

    /// <summary>
    /// 以查询的方式实现分布式事件队列引擎
    /// </summary>
    public class ClonableMessageTrunkQuery : IDisposable
    {
        //bool newCreate;
        //System.Threading.Mutex sync = new System.Threading.Mutex(true, "ClonableMessageTrunkQuery", out newCreate);//Mutex用于跨应用程序域，即进程间也可以保持同步
        static KellPersistence.Trunk<ClonableObject> trunk;
        static ClonableMessageTrunkQuery mq;
        RegisterServer<BroadcastEvent> reg;
        /// <summary>
        /// 获取通道注册器
        /// </summary>
        public RegisterServer<BroadcastEvent> Register
        {
            get { return reg; }
        }
        IChannel channel;
        Uri uri;
        /// <summary>
        /// 全局公共消息队列
        /// </summary>
        public static ClonableMessageTrunkQuery GlobalQueue
        {
            get
            {
                if (mq == null)
                    mq = new ClonableMessageTrunkQuery();

                return mq;
            }
        }
        

        private Dictionary<Guid, ClonableConsumeEventHandler> messageNotifyEvent;
        private Dictionary<Guid, ClonableConsumingEventHandler> messagesNotifyEvent;

        /// <summary>
        /// 私人消息队列
        /// </summary>
        public ClonableMessageTrunkQuery()
        {
            trunk = new KellPersistence.Trunk<ClonableObject>("KellMQ.ClonableObject");
            messageNotifyEvent = new Dictionary<Guid, ClonableConsumeEventHandler>();
            messagesNotifyEvent = new Dictionary<Guid, ClonableConsumingEventHandler>();
            BroadcastEvent be = new BroadcastEvent();
            reg = MessageServer<BroadcastEvent>.Intialize(be, ConfigurationManager.AppSettings["service2"], Convert.ToInt32(ConfigurationManager.AppSettings["port2"]));
            if (reg.Channels.Count > 0)
                this.channel = reg.Channels[0];//.Find(a => a.ChannelName == ConfigurationManager.AppSettings["service2"]);
            be.Channel = this.channel;
            if (reg.Uris.Count > 0)
                this.uri = reg.Uris.Find(a => a.Segments[a.Segments.Length - 1] == ConfigurationManager.AppSettings["service2"]);
        }
        /// <summary>
        /// 通信通道
        /// </summary>
        public IChannel Channel
        {
            get
            {
                return channel;
            }
        }
        /// <summary>
        /// 远程共享资源
        /// </summary>
        public Uri Uri
        {
            get
            {
                return uri;
            }
        }

        public KellPersistence.Data<ClonableObject> Query(Guid id)
        {
            KellPersistence.Data<ClonableObject> data = trunk.Select(id);
            return data;
        }

        public List<KellPersistence.Data<ClonableObject>> Query(string clientId)
        {
            List<KellPersistence.Data<ClonableObject>> datas = trunk.Select(clientId);
            return datas;
        }

        public bool Insert(ClonableObject msg)
        {
            return trunk.Insert(new KellPersistence.Data<ClonableObject>(trunk, msg));
        }

        public void Consume(string client, ClonableConsumingEventHandler eventConsume = null)
        {
            if (trunk.DataCount > 0)
            {
                List<ClonableObject> messages = new List<ClonableObject>();
                List<KellPersistence.Data<ClonableObject>> datas = trunk.Select(client);
                foreach (KellPersistence.Data<ClonableObject> data in datas)
                {
                    if (data.Header.User.Equals(client, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (this.messagesNotifyEvent != null)
                        {
                            if (!messagesNotifyEvent[data.ID].GetInvocationList().Contains(eventConsume))
                                messagesNotifyEvent[data.ID] += eventConsume;
                        }
                        else
                        {
                            messagesNotifyEvent[data.ID] += eventConsume;
                        }
                        if (messagesNotifyEvent[data.ID] != null)
                            messagesNotifyEvent[data.ID](messages);
                        messages.Add(data.Buffer);
                        trunk.Delete(data.ID);
                    }
                }
                if (eventConsume != null)
                    eventConsume(messages);
            }
        }

        public bool Reply(BroadcastEventArgs arg, List<KellPersistence.Data<ClonableObject>> msgs)
        {
            if (true)//待完善...
            {
            foreach (KellPersistence.Data<ClonableObject> msg in msgs)
            {

                trunk.Delete(msg.ID);
            }
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            
        }
    }
}
