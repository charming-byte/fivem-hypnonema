using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using CitizenFX.Core;
using Newtonsoft.Json.Linq;

namespace Hypnonema.Server.Utilities;

public sealed class UpdateHelper
{
    private const string RepositoryName = "fivem-hypnonema";

    private const string RepositoryOwner = "charming-byte";

    private static readonly HttpClient Client = new();

    private static Version LocalVersion => new(GetAssemblyFileVersion());

    public static async Task CheckForUpdates()
    {
        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

        try
        {
            Client.DefaultRequestHeaders.UserAgent.ParseAdd("hypnonema");
            var json = await Client.GetStringAsync(
                $"https://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases/latest");
            var latestRelease = JObject.Parse(json);
            var latestVersion = new Version(latestRelease.Value<string>("tag_name"));

            var versionComparison = LocalVersion.CompareTo(latestVersion);

            if (versionComparison < 0)
            {
                await BaseScript.Delay(1);

                Debug.WriteLine(
                    $"^3Update available for hypnonema (current: {PrintVersion(LocalVersion)}).\nDetails: {latestRelease.Value<string>("html_url")} ^7");
            }
        }
        catch (Exception)
        {
            // TODO: Sentry
        }
    }

    private static string PrintVersion(Version version) => $"{version.Major}.{version.Minor}.{version.Build}";

    private static string GetAssemblyFileVersion()
    {
        var attribute = (AssemblyFileVersionAttribute)Assembly.GetExecutingAssembly()
            .GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).Single();

        return attribute.Version;
    }
}