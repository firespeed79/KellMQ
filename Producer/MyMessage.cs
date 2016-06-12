using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Producer
{
    [Serializable]
    public class MyMessage<T> : KellMQ.BaseMessage where T : class, ISerializable, IDisposable
    {
        public MyMessage(T obj)
            : base()
        {
            this.obj = obj;
            this.Client = KellMQ.Common.ClientId;
            this.ID = Guid.NewGuid();
            this.Body = obj;
            this.Type = typeof(T);
        }

        T obj;

        public override void Dispose()
        {
            obj.Dispose();
        }

        public override string Message
        {
            get { return "{" + ID + "}" + this.Body.ToString(); }
        }

        public override object Clone()
        {
            return new MyMessage<T>(this.obj);
        }
    }
}
