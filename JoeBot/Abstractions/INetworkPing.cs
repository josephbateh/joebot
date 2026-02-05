namespace JoeBot.Abstractions;

public interface INetworkPing {
  bool CanReach(string host, int timeoutMs);
}
