using System.Net;
using System.Net.Http.Headers;
using System.Text;
using ExpenseGuard.McpClient;
using Microsoft.AspNetCore.Mvc.Testing;
using ModelContextProtocol.Client;

namespace ExpenseGuard.McpTests;

public sealed class BoundaryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("untrusted-token")]
    public async Task Mcp_is_inaccessible_before_authorization_is_implemented(string? token)
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("authentication_required", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain("tools", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("http://mcp.example/mcp", true)]
    [InlineData("http://localhost:5102/mcp", false)]
    [InlineData("https://user:password@mcp.example/mcp", false)]
    [InlineData("https://mcp.example/mcp?token=secret", false)]
    [InlineData("file:///receipt.pdf", false)]
    public void Unsafe_transport_configuration_is_rejected(string endpoint, bool allowLocalHttp) =>
        Assert.Throws<ArgumentException>(() => RunMcpConnection.CreateTransportOptions(new Uri(endpoint), "test-token", allowLocalHttp));

    [Theory]
    [InlineData("https://mcp.example/mcp", false)]
    [InlineData("http://localhost:5102/mcp", true)]
    public void Explicit_transport_uses_streamable_http(string endpoint, bool allowLocalHttp)
    {
        var options = RunMcpConnection.CreateTransportOptions(new Uri(endpoint), "test-token", allowLocalHttp);
        Assert.Equal(HttpTransportMode.StreamableHttp, options.TransportMode);
        Assert.Equal(TimeSpan.FromSeconds(10), options.ConnectionTimeout);
    }

    [Theory]
    [InlineData("")]
    [InlineData("two tokens")]
    [InlineData("token\r\nHeader: injected")]
    public void Invalid_bearer_header_is_rejected(string token) =>
        Assert.ThrowsAny<ArgumentException>(() => RunMcpConnection.CreateTransportOptions(new Uri("https://mcp.example/mcp"), token));
}
