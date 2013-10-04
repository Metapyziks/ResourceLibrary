using System;
using System.Collections.Generic;
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
            var libs = parser.GetValues<String>("l", "lib");
            var input = parser.GetRemaining().FirstOrDefault();

            if (input == null) {
                throw new ArgumentException("No input file specified");
            }

            foreach (var lib in libs) {
                var asm = Assembly.LoadFile(lib);
                Archive.RegisterAll(asm);
            }

            Archive.FromDirectory(input).Save(output ?? String.Format("{0}.dat", input));
        }
    }
}
