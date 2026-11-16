using System.Reflection;
using Core.Domain;

namespace Core.Tests;

// «Вартовий» архітектури: падає, якщо Core почне залежати від зовнішніх технологій.
public class ArchitectureTests
{
    private static readonly string[] Forbidden =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Microsoft.Data.Sqlite",
        "Scalar",
        "Infrastructure",
        "Api",
        "Web",
        "Cli"
    ];

    [Fact]
    public void Core_DoesNotReference_Infrastructure_Web_Or_Database_Assemblies()
    {
        Assembly core = typeof(Product).Assembly;

        string[] referenced = core.GetReferencedAssemblies().Select(a => a.Name ?? "").ToArray();

        string[] violations = referenced
            .Where(name => Forbidden.Any(f => name == f || name.StartsWith(f + ".", StringComparison.Ordinal)))
            .ToArray();

        Assert.True(violations.Length == 0, "Core посилається на заборонені збірки: " + string.Join(", ", violations));
    }
}
