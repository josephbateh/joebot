using FluentAssertions;
using Xunit;

namespace JoeBot.Tests.Commands;

public class ExecuteScriptCommandTests : CommandTestBase {
  [Fact]
  public void ExecuteScript_WithValidScript_RunsAndCapturesOutput() {
    // Arrange
    var scriptPath = "/scripts/test.sh";
    var expectedOutput = "Hello from script\nLine 2\n";

    // Set a fixed time for predictable filename
    Time.SetUtcNow(new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero));
    var expectedTimestamp = Time.GetUtcNow().ToUnixTimeMilliseconds();

    ProcessRunner.SetupNextResult(0, expectedOutput, "");

    // Act
    var result = RunCommand("execute", "script", scriptPath);

    // Assert
    result.Should().Be(0);

    // Verify the script was executed
    ProcessRunner.Calls.Should().HaveCount(1);
    var (fileName, _, _) = ProcessRunner.Calls[0];
    fileName.Should().Be(scriptPath);

    // Verify output file was created with correct content
    var outputFileName = $"output-{expectedTimestamp}.txt";
    FileSystem.File.Exists(outputFileName).Should().BeTrue();
    FileSystem.File.ReadAllText(outputFileName).Should().Be(expectedOutput);

    // Verify console output
    Console.Lines.Should().Contain("Process started.");
    Console.Lines.Should().Contain("Process has exited.");
  }

  [Fact]
  public void ExecuteScript_WhenScriptFails_CapturesStderr() {
    // Arrange
    var scriptPath = "/scripts/failing.sh";
    var stdoutContent = "Starting...\n";
    var stderrContent = "Error: Something went wrong\n";

    // Set a fixed time
    Time.SetUtcNow(new DateTimeOffset(2024, 6, 20, 14, 30, 0, TimeSpan.Zero));
    var expectedTimestamp = Time.GetUtcNow().ToUnixTimeMilliseconds();

    ProcessRunner.SetupNextResult(1, stdoutContent, stderrContent);

    // Act
    var result = RunCommand("execute", "script", scriptPath);

    // Assert
    result.Should().Be(0); // Command itself completes

    // Verify output file contains both stdout and stderr
    var outputFileName = $"output-{expectedTimestamp}.txt";
    FileSystem.File.Exists(outputFileName).Should().BeTrue();
    var outputContent = FileSystem.File.ReadAllText(outputFileName);
    outputContent.Should().Contain(stdoutContent);
    outputContent.Should().Contain(stderrContent);
  }

  [Fact]
  public void ExecuteScript_WithEmptyOutput_CreatesEmptyFile() {
    // Arrange
    var scriptPath = "/scripts/silent.sh";

    Time.SetUtcNow(new DateTimeOffset(2024, 3, 10, 8, 0, 0, TimeSpan.Zero));
    var expectedTimestamp = Time.GetUtcNow().ToUnixTimeMilliseconds();

    ProcessRunner.SetupNextResult(0, "", "");

    // Act
    var result = RunCommand("execute", "script", scriptPath);

    // Assert
    result.Should().Be(0);

    var outputFileName = $"output-{expectedTimestamp}.txt";
    FileSystem.File.Exists(outputFileName).Should().BeTrue();
    FileSystem.File.ReadAllText(outputFileName).Should().BeEmpty();
  }
}
