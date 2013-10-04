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
        private static readonly String _sTestDataDir = "../../testdata";

        [TestMethod]
        public void Empty()
        {
            var path = Path.Combine(_sTestDataDir, "empty");
            using (var archive = Archive.FromDirectory(path).Mount()) {
                archive.Save("../../TestData/empty.dat");
            }
        }

        private void TestComplexArchive(String description)
        {
            var path = Path.Combine(_sTestDataDir, "complex/images/tiles/floor");
            var actual = Directory.GetDirectories(path)
                .Select(x => Path.GetFileName(x)).OrderBy(x => x)
                .ToArray();

            var names = Archive.GetAllNames<Archive>("images", "tiles", "floor").ToArray();

            Assert.AreEqual(actual.Length, names.Length);
            Assert.IsTrue(actual.Zip(names, (x, y) => x == y).All(x => x));

            ////
            
            path = Path.Combine(_sTestDataDir, "complex/images/ents/human");
            actual = Directory.GetFiles(path)
                .Where(x => Path.GetExtension(x) == ".png")
                .Select(x => Path.GetFileNameWithoutExtension(x)).OrderBy(x => x)
                .ToArray();

            names = Archive.GetAllNames<Bitmap>("images", "ents", "human").ToArray();
            
            Assert.AreEqual(actual.Length, names.Length);
            Assert.IsTrue(actual.Zip(names, (x, y) => x == y).All(x => x));

            ////

            var bmp = Archive.Get<Bitmap>("images", "ents", "human");

            Assert.AreEqual(16, bmp.Width);
            Assert.AreEqual(40, bmp.Height);
        }

        [TestMethod]
        public void Complex()
        {
            var path = Path.Combine(_sTestDataDir, "complex");
            using (var archive = Archive.FromDirectory(path).Mount()) {
                TestComplexArchive("Loose");
                archive.Save("../../TestData/complex.dat");
            }
            
            path = Path.Combine(_sTestDataDir, "complex.dat");
            using (var archive = Archive.FromFile(path).Mount()) {
                TestComplexArchive("Packed 1st gen");
                archive.Save("../../TestData/complex2.dat");
            }
            
            path = Path.Combine(_sTestDataDir, "complex2.dat");
            using (var archive = Archive.FromFile(path).Mount()) {
                TestComplexArchive("Packed 2nd gen");
            }
        }
    }
}
