using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ResourceLibrary
{
    public abstract class Archive
    {
        internal const ushort Alignment = 0x0001;
        internal const uint Version = 0x00000000;
        
        internal static readonly String MagicWord = "RSAR";

        private static Dictionary<Type, ResourceType> _resTypes;

        static Archive()
        {
            _resTypes = new Dictionary<Type, ResourceType>();

            RegisterAll(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// Checks to see if a given type is already registered as a resource type.
        /// </summary>
        /// <typeparam name="T">The type to check</typeparam>
        /// <returns>True if the type has already been registered</returns>
        public static bool IsRegistered<T>()
        {
            var type = typeof(T);
            return _resTypes.ContainsKey(type);
        }

        /// <summary>
        /// Register a type to be recognised as a resource type, and provide the methods
        /// to save and load resources of that type.
        /// </summary>
        /// <typeparam name="T">The type to be registered as a resource type</typeparam>
        /// <param name="saveDelegate">The method to be used when saving resources of the given type</param>
        /// <param name="loadDelegate">The method to be used when loading resources of the given type</param>
        public static void Register<T>(SaveResourceDelegate<T> saveDelegate,
            LoadResourceDelegate<T> loadDelegate, params String[] extensions)
        {
            var resType = new ResourceType<T>(saveDelegate, loadDelegate, extensions);
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

        /// <summary>
        /// Load an archive from an existing file.
        /// </summary>
        /// <param name="path">Absolute or relative path to a resource archive file</param>
        /// <returns>Archive loaded from the given file</returns>
        public static Archive FromFile(String path)
        {
            using (var fileStream = File.OpenRead(path)) {
                return FromStream(fileStream);
            }
        }

        /// <summary>
        /// Load an archive from a stream.
        /// </summary>
        /// <param name="stream">Stream containing a resource archive</param>
        /// <returns>Archive loaded from the given stream</returns>
        public static Archive FromStream(Stream stream)
        {
            return new PackedArchive(stream);
        }

        public static Archive FromDirectory(String directory)
        {
            return new LooseArchive(directory);
        }

        public bool IsRoot { get; private set; }

        protected Archive(bool root)
        {
            IsRoot = root;
        }

        internal abstract Object Get(ResourceType resType, params String[] locator);

        internal abstract IEnumerable<KeyValuePair<String, ResourceType>> GetResources();
        internal abstract IEnumerable<KeyValuePair<String, Archive>> GetInnerArchives();

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
            if (IsRoot) {
                writer.Write(MagicWord.ToCharArray());
                writer.Write(Version);
            }

            var inners = GetInnerArchives().ToArray();
            var resources = GetResources().ToArray();

            writer.Write(inners.Length);
            writer.Write(resources.Length);

            writer.Flush();
            long innerPos = writer.BaseStream.Position;

            foreach (var kv in inners) {
                writer.Write(GetNameBytes(kv.Key));
                writer.Write((long) 0);
            }
            
            writer.Flush();
            long resourcePos = writer.BaseStream.Position;

            foreach (var kv in resources) {
                writer.Write(GetNameBytes(kv.Key));
                writer.Write((long) 0);
            }
            
            int i = 0;
            foreach (var kv in inners) {
                writer.Flush();
                long start = writer.BaseStream.Position;

                kv.Value.Save(writer);

                writer.Flush();
                long end = writer.BaseStream.Position;

                writer.BaseStream.Seek(innerPos + 32 * i++, SeekOrigin.Begin);
                writer.Write(start);
                writer.BaseStream.Seek(end, SeekOrigin.Begin);
            }
            
            i = 0;
            foreach (var kv in resources) {
                writer.Flush();

                var offset = writer.BaseStream.Position % Alignment;
                if (offset != 0) {
                    writer.BaseStream.Seek(Alignment - offset, SeekOrigin.Current);
                }

                long start = writer.BaseStream.Position;

                var resource = Get(kv.Value, kv.Key);
                kv.Value.Save(writer.BaseStream, resource);

                writer.Flush();
                long end = writer.BaseStream.Position;

                writer.BaseStream.Seek(resourcePos + 32 * i++, SeekOrigin.Begin);
                writer.Write(start);
                writer.BaseStream.Seek(end, SeekOrigin.Begin);
            }
        }
    }
}
