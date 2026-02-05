using System.CommandLine;
using System.IO.Abstractions.TestingHelpers;
using JoeBot.Commands;
using JoeBot.Commands.Rename;
using JoeBot.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;
using RichardSzalay.MockHttp;

namespace JoeBot.Tests;

public abstract class CommandTestBase : IDisposable {
  protected MockFileSystem FileSystem { get; }
  protected MockHttpMessageHandler HttpHandler { get; }
  protected FakeTimeProvider Time { get; }
  protected FakeProcessRunner ProcessRunner { get; }
  protected FakeNetworkPing NetworkPing { get; }
  protected FakeConsole Console { get; }
  protected FakeEnvironment Environment { get; }

  protected CommandTestBase() {
    FileSystem = new MockFileSystem();
    HttpHandler = new MockHttpMessageHandler();
    Time = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero));
    ProcessRunner = new FakeProcessRunner();
    NetworkPing = new FakeNetworkPing();
    Console = new FakeConsole();
    Environment = new FakeEnvironment();

    // Inject into Services
    Services.FileSystem = FileSystem;
    Services.HttpClient = new HttpClient(HttpHandler);
    Services.Time = Time;
    Services.ProcessRunner = ProcessRunner;
    Services.NetworkPing = NetworkPing;
    Services.Console = Console;
    Services.Environment = Environment;
  }

  protected int RunCommand(params string[] args) {
    var root = new RootCommand("JoeBot");
    root.Subcommands.Add(GetCommand.Get());
    root.Subcommands.Add(ExecuteCommand.Get());
    root.Subcommands.Add(RenameCommand.Get());
    root.Subcommands.Add(ConvertCommand.Get());
    return root.Parse(args).Invoke();
  }

  protected async Task<int> RunCommandAsync(params string[] args) {
    var root = new RootCommand("JoeBot");
    root.Subcommands.Add(GetCommand.Get());
    root.Subcommands.Add(ExecuteCommand.Get());
    root.Subcommands.Add(RenameCommand.Get());
    root.Subcommands.Add(ConvertCommand.Get());
    return await root.Parse(args).InvokeAsync();
  }

  public void Dispose() {
    Services.Reset();
    GC.SuppressFinalize(this);
  }
}
