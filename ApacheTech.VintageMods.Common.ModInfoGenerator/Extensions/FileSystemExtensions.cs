using System.Threading.Tasks;

namespace ApacheTech.VintageMods.Common.ModInfoGenerator.Extensions;

public static class FileSystemExtensions
{
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
            srcInfo => File.Move(srcInfo.FullName, $"{destPath}{srcInfo.FullName[srcPath.FullName.Length..]}"));
        srcPath.Delete(true);
    }
}