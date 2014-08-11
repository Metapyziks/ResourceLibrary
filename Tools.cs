using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ResourceLibrary
{
    internal static class Tools
    {
        public static String CombinePath(IEnumerable<String> path)
        {
            if (path.Count() == 0) return String.Empty;
            if (path.Count() == 1) return path.First();

            return Path.Combine(path.First(), CombinePath(path.Skip(1)));
        }

        public static void CopyTo(this Stream from, Stream dest, int bufferSize = 2048)
        {
            var buffer = new byte[bufferSize];
            int read;
            while ((read = from.Read(buffer, 0, bufferSize)) > 0) {
                dest.Write(buffer, 0, read);
            }
        }
    }
}
