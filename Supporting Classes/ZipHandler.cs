using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    public static class ZipHandler
    {
        public static void Unzip(string zipFile, string destinationDirectory)
        {
            using (ZipFile zip = ZipFile.Read(zipFile))
            {
                zip.ExtractAll(destinationDirectory, ExtractExistingFileAction.OverwriteSilently);
            }
        }
        public static void Zip(string directory, string zipPath)
        {
            using (ZipFile zip = new ZipFile(zipPath))
            {
                zip.CompressionLevel = Ionic.Zlib.CompressionLevel.None;
                zip.AddDirectory(directory, "");
                zip.Save();
            }
        }
    }
}
