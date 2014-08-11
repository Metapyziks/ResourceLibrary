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

        internal LooseArchive(String directory, bool root = true)
            : base(root)
        {
            _directory = Path.GetFullPath(directory);
        }

        internal override Object Get(ResourceType resType, IEnumerable<String> locator)
        {
            var joined = Path.Combine(_directory, Tools.CombinePath(locator));
            var path = resType.Extensions.Select(x => String.Format("{0}{1}", joined, x))
                .FirstOrDefault(x => File.Exists(x));

            if (path == null) {
                throw new FileNotFoundException(joined);
            }

            using (var stream = File.OpenRead(path)) {
                return resType.Load(stream);
            }
        }

        internal override Archive GetInnerArchive(string name)
        {
            var path = Path.Combine(_directory, name);
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
            foreach (var dir in Directory.GetDirectories(_directory)) {
                var name = Path.GetFileName(dir);
                var inner = new LooseArchive(dir, false);
                yield return new KeyValuePair<String, Archive>(name, inner);
            }
        }
    }
}
