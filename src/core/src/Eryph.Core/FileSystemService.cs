﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Eryph.Core
{
    public class FileSystemService : IFileSystemService
    {

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public void WriteAllBytes(string filePath, byte[] data)
        {
            File.WriteAllBytes(filePath, data);
        }

        public byte[] ReadAllBytes(string filePath)
        {
            return FileExists(filePath) ? File.ReadAllBytes(filePath) : new byte[0];
        }

        public string ReadText(string filePath)
        {
            return FileExists(filePath) ? File.ReadAllText(filePath) : "";
        }

        public void WriteText(string filePath, string data)
        { 
            File.WriteAllText(filePath, data);
        }

        public void FileDelete(string filePath)
        {
            File.Delete(filePath);
        }

        public IEnumerable<string> GetFiles(string path, string pattern)
        {
            return !Directory.Exists(path) ? new string[0] : Directory.GetFiles(path, pattern);
        }

        public void MoveFile(string path, string newPath)
        {

            var directory = Path.GetDirectoryName(newPath);

            if(directory!= null)
                Directory.CreateDirectory(directory);

            File.Move(path, newPath);

            var fi = new FileInfo(path);
            for (var di = fi.Directory; di?.Parent != null && !di.EnumerateFileSystemInfos().Any(); di = di.Parent)
            {
                di.Delete();
            }
        }

        public long GetFileSize(string filePath)
        {

            var fInfo = new FileInfo(filePath);
            return fInfo.Length;
        }

        public void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public void DeleteOldestFiles(string path, string pattern, int filesToKeep)
        {
            foreach (var fileInfo in Directory.GetFiles(path, pattern).Select(x=>new FileInfo(x)).OrderByDescending(x=>x.LastAccessTime).Skip(filesToKeep))
            {
                fileInfo.Delete();
            }

        }

        public Stream OpenRead(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 0x1000);
        }

        public Stream OpenWrite(string path)
        {
            return new FileStream(path, FileMode.Create, FileAccess.Write);
        }

        public void DirectoryDelete(string directoryPath)
        {
            Directory.Delete(directoryPath, true);
        }
    }
}
