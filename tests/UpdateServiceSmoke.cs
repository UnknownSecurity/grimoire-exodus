using Grimoire.Utils;
using System;

internal static class UpdateServiceSmoke
{
    private const string Digest = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private static int Main()
    {
        try
        {
            Version parsed;
            if (!UpdateService.TryParseReleaseVersion("v1.2.3-beta.1", out parsed)
                || parsed != new Version(1, 2, 3, 0))
            {
                return Fail("Prerelease version parsing failed.");
            }

            string releases = "["
                + Release("v9.0.0", "../Grimoire-Exodus-Portable-9.0.0.zip", "Grimoire-Exodus-Portable-9.0.0.zip", Digest, false)
                + "," + Release("v8.0.0", "Grimoire-Exodus-Portable-8.0.0.zip", "Grimoire-Exodus-Portable-8.0.0.zip", null, false)
                + "," + Release("v2.0.0", "Grimoire-Exodus-Portable-2.0.0.zip", "Grimoire-Exodus-Portable-2.0.0.zip", Digest, true)
                + "," + Release("v1.2.0-beta.1", "Grimoire-Exodus-Portable-1.2.0.zip", "Grimoire-Exodus-Portable-1.2.0.zip", Digest, false)
                + "," + Release("v1.1.0", "Grimoire-Exodus-Portable-1.1.0.zip", "Grimoire-Exodus-Portable-1.1.0.zip", Digest, false)
                + "]";

            UpdateRelease update = UpdateService.SelectUpdate(releases, new Version(1, 1, 0, 0));
            if (update != null)
                return Fail("A prerelease was offered to the stable update channel.");

            update = UpdateService.SelectUpdate(releases, new Version(1, 1, 0, 0), true);
            if (update == null || update.Version != new Version(1, 2, 0, 0))
                return Fail("The newest valid prerelease was not selected for the prerelease channel.");
            if (!update.IsPrerelease || update.PortableAsset == null)
                return Fail("The prerelease or portable asset metadata was not retained.");

            UpdateRelease equalVersion = UpdateService.SelectUpdate(
                "[" + Release("v1.1.0", "Grimoire-Exodus-Portable-1.1.0.zip", "Grimoire-Exodus-Portable-1.1.0.zip", Digest, false) + "]",
                new Version(1, 1, 0, 0));
            if (equalVersion != null)
                return Fail("An equal release version was incorrectly offered as an update.");

            Console.WriteLine("GitHub release selection and validation passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static string Release(
        string tag,
        string assetName,
        string downloadName,
        string digest,
        bool draft)
    {
        string digestJson = digest == null ? "null" : "\"sha256:" + digest + "\"";
        return "{\"tag_name\":\"" + tag
            + "\",\"name\":\"" + tag
            + "\",\"draft\":" + draft.ToString().ToLowerInvariant()
            + ",\"prerelease\":" + tag.Contains("-").ToString().ToLowerInvariant()
            + ",\"html_url\":\"https://github.com/UnknownSecurity/grimoire-exodus/releases/tag/" + tag
            + "\",\"assets\":[{\"name\":\"" + assetName
            + "\",\"browser_download_url\":\"https://github.com/UnknownSecurity/grimoire-exodus/releases/download/"
            + tag + "/" + downloadName
            + "\",\"digest\":" + digestJson
            + ",\"size\":1024}]}";
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 2;
    }
}
