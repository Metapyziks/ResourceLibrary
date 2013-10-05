using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ResourceLibrary;

namespace ArchiveTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new CommandLine.Parser(args);

            var output = parser.GetValue<String>("o", "output");
            var libs = parser.GetValues<String>("l", "lib").Select(x => Path.GetFullPath(x));
            var input = parser.GetRemaining().FirstOrDefault();

            if (input == null) {
                throw new ArgumentException("No input file specified");
            }

            input = Path.GetFullPath(input);
            output = output ?? String.Format("{0}.dat", input);

            foreach (var lib in libs) {
                var asm = Assembly.LoadFrom(lib);
                Archive.RegisterAll(asm);
            }

            Archive.FromDirectory(input).Save(output);
        }
    }
}
