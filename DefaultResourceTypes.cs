using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceLibrary
{
    internal static class DefaultResourceTypes
    {
        [ResourceTypeRegistration]
        public static void Register()
        {
            Archive.Register<Bitmap>(SaveBitmap, LoadBitmap, ".png", ".gif", ".jpg", ".jpeg", ".ico");
        }

        private static void SaveBitmap(Stream stream, Bitmap resource)
        {
            resource.Save(stream, ImageFormat.Png);
        }

        private static Bitmap LoadBitmap(Stream stream)
        {
            return (Bitmap) Bitmap.FromStream(stream);
        }
    }
}
