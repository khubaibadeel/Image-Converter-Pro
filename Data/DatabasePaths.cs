using System;
using System.IO;

namespace ImageConverterPro.Data
{
    public static class DatabasePaths
    {
        public static string DatabasePath
        {
            get
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ImageConverterPro");
                Directory.CreateDirectory(directory);
                return Path.Combine(directory, "ImageConverterPro.db");
            }
        }
    }
}
