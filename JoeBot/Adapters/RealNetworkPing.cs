using System.Net.NetworkInformation;
using JoeBot.Abstractions;

namespace JoeBot.Adapters;

public class RealNetworkPing : INetworkPing {
  public bool CanReach(string host, int timeoutMs) {
    try {
      using var ping = new Ping();
      var buffer = new byte[32];
      var options = new PingOptions();
      var reply = ping.Send(host, timeoutMs, buffer, options);
      return reply.Status == IPStatus.Success;
    }
    catch {
      return false;
    }
  }
}
