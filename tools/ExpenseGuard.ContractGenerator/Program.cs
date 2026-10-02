extern alias Api;
using ExpenseGuard.ServiceDefaults;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ApiProgram = Api::Program;

var output = args.Length == 1 ? args[0] : throw new ArgumentException("Pass the OpenAPI output file path.");
await using var factory = new ContractFactory();
using var client = factory.CreateClient();
var document = await client.GetStringAsync("/openapi/v1.json");
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
await File.WriteAllTextAsync(output, document + "\n");
Console.WriteLine($"OpenAPI contract written to {output}");

internal sealed class ContractFactory : WebApplicationFactory<ApiProgram>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "ExpenseGuard.slnx")))
        {
            root = root.Parent;
        }
        if (root is null)
        {
            throw new InvalidOperationException("Run contract generation inside the ExpenseGuard repository.");
        }
        builder.UseContentRoot(Path.Combine(root.FullName, "src", "ExpenseGuard.Api"));
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDependencyProbe>();
            services.AddSingleton<IDependencyProbe, NoNetworkProbe>();
        });
    }
}

internal sealed class NoNetworkProbe : IDependencyProbe
{
    public Task<ProbeResult> CheckAsync(string dependency, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Contract generation must not check external dependencies.");
}
