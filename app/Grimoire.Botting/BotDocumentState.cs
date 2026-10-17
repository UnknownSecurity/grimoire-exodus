using System;
using System.IO;

namespace Grimoire.Botting
{
    public sealed class BotDocumentChangedEventArgs : EventArgs
    {
        public BotDocumentChangedEventArgs(bool configurationChanged, bool metadataChanged)
        {
            ConfigurationChanged = configurationChanged;
            MetadataChanged = metadataChanged;
        }

        public bool ConfigurationChanged { get; }

        public bool MetadataChanged { get; }
    }

    /// <summary>
    /// Owns the editable bot document for one client session. This is UI state
    /// only; it is deliberately not part of the serialized GBot contract.
    /// </summary>
    public sealed class BotDocumentState
    {
        private readonly object syncRoot = new object();
        private Configuration configuration = new Configuration();
        private string sourcePath;
        private string displayName = "Untitled Bot";
        private bool isDirty;
        private bool hasSnapshot;

        public Configuration Configuration
        {
            get
            {
                lock (syncRoot)
                    return configuration;
            }
        }

        public string SourcePath
        {
            get
            {
                lock (syncRoot)
                    return sourcePath;
            }
        }

        public string DisplayName
        {
            get
            {
                lock (syncRoot)
                    return displayName;
            }
        }

        public bool IsDirty
        {
            get
            {
                lock (syncRoot)
                    return isDirty;
            }
        }

        public bool HasSnapshot
        {
            get
            {
                lock (syncRoot)
                    return hasSnapshot;
            }
        }

        public event EventHandler<BotDocumentChangedEventArgs> Changed;

        public void Commit(Configuration value)
        {
            Set(value, null, false, true, false);
        }

        public void Replace(Configuration value, string path)
        {
            Set(value, path, true, false, true);
        }

        public void UpdateRuntimeConfiguration(Configuration value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            bool changed;
            lock (syncRoot)
            {
                changed = !ReferenceEquals(configuration, value);
                configuration = value;
                hasSnapshot = true;
            }

            if (changed)
                Changed?.Invoke(this, new BotDocumentChangedEventArgs(true, false));
        }

        public void SetSourcePath(string path)
        {
            string normalizedPath = NormalizePath(path);
            bool changed;
            lock (syncRoot)
            {
                changed = !string.Equals(sourcePath, normalizedPath, StringComparison.OrdinalIgnoreCase);
                sourcePath = normalizedPath;
                displayName = GetDisplayName(normalizedPath);
            }

            if (changed)
                Changed?.Invoke(this, new BotDocumentChangedEventArgs(false, true));
        }

        public void MarkSaved(string path, Configuration value)
        {
            Set(value, path, true, false, true);
        }

        private void Set(
            Configuration value,
            string path,
            bool replacePath,
            bool dirty,
            bool metadataMayChange)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            bool configurationChanged;
            bool metadataChanged = false;
            lock (syncRoot)
            {
                configurationChanged = !ReferenceEquals(configuration, value);
                configuration = value;
                isDirty = dirty;
                hasSnapshot = true;
                if (replacePath)
                {
                    string normalizedPath = NormalizePath(path);
                    metadataChanged = !string.Equals(sourcePath, normalizedPath, StringComparison.OrdinalIgnoreCase);
                    sourcePath = normalizedPath;
                    displayName = GetDisplayName(normalizedPath);
                }
            }

            if (configurationChanged || (metadataMayChange && metadataChanged))
            {
                Changed?.Invoke(
                    this,
                    new BotDocumentChangedEventArgs(configurationChanged, metadataChanged));
            }
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }

        private static string GetDisplayName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Untitled Bot";

            try
            {
                string name = Path.GetFileNameWithoutExtension(path);
                return string.IsNullOrWhiteSpace(name) ? "Untitled Bot" : name;
            }
            catch
            {
                return "Untitled Bot";
            }
        }
    }
}
