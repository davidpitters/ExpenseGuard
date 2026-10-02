using System.Xml.Linq;

namespace ExpenseGuard.ArchitectureTests;

public sealed class DependencyTests
{
    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    [InlineData("Agent")]
    [InlineData("Worker")]
    [InlineData("McpClient")]
    public void Project_references_respect_the_business_boundary(string project)
    {
        var allowed = project switch
        {
            "Domain" => Array.Empty<string>(),
            "Application" => ["Domain"],
            "Agent" => ["Domain", "McpClient"],
            "Worker" => ["Agent", "ServiceDefaults"],
            "McpClient" => [],
            _ => throw new ArgumentOutOfRangeException(nameof(project))
        };
        var document = Load(project);
        var references = document.Descendants("ProjectReference")
            .Select(element => Path.GetFileNameWithoutExtension(element.Attribute("Include")!.Value).Replace("ExpenseGuard.", ""));
        Assert.All(references, reference => Assert.Contains(reference, allowed));
        if (project == "Domain")
        {
            Assert.Empty(document.Descendants("PackageReference"));
        }
    }

    [Fact]
    public void Shared_host_plumbing_does_not_reference_business_projects() =>
        Assert.Empty(Load("ServiceDefaults").Descendants("ProjectReference"));

    private static XDocument Load(string project)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "ExpenseGuard.slnx")))
        {
            root = root.Parent;
        }
        Assert.NotNull(root);
        return XDocument.Load(Path.Combine(root.FullName, "src", $"ExpenseGuard.{project}", $"ExpenseGuard.{project}.csproj"));
    }
}
