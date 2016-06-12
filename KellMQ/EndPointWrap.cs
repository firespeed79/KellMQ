using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace KellMQ
{
    /// <summary>
    /// 终结点与消息的封装类
    /// </summary>
    [Serializable]
    public class EndPointWrap
    {
        EndPoint ep;
        /// <summary>
        /// 终结点
        /// </summary>
        public EndPoint EndPoint
        {
            get { return ep; }
            set { ep = value; }
        }
        BaseMessage msg;
        /// <summary>
        /// 消息
        /// </summary>
        public BaseMessage Message
        {
            get { return msg; }
            set { msg = value; }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="msg"></param>
        public EndPointWrap(EndPoint ep, BaseMessage msg)
        {
            this.ep = ep;
            this.msg = msg;
        }
    }
    
    /// <summary>
    /// 终结点与消息的封装类
    /// </summary>
    [Serializable]
    public class EndPointWrapClonable
    {
        EndPoint ep;
        /// <summary>
        /// 终结点
        /// </summary>
        public EndPoint EndPoint
        {
            get { return ep; }
            set { ep = value; }
        }
        ClonableObject msg;
        /// <summary>
        /// 消息
        /// </summary>
        public ClonableObject Message
        {
            get { return msg; }
            set { msg = value; }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="msg"></param>
        public EndPointWrapClonable(EndPoint ep, ClonableObject msg)
        {
            this.ep = ep;
            this.msg = msg;
        }
    }
}
