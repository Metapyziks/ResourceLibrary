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

            var manager = new ArchiveManager();
            DefaultResourceTypes.Register(manager);

            using (var archive = manager.FromDirectory(path).Mount()) {
                archive.Save("../../TestData/empty.dat");
            }
        }

        private void TestComplexArchive(ArchiveManager manager, String description)
        {
            var names = manager.FindAll<Archive>(new ResourceLocator("images", "tiles", "wall")).ToArray();
            names = manager.FindAll<Bitmap>(new ResourceLocator("images", "ents"), true).ToArray();

            var bmp = manager.Get<Bitmap>("images", "ents", "human");

            Assert.AreEqual(16, bmp.Width);
            Assert.AreEqual(40, bmp.Height);
        }

        [TestMethod]
        public void Complex()
        {
            var path = Path.Combine(_sTestDataDir, "complex");

            var manager = new ArchiveManager();
            DefaultResourceTypes.Register(manager);

            using (var archive = manager.FromDirectory(path).Mount()) {
                TestComplexArchive(manager, "Loose");
                archive.Save("../../TestData/complex.dat");
            }
            
            path = Path.Combine(_sTestDataDir, "complex.dat");
            using (var archive = manager.FromFile(path).Mount()) {
                TestComplexArchive(manager, "Packed 1st gen");
                archive.Save("../../TestData/complex2.dat");
            }
            
            path = Path.Combine(_sTestDataDir, "complex2.dat");
            using (var archive = manager.FromFile(path).Mount()) {
                TestComplexArchive(manager, "Packed 2nd gen");
            }
        }
    }
}
