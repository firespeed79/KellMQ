using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Producer
{
    [Serializable]
    public class MyMessageClonable<T> : KellMQ.ClonableObject where T : class, ISerializable, IDisposable
    {
        public MyMessageClonable(T obj)
            : base()
        {
            this.obj = obj;
        }

        T obj;

        public override object Clone()
        {
            return new MyMessageClonable<T>(this.obj);
        }
    }
}
