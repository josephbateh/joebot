using System.CommandLine;
using System.Security.Cryptography;
using System.Text;

namespace JoeBot.Commands.Rename;

public class RenameCommand
{
    public static Command Get()
    {
        var directoryArg = new Argument<string>(
            name: "directory",
            description: "The directory containing files to rename"
        );

        var command = new Command("rename", "Rename files in a directory based on their creation date");
        command.AddArgument(directoryArg);
            
        command.SetHandler((string directory) =>
        {
            try
            {
                var resolvedPath = ResolvePath(directory);
                    
                if (!Directory.Exists(resolvedPath))
                {
                    Console.WriteLine($"Error: Directory '{directory}' does not exist.");
                    return;
                }

                var files = Directory.GetFiles(resolvedPath);
                if (files.Length == 0)
                {
                    Console.WriteLine($"No files found in directory '{directory}'.");
                    return;
                }

                foreach (var filePath in files)
                {
                    var file = new FileInfo(filePath);
                    var originalName = Path.GetFileNameWithoutExtension(file.Name);
                    var extension = file.Extension;
                    var date = file.CreationTime.Date;
                    var hash = GenerateHash(originalName);
                    
                    var newName = $"{date:yyyy-MM-dd}-{hash}{extension}";
                    var newPath = Path.Combine(resolvedPath, newName);

                    if (newPath != file.FullName)
                    {
                        File.Move(file.FullName, newPath);
                        Console.WriteLine($"Renamed: {file.Name} -> {newName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }, directoryArg);

        return command;
    }

    private static string GenerateHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        
        // Take first 8 characters of the hash (in uppercase)
        return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8);
    }

    private static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        if (path.StartsWith("~"))
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(homeDir, path.Substring(1).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        return Path.GetFullPath(path);
    }
}