using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceLibrary
{
    internal sealed class Resource
    {
        public Object Value { get; set; }
        public long Offset { get; set; }

        public Resource()
        {
            Value = null;
            Offset = -1;
        }
    }
}
