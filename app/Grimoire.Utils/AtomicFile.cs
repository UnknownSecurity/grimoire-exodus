using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Grimoire.Utils
{
    internal static class AtomicFile
    {
        public static void WriteAllLines(string path, IEnumerable<string> lines)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A destination path is required.", nameof(path));

            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(directory);
            string temporaryPath = Path.Combine(
                directory,
                Path.GetFileName(fullPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                using (FileStream stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.WriteThrough))
                using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (string line in lines)
                    {
                        writer.WriteLine(line);
                    }
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(fullPath))
                {
                    File.Replace(temporaryPath, fullPath, null, true);
                }
                else
                {
                    File.Move(temporaryPath, fullPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
    }
}
