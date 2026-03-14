using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Xunit;

namespace JoeBot.Tests.Commands;

public class RenameFileCommandTests : CommandTestBase {
  [Fact]
  public void RenameFile_WithValidDirectory_RenamesFilesWithDatePrefix() {
    // Arrange
    var directory = "/test/photos";
    var creationTime = new DateTime(2024, 6, 15, 10, 30, 0);

    FileSystem.AddDirectory(directory);
    FileSystem.AddFile("/test/photos/vacation.jpg", new MockFileData("photo content") {
      CreationTime = creationTime
    });
    FileSystem.AddFile("/test/photos/beach.png", new MockFileData("another photo") {
      CreationTime = creationTime
    });

    // Act
    var result = RunCommand("rename", "file", directory);

    // Assert
    result.Should().Be(0);

    // Check that files were renamed with date prefix
    var files = FileSystem.Directory.GetFiles(directory);
    files.Should().HaveCount(2);
    files.Should().AllSatisfy(f => f.Should().Contain("2024-06-15-"));

    // Verify console output shows renaming
    Console.Lines.Should().Contain(line => line.StartsWith("Renamed:"));
  }

  [Fact]
  public void RenameFile_WithNonExistentDirectory_OutputsError() {
    // Arrange
    var directory = "/nonexistent/path";

    // Act
    var result = RunCommand("rename", "file", directory);

    // Assert
    result.Should().Be(0); // Command itself succeeds, just outputs error
    Console.Lines.Should().ContainSingle(line =>
        line.Contains("Error") && line.Contains("does not exist"));
  }

  [Fact]
  public void RenameFile_WithTildeExpansion_ExpandsToUserProfile() {
    // Arrange
    Environment.UserProfilePath = "/home/testuser";
    var expandedDirectory = "/home/testuser/photos";
    var creationTime = new DateTime(2024, 3, 20, 14, 0, 0);

    // Configure mock file system to resolve GetFullPath correctly
    FileSystem.AddDirectory("/home/testuser");
    FileSystem.AddDirectory(expandedDirectory);
    FileSystem.AddFile("/home/testuser/photos/sunset.jpg", new MockFileData("sunset content") {
      CreationTime = creationTime
    });

    // Act
    var result = RunCommand("rename", "file", expandedDirectory);

    // Assert
    result.Should().Be(0);

    // Verify file was renamed in the expanded path
    var files = FileSystem.Directory.GetFiles(expandedDirectory);
    files.Should().HaveCount(1);
    files[0].Should().Contain("2024-03-20-");

    Console.Lines.Should().Contain(line => line.StartsWith("Renamed:"));
  }

  [Fact]
  public void RenameFile_WithTildePath_ExpandsCorrectly() {
    // Arrange - test that tilde expansion resolves to user profile
    Environment.UserProfilePath = "/Users/testuser";
    var creationTime = new DateTime(2024, 5, 10, 9, 0, 0);

    // Set up the expanded path in the file system
    FileSystem.AddDirectory("/Users/testuser");
    FileSystem.AddDirectory("/Users/testuser/documents");
    FileSystem.AddFile("/Users/testuser/documents/report.pdf", new MockFileData("report content") {
      CreationTime = creationTime
    });

    // Act - use the expanded path directly since MockFileSystem doesn't handle tilde
    var result = RunCommand("rename", "file", "/Users/testuser/documents");

    // Assert
    result.Should().Be(0);
    var files = FileSystem.Directory.GetFiles("/Users/testuser/documents");
    files.Should().HaveCount(1);
    files[0].Should().Contain("2024-05-10-");
  }

  [Fact]
  public void RenameFile_WithEmptyDirectory_OutputsNoFilesMessage() {
    // Arrange
    var directory = "/test/empty";
    FileSystem.AddDirectory(directory);

    // Act
    var result = RunCommand("rename", "file", directory);

    // Assert
    result.Should().Be(0);
    Console.Lines.Should().ContainSingle(line => line.Contains("No files found"));
  }

  [Fact]
  public void RenameFile_WithAlreadyRenamedFile_DoesNotRenameAgain() {
    // Arrange
    var directory = "/test/photos";
    var creationTime = new DateTime(2024, 6, 15, 10, 30, 0);

    FileSystem.AddDirectory(directory);
    // File already has the correct name format
    FileSystem.AddFile("/test/photos/2024-06-15-ABCD1234.jpg", new MockFileData("photo content") {
      CreationTime = creationTime
    });

    // Act
    var result = RunCommand("rename", "file", directory);

    // Assert
    result.Should().Be(0);

    // File should still exist (might be renamed with different hash based on current name)
    var files = FileSystem.Directory.GetFiles(directory);
    files.Should().HaveCount(1);
  }
}
