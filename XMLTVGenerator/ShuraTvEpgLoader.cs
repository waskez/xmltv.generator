using System.IO;
using System.IO.Compression;
using System.Net.Http;

namespace XMLTVGenerator
{
    static class ShuraTvEpgLoader
    {
        public static void Run(string baseDirectory)
        {
            DownloadArchive(baseDirectory);
            Decompress(baseDirectory);
        }

        private static void DownloadArchive(string baseDirectory)
        {
            using (var client = new HttpClient())
            {
                var req = client.GetAsync("http://s2.tvshka.net/epg/xmltv.xml.gz").ContinueWith(res =>
                {
                    var result = res.Result;
                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var data = result.Content.ReadAsStreamAsync();
                        data.Wait();

                        using (var stream = data.Result)
                        {
                            var tmpDirectory = Path.Combine(baseDirectory, "temp");
                            Directory.CreateDirectory(tmpDirectory);
                            using (var fileStream = File.Create(Path.Combine(tmpDirectory, "xmltv.xml.gz"), (int)stream.Length))
                            {
                                byte[] bytesInStream = new byte[stream.Length];
                                stream.Read(bytesInStream, 0, bytesInStream.Length);
                                fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                            }
                        }                                                   
                    }
                });
                req.Wait();
            }
        }

        private static void Decompress(string baseDirectory)
        {
            var tmpDirectory = Path.Combine(baseDirectory, "temp");
            var directorySelected = new DirectoryInfo(tmpDirectory);
            var fileToDecompress = directorySelected.GetFiles("xmltv.xml.gz")[0];

            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }
            }
        }

        //private void Compress(DirectoryInfo directorySelected)
        //{
        //    foreach (FileInfo fileToCompress in directorySelected.GetFiles())
        //    {
        //        using (FileStream originalFileStream = fileToCompress.OpenRead())
        //        {
        //            if ((File.GetAttributes(fileToCompress.FullName) &
        //               FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ".gz")
        //            {
        //                using (FileStream compressedFileStream = File.Create(fileToCompress.FullName + ".gz"))
        //                {
        //                    using (GZipStream compressionStream = new GZipStream(compressedFileStream,
        //                       CompressionMode.Compress))
        //                    {
        //                        originalFileStream.CopyTo(compressionStream);

        //                    }
        //                }
        //                FileInfo info = new FileInfo(directoryPath + "\\" + fileToCompress.Name + ".gz");
        //                Console.WriteLine("Compressed {0} from {1} to {2} bytes.",
        //                fileToCompress.Name, fileToCompress.Length.ToString(), info.Length.ToString());
        //            }

        //        }
        //    }
        //}
    }
}