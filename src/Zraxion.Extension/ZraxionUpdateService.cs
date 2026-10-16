using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Zraxion.Extension
{
    internal sealed class ZraxionReleaseInfo
    {
        internal ZraxionReleaseInfo(Version version, string tagName, string htmlUrl)
        {
            Version = version;
            TagName = tagName;
            HtmlUrl = htmlUrl;
        }

        internal Version Version { get; }
        internal string TagName { get; }
        internal string HtmlUrl { get; }
    }

    internal static class ZraxionUpdateService
    {
        private static readonly Regex tagRegex = new Regex(
            "\\\"tag_name\\\"\\s*:\\s*\\\"(?<value>[^\\\"]+)\\\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex urlRegex = new Regex(
            "\\\"html_url\\\"\\s*:\\s*\\\"(?<value>[^\\\"]+)\\\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        internal static async Task<ZraxionReleaseInfo?> CheckForUpdateAsync()
        {
            string json;

            try
            {
                using var client = new WebClient();
                client.Headers[HttpRequestHeader.UserAgent] = "Zraxion/" + ZraxionProductInfo.Version;
                client.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                json = await client.DownloadStringTaskAsync(ZraxionProductInfo.GitHubApiLatestReleaseUrl);
            }
            catch (WebException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }

            var tagMatch = tagRegex.Match(json);
            if (!tagMatch.Success)
            {
                return null;
            }

            var tagName = tagMatch.Groups["value"].Value.Trim();
            if (!TryParseVersion(tagName, out var latestVersion) ||
                !TryParseVersion(ZraxionProductInfo.Version, out var currentVersion) ||
                latestVersion <= currentVersion)
            {
                return null;
            }

            var urlMatch = urlRegex.Match(json);
            var htmlUrl = urlMatch.Success
                ? urlMatch.Groups["value"].Value.Trim()
                : ZraxionProductInfo.LatestReleaseUrl;

            return new ZraxionReleaseInfo(latestVersion, tagName, htmlUrl);
        }

        private static bool TryParseVersion(string value, out Version version)
        {
            var normalized = value.Trim();
            if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(1);
            }

            var separatorIndex = normalized.IndexOfAny(new[] { '-', '+' });
            if (separatorIndex >= 0)
            {
                normalized = normalized.Substring(0, separatorIndex);
            }

            if (Version.TryParse(normalized, out var parsed))
            {
                version = parsed;
                return true;
            }

            version = new Version(0, 0);
            return false;
        }
    }
}
