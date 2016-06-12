using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace KellMQ
{
    [Serializable]
    public abstract class BaseMessage : IDisposable, ICloneable
    {
        public string Client
        {
            get;
            set;
        }

        public Guid ID
        {
            get;
            set;
        }

        public object Body
        {
            get;
            set;
        }

        public Type Type
        {
            get;
            set;
        }

        public abstract string Message
        {
            get;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// 克隆对象本身
        /// </summary>
        /// <returns></returns>
        public abstract object Clone();
    }
}