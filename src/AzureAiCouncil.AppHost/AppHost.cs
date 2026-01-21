var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.AzureAiCouncil_Api>("api")
	// Keep a stable local dev port so the SPA's defaultBaseUrl (http://localhost:5139) works
	// even when the SPA isn't started by Aspire and VITE_API_BASE_URL isn't injected.
	.WithEnvironment("ASPNETCORE_URLS", "http://localhost:5139")
	.WithExternalHttpEndpoints();

// Run the Vite SPA dev server as an executable resource so it starts/stops with the AppHost.
// The SPA reads its API base URL from VITE_API_BASE_URL (see web/azure-ai-council-spa/src/services/apiClient.ts).
static string FindRepoRoot(string startDir)
{
	var dir = new DirectoryInfo(startDir);
	while (dir is not null)
	{
		if (File.Exists(Path.Combine(dir.FullName, "AzureAiCouncil.slnx")))
		{
			return dir.FullName;
		}
		dir = dir.Parent;
	}

	throw new DirectoryNotFoundException("Could not locate repo root containing AzureAiCouncil.slnx.");
}

var repoRoot = FindRepoRoot(builder.Environment.ContentRootPath);
var spaWorkingDir = Path.Combine(repoRoot, "web", "azure-ai-council-spa");

static string? FindOnPath(string fileName)
{
	var path = Environment.GetEnvironmentVariable("PATH");
	if (string.IsNullOrWhiteSpace(path)) return null;

	foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
	{
		try
		{
			var candidate = Path.Combine(dir, fileName);
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}
		catch
		{
			// ignore malformed PATH entries
		}
	}

	return null;
}

var npmExe = OperatingSystem.IsWindows() ? "npm.cmd" : "npm";
var npmPath = FindOnPath(npmExe);

if (npmPath is not null && Directory.Exists(spaWorkingDir))
{
	var spa = builder.AddExecutable(
			"spa",
			npmExe,
			"run dev -- --port 5173 --strictPort")
		.WithWorkingDirectory(spaWorkingDir)
		.WithExternalHttpEndpoints();

	// Best-effort wiring: pass API base URL into the SPA.
	// Prefer HTTP locally to avoid dev-cert trust issues that can break fetch/EventSource.
	try
	{
		spa.WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http").Url);
	}
	catch
	{
		try
		{
			spa.WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("https").Url);
		}
		catch
		{
			// no-op
		}
	}
}

builder.Build().Run();
