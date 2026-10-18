using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Grimoire.Utils
{
    public static class UpdateService
    {
        private const string ReleasesApi = "https://api.github.com/repos/UnknownSecurity/grimoire-exodus/releases?per_page=10";
        private const long MaximumAssetBytes = 250L * 1024L * 1024L;
        private static readonly HttpClient Client = CreateClient();

        public static Version CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version;

        public static async Task<UpdateRelease> CheckForUpdateAsync(CancellationToken cancellationToken)
        {
            return await CheckForUpdateAsync(
                AppPreferences.Load().UsePrereleaseUpdates,
                cancellationToken);
        }

        public static async Task<UpdateRelease> CheckForUpdateAsync(
            bool includePrereleases,
            CancellationToken cancellationToken)
        {
            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, ReleasesApi))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(20));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
                using (HttpResponseMessage response = await Client.SendAsync(request, timeout.Token))
                {
                    response.EnsureSuccessStatusCode();
                    string json = await response.Content.ReadAsStringAsync();
                    return SelectUpdate(json, CurrentVersion, includePrereleases);
                }
            }
        }

        public static UpdateRelease SelectUpdate(string releasesJson, Version currentVersion)
        {
            return SelectUpdate(releasesJson, currentVersion, includePrereleases: false);
        }

        public static UpdateRelease SelectUpdate(string releasesJson, Version currentVersion, bool includePrereleases)
        {
            if (string.IsNullOrWhiteSpace(releasesJson))
                return null;

            JArray releases = JArray.Parse(releasesJson);
            UpdateRelease selected = null;
            foreach (JObject releaseJson in releases.OfType<JObject>())
            {
                if (releaseJson.Value<bool?>("draft") == true)
                    continue;
                if (!includePrereleases && releaseJson.Value<bool?>("prerelease") == true)
                    continue;

                string tag = releaseJson.Value<string>("tag_name");
                Version version;
                if (!TryParseReleaseVersion(tag, out version) || version <= currentVersion)
                    continue;

                UpdateAsset portable = null;
                UpdateAsset installer = null;
                JArray assets = releaseJson["assets"] as JArray;
                if (assets != null)
                {
                    foreach (JObject assetJson in assets.OfType<JObject>())
                    {
                        UpdateAsset asset = ParseAsset(assetJson);
                        if (asset == null)
                            continue;

                        if (asset.Name.IndexOf("Portable", StringComparison.OrdinalIgnoreCase) >= 0
                            && asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            portable = asset;
                        }
                        else if (asset.Name.IndexOf("Setup", StringComparison.OrdinalIgnoreCase) >= 0
                            && asset.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                        {
                            installer = asset;
                        }
                    }
                }

                if (portable == null && installer == null)
                    continue;

                UpdateRelease candidate = new UpdateRelease
                {
                    Version = version,
                    TagName = tag,
                    Name = releaseJson.Value<string>("name") ?? tag,
                    IsPrerelease = releaseJson.Value<bool?>("prerelease") == true,
                    HtmlUrl = releaseJson.Value<string>("html_url"),
                    PortableAsset = portable,
                    InstallerAsset = installer
                };
                if (selected == null || candidate.Version > selected.Version)
                {
                    selected = candidate;
                }
            }
            return selected;
        }

        public static async Task<PreparedUpdate> PrepareUpdateAsync(
            UpdateRelease release,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            if (release == null)
                throw new ArgumentNullException(nameof(release));

            string applicationDirectory = AppPaths.ApplicationDirectory;
            bool useInstaller = (ShouldUseInstaller(applicationDirectory) && release.InstallerAsset != null)
                || release.PortableAsset == null;
            UpdateAsset asset = useInstaller ? release.InstallerAsset : release.PortableAsset;
            if (asset == null)
            {
                throw new InvalidOperationException(
                    useInstaller
                        ? "This release does not contain a Windows installer."
                        : "This release does not contain a portable update package.");
            }

            string versionDirectory = Path.Combine(AppPaths.UpdatesDirectory, release.Version.ToString(3));
            Directory.CreateDirectory(versionDirectory);
            await Task.Run(() => CleanupOldUpdates(versionDirectory), cancellationToken);
            string packagePath = Path.Combine(versionDirectory, asset.Name);

            bool packageReady = File.Exists(packagePath)
                && string.Equals(
                    await Task.Run(() => ComputeSha256(packagePath), cancellationToken),
                    asset.Sha256,
                    StringComparison.OrdinalIgnoreCase);
            if (!packageReady)
            {
                string partialPath = packagePath + ".download";
                TryDeleteFile(partialPath);
                await DownloadAssetAsync(asset, partialPath, progress, cancellationToken);
                string actualHash = await Task.Run(() => ComputeSha256(partialPath), cancellationToken);
                if (!string.Equals(actualHash, asset.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    TryDeleteFile(partialPath);
                    throw new InvalidDataException("The downloaded update failed SHA-256 verification.");
                }

                TryDeleteFile(packagePath);
                File.Move(partialPath, packagePath);
            }
            progress?.Report(100);

            string payloadDirectory = null;
            if (!useInstaller)
            {
                payloadDirectory = await Task.Run(
                    () => PreparePortablePayload(versionDirectory, packagePath, asset.Sha256, cancellationToken),
                    cancellationToken);
            }

            string bundledUpdater = Path.Combine(applicationDirectory, "Grimoire.Updater.exe");
            if (!File.Exists(bundledUpdater))
                throw new FileNotFoundException("The bundled update helper is missing.", bundledUpdater);

            string updaterCopy = Path.Combine(versionDirectory, "Apply-Grimoire-Update.exe");
            await Task.Run(() => File.Copy(bundledUpdater, updaterCopy, true), cancellationToken);
            return new PreparedUpdate
            {
                Release = release,
                InstallerMode = useInstaller,
                PackagePath = packagePath,
                PayloadDirectory = payloadDirectory,
                UpdaterPath = updaterCopy,
                RequiresElevation = useInstaller || !CanWriteToDirectory(applicationDirectory)
            };
        }

        private static string PreparePortablePayload(
            string versionDirectory,
            string packagePath,
            string expectedHash,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string payloadDirectory = Path.Combine(versionDirectory, "payload");
            string readyMarker = Path.Combine(payloadDirectory, ".update-sha256");
            bool payloadReady = File.Exists(readyMarker)
                && string.Equals(File.ReadAllText(readyMarker).Trim(), expectedHash, StringComparison.OrdinalIgnoreCase)
                && ValidatePayload(payloadDirectory);
            if (payloadReady)
            {
                return payloadDirectory;
            }

            SafeDeleteUpdateDirectory(payloadDirectory);
            cancellationToken.ThrowIfCancellationRequested();
            ExtractVerifiedArchive(packagePath, payloadDirectory);
            cancellationToken.ThrowIfCancellationRequested();
            if (!ValidatePayload(payloadDirectory))
            {
                SafeDeleteUpdateDirectory(payloadDirectory);
                throw new InvalidDataException("The portable update is missing required Grimoire files.");
            }
            File.WriteAllText(readyMarker, expectedHash, Encoding.ASCII);
            return payloadDirectory;
        }

        public static Process LaunchUpdater(PreparedUpdate update, int processId)
        {
            if (update == null)
                throw new ArgumentNullException(nameof(update));

            string executablePath = Assembly.GetExecutingAssembly().Location;
            string logPath = Path.Combine(AppPaths.UpdatesDirectory, "last-update.log");
            StringBuilder arguments = new StringBuilder();
            arguments.Append("--process-id ").Append(processId).Append(' ');
            arguments.Append("--target ").Append(QuoteArgument(AppPaths.ApplicationDirectory)).Append(' ');
            arguments.Append("--restart ").Append(QuoteArgument(executablePath)).Append(' ');
            arguments.Append("--log ").Append(QuoteArgument(logPath)).Append(' ');
            if (update.InstallerMode)
            {
                arguments.Append("--installer ").Append(QuoteArgument(update.PackagePath));
            }
            else
            {
                arguments.Append("--source ").Append(QuoteArgument(update.PayloadDirectory));
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = update.UpdaterPath,
                Arguments = arguments.ToString(),
                WorkingDirectory = Path.GetDirectoryName(update.UpdaterPath),
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            if (update.RequiresElevation)
            {
                startInfo.Verb = "runas";
            }
            return Process.Start(startInfo);
        }

        public static bool TryParseReleaseVersion(string tag, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            string value = tag.Trim();
            if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(1);
            }
            int suffix = value.IndexOfAny(new[] { '-', '+' });
            if (suffix >= 0)
            {
                value = value.Substring(0, suffix);
            }

            Version parsed;
            if (!Version.TryParse(value, out parsed))
                return false;
            version = new Version(
                Math.Max(0, parsed.Major),
                Math.Max(0, parsed.Minor),
                Math.Max(0, parsed.Build),
                Math.Max(0, parsed.Revision));
            return true;
        }

        private static UpdateAsset ParseAsset(JObject assetJson)
        {
            string name = assetJson.Value<string>("name");
            string url = assetJson.Value<string>("browser_download_url");
            string digest = assetJson.Value<string>("digest");
            long size = assetJson.Value<long?>("size") ?? 0L;
            Uri uri;
            if (string.IsNullOrWhiteSpace(name)
                || !string.Equals(Path.GetFileName(name), name, StringComparison.Ordinal)
                || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || !Uri.TryCreate(url, UriKind.Absolute, out uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase)
                || !uri.AbsolutePath.StartsWith(
                    "/UnknownSecurity/grimoire-exodus/releases/download/",
                    StringComparison.OrdinalIgnoreCase)
                || size <= 0L
                || size > MaximumAssetBytes
                || string.IsNullOrWhiteSpace(digest)
                || !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string sha256 = digest.Substring("sha256:".Length);
            if (sha256.Length != 64 || sha256.Any(character => !Uri.IsHexDigit(character)))
                return null;

            return new UpdateAsset
            {
                Name = name,
                DownloadUrl = uri,
                Sha256 = sha256,
                Size = size
            };
        }

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            HttpClient client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = true
            });
            client.Timeout = TimeSpan.FromMinutes(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Grimoire-Exodus-Updater/" + CurrentVersion.ToString());
            return client;
        }

        private static async Task DownloadAssetAsync(
            UpdateAsset asset,
            string destination,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, asset.DownloadUrl))
            using (HttpResponseMessage response = await Client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                Uri finalUri = response.RequestMessage?.RequestUri;
                if (finalUri == null
                    || finalUri.Scheme != Uri.UriSchemeHttps
                    || !IsTrustedDownloadHost(finalUri.Host))
                {
                    throw new InvalidDataException("GitHub redirected the update to an untrusted download host.");
                }
                long total = response.Content.Headers.ContentLength ?? asset.Size;
                if (total <= 0L || total > MaximumAssetBytes)
                    throw new InvalidDataException("The update download has an invalid size.");

                using (Stream input = await response.Content.ReadAsStreamAsync())
                using (FileStream output = new FileStream(
                    destination,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    byte[] buffer = new byte[81920];
                    long received = 0L;
                    int read;
                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        received += read;
                        if (received > MaximumAssetBytes)
                            throw new InvalidDataException("The update download exceeded the allowed size.");
                        await output.WriteAsync(buffer, 0, read, cancellationToken);
                        progress?.Report((int)Math.Min(99L, received * 100L / total));
                    }
                }
            }
        }

        private static void ExtractVerifiedArchive(string archivePath, string destination)
        {
            Directory.CreateDirectory(destination);
            string destinationRoot = Path.GetFullPath(destination)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            long extractedBytes = 0L;
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string target = Path.GetFullPath(Path.Combine(destinationRoot, entry.FullName));
                    if (!target.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The update archive contains an unsafe path.");

                    extractedBytes += entry.Length;
                    if (extractedBytes > MaximumAssetBytes * 2L)
                        throw new InvalidDataException("The extracted update exceeds the allowed size.");

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(target);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    using (Stream input = entry.Open())
                    using (FileStream output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        input.CopyTo(output);
                    }
                }
            }
        }

        private static bool ValidatePayload(string directory)
        {
            return Directory.Exists(directory)
                && File.Exists(Path.Combine(directory, "Grimoire Exodus.exe"))
                && File.Exists(Path.Combine(directory, "Grimoire.Updater.exe"))
                && File.Exists(Path.Combine(directory, "Flash.ocx"))
                && File.Exists(Path.Combine(directory, "grimoire-exodus.swf"));
        }

        private static bool ShouldUseInstaller(string applicationDirectory)
        {
            return IsUnderDirectory(applicationDirectory, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
                || IsUnderDirectory(applicationDirectory, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86))
                || !CanWriteToDirectory(applicationDirectory);
        }

        private static bool CanWriteToDirectory(string directory)
        {
            string probe = null;
            try
            {
                probe = Path.Combine(directory, ".update-write-test-" + Guid.NewGuid().ToString("N"));
                using (FileStream stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                }
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                TryDeleteFile(probe);
            }
        }

        private static bool IsUnderDirectory(string path, string parent)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(parent))
                return false;
            string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return fullPath.StartsWith(fullParent, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTrustedDownloadHost(string host)
        {
            return string.Equals(host, "github.com", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "githubusercontent.com", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeSha256(string path)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static void CleanupOldUpdates(string keepDirectory)
        {
            try
            {
                foreach (string directory in Directory.GetDirectories(AppPaths.UpdatesDirectory))
                {
                    if (string.Equals(Path.GetFullPath(directory), Path.GetFullPath(keepDirectory), StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (DateTime.UtcNow - Directory.GetLastWriteTimeUtc(directory) > TimeSpan.FromDays(30))
                    {
                        SafeDeleteUpdateDirectory(directory);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Unable to clean old updates: " + exception.Message);
            }
        }

        private static void SafeDeleteUpdateDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return;
            string updatesRoot = Path.GetFullPath(AppPaths.UpdatesDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string target = Path.GetFullPath(directory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!target.StartsWith(updatesRoot, StringComparison.OrdinalIgnoreCase) || target == updatesRoot)
                throw new InvalidOperationException("Refusing to remove a directory outside the update cache.");
            Directory.Delete(directory, true);
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        private static string QuoteArgument(string value)
        {
            value = value ?? string.Empty;
            StringBuilder quoted = new StringBuilder("\"");
            int backslashes = 0;
            foreach (char character in value)
            {
                if (character == '\\')
                {
                    backslashes++;
                    continue;
                }

                if (character == '"')
                {
                    quoted.Append('\\', backslashes * 2 + 1);
                    quoted.Append('"');
                }
                else
                {
                    quoted.Append('\\', backslashes);
                    quoted.Append(character);
                }
                backslashes = 0;
            }
            quoted.Append('\\', backslashes * 2);
            quoted.Append('"');
            return quoted.ToString();
        }
    }

    public sealed class UpdateRelease
    {
        public Version Version { get; internal set; }
        public string TagName { get; internal set; }
        public string Name { get; internal set; }
        public bool IsPrerelease { get; internal set; }
        public string HtmlUrl { get; internal set; }
        public UpdateAsset PortableAsset { get; internal set; }
        public UpdateAsset InstallerAsset { get; internal set; }
    }

    public sealed class UpdateAsset
    {
        public string Name { get; internal set; }
        public Uri DownloadUrl { get; internal set; }
        public string Sha256 { get; internal set; }
        public long Size { get; internal set; }
    }

    public sealed class PreparedUpdate
    {
        public UpdateRelease Release { get; internal set; }
        public bool InstallerMode { get; internal set; }
        public string PackagePath { get; internal set; }
        public string PayloadDirectory { get; internal set; }
        public string UpdaterPath { get; internal set; }
        public bool RequiresElevation { get; internal set; }
    }
}
