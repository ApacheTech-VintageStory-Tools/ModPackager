using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static ModPackager.Extensions.FileSystemExtensions;

namespace ModPackager.Extensions
{
    public static class FileSystemExtensions
    {
        public delegate void LockStepFunction<in T>(T source, T other);

        public static void LockStep<T>(this IEnumerable<T> first, IEnumerable<T> second, LockStepFunction<T> action)
        {
            using var firstEnumerator = first.GetEnumerator();
            using var secondEnumerator = second.GetEnumerator();
            while (firstEnumerator.MoveNext() && secondEnumerator.MoveNext())
            {
                action(firstEnumerator.Current, secondEnumerator.Current);
            }
        }

        public static string NameWithoutExtension(this FileInfo fi) => Path.GetFileNameWithoutExtension(fi.FullName);

        public static bool IsDuplicate(this FileInfo file, FileInfo otherFile)
        {
            if (!file.Exists || !otherFile.Exists) return false;
            if (file.Length != otherFile.Length) return false;

            var hashAlgorithmProvider = SHA256.Create();

            using var stream = file.OpenRead();
            var firstHash = hashAlgorithmProvider.ComputeHash(stream).AsSpan();

            using var otherStream = otherFile.OpenRead();
            var secondHash = hashAlgorithmProvider.ComputeHash(otherStream).AsSpan();

            return firstHash.SequenceEqual(secondHash);
        }

        public static void Purge(this DirectoryInfo directory)
        {
            foreach (var file in directory.GetFiles())
            {
                file.Delete();
            }
            foreach (var dir in directory.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        public static void CopyFilesAndFoldersTo(this DirectoryInfo srcPath, string destPath)
        {
            Directory.CreateDirectory(destPath);
            Parallel.ForEach(srcPath.GetDirectories("*", SearchOption.AllDirectories),
                srcInfo => Directory.CreateDirectory($"{destPath}{srcInfo.FullName[srcPath.FullName.Length..]}"));
            Parallel.ForEach(srcPath.GetFiles("*", SearchOption.AllDirectories),
                srcInfo => File.Copy(srcInfo.FullName, $"{destPath}{srcInfo.FullName[srcPath.FullName.Length..]}", true));
        }

        public static void MoveFilesAndFoldersTo(this DirectoryInfo srcPath, string destPath)
        {
            Directory.CreateDirectory(destPath);
            Parallel.ForEach(srcPath.GetDirectories("*", SearchOption.AllDirectories),
                srcInfo => Directory.CreateDirectory($"{destPath}{srcInfo.FullName[srcPath.FullName.Length..]}"));
            Parallel.ForEach(srcPath.GetFiles("*", SearchOption.AllDirectories),
                srcInfo => File.Move(srcInfo.FullName, $"{destPath}{srcInfo.FullName[srcPath.FullName.Length..]}", true));
            srcPath.Delete(true);
        }
    }
}