using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceLibrary
{
    public enum ResourceFormat
    {
        Default = 0,
        Compressed = 1
    }

    public class ResourceLocator : IEnumerable<String>
    {
        public static readonly ResourceLocator None = new ResourceLocator();

        public static implicit operator String[](ResourceLocator locator)
        {
            return (locator ?? None).Parts.ToArray();
        }

        public static implicit operator ResourceLocator(String[] locator)
        {
            return new ResourceLocator(locator);
        }
        
        public static implicit operator ResourceLocator(String locator)
        {
            return new ResourceLocator(locator.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public IEnumerable<String> Parts { get; private set; }

        public int Length { get { return Parts.Count(); } }

        public ResourceLocator(params String[] locator)
        {
            Parts = locator;
        }

        public ResourceLocator Prepend(ResourceLocator locator)
        {
            return locator.Append(this);
        }

        public ResourceLocator Append(params String[] locator)
        {
            return new ResourceLocator(Parts.Concat(locator).ToArray());
        }

        public ResourceLocator Append(ResourceLocator locator)
        {
            return new ResourceLocator(Parts.Concat(locator.Parts).ToArray());
        }

        public IEnumerator<string> GetEnumerator()
        {
            return Parts.AsEnumerable().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Parts.GetEnumerator();
        }

        public ResourceLocator this[ResourceLocator append]
        {
            get { return Append(append); }
        }

        public override string ToString()
        {
            return String.Join("/", Parts);
        }

        public override bool Equals(object obj)
        {
            return (obj is ResourceLocator || obj is String || obj is String[])
                && Equals((ResourceLocator) obj);
        }

        public bool Equals(ResourceLocator locator)
        {
            return locator.Length == Length && locator.Parts.Zip(Parts, (x, y) => x.Equals(y)).All(x => x);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ResourceTypeRegistrationAttribute : Attribute { }

    public delegate void SaveResourceDelegate<T>(Stream stream, T resource);
    public delegate T LoadResourceDelegate<T>(Stream stream);

    internal abstract class ResourceType
    {
        public Type Type { get; private set; }
        public ResourceFormat Format { get; private set; }
        public String[] Extensions { get; private set; }

        protected ResourceType(Type type, ResourceFormat format, params String[] extensions)
        {
            Type = type;
            Format = format;
            Extensions = extensions;
        }
        
        public abstract void Save(Stream stream, Object resource);
        public abstract Object Load(Stream stream);
    }

    internal sealed class ResourceType<T> : ResourceType
    {
        public SaveResourceDelegate<T> SaveDelegate { get; private set; }
        public LoadResourceDelegate<T> LoadDelegate { get; private set; }

        public ResourceType(ResourceFormat format, SaveResourceDelegate<T> saveDelegate,
            LoadResourceDelegate<T> loadDelegate, params String[] extensions)
            : base(typeof(T), format, extensions)
        {
            SaveDelegate = saveDelegate;
            LoadDelegate = loadDelegate;
        }

        public override void Save(Stream stream, Object resource)
        {
            SaveDelegate(stream, (T) resource);
        }

        public override Object Load(Stream stream)
        {
            return LoadDelegate(stream);
        }
    }
}
