using System.IO;
using UnityEngine;

namespace CityGen.ParaMaps
{
    public static class MapsLoader
    {
        public static void loadMap(
            string filename, 
            ref Texture2D map)
        {
            // Create file stream
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            fs.Seek(0, SeekOrigin.Begin);

            // Create buffer with the length of file stream
            byte[] buffer = new byte[fs.Length];

            // Read the file
            fs.Read(buffer, 0, (int)fs.Length);

            // Release file stream
            fs.Close();
            fs.Dispose();

            // Set data to texture
            map.LoadImage(buffer);
        }
    }
}

