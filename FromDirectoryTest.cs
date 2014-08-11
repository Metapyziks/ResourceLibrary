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
            var names = Archive.FindAll<Archive>(new ResourceLocator("images", "tiles", "wall")).ToArray();
            names = Archive.FindAll<Bitmap>(new ResourceLocator("images", "ents"), true).ToArray();
            
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
