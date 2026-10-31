using Grimoire.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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

            IList<UpdateRelease> stableReleases = UpdateService.ParseAvailableReleases(releases, false);
            if (stableReleases.Count != 1 || stableReleases[0].Version != new Version(1, 1, 0, 0))
                return Fail("The stable version list did not retain the verified older release.");

            IList<UpdateRelease> allReleases = UpdateService.ParseAvailableReleases(releases, true);
            if (allReleases.Count != 2
                || allReleases[0].Version != new Version(1, 2, 0, 0)
                || allReleases[1].Version != new Version(1, 1, 0, 0))
            {
                return Fail("Verified releases were not returned in descending version order.");
            }

            VerifyVersionAwarePayloadValidation();

            Console.WriteLine("GitHub release selection and validation passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void VerifyVersionAwarePayloadValidation()
    {
        MethodInfo validate = typeof(UpdateService).GetMethod(
            "ValidatePayload",
            BindingFlags.Static | BindingFlags.NonPublic);
        if (validate == null)
            throw new InvalidOperationException("The portable payload validator was not found.");

        string directory = Path.Combine(Path.GetTempPath(), "grimoire-version-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            foreach (string file in new[]
            {
                "Grimoire Exodus.exe",
                "Grimoire.Updater.exe",
                "Flash.ocx",
                "grimoire-exodus.swf"
            })
            {
                File.WriteAllText(Path.Combine(directory, file), string.Empty);
            }

            bool legacyValid = (bool)validate.Invoke(null, new object[] { directory, new Version(1, 4, 0, 0) });
            bool currentValid = (bool)validate.Invoke(null, new object[] { directory, new Version(1, 5, 0, 0) });
            if (!legacyValid || currentValid)
                throw new InvalidOperationException("Version-aware portable payload validation is not enforced.");
        }
        finally
        {
            Directory.Delete(directory, true);
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
