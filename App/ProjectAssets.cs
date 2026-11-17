using System;
using System.Drawing;
using System.IO;

namespace HuffPressStudio
{
    // Loads the pictures from the Assets folder next to the .exe.
    internal static class ProjectAssets
    {
        public static string AssetsFolder
        {
            get { return Path.Combine(AppContext.BaseDirectory, "Assets"); }
        }

        public static string SamplesFolder
        {
            get { return Path.Combine(AppContext.BaseDirectory, "Samples"); }
        }

        // Returns the image, or null if it is missing (the UI then draws a fallback).
        public static Image Load(string fileName)
        {
            try
            {
                string path = Path.Combine(AssetsFolder, fileName);
                if (!File.Exists(path)) return null;
                using (Image raw = Image.FromFile(path))
                {
                    return new Bitmap(raw);     // copy, so the file is not left locked
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
