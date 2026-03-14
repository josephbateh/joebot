using System.CommandLine;
using System.Security.Cryptography;
using System.Text;

namespace JoeBot.Commands.Rename;

public class RenameFileCommand {
  public static Command Get() {
    var directoryArg = new Argument<string>("directory") {
      Description = "The directory containing files to rename"
    };

    var command = new Command("file", "Rename files in a directory based on their creation date");
    command.Arguments.Add(directoryArg);

    command.SetAction(parseResult => {
      var directory = parseResult.GetValue<string>("directory")!;

      try {
        var resolvedPath = ResolvePath(directory);

        if (!Services.FileSystem.Directory.Exists(resolvedPath)) {
          Services.Console.WriteLine($"Error: Directory '{directory}' does not exist.");
          return;
        }

        var files = Services.FileSystem.Directory.GetFiles(resolvedPath);
        if (files.Length == 0) {
          Services.Console.WriteLine($"No files found in directory '{directory}'.");
          return;
        }

        foreach (var filePath in files) {
          var file = Services.FileSystem.FileInfo.New(filePath);
          var originalName = Services.FileSystem.Path.GetFileNameWithoutExtension(file.Name);
          var extension = file.Extension;
          var date = file.CreationTime.Date;
          var hash = GenerateHash(originalName);

          var newName = $"{date:yyyy-MM-dd}-{hash}{extension}";
          var newPath = Services.FileSystem.Path.Combine(resolvedPath, newName);

          if (newPath != file.FullName) {
            Services.FileSystem.File.Move(file.FullName, newPath);
            Services.Console.WriteLine($"Renamed: {file.Name} -> {newName}");
          }
        }
      }
      catch (Exception ex) {
        Services.Console.WriteLine($"Error: {ex.Message}");
      }
    });

    return command;
  }

  private static string GenerateHash(string input) {
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(input);
    var hash = sha256.ComputeHash(bytes);

    // Take first 8 characters of the hash (in uppercase)
    return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8);
  }

  private static string ResolvePath(string path) {
    if (string.IsNullOrWhiteSpace(path)) {
      throw new ArgumentException("Path cannot be null or empty.", nameof(path));
    }

    if (path.StartsWith("~")) {
      var homeDir = Services.Environment.UserProfilePath;
      path = Services.FileSystem.Path.Combine(homeDir, path.Substring(1).TrimStart(
          Services.FileSystem.Path.DirectorySeparatorChar,
          Services.FileSystem.Path.AltDirectorySeparatorChar));
    }

    return Services.FileSystem.Path.GetFullPath(path);
  }
}
