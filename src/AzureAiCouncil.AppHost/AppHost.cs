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
var npmExe = OperatingSystem.IsWindows() ? "npm.cmd" : "npm";

var spa = builder.AddExecutable(
		"spa",
		npmExe,
		"run dev -- --port 5173 --strictPort")
	.WithWorkingDirectory(spaWorkingDir)
	.WithExternalHttpEndpoints();

// Best-effort wiring: if the API exposes an HTTP endpoint, pass it into the SPA.
// (If this API shape changes across Aspire versions, the SPA still falls back to its default base URL.)
try
{
	// Prefer HTTPS so the SPA can be served over https without mixed-content blocking.
	spa.WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("https").Url);
}
catch
{
	// no-op
}

builder.Build().Run();
