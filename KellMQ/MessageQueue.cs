using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Configuration;

namespace KellMQ
{
    public delegate void ConsumeEventHandler(BaseMessage message);
    public delegate void ConsumingEventHandler(IEnumerable<BaseMessage> messages);

    /// <summary>
    /// 以事件驱动的方式实现分布式事件队列引擎(且同一个事件处理程序只订阅一次)
    /// </summary>
    public class MessageQueueEvent : Queue<BaseMessage>, IDisposable
    {
        //bool newCreate;
        //System.Threading.Mutex sync = new System.Threading.Mutex(true, "MessageQueueEvent", out newCreate);//Mutex用于跨应用程序域，即进程间也可以保持同步
        static MessageQueueEvent mq;
        /// <summary>
        /// 全局公共消息队列
        /// </summary>
        public static MessageQueueEvent GlobalQueue
        {
            get
            {
                if (mq == null)
                    mq = new MessageQueueEvent();

                return mq;
            }
        }
        private Dictionary<string, ConsumeEventHandler> messagesNotifyEvent;
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
        public MessageQueueEvent()
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
            this.messagesNotifyEvent = new Dictionary<string, ConsumeEventHandler>();
            if (notifyInterval < 20)
                notifyInterval = 20;
            timer.Interval = notifyInterval;
            timer.Elapsed += Notify;
            timer.Start();
        }

        public void SubscrMessage(string clientId, ConsumeEventHandler eventConsume)
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

        public void CancelSubscriMessage(string clientId, ConsumeEventHandler eventConsume)
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
            MessageServer<BroadcastEvent>.Dispose(reg);
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
    public class MessageQueueQuery : Dictionary<Guid, BaseMessage>, IDisposable
    {
        //bool newCreate;
        //System.Threading.Mutex sync = new System.Threading.Mutex(true, "MessageQueueQuery", out newCreate);//Mutex用于跨应用程序域，即进程间也可以保持同步
        static MessageQueueQuery mq;
        /// <summary>
        /// 全局公共消息队列
        /// </summary>
        public static MessageQueueQuery GlobalQueue
        {
            get
            {
                if (mq == null)
                    mq = new MessageQueueQuery();

                return mq;
            }
        }


        private Dictionary<Guid, ConsumeEventHandler> messageNotifyEvent;
        private Dictionary<Guid, ConsumingEventHandler> messagesNotifyEvent;
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
        public MessageQueueQuery()
        {
            messageNotifyEvent = new Dictionary<Guid, ConsumeEventHandler>();
            messagesNotifyEvent = new Dictionary<Guid, ConsumingEventHandler>();
            List<BaseMessage> oldMsgs = GetAllMessages();
            foreach (BaseMessage msg in oldMsgs)
            {
                this.Add(msg.ID, msg);
            }
            BroadcastEvent be = new BroadcastEvent();
            reg = MessageServer<BroadcastEvent>.Intialize(be, ConfigurationManager.AppSettings["service2"], Convert.ToInt32(ConfigurationManager.AppSettings["port2"]));
            if (reg.Channels.Count > 0)
                this.channel = reg.Channels[0];//.Find(a => a.ChannelName == ConfigurationManager.AppSettings["service2"]);
            be.Channel = this.channel;
            if (reg.Uris.Count > 0)
                this.uri = reg.Uris.Find(a => a.Segments[a.Segments.Length - 1] == ConfigurationManager.AppSettings["service2"]);
        }

        public BaseMessage Query(Guid id)
        {
            if (this.ContainsKey(id))
            {
                BaseMessage msg = this[id];
                this.Consume(id);
                return msg;
            }
            return null;
        }

        public List<BaseMessage> Query(string clientId)
        {
            List<BaseMessage> msgs = new List<BaseMessage>();
            Enumerator en = this.GetEnumerator();
            while (en.MoveNext())
            {
                KeyValuePair<Guid, BaseMessage> msg = en.Current;
                if (msg.Value.Client.Equals(clientId, StringComparison.InvariantCultureIgnoreCase))
                {
                    msgs.Add(msg.Value);
                }
            }
            this.Consume(clientId);
            return msgs;
        }

        public void Consume(Guid id, ConsumeEventHandler eventConsume = null)
        {
            if (eventConsume != null)
            {
                if (messageNotifyEvent != null)
                {
                    if (!messageNotifyEvent[id].GetInvocationList().Contains(eventConsume))
                        messageNotifyEvent[id] += eventConsume;
                }
                else
                {
                    this.messageNotifyEvent[id] += eventConsume;
                }
            }
            if (this.ContainsKey(id))
            {
                BaseMessage msg = this[id];
                if (messageNotifyEvent != null)
                    messageNotifyEvent[id](msg);
            }
        }

        public void Consume(string client, ConsumingEventHandler eventConsume = null)
        {
            if (this.Count > 0)
            {
                List<BaseMessage> messages = new List<BaseMessage>();
                Enumerator en = this.GetEnumerator();
                while (en.MoveNext())
                {
                    KeyValuePair<Guid, BaseMessage> msg = en.Current;
                    if (msg.Value.Client.Equals(client, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (this.messagesNotifyEvent != null)
                        {
                            if (!messagesNotifyEvent[msg.Key].GetInvocationList().Contains(eventConsume))
                                messagesNotifyEvent[msg.Key] += eventConsume;
                        }
                        else
                        {
                            messagesNotifyEvent[msg.Key] += eventConsume;
                        }
                        if (messagesNotifyEvent[msg.Key] != null)
                            messagesNotifyEvent[msg.Key](messages);
                        messages.Add(msg.Value);
                    }
                }
                if (eventConsume != null)
                    eventConsume(messages);
            }
        }

        public void Dispose()
        {
            if (this.Count > 0)
            {
                Enumerator en = this.GetEnumerator();
                while (en.MoveNext())
                {
                    SaveMessage(en.Current.Value);
                    en.Dispose();
                }
                this.Clear();
            }
        }

        private void SaveMessage(BaseMessage msg)
        {
            KellPersistence.Trunk<BaseMessage> trunk = new KellPersistence.Trunk<BaseMessage>(msg.Type.FullName);
            KellPersistence.Data<BaseMessage> data = trunk.Select(msg.ID);
            if (data != null)
            {
                trunk.Update(data);
            }
            else
            {
                trunk.Insert(new KellPersistence.Data<BaseMessage>(trunk, msg, Common.ClientId));
            }
        }

        public List<BaseMessage> GetAllMessages()
        {
            List<BaseMessage> msgs = new List<BaseMessage>();
            List<string> tables = KellPersistence.Trunk<BaseMessage>.Tables;
            foreach (string table in tables)
            {
                KellPersistence.Trunk<BaseMessage> trunk = new KellPersistence.Trunk<BaseMessage>(table);
                List<KellPersistence.Data<BaseMessage>> datas = trunk.Select(Common.ClientId);
                foreach (KellPersistence.Data<BaseMessage> data in datas)
                {
                    msgs.Add(data.Buffer);
                }
            }
            return msgs;
        }
    }
}
