using System;
using ResourceLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;

namespace UnitTests
{
    [TestClass]
    public class FromDirectoryTest
    {
        [TestMethod]
        public void Empty()
        {
            using (var archive = Archive.FromDirectory("../../TestData/empty")) {
                archive.Save("../../TestData/empty.dat");
            }
        }
        
        [TestMethod]
        public void Complex()
        {
            using (var archive = Archive.FromDirectory("../../TestData/complex")) {
                var bmp = archive.Get<Bitmap>("images", "ents", "human");

                Assert.AreEqual(16, bmp.Width);
                Assert.AreEqual(40, bmp.Height);
            
                archive.Save("../../TestData/complex.dat");
            }
            using (var archive = Archive.FromFile("../../TestData/complex.dat")) {
                var bmp = archive.Get<Bitmap>("images", "ents", "human");

                Assert.AreEqual(16, bmp.Width);
                Assert.AreEqual(40, bmp.Height);
                
                archive.Save("../../TestData/complex2.dat");
            }
            using (var archive = Archive.FromFile("../../TestData/complex2.dat")) {
                var bmp = archive.Get<Bitmap>("images", "ents", "human");

                Assert.AreEqual(16, bmp.Width);
                Assert.AreEqual(40, bmp.Height);
            }
        }
    }
}
