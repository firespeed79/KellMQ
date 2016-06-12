using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace KellMQ
{
    [Obsolete("暂时没用！")]
    [Serializable]
    public class ClonableMessage : IDisposable, ICloneable
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

        public virtual string Message
        {
            get;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose();

        /// <summary>
        /// 克隆对象本身
        /// </summary>
        /// <returns></returns>
        public virtual object Clone();
    }
}