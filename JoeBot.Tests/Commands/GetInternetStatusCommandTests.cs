using FluentAssertions;
using Xunit;

namespace JoeBot.Tests.Commands;

public class GetInternetStatusCommandTests : CommandTestBase {
  [Fact]
  public void GetInternetStatus_WhenAllHostsReachable_OutputsConnected() {
    // Arrange
    NetworkPing.SetHostResult("google.com", true);
    NetworkPing.SetHostResult("wikipedia.org", true);

    // Act
    var result = RunCommand("get", "internet-status", "--log");

    // Assert
    result.Should().Be(0);
    Console.Lines.Should().Contain("Internet connected.");
    Environment.ExitCode.Should().BeNull(); // No exit code set = success
  }

  [Fact]
  public void GetInternetStatus_WhenNoHostsReachable_ExitsWithCode1() {
    // Arrange
    NetworkPing.SetHostResult("google.com", false);
    NetworkPing.SetHostResult("wikipedia.org", false);

    // Act
    var result = RunCommand("get", "internet-status", "--log");

    // Assert
    Console.Lines.Should().Contain("Internet connection failure.");
    Environment.ExitCode.Should().Be(1);
  }

  [Fact]
  public void GetInternetStatus_WhenPartiallyReachable_OutputsPartialStatus() {
    // Arrange - only Google is reachable
    NetworkPing.SetHostResult("google.com", true);
    NetworkPing.SetHostResult("wikipedia.org", false);

    // Act
    var result = RunCommand("get", "internet-status", "--log");

    // Assert
    result.Should().Be(0);
    Console.Lines.Should().Contain(line =>
        line.Contains("Partial connectivity") && line.Contains("google.com"));
    Environment.ExitCode.Should().BeNull();
  }

  [Fact]
  public void GetInternetStatus_WhenOnlySecondaryReachable_OutputsPartialStatus() {
    // Arrange - only Wikipedia is reachable
    NetworkPing.SetHostResult("google.com", false);
    NetworkPing.SetHostResult("wikipedia.org", true);

    // Act
    var result = RunCommand("get", "internet-status", "--log");

    // Assert
    result.Should().Be(0);
    Console.Lines.Should().Contain(line =>
        line.Contains("Partial connectivity") && line.Contains("wikipedia.org"));
  }

  [Fact]
  public void GetInternetStatus_WithoutLogFlag_DoesNotOutputMessages() {
    // Arrange
    NetworkPing.SetHostResult("google.com", true);
    NetworkPing.SetHostResult("wikipedia.org", true);

    // Act - no --log flag
    var result = RunCommand("get", "internet-status");

    // Assert
    result.Should().Be(0);
    Console.Lines.Should().BeEmpty(); // No output when logging is disabled
  }

  [Fact]
  public void GetInternetStatus_PingsBothHosts() {
    // Arrange
    NetworkPing.DefaultResult = true;

    // Act
    var result = RunCommand("get", "internet-status");

    // Assert
    result.Should().Be(0);

    // Verify both hosts were pinged
    NetworkPing.Calls.Should().HaveCount(2);
    NetworkPing.Calls.Should().Contain(call => call.Host == "google.com");
    NetworkPing.Calls.Should().Contain(call => call.Host == "wikipedia.org");

    // Verify timeout was 1000ms
    NetworkPing.Calls.Should().AllSatisfy(call => call.TimeoutMs.Should().Be(1000));
  }
}
