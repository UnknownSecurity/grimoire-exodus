using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

        private static string VaultDirectory => AppPaths.DataDirectory;

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
                    .Select(account => new AccountSummary
                    {
                        Id = account.Id,
                        Label = account.Label,
                        Username = account.Username
                    })
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

                return new AccountSummary
                {
                    Id = account.Id,
                    Label = account.Label,
                    Username = account.Username
                };
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
            }
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
            public int Version { get; set; } = 1;

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
        }
    }
}
