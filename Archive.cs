using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ResourceLibrary
{
    public abstract class Archive : IDisposable
    {
        internal const int Alignment = 0x00000001;
        internal const int Version = 0x00000000;
        
        internal static readonly String MagicWord = "RSAR";

        private static Dictionary<Type, ResourceType> _resTypes;
        private static List<Archive> _mounted;

        static Archive()
        {
            _resTypes = new Dictionary<Type, ResourceType>();
            _mounted = new List<Archive>();

            RegisterAll(Assembly.GetExecutingAssembly());
        }

        public static bool IsRegistered<T>()
        {
            var type = typeof(T);
            return _resTypes.ContainsKey(type);
        }

        public static void Register<T>(ResourceFormat format, SaveResourceDelegate<T> saveDelegate,
            LoadResourceDelegate<T> loadDelegate, params String[] extensions)
        {
            var resType = new ResourceType<T>(format, saveDelegate, loadDelegate, extensions);
            _resTypes.Add(resType.Type, resType);
        }

        public static void RegisterAll(Assembly assembly)
        {
            var methods =
                from type in assembly.GetTypes()
                from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                where method.GetCustomAttribute<ResourceTypeRegistrationAttribute>() != null
                    && !method.ContainsGenericParameters && method.GetParameters().Length == 0
                select method;

            foreach (var method in methods) {
                method.Invoke(null, new Object[0]);
            }
        }

        internal static ResourceType ResourceTypeFromExtension(String extension)
        {
            return _resTypes.Values.FirstOrDefault(x => x.Extensions.Contains(extension));
        }

        internal static ResourceType ResourceTypeFromType(Type type)
        {
            return _resTypes.ContainsKey(type) ? _resTypes[type] : null;
        }

        internal static ResourceType ResourceTypeFromTypeName(String name)
        {
            return _resTypes.Values.FirstOrDefault(x => x.Type.FullName == name || x.Type.Name == name);
        }

        public static Archive FromFile(String path)
        {
            return FromStream(File.OpenRead(path));
        }

        public static Archive FromStream(Stream stream)
        {
            return new PackedArchive(stream);
        }

        public static Archive FromDirectory(String directory)
        {
            return new LooseArchive(directory);
        }

        public static T Get<T>(params String[] locator)
        {
            return Get<T>(locator.AsEnumerable());
        }

        public static T Get<T>(String[] locatorPrefix, params String[] locatorSuffix)
        {
            return Get<T>(locatorPrefix.Concat(locatorSuffix));
        }

        public static T Get<T>(IEnumerable<String> locator)
        {
            var resType = ResourceTypeFromType(typeof(T));
            if (resType == null) {
                throw new FileNotFoundException(String.Join("/", locator));
            }

            foreach (var archive in _mounted) {
                var resource = archive.Get(resType, locator);
                if (resource != null) {
                    return (T) resource;
                }
            }

            throw new FileNotFoundException(String.Join("/", locator));
        }

        public static IEnumerable<String> GetAllNames<T>(params String[] locator)
        {
            return GetAllNames<T>(locator.AsEnumerable());
        }

        public static IEnumerable<String> GetAllNames<T>(String[] locatorPrefix, params String[] locatorSuffix)
        {
            return GetAllNames<T>(locatorPrefix.Concat(locatorSuffix));
        }

        public static IEnumerable<String> GetAllNames<T>(IEnumerable<String> locator)
        {
            if (typeof(T) == typeof(Archive)) {
                return _mounted.SelectMany(x => x.GetAllDirectories(locator)).Distinct();
            }

            var resType = ResourceTypeFromType(typeof(T));
            if (resType == null) {
                throw new FileNotFoundException(String.Join("/", locator));
            }
        
            return _mounted.SelectMany(x => x.GetAllNames(resType, locator)).Distinct();
        }

        public bool IsRoot { get; private set; }
        public bool IsMounted { get { return _mounted.Contains(this); } }

        protected Archive(bool root)
        {
            IsRoot = root;
        }

        internal Object Get(ResourceType resType, params String[] locator)
        {
            return Get(resType, locator.AsEnumerable());
        }

        internal abstract Object Get(ResourceType resType, IEnumerable<String> locator);
        internal abstract bool IsModified(ResourceType resType, IEnumerable<String> locator, DateTime lastAccess);
        internal abstract Archive GetInnerArchive(String name);
        
        internal IEnumerable<String> GetAllNames(ResourceType resType, IEnumerable<String> locator)
        {
            if (locator.Count() == 0) {
                return GetResources()
                    .Where(x => x.Value == resType)
                    .Select(x => x.Key);
            }

            var name = locator.First();
            var inner = GetInnerArchive(name);

            if (inner == null) return Enumerable.Empty<String>();

            return inner.GetAllNames(resType, locator.Skip(1));
        }

        internal IEnumerable<String> GetAllDirectories(IEnumerable<String> locator)
        {
            if (locator.Count() == 0) {
                return GetInnerArchives().Select(x => x.Key);
            }

            var name = locator.First();
            var inner = GetInnerArchive(name);

            if (inner == null) return Enumerable.Empty<String>();

            return inner.GetAllDirectories(locator.Skip(1));
        }

        internal abstract IEnumerable<KeyValuePair<String, ResourceType>> GetResources();
        internal abstract IEnumerable<KeyValuePair<String, Archive>> GetInnerArchives();

        public Archive Mount()
        {
            _mounted.Add(this);
            return this;
        }

        public Archive Unmount()
        {
            _mounted.Remove(this);
            return this;
        }
        
        public void Save(String path)
        {
            using (var stream = File.Create(path)) {
                Save(stream);
            }
        }

        public void Save(Stream stream)
        {
            var writer = new BinaryWriter(stream);
            Save(writer);
            writer.Flush();
        }

        private static char[] GetNameBytes(String name)
        {
            return name.ToCharArray()
                .Concat(Enumerable.Range(0, 24).Select(x => '\0'))
                .Take(24).ToArray();
        }

        private void Save(BinaryWriter writer)
        {
            var types = _resTypes.Keys.ToList();

            if (IsRoot) {
                writer.Write(MagicWord.ToCharArray());
                writer.Write(Version);
                
                writer.Write(types.Count);
                foreach (var type in types) {
                    writer.Write(type.FullName);
                }
            }

            var inners = GetInnerArchives().OrderBy(x => x.Key).ToArray();
            var resources = GetResources().OrderBy(x => x.Key).ToArray();

            writer.Write(inners.Length);
            writer.Write(resources.Length);
            
            var innerPositions = new long[inners.Length];
            var resourcePositions = new long[resources.Length];

            int i = 0;
            foreach (var kv in inners) {
                writer.Write(kv.Key);
                writer.Write(-1);
                innerPositions[i++] = writer.BaseStream.Position;
                writer.Write((long) 0);
                writer.Write((long) 0);
            }
            
            i = 0;
            foreach (var kv in resources) {
                writer.Write(kv.Key);
                writer.Write(types.IndexOf(kv.Value.Type));
                resourcePositions[i++] = writer.BaseStream.Position;
                writer.Write((long) 0);
                writer.Write((long) 0);
            }
            
            i = 0;
            foreach (var kv in inners) {
                long start = writer.BaseStream.Position;
                kv.Value.Save(writer);
                long end = writer.BaseStream.Position;

                writer.BaseStream.Seek(innerPositions[i++], SeekOrigin.Begin);
                writer.Write(start);
                writer.Write(end - start);
                writer.BaseStream.Seek(end, SeekOrigin.Begin);
            }
            
            i = 0;
            foreach (var kv in resources) {
                var offset = writer.BaseStream.Position % Alignment;
                if (offset != 0) {
                    writer.BaseStream.Seek(Alignment - offset, SeekOrigin.Current);
                }

                long start = writer.BaseStream.Position;
                var resource = Get(kv.Value, kv.Key);

                if (kv.Value.Format.HasFlag(ResourceFormat.Compressed)) {
                    using (var memStream = new MemoryStream()) {
                        kv.Value.Save(memStream, resource);
                        memStream.Seek(0, SeekOrigin.Begin);
                        using (var zipStream = new GZipStream(writer.BaseStream, CompressionMode.Compress, true)) {
                            memStream.CopyTo(zipStream);
                        }
                    }
                } else {
                    kv.Value.Save(writer.BaseStream, resource);
                }

                long end = writer.BaseStream.Position;

                writer.BaseStream.Seek(resourcePositions[i++], SeekOrigin.Begin);
                writer.Write(start);
                writer.Write(end - start);
                writer.BaseStream.Seek(end, SeekOrigin.Begin);
            }
        }

        public virtual void Dispose()
        {
            if (IsMounted) Unmount();
        }
    }
}
