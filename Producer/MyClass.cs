using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Producer
{
    [Serializable]
    public class MyClass : ISerializable, IDisposable
    {
        int num;
        string name;

        public MyClass()
        {
        }

        public MyClass(int num, string name)
        {
            this.num = num;
            this.name = name;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("num", num);
            info.AddValue("name", name);
        }

        private MyClass GetInstance(SerializationInfo s, StreamingContext c)
        {
            num = s.GetInt32("num");
            name = s.GetString("name");
            return new MyClass(num, name);
        }

        public override string ToString()
        {
            return "[" + num.ToString() + "]" + name;
        }

        public void Dispose()
        {
            
        }
    }
}
