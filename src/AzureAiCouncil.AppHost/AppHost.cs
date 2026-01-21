var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.AzureAiCouncil_Api>("api")
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
	// On Windows, npm is a .cmd shim. Execute it through cmd.exe to avoid CreateProcess issues.
	var spaExe = OperatingSystem.IsWindows() ? "cmd.exe" : npmExe;
	var spa = (OperatingSystem.IsWindows()
		? builder.AddExecutable("spa", spaExe, spaWorkingDir, "/c", "npm", "run", "dev", "--", "--port", "5173")
		: builder.AddExecutable("spa", spaExe, spaWorkingDir, "run", "dev", "--", "--port", "5173"))
		.WithHttpEndpoint(targetPort: 5173, name: "http")
		.WithExternalHttpEndpoints()
		.WithUrlForEndpoint("http", url => url.DisplayText = "Open SPA");

	spa.WithEnvironment("BROWSER", "none");

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
