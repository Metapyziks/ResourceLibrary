using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ResourceLibrary
{
    public sealed class Archive
    {
        public const ushort Version = 0x0000;

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
            throw new NotImplementedException();
        }

        public static Archive FromDirectory(String path)
        {
            return new Archive().LoadFromDirectory(path);
        }

        private Dictionary<String, Archive> _innerArchives;
        private Dictionary<ResourceType, Dictionary<String, Resource>> _resources;

        private Archive()
        {
            _innerArchives = new Dictionary<string, Archive>();
            _resources = new Dictionary<ResourceType, Dictionary<string, Resource>>();
        }

        private Archive LoadFromDirectory(String path)
        {
            path = Path.GetFullPath(path);

            foreach (var file in Directory.GetFiles(path)) {
                var extension = Path.GetExtension(file);
                
                var resType = _resTypes.Values.FirstOrDefault(x => x.Extensions.Contains(extension));
                if (resType == null) continue;

                var res = new Resource();
                try {
                    using (var stream = File.OpenRead(file)) {
                        res.Value = resType.Load(stream);
                    }
                } catch {
                    continue;
                }

                if (!_resources.ContainsKey(resType)) {
                    _resources.Add(resType, new Dictionary<string,Resource>());
                }

                _resources[resType].Add(Path.GetFileNameWithoutExtension(file), res);
            }

            foreach (var dir in Directory.GetDirectories(path)) {
                var name = Path.GetFileName(dir);
                var inner = Archive.FromDirectory(dir);
                _innerArchives.Add(name, inner);
            }

            return this;
        }

        private void Save(String path)
        {
            throw new NotImplementedException();
        }
    }
}
