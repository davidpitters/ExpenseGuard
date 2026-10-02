using ModelContextProtocol.Client;
using SdkMcpClient = ModelContextProtocol.Client.McpClient;

namespace ExpenseGuard.McpClient;

public static class RunMcpConnection
{
    // endpoint and bearerToken must come from trusted worker configuration / token issuance,
    // never model arguments. Run binding and manifest persistence arrive in milestone 5.
    public static HttpClientTransportOptions CreateTransportOptions(
        Uri endpoint, string bearerToken, bool allowLocalHttp = false)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);
        if (!endpoint.IsAbsoluteUri || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment)
            || (endpoint.Scheme != Uri.UriSchemeHttps
                && !(allowLocalHttp && endpoint.IsLoopback && endpoint.Scheme == Uri.UriSchemeHttp)))
        {
            throw new ArgumentException("MCP requires HTTPS or explicitly enabled loopback HTTP.", nameof(endpoint));
        }

        if (bearerToken.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("A bearer token must not contain whitespace.", nameof(bearerToken));
        }

        return new HttpClientTransportOptions
        {
            Endpoint = endpoint,
            Name = "expenseguard",
            TransportMode = HttpTransportMode.StreamableHttp,
            ConnectionTimeout = TimeSpan.FromSeconds(10),
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {bearerToken}"
            }
        };
    }

    public static async Task<SdkMcpClient> ConnectAsync(
        Uri endpoint, string bearerToken, bool allowLocalHttp, CancellationToken cancellationToken)
    {
        var transport = new HttpClientTransport(CreateTransportOptions(endpoint, bearerToken, allowLocalHttp));
        try
        {
            return await SdkMcpClient.CreateAsync(transport, cancellationToken: cancellationToken);
        }
        catch
        {
            await transport.DisposeAsync();
            throw;
        }
    }
}
