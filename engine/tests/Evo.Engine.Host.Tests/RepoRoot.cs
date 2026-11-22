namespace Evo.Engine.Host.Tests;

internal static class RepoRoot
{
    public static string Find()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string schemaPath = Path.Combine(current.FullName, "schemas", "ipc-host.schema.json");
            string hostDir = Path.Combine(current.FullName, "engine", "src", "Evo.Engine.Host");
            if (File.Exists(schemaPath) && Directory.Exists(hostDir))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate repository root starting from '{AppContext.BaseDirectory}'. " +
            "Expected both 'schemas/ipc-host.schema.json' and 'engine/src/Evo.Engine.Host'.");
    }
}
