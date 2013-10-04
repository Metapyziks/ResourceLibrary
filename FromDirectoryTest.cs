using System;
using ResourceLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace UnitTests
{
    [TestClass]
    public class FromDirectoryTest
    {
        [TestMethod]
        public void Empty()
        {
            using (var archive = Archive.FromDirectory("../../TestData/empty").Mount()) {
                archive.Save("../../TestData/empty.dat");
            }
        }

        private void TestComplexArchive(String description)
        {
            var actual = Directory.GetDirectories("../../TestData/complex/images/tiles/floor")
                .Select(x => Path.GetFileName(x)).OrderBy(x => x);

            var names = Archive.GetAllNames<Archive>("images", "tiles", "floor");

            Assert.IsTrue(actual.Zip(names, (x, y) => x == y).All(x => x));

            names = Archive.GetAllNames<Bitmap>("images", "ents", "human");
            
            Debug.WriteLine("Human anim frames");
            foreach (var name in names) {
                Debug.WriteLine(name);
            }

            var bmp = Archive.Get<Bitmap>("images", "ents", "human");

            Assert.AreEqual(16, bmp.Width);
            Assert.AreEqual(40, bmp.Height);
        }

        [TestMethod]
        public void Complex()
        {
            using (var archive = Archive.FromDirectory("../../TestData/complex").Mount()) {
                TestComplexArchive("Loose");
                archive.Save("../../TestData/complex.dat");
            }
            using (var archive = Archive.FromFile("../../TestData/complex.dat").Mount()) {
                TestComplexArchive("Packed 1st gen");
                archive.Save("../../TestData/complex2.dat");
            }
            using (var archive = Archive.FromFile("../../TestData/complex2.dat").Mount()) {
                TestComplexArchive("Packed 2nd gen");
            }
        }
    }
}
