using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib
{
    /// <summary>
    /// 消息对象类
    /// </summary>
    [Serializable]
    public class MsgClass
    {
        string source;
        /// <summary>
        /// 消息来源，最大长度50Byte
        /// </summary>
        public string Source
        {
            get { return source; }
            set { source = value; }
        }
        string message;
        /// <summary>
        /// 消息内容，从301Byte开始的数据
        /// </summary>
        public string Message
        {
            get { return message; }
            set { message = value; }
        }
        string link;
        /// <summary>
        /// 消息携带的超链接，最大长度250Byte
        /// </summary>
        public string Link
        {
            get { return link; }
            set { link = value; }
        }
        DateTime time;
        /// <summary>
        /// 消息时间
        /// </summary>
        public DateTime Time
        {
            get { return time; }
            set { time = value; }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="source"></param>
        /// <param name="message"></param>
        /// <param name="link"></param>
        public MsgClass(string source, string message, string link)
        {
            this.source = source;
            this.message = message;
            this.link = link;
            this.time = DateTime.Now;
        }
    }
}
