using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ResourceLibrary
{
    /// <remarks>TODO: Caching</remarks>
    internal sealed class LooseArchive : Archive
    {
        private String _directory;
        private ResourceLocator[] _ignore;

        internal LooseArchive(ArchiveManager manager, String directory, bool root, params ResourceLocator[] ignore)
            : base(manager, root)
        {
            _directory = Path.GetFullPath(directory);
            _ignore = ignore;
        }

        internal override Object Get(ResourceType resType, IEnumerable<String> locator)
        {
            if (_ignore.Any(x => x.IsPrefixOf(locator))) return null;

            if (locator.Count() > 1) {
                var inner = GetInnerArchive(locator.First());
                if (inner == null) return null;
                return inner.Get(resType, locator.Skip(1));
            }

            var joined = Path.Combine(_directory, Tools.CombinePath(locator));
            var path = resType.Extensions.Select(x => String.Format("{0}{1}", joined, x))
                .FirstOrDefault(x => File.Exists(x));

            if (path == null) return null;

            using (var stream = File.OpenRead(path)) {
                return resType.Load(stream);
            }
        }

        internal override bool IsModified(ResourceType resType, IEnumerable<string> locator, DateTime lastAccess)
        {
            var joined = Path.Combine(_directory, Tools.CombinePath(locator));
            var path = resType.Extensions.Select(x => String.Format("{0}{1}", joined, x))
                .FirstOrDefault(x => File.Exists(x));

            if (path == null || _ignore.Any(x => x.IsPrefixOf(locator))) {
                throw new FileNotFoundException(joined);
            }

            return File.GetLastWriteTime(path) > lastAccess;
        }

        private ResourceLocator[] GetInnerIgnored(string prefix)
        {
            return _ignore.Where(x => x.First() == prefix)
                .Select(x => new ResourceLocator(x.Skip(1).ToArray()))
                .ToArray();
        }

        internal override Archive GetInnerArchive(string name)
        {
            var path = Path.Combine(_directory, name);
            while (File.Exists(path)) {
                try {
                    path = File.ReadAllLines(path).First().Trim();
                } catch { break; }
            }
            if (!Directory.Exists(path)) return null;
            return new LooseArchive(Manager, path, false, GetInnerIgnored(name));
        }

        internal override IEnumerable<KeyValuePair<String, ResourceType>> GetResources()
        {
            foreach (var file in Directory.GetFiles(_directory)) {
                var extension = Path.GetExtension(file);
                
                var resType = Manager.ResourceTypeFromExtension(extension);
                if (resType == null) continue;
                
                var name = Path.GetFileNameWithoutExtension(file);

                if (_ignore.Any(x => x.Length == 1 && x.First() == name)) continue;

                yield return new KeyValuePair<String, ResourceType>(name, resType);
            }
        }

        internal override IEnumerable<KeyValuePair<String, Archive>> GetInnerArchives()
        {
            foreach (var file in Directory.GetFiles(_directory)) {
                var ext = Path.GetExtension(file);
                if (ext == null || ext.Length == 0) {
                    var path = file;
                    while (File.Exists(path)) {
                        try {
                            var next = File.ReadAllLines(path).First().Trim();

                            if (!Path.IsPathRooted(next)) {
                                path = Path.Combine(_directory, next);
                            } else {
                                path = next;
                            }
                        } catch { break; }
                    }
                    if (Directory.Exists(path)) {
                        var name = Path.GetFileName(file);

                        if (_ignore.Any(x => x.Length == 1 && x.First() == name)) continue;

                        var inner = new LooseArchive(Manager, path, false, GetInnerIgnored(name));
                        yield return new KeyValuePair<String, Archive>(name, inner);
                    }
                }
            }

            foreach (var dir in Directory.GetDirectories(_directory)) {
                var name = Path.GetFileName(dir);

                if (_ignore.Any(x => x.Length == 1 && x.First() == name)) continue;

                var inner = new LooseArchive(Manager, dir, false, GetInnerIgnored(name));
                yield return new KeyValuePair<String, Archive>(name, inner);
            }
        }
    }
}
