using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml.Linq;

namespace ResourceLibrary
{
    internal static class DefaultResourceTypes
    {
        public static void RegisterResourceTypes()
        {
            Archive.Register<Bitmap>(ResourceFormat.Compressed, SaveBitmap, LoadBitmap,
                ".png", ".gif", ".jpg", ".jpeg", ".ico");

            Archive.Register<XDocument>(ResourceFormat.Compressed, SaveXDocument, LoadXDocument,
                ".xml");
        }

        private static void SaveBitmap(Stream stream, Bitmap resource)
        {
            resource.Save(stream, ImageFormat.Png);
        }

        private static Bitmap LoadBitmap(Stream stream)
        {
            return (Bitmap) Bitmap.FromStream(stream);
        }

        private static void SaveXDocument(Stream stream, XDocument resource)
        {
            resource.Save(new StreamWriter(stream));
        }

        private static XDocument LoadXDocument(Stream stream)
        {
            return XDocument.Load(new StreamReader(stream));
        }
    }
}
