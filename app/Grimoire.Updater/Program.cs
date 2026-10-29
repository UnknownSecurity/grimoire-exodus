using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Grimoire.Updater
{
    internal static class Program
    {
        private const int WaitForClientMilliseconds = 120000;
        private static string logPath;
        private static bool suppressUi;

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                suppressUi = args.Any(argument => string.Equals(
                    argument,
                    "--no-ui",
                    StringComparison.OrdinalIgnoreCase));
                Dictionary<string, string> options = ParseArguments(args);
                logPath = Require(options, "--log");
                int processId;
                if (!int.TryParse(Require(options, "--process-id"), out processId) || processId <= 0)
                    throw new ArgumentException("The client process identifier is invalid.");

                string targetDirectory = NormalizeDirectory(Require(options, "--target"));
                string restartExecutable = Path.GetFullPath(Require(options, "--restart"));
                bool noRestart = options.ContainsKey("--no-restart");
                WriteLog("Waiting for Grimoire Exodus process " + processId + " to exit.");
                WaitForClient(processId);

                if (options.ContainsKey("--installer"))
                {
                    ApplyInstaller(Path.GetFullPath(options["--installer"]));
                }
                else
                {
                    string sourceDirectory = NormalizeDirectory(Require(options, "--source"));
                    ApplyPortableUpdate(sourceDirectory, targetDirectory);
                }

                WriteLog("Update completed successfully.");
                if (!noRestart)
                {
                    RestartClient(restartExecutable, targetDirectory);
                }
                return 0;
            }
            catch (Exception exception)
            {
                WriteLog("Update failed: " + exception);
                try
                {
                    if (!suppressUi)
                    {
                        MessageBox.Show(
                            "Grimoire Exodus could not finish the update. Your previous installation was preserved where possible.\r\n\r\n"
                                + exception.Message
                                + (string.IsNullOrWhiteSpace(logPath) ? string.Empty : "\r\n\r\nLog: " + logPath),
                            "Grimoire Exodus update failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                catch
                {
                }
                return 1;
            }
        }

        private static Dictionary<string, string> ParseArguments(string[] args)
        {
            Dictionary<string, string> options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < args.Length; index++)
            {
                string name = args[index];
                if (!name.StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException("Unexpected updater argument: " + name);

                if (string.Equals(name, "--no-restart", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "--no-ui", StringComparison.OrdinalIgnoreCase))
                {
                    options[name] = "true";
                    continue;
                }

                if (++index >= args.Length)
                    throw new ArgumentException("Missing value for updater argument " + name + ".");
                options[name] = args[index];
            }
            return options;
        }

        private static string Require(IDictionary<string, string> options, string name)
        {
            string value;
            if (!options.TryGetValue(name, out value) || string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Missing updater argument " + name + ".");
            return value;
        }

        private static void WaitForClient(int processId)
        {
            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    if (!process.WaitForExit(WaitForClientMilliseconds))
                        throw new TimeoutException("Grimoire Exodus did not close in time.");
                }
            }
            catch (ArgumentException)
            {
                // The app already exited before the updater opened the process.
            }
            Thread.Sleep(750);
        }

        private static void ApplyInstaller(string installerPath)
        {
            if (!File.Exists(installerPath)
                || !installerPath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
            {
                throw new FileNotFoundException("The verified installer package is missing.", installerPath);
            }

            WriteLog("Applying verified MSI package " + installerPath + ".");
            using (Process installer = Process.Start(new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = "/i " + QuoteArgument(installerPath) + " /qn /norestart",
                UseShellExecute = false,
                CreateNoWindow = true
            }))
            {
                if (installer == null)
                    throw new InvalidOperationException("Windows Installer could not be started.");
                installer.WaitForExit();
                if (installer.ExitCode != 0 && installer.ExitCode != 3010)
                    throw new InvalidOperationException("Windows Installer returned error " + installer.ExitCode + ".");
            }
        }

        private static void ApplyPortableUpdate(string sourceDirectory, string targetDirectory)
        {
            ValidatePortablePayload(sourceDirectory);
            if (PathsEqual(sourceDirectory, targetDirectory))
                throw new InvalidOperationException("The update source and installation directory are the same.");
            if (IsRootDirectory(targetDirectory))
                throw new InvalidOperationException("Refusing to update a drive root.");

            string[] sourceFiles = Directory
                .GetFiles(sourceDirectory, "*", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (sourceFiles.Length == 0)
                throw new InvalidDataException("The update payload is empty.");

            string updateRoot = Directory.GetParent(sourceDirectory.TrimEnd(Path.DirectorySeparatorChar)).FullName;
            string backupDirectory = Path.Combine(updateRoot, "backup-" + Guid.NewGuid().ToString("N"));
            List<string> createdFiles = new List<string>();
            List<KeyValuePair<string, string>> backups = new List<KeyValuePair<string, string>>();
            Directory.CreateDirectory(backupDirectory);
            WriteLog("Merging portable update into " + targetDirectory + ".");

            try
            {
                foreach (string sourceFile in sourceFiles)
                {
                    string relativePath = sourceFile.Substring(sourceDirectory.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (string.Equals(relativePath, ".update-sha256", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string destination = ResolveChildPath(targetDirectory, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    if (File.Exists(destination))
                    {
                        string backup = ResolveChildPath(backupDirectory, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(backup));
                        File.Copy(destination, backup, true);
                        backups.Add(new KeyValuePair<string, string>(destination, backup));
                    }
                    else
                    {
                        createdFiles.Add(destination);
                    }

                    ReplaceFile(sourceFile, destination);
                }
            }
            catch
            {
                WriteLog("Update failed; restoring the previous files.");
                foreach (KeyValuePair<string, string> backup in backups.AsEnumerable().Reverse())
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(backup.Key));
                        File.Copy(backup.Value, backup.Key, true);
                    }
                    catch (Exception rollbackException)
                    {
                        WriteLog("Rollback warning for " + backup.Key + ": " + rollbackException.Message);
                    }
                }
                foreach (string created in createdFiles.AsEnumerable().Reverse())
                {
                    try
                    {
                        if (File.Exists(created))
                            File.Delete(created);
                    }
                    catch (Exception rollbackException)
                    {
                        WriteLog("Rollback warning for " + created + ": " + rollbackException.Message);
                    }
                }
                throw;
            }

            TryDeleteDirectory(backupDirectory);
            TryDeleteDirectory(updateRoot);
        }

        private static void ReplaceFile(string source, string destination)
        {
            string temporary = destination + ".update-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.Copy(source, temporary, true);
                if (File.Exists(destination))
                {
                    File.Replace(temporary, destination, null, true);
                }
                else
                {
                    File.Move(temporary, destination);
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(temporary))
                        File.Delete(temporary);
                }
                catch
                {
                }
            }
        }

        private static void ValidatePortablePayload(string sourceDirectory)
        {
            string[] requiredFiles =
            {
                "Grimoire Exodus.exe",
                "Grimoire.Updater.exe",
                "Flash.ocx",
                "grimoire-exodus.swf",
                "Microsoft.Web.WebView2.Core.dll",
                "Microsoft.Web.WebView2.WinForms.dll",
                Path.Combine("Ruffle", "ruffle.js"),
                Path.Combine("Ruffle", "core.ruffle.16d61072ac7fdc4d5013.js"),
                Path.Combine("Ruffle", "core.ruffle.994eeef0a471a88cd0e3.js"),
                Path.Combine("Ruffle", "172de42e6619eac8f371.wasm"),
                Path.Combine("Ruffle", "878d548199cddef07035.wasm"),
                Path.Combine("Ruffle", "LICENSE_APACHE"),
                Path.Combine("Ruffle", "LICENSE_MIT"),
                Path.Combine("Resources", "Ruffle", "index.html"),
                Path.Combine("ThirdParty", "WebView2.LICENSE.txt"),
                Path.Combine("ThirdParty", "WebView2.NOTICE.txt"),
                Path.Combine("runtimes", "win-x86", "native", "WebView2Loader.dll")
            };
            foreach (string requiredFile in requiredFiles)
            {
                if (!File.Exists(Path.Combine(sourceDirectory, requiredFile)))
                    throw new InvalidDataException("The update payload is missing " + requiredFile + ".");
            }
        }

        private static string ResolveChildPath(string root, string relativePath)
        {
            string rootPrefix = NormalizeDirectory(root) + Path.DirectorySeparatorChar;
            string candidate = Path.GetFullPath(Path.Combine(rootPrefix, relativePath));
            if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The update payload contains an unsafe path.");
            return candidate;
        }

        private static string NormalizeDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A directory path is empty.");
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool PathsEqual(string first, string second)
        {
            return string.Equals(NormalizeDirectory(first), NormalizeDirectory(second), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRootDirectory(string directory)
        {
            DirectoryInfo info = new DirectoryInfo(directory);
            return info.Parent == null;
        }

        private static void RestartClient(string executablePath, string workingDirectory)
        {
            if (!File.Exists(executablePath))
                throw new FileNotFoundException("The updated Grimoire executable was not found.", executablePath);

            WriteLog("Restarting Grimoire Exodus.");
            bool elevated = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);
            ProcessStartInfo startInfo = elevated
                ? new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = QuoteArgument(executablePath),
                    UseShellExecute = true
                }
                : new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = true
                };
            Process.Start(startInfo);
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

        private static void WriteLog(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(logPath))
                    return;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(logPath)));
                File.AppendAllText(
                    logPath,
                    "[" + DateTime.UtcNow.ToString("O") + "] " + message + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch
            {
            }
        }

        private static void TryDeleteDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch (Exception exception)
            {
                WriteLog("Cleanup warning for " + directory + ": " + exception.Message);
            }
        }
    }
}
