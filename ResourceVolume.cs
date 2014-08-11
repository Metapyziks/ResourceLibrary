using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResourceLibrary
{
    public abstract class ResourceVolume
    {
        public T Get<T>(params String[] locator)
        {
            return Get<T>(locator.AsEnumerable());
        }

        public IEnumerable<ResourceLocator> FindAll(bool recursive = false)
        {
            return FindAll(ResourceLocator.None, recursive);
        }

        public IEnumerable<ResourceLocator> FindAll<T>(bool recursive = false)
        {
            return FindAll<T>(ResourceLocator.None, recursive);
        }

        public abstract T Get<T>(IEnumerable<String> locator);
        public abstract IEnumerable<ResourceLocator> FindAll(ResourceLocator locator, bool recursive = false);
        public abstract IEnumerable<ResourceLocator> FindAll<T>(ResourceLocator locator, bool recursive = false);
    }
}
