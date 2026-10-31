using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using Grimoire.Utils;

namespace Grimoire.Tools
{
    public sealed class AccountSummary
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("characterName")]
        public string CharacterName { get; set; }

        [JsonProperty("characterClass")]
        public string CharacterClass { get; set; }

        [JsonProperty("characterLevel")]
        public int? CharacterLevel { get; set; }

        [JsonProperty("portraitDataUri")]
        public string PortraitDataUri { get; set; }

        [JsonProperty("hasPortrait")]
        public bool HasPortrait { get; set; }
    }

    public sealed class AccountCredentials
    {
        public string Id { get; set; }

        public string Label { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }

    public static class AccountVault
    {
        private static readonly object SyncRoot = new object();
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Grimoire Exodus account vault v1");
        private const int MaximumPortraitBytes = 256 * 1024;

        private static string VaultDirectory => AppPaths.DataDirectory;

        private static string VisualDirectory => AppPaths.AccountVisualsDirectory;

        private static string VaultPath => Path.Combine(VaultDirectory, "account-vault.json");

        private static string LegacyVaultPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Grimoire Exodus",
            "account-vault.json");

        public static IReadOnlyList<AccountSummary> GetAccounts()
        {
            lock (SyncRoot)
            {
                return LoadVault().Accounts
                    .Select(CreateSummary)
                    .ToList();
            }
        }

        public static AccountSummary SaveAccount(string label, string username, string password)
        {
            label = (label ?? string.Empty).Trim();
            username = (username ?? string.Empty).Trim();
            password = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username is required.", nameof(username));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password is required.", nameof(password));
            }

            if (username.Length > 100 || password.Length > 256 || label.Length > 60)
            {
                throw new ArgumentException("One or more account fields are too long.");
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                label = username;
            }

            lock (SyncRoot)
            {
                AccountVaultFile vault = LoadVault();
                AccountRecord account = vault.Accounts.FirstOrDefault(item =>
                    string.Equals(item.Username, username, StringComparison.OrdinalIgnoreCase));

                if (account == null)
                {
                    account = new AccountRecord { Id = Guid.NewGuid().ToString("N") };
                    vault.Accounts.Add(account);
                }

                account.Label = label;
                account.Username = username;
                account.ProtectedPassword = Protect(password);
                SaveVault(vault);

                return CreateSummary(account);
            }
        }

        public static bool UpdateCharacterProfile(
            string id,
            string username,
            string characterName,
            string characterClass,
            int level,
            byte[] portraitPng)
        {
            id = Normalize(id, 64);
            username = Normalize(username, 100);
            characterName = Normalize(characterName, 100);
            characterClass = Normalize(characterClass, 80);
            if (id == null && username == null)
                return false;
            if (level < 1 || level > 1000)
                level = 0;
            if (portraitPng != null)
                ValidatePortrait(portraitPng);

            lock (SyncRoot)
            {
                AccountVaultFile vault = LoadVault();
                AccountRecord account = id == null
                    ? null
                    : vault.Accounts.FirstOrDefault(item =>
                        string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
                if (account == null && username != null)
                {
                    account = vault.Accounts.FirstOrDefault(item =>
                        string.Equals(item.Username, username, StringComparison.OrdinalIgnoreCase));
                }
                if (account == null)
                    return false;

                bool metadataChanged = !string.Equals(account.CharacterName, characterName, StringComparison.Ordinal)
                    || !string.Equals(account.CharacterClass, characterClass, StringComparison.Ordinal)
                    || account.CharacterLevel != (level == 0 ? (int?)null : level);
                account.CharacterName = characterName;
                account.CharacterClass = characterClass;
                account.CharacterLevel = level == 0 ? (int?)null : level;
                if (metadataChanged)
                    SaveVault(vault);
                if (portraitPng != null)
                    WritePortrait(account.Id, portraitPng);
                return metadataChanged || portraitPng != null;
            }
        }

        public static AccountCredentials GetCredentials(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Account identifier is required.", nameof(id));
            }

            lock (SyncRoot)
            {
                AccountRecord account = LoadVault().Accounts.FirstOrDefault(item =>
                    string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));

                if (account == null)
                {
                    throw new InvalidOperationException("The selected account is no longer in the locker.");
                }

                return new AccountCredentials
                {
                    Id = account.Id,
                    Label = account.Label,
                    Username = account.Username,
                    Password = Unprotect(account.ProtectedPassword)
                };
            }
        }

        public static bool RemoveAccount(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            lock (SyncRoot)
            {
                AccountVaultFile vault = LoadVault();
                int removed = vault.Accounts.RemoveAll(item =>
                    string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));

                if (removed > 0)
                {
                    SaveVault(vault);
                    DeletePortrait(id);
                }

                return removed > 0;
            }
        }

        public static void DeleteAllAccounts()
        {
            lock (SyncRoot)
            {
                if (File.Exists(LegacyVaultPath))
                {
                    File.Delete(LegacyVaultPath);
                }
                SaveVault(new AccountVaultFile());
                if (Directory.Exists(VisualDirectory))
                    Directory.Delete(VisualDirectory, true);
            }
        }

        private static AccountSummary CreateSummary(AccountRecord account)
        {
            string portraitDataUri = ReadPortraitDataUri(account.Id);
            return new AccountSummary
            {
                Id = account.Id,
                Label = account.Label,
                Username = account.Username,
                CharacterName = account.CharacterName,
                CharacterClass = account.CharacterClass,
                CharacterLevel = account.CharacterLevel,
                PortraitDataUri = portraitDataUri,
                HasPortrait = portraitDataUri != null
            };
        }

        private static string Normalize(string value, int maximumLength)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (normalized != null && normalized.Length > maximumLength)
                normalized = normalized.Substring(0, maximumLength);
            return normalized;
        }

        private static void ValidatePortrait(byte[] portraitPng)
        {
            if (portraitPng.Length == 0 || portraitPng.Length > MaximumPortraitBytes)
                throw new ArgumentException("The account visual is empty or too large.", nameof(portraitPng));
            try
            {
                using (MemoryStream stream = new MemoryStream(portraitPng, false))
                using (Image image = Image.FromStream(stream, false, true))
                {
                    if (image.RawFormat.Guid != ImageFormat.Png.Guid
                        || image.Width < 16
                        || image.Height < 16
                        || image.Width > 512
                        || image.Height > 512)
                    {
                        throw new ArgumentException("The account visual must be a bounded PNG image.", nameof(portraitPng));
                    }
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception exception) when (exception is OutOfMemoryException || exception is ExternalException)
            {
                throw new ArgumentException("The account visual is not a valid PNG image.", nameof(portraitPng), exception);
            }
        }

        private static string GetPortraitPath(string id)
        {
            if (!Guid.TryParseExact(id, "N", out Guid accountId))
                return null;
            string root = Path.GetFullPath(VisualDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(Path.Combine(root, accountId.ToString("N") + ".png"));
            return path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? path : null;
        }

        private static string ReadPortraitDataUri(string id)
        {
            string path = GetPortraitPath(id);
            if (path == null || !File.Exists(path))
                return null;
            try
            {
                FileInfo file = new FileInfo(path);
                if (file.Length <= 0 || file.Length > MaximumPortraitBytes)
                    return null;
                byte[] bytes = File.ReadAllBytes(path);
                ValidatePortrait(bytes);
                return "data:image/png;base64," + Convert.ToBase64String(bytes);
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is ArgumentException)
            {
                return null;
            }
        }

        private static void WritePortrait(string id, byte[] portraitPng)
        {
            string path = GetPortraitPath(id);
            if (path == null)
                throw new InvalidOperationException("The account identifier cannot own an account visual.");
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            string temporaryPath = Path.Combine(
                directory,
                Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllBytes(temporaryPath, portraitPng);
                if (File.Exists(path))
                    File.Replace(temporaryPath, path, null, true);
                else
                    File.Move(temporaryPath, path);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static void DeletePortrait(string id)
        {
            string path = GetPortraitPath(id);
            if (path != null && File.Exists(path))
                File.Delete(path);
        }

        private static string Protect(string password)
        {
            byte[] clearBytes = Encoding.UTF8.GetBytes(password);
            byte[] protectedBytes = ProtectedData.Protect(clearBytes, Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }

        private static string Unprotect(string protectedPassword)
        {
            try
            {
                byte[] protectedBytes = Convert.FromBase64String(protectedPassword);
                byte[] clearBytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(clearBytes);
            }
            catch (Exception ex) when (ex is FormatException || ex is CryptographicException)
            {
                throw new InvalidOperationException(
                    "This password cannot be decrypted for the current Windows user.",
                    ex);
            }
        }

        private static AccountVaultFile LoadVault()
        {
            MigrateLegacyVault();
            if (!File.Exists(VaultPath))
            {
                return new AccountVaultFile();
            }

            string json = File.ReadAllText(VaultPath, Encoding.UTF8);
            AccountVaultFile vault = JsonConvert.DeserializeObject<AccountVaultFile>(json) ?? new AccountVaultFile();
            vault.Accounts = vault.Accounts ?? new List<AccountRecord>();
            return vault;
        }

        private static void MigrateLegacyVault()
        {
            if (File.Exists(VaultPath)
                || !File.Exists(LegacyVaultPath)
                || string.Equals(VaultPath, LegacyVaultPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Directory.CreateDirectory(VaultDirectory);
            try
            {
                File.Move(LegacyVaultPath, VaultPath);
            }
            catch (IOException)
            {
                // Another client instance may have completed the migration first.
                if (!File.Exists(VaultPath))
                {
                    throw;
                }
            }
        }

        private static void SaveVault(AccountVaultFile vault)
        {
            Directory.CreateDirectory(VaultDirectory);
            vault.Version = 2;
            string temporaryPath = VaultPath + ".tmp";
            string json = JsonConvert.SerializeObject(vault, Formatting.Indented);
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));

            if (File.Exists(VaultPath))
            {
                File.Replace(temporaryPath, VaultPath, null);
            }
            else
            {
                File.Move(temporaryPath, VaultPath);
            }
        }

        private sealed class AccountVaultFile
        {
            [JsonProperty("version")]
            public int Version { get; set; } = 2;

            [JsonProperty("accounts")]
            public List<AccountRecord> Accounts { get; set; } = new List<AccountRecord>();
        }

        private sealed class AccountRecord
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("label")]
            public string Label { get; set; }

            [JsonProperty("username")]
            public string Username { get; set; }

            [JsonProperty("protectedPassword")]
            public string ProtectedPassword { get; set; }

            [JsonProperty("characterName", NullValueHandling = NullValueHandling.Ignore)]
            public string CharacterName { get; set; }

            [JsonProperty("characterClass", NullValueHandling = NullValueHandling.Ignore)]
            public string CharacterClass { get; set; }

            [JsonProperty("characterLevel", NullValueHandling = NullValueHandling.Ignore)]
            public int? CharacterLevel { get; set; }
        }
    }
}
