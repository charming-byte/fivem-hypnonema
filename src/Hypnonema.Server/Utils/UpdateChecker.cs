using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using CitizenFX.Core;
using Octokit;

namespace Hypnonema.Server.Utils
{
    public sealed class UpdateChecker
    {
        private const string RepositoryName = "fivem-hypnonema";

        private const string RepositoryOwner = "charming-byte";

        private static readonly GitHubClient Client = new GitHubClient( new ProductHeaderValue("hypnonema"));

        private static Version LocalVersion => new Version(GetAssemblyFileVersion());

        public static async Task CheckForUpdate()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            try
            {
                var latestRelease = await Client.Repository.Release.GetLatest(RepositoryOwner, RepositoryName);

                var latestVersion = new Version(latestRelease.TagName);

                var versionComparison = LocalVersion.CompareTo(latestVersion);

                if (versionComparison < 0)
                {
                    await BaseScript.Delay(1);

                    Debug.WriteLine($"^3An update is available for hypnonema (current version: {LocalVersion})\n {latestRelease.HtmlUrl} ^7");
                }
            }
            catch (Exception)
            {
                // Do nothing with the exception
            }
        }

        private static string GetAssemblyFileVersion()
        {
            var attribute = (AssemblyFileVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).Single();

            return attribute.Version;
        }
    }
}
