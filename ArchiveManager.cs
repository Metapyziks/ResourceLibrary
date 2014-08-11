using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ResourceLibrary
{
    public sealed class ArchiveManager : IEnumerable<Archive>
    {
        private Stack<ResourceType[]> _typeStack;
        private Dictionary<Type, ResourceType> _resTypes;

        private List<Archive> _mounted;

        public IEnumerable<Archive> Mounted { get { return _mounted; } }

        public IEnumerable<Type> RegisteredTypes { get { return _resTypes.Keys; } }

        internal IEnumerable<ResourceType> ResourceTypes { get { return _resTypes.Values; } }

        public ArchiveManager()
        {
            _mounted = new List<Archive>();
            _typeStack = new Stack<ResourceType[]>();
            _resTypes = new Dictionary<Type,ResourceType>();
        }

        internal void Mount(Archive archive)
        {
            if (archive.Manager != this) {
                throw new InvalidOperationException("Cannot mount an archive created by a different manager.");
            }

            _mounted.Add(archive);
        }

        internal void Unmount(Archive archive)
        {
            if (_mounted.Contains(archive)) {
                _mounted.Remove(archive);
            }
        }

        public void PushRegisteredTypes()
        {
            _typeStack.Push(_resTypes.Values.ToArray());
        }

        public void PopRegisteredTypes()
        {
            _resTypes.Clear();

            if (_typeStack.Count > 0) {
                foreach (var resType in _typeStack.Pop()) {
                    _resTypes.Add(resType.Type, resType);
                }
            }
        }

        public bool IsRegistered<T>()
        {
            var type = typeof(T);
            return _resTypes.ContainsKey(type);
        }

        public void Register<T>(ResourceFormat format, SaveResourceDelegate<T> saveDelegate,
            LoadResourceDelegate<T> loadDelegate, params String[] extensions)
        {
            var resType = new ResourceType<T>(format, saveDelegate, loadDelegate, extensions);
            _resTypes.Add(resType.Type, resType);
        }

        public void RegisterAll(Assembly assembly)
        {
            var methods =
                from type in assembly.GetTypes()
                from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                where method.GetCustomAttributes(typeof(ResourceTypeRegistrationAttribute), false).Count() > 0
                    && !method.ContainsGenericParameters && method.GetParameters().Length == 0
                select method;

            foreach (var method in methods) {
                method.Invoke(null, new Object[0]);
            }
        }
        internal ResourceType ResourceTypeFromExtension(String extension)
        {
            return _resTypes.Values.FirstOrDefault(x => x.Extensions.Contains(extension));
        }

        internal ResourceType ResourceTypeFromType(Type type)
        {
            return _resTypes.ContainsKey(type) ? _resTypes[type] : null;
        }

        internal ResourceType ResourceTypeFromTypeName(String name)
        {
            return _resTypes.Values.FirstOrDefault(x => x.Type.FullName == name || x.Type.Name == name);
        }

        public Archive FromFile(String path)
        {
            return FromStream(File.OpenRead(path));
        }

        public Archive FromStream(Stream stream)
        {
            return new PackedArchive(this, stream);
        }

        public Archive FromDirectory(String directory, params ResourceLocator[] ignore)
        {
            return new LooseArchive(this, directory, true, ignore);
        }

        public T Get<T>(params String[] locator)
        {
            return Get<T>(locator.AsEnumerable());
        }

        public T Get<T>(IEnumerable<String> locator)
        {
            var resType = ResourceTypeFromType(typeof(T));
            if (resType == null) {
                throw new FileNotFoundException(String.Join("/", locator.ToArray()));
            }

            for (int i = _mounted.Count - 1; i >= 0; --i) {
                var archive = _mounted[i];
                var resource = archive.Get(resType, locator);
                if (resource != null) {
                    return (T) resource;
                }
            }

            throw new FileNotFoundException(String.Join("/", locator.ToArray()));
        }
        
        public IEnumerable<ResourceLocator> FindAll(bool recursive = false)
        {
            return FindAll(ResourceLocator.None, recursive);
        }

        public IEnumerable<ResourceLocator> FindAll<T>(bool recursive = false)
        {
            return FindAll<T>(ResourceLocator.None, recursive);
        }

        public IEnumerable<ResourceLocator> FindAll(ResourceLocator locator, bool recursive = false)
        {
            return _resTypes.Values
                .SelectMany(resType => _mounted
                    .SelectMany(x => x
                        .FindAll(resType, locator, recursive))
                    .Distinct()
                    .Select(x => x.Prepend(locator)))
                .OrderBy(x => x.ToString());
        }

        public IEnumerable<ResourceLocator> FindAll<T>(ResourceLocator locator, bool recursive = false)
        {
            if (typeof(T) == typeof(Archive)) {
                return _mounted.SelectMany(x => x.FindAllDirectories(locator)).Distinct();
            }

            var resType = ResourceTypeFromType(typeof(T));
            if (resType == null) {
                throw new FileNotFoundException(String.Join("/", locator));
            }

            return _mounted.SelectMany(x => x.FindAll(resType, locator, recursive))
                .Distinct().Select(x => x.Prepend(locator)).OrderBy(x => x.ToString());
        }

        public IEnumerator<Archive> GetEnumerator()
        {
            return _mounted.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _mounted.GetEnumerator();
        }
    }
}
