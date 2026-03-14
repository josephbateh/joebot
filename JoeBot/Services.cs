using System.IO.Abstractions;
using JoeBot.Abstractions;
using JoeBot.Adapters;

namespace JoeBot;

public static class Services {
  // From System.IO.Abstractions library
  public static IFileSystem FileSystem { get; set; } = new FileSystem();

  // From .NET BCL (will use MockHttpMessageHandler in tests)
  public static HttpClient HttpClient { get; set; } = new HttpClient();

  // From Microsoft.Extensions.Time.Testing (use TimeProvider.System in prod)
  public static TimeProvider Time { get; set; } = TimeProvider.System;

  // Custom abstractions
  public static IProcessRunner ProcessRunner { get; set; } = new RealProcessRunner();
  public static INetworkPing NetworkPing { get; set; } = new RealNetworkPing();
  public static IConsole Console { get; set; } = new RealConsole();
  public static IEnvironment Environment { get; set; } = new RealEnvironment();

  /// <summary>
  /// Resets all services to their default real implementations.
  /// </summary>
  public static void Reset() {
    FileSystem = new FileSystem();
    HttpClient = new HttpClient();
    Time = TimeProvider.System;
    ProcessRunner = new RealProcessRunner();
    NetworkPing = new RealNetworkPing();
    Console = new RealConsole();
    Environment = new RealEnvironment();
  }
}
