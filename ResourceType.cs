using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceLibrary
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ResourceTypeRegistrationAttribute : Attribute { }

    public delegate void SaveResourceDelegate<T>(Stream stream, T resource);
    public delegate T LoadResourceDelegate<T>(Stream stream);

    internal abstract class ResourceType
    {
        public Type Type { get; private set; }
        public String[] Extensions { get; private set; }

        protected ResourceType(Type type, params String[] extensions)
        {
            Type = type;
            Extensions = extensions;
        }
        
        public abstract void Save(Stream stream, Object resource);
        public abstract Object Load(Stream stream);
    }

    internal sealed class ResourceType<T> : ResourceType
    {
        public SaveResourceDelegate<T> SaveDelegate { get; private set; }
        public LoadResourceDelegate<T> LoadDelegate { get; private set; }

        public ResourceType(SaveResourceDelegate<T> saveDelegate,
            LoadResourceDelegate<T> loadDelegate, params String[] extensions)
            : base(typeof(T), extensions)
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
