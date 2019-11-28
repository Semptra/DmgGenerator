using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfflineInstallerPoC.Common;

namespace OfflineInstallerPoC.DmgGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter path to create DMG: ");
            var path = Console.ReadLine().Trim().Replace("\\", string.Empty);

            ShowContentTree(path);

            Console.WriteLine();
            Console.WriteLine("We are going to pack all this content to DMG.");

            Console.Write("Enter new volume name: ");
            var volume = Console.ReadLine();

            Console.Write("Enter new DMG name: ");
            var dmgName = Console.ReadLine();
            Console.WriteLine();

            MoveContentsToResources(path);

            ProcessWrapper.Invoke("hdiutil", $"create -size 5g -volname {volume} -srcfolder \"{path}\" -ov -format UDZO \"{dmgName}\" -verbose", Console.WriteLine);
        }

        static void ShowContentTree(string initialPath)
        {
            var stack = new Stack<string>();
            var currentDirectory = new DirectoryInfo(initialPath);
            stack.Push(currentDirectory.FullName);

            while (stack.Any())
            {
                var nextPath = stack.Pop();
                var nextPathTrimmed = nextPath.TrimStart('\t');
                var tabs = new string(nextPath.TakeWhile(c => c == '\t').ToArray());
                var pathAttributes = File.GetAttributes(nextPathTrimmed);

                if (nextPathTrimmed.EndsWith(".app", StringComparison.InvariantCulture))
                {
                    Console.WriteLine(tabs + Path.GetFileName(nextPath));
                    continue;
                }

                if (pathAttributes.HasFlag(FileAttributes.Directory))
                {
                    var directory = new DirectoryInfo(nextPathTrimmed);
                    Console.WriteLine(tabs + directory.Name + "/");

                    foreach(var nextDirectory in directory.EnumerateDirectories().OrderByDescending(x => x.Name))
                    {
                        stack.Push("\t" + tabs + nextDirectory.FullName);
                    }

                    foreach (var nextFile in directory.EnumerateFiles().OrderByDescending(x => x.Name))
                    {
                        stack.Push("\t" + tabs + nextFile.FullName);
                    }
                }
                else
                {
                    Console.WriteLine(tabs + Path.GetFileName(nextPath));
                }
            }
        }

        static void MoveContentsToResources(string path)
        {
            var resourcesPath = Path.Combine(path, ".Resources");
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
            }

            var filesToMove = Directory.EnumerateFiles(path)
                                       .Union(Directory.EnumerateDirectories(path)
                                                       .Where(x => !x.EndsWith(".app", StringComparison.InvariantCulture))
                                                       .SelectMany(x => Directory.EnumerateFiles(x, "*", SearchOption.AllDirectories)))
                                       .Where(x => !x.EndsWith(".DS_Store", StringComparison.InvariantCulture))
                                       .Distinct();

            foreach (var file in filesToMove)
            {
                ProcessWrapper.Invoke("mv", $"\"{Path.GetFullPath(file)}\" \"{resourcesPath}\"", Console.WriteLine);
            }

            var directoriesToDelete = Directory.EnumerateDirectories(path)
                                               .Where(x => !x.EndsWith(".Resources", StringComparison.InvariantCulture) &&
                                                           !x.EndsWith(".app", StringComparison.InvariantCulture));

            foreach (var directory in directoriesToDelete)
            {
                ProcessWrapper.Invoke("rm", $"-rf \"{Path.GetFullPath(directory)}\"", Console.WriteLine);
            }
        }
    }
}
