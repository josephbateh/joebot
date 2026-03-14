using JoeBot.Abstractions;

namespace JoeBot.Tests.Fakes;

public class FakeNetworkPing : INetworkPing {
  private readonly Dictionary<string, bool> _hostResults = new();
  public bool DefaultResult { get; set; } = true;
  public List<(string Host, int TimeoutMs)> Calls { get; } = [];

  public void SetHostResult(string host, bool canReach) {
    _hostResults[host] = canReach;
  }

  public bool CanReach(string host, int timeoutMs) {
    Calls.Add((host, timeoutMs));
    return _hostResults.TryGetValue(host, out var result) ? result : DefaultResult;
  }
}
