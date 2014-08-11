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
    }
}
