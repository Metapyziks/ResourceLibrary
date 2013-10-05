using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceLibrary
{
    internal class PackedArchive : Archive
    {
        private struct ResourcePosition
        {
            public long Offset;
            public long Length;

            public ResourcePosition(long offset, long length)
            {
                Offset = offset;
                Length = length;
            }
        }

        private static readonly String BadArchiveFormatString = "Archive not in correct format";

        private static int ReadVersion(Stream stream)
        {
            var reader = new BinaryReader(stream);

            var magicWord = reader.ReadChars(4);
            if (new String(magicWord) != Archive.MagicWord) {
                throw new Exception(BadArchiveFormatString);
            }

            return reader.ReadInt32();
        }

        private static ResourceType[] ReadResourceTypes(Stream stream)
        {
            var reader = new BinaryReader(stream);

            var count = reader.ReadInt32();
            var types = new ResourceType[count];

            for (int i = 0; i < count; ++i) {
                var name = reader.ReadString();
                types[i] = Archive.ResourceTypeFromTypeName(name);
            }

            return types;
        }

        private Stream _stream;
        private Dictionary<String, Archive> _innerArchives;
        private Dictionary<ResourceType, Dictionary<String, ResourcePosition>> _resPositions;

        internal PackedArchive(Stream stream)
            : this(stream, true, ReadVersion(stream), ReadResourceTypes(stream)) { }

        private PackedArchive(Stream stream, bool root, int version, ResourceType[] types)
            : base(root)
        {
            _stream = stream;
            _innerArchives = new Dictionary<string,Archive>();
            _resPositions = new Dictionary<ResourceType,Dictionary<string, ResourcePosition>>();

            var reader = new BinaryReader(stream);

            var innerCount = reader.ReadInt32();
            var resourceCount = reader.ReadInt32();

            var innerPositions = new Dictionary<String, long>();

            for (int i = 0; i < innerCount; ++i) {
                var name = reader.ReadString();
                var typeID = reader.ReadInt32();
                var pos = reader.ReadInt64();
                var len = reader.ReadInt64();

                if (typeID != -1) continue;

                innerPositions.Add(name, pos);
            }

            for (int i = 0; i < resourceCount; ++i) {
                var name = reader.ReadString();
                var typeID = reader.ReadInt32();
                var pos = reader.ReadInt64();
                var len = reader.ReadInt64();

                if (typeID == -1) continue;

                var type = types[typeID];
                if (type == null) continue;

                if (!_resPositions.ContainsKey(type)) {
                    _resPositions.Add(type, new Dictionary<String, ResourcePosition>());
                }

                _resPositions[type].Add(name, new ResourcePosition(pos, len));
            }

            foreach (var kv in innerPositions) {
                var name = kv.Key;
                var pos = kv.Value;

                _stream.Position = pos;
                var inner = new PackedArchive(_stream, false, version, types);

                _innerArchives.Add(name, inner);
            }
        }

        internal override Object Get(ResourceType resType, IEnumerable<String> locator)
        {
            if (locator.Count() == 0) {
                throw new ArgumentException("No resource location given");
            }
            
            var name = locator.First();

            if (locator.Count() > 1) {
                if (!_innerArchives.ContainsKey(name)) return null;

                return _innerArchives[name].Get(resType, locator.Skip(1));
            }

            if (!_resPositions.ContainsKey(resType)) return null;

            var dict = _resPositions[resType];

            if (!dict.ContainsKey(name)) return null;

            lock (_stream) {
                var position = dict[name];
                var bytes = new byte[position.Length];

                _stream.Seek(position.Offset, SeekOrigin.Begin);
                _stream.Read(bytes, 0, (int) position.Length);
                
                if (resType.Format.HasFlag(ResourceFormat.Compressed)) {
                    using (var srcStream = new MemoryStream(bytes)) {
                        using (var zipStream = new GZipStream(srcStream, CompressionMode.Decompress)) {
                            using (var dstStream = new MemoryStream()) {
                                zipStream.CopyTo(dstStream);
                                return resType.Load(dstStream);
                            }
                        }
                    }
                } else {
                    using (var memStream = new MemoryStream(bytes)) {
                        return resType.Load(memStream);
                    }
                }    
            }
        }

        internal override bool IsModified(ResourceType resType, IEnumerable<string> locator, DateTime lastAccess)
        {
            return false;
        }

        internal override Archive GetInnerArchive(string name)
        {
            if (!_innerArchives.ContainsKey(name)) return null;
            return _innerArchives[name];
        }

        internal override IEnumerable<KeyValuePair<String, ResourceType>> GetResources()
        {
            return _resPositions.SelectMany(x => x.Value.Select(y =>
                new KeyValuePair<String, ResourceType>(y.Key, x.Key)));
        }

        internal override IEnumerable<KeyValuePair<String, Archive>> GetInnerArchives()
        {
            return _innerArchives;
        }

        public override void Dispose()
        {
            base.Dispose();

            _stream.Close();
            _stream.Dispose();
        }
    }
}
