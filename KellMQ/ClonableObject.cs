using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KellMQ
{
    [Serializable]
    public class ClonableObject : ICloneable
    {
        public virtual object Clone()
        {
            return new ClonableObject();
        }
    }
}
