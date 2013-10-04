using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceLibrary
{
    internal class PackedArchive : Archive
    {
        private sealed class Resource
        {
            public Object Value { get; set; }
            public long Offset { get; set; }

            public Resource()
            {
                Value = null;
                Offset = -1;
            }
        }
        
        private Dictionary<String, Archive> _innerArchives;
        private Dictionary<ResourceType, Dictionary<String, Resource>> _resources;

        internal PackedArchive(Stream stream, bool root = true)
            : base(root)
        {
            _innerArchives = new Dictionary<string,Archive>();
            _resources = new Dictionary<ResourceType,Dictionary<string,Resource>>();
        }

        internal override Object Get(ResourceType resType, params string[] locator)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<KeyValuePair<string, ResourceType>> GetResources()
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<KeyValuePair<string, Archive>> GetInnerArchives()
        {
            throw new NotImplementedException();
        }
    }
}
