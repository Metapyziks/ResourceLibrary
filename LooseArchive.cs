using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceLibrary
{
    /// <remarks>TODO: Caching</remarks>
    internal sealed class LooseArchive : Archive
    {
        private String _directory;

        internal LooseArchive(String directory, bool root = true)
            : base(root)
        {
            _directory = Path.GetFullPath(directory);
        }

        internal override Object Get(ResourceType resType, IEnumerable<String> locator)
        {
            if (locator.Count() > 1) {
                var inner = GetInnerArchive(locator.First());
                if (inner == null) return null;
                return inner.Get(resType, locator.Skip(1));
            }

            var joined = Path.Combine(_directory, Path.Combine(locator.ToArray()));
            var path = resType.Extensions.Select(x => String.Format("{0}{1}", joined, x))
                .FirstOrDefault(x => File.Exists(x));

            if (path == null) return null;

            using (var stream = File.OpenRead(path)) {
                return resType.Load(stream);
            }
        }

        internal override bool IsModified(ResourceType resType, IEnumerable<string> locator, DateTime lastAccess)
        {
            var joined = Path.Combine(_directory, Path.Combine(locator.ToArray()));
            var path = resType.Extensions.Select(x => String.Format("{0}{1}", joined, x))
                .FirstOrDefault(x => File.Exists(x));

            if (path == null) {
                throw new FileNotFoundException(joined);
            }

            return File.GetLastWriteTime(path) > lastAccess;
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
            return new LooseArchive(path, false);
        }

        internal override IEnumerable<KeyValuePair<String, ResourceType>> GetResources()
        {
            foreach (var file in Directory.GetFiles(_directory)) {
                var extension = Path.GetExtension(file);
                
                var resType = ResourceTypeFromExtension(extension);
                if (resType == null) continue;
                
                var name = Path.GetFileNameWithoutExtension(file);
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
                            path = File.ReadAllLines(path).First().Trim();
                        } catch { break; }
                    }
                    if (Directory.Exists(path)) {
                        var name = Path.GetFileName(path);
                        var inner = new LooseArchive(path, false);
                        yield return new KeyValuePair<String, Archive>(name, inner);
                    }
                }
            }
            foreach (var dir in Directory.GetDirectories(_directory)) {
                var name = Path.GetFileName(dir);
                var inner = new LooseArchive(dir, false);
                yield return new KeyValuePair<String, Archive>(name, inner);
            }
        }
    }
}
