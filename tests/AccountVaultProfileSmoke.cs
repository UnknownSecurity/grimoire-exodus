using Grimoire.Tools;
using Grimoire.UI;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Grimoire.Utils
{
    internal static class AppPaths
    {
        public static string DataDirectory { get; set; }

        public static string AccountVisualsDirectory => Path.Combine(DataDirectory, "Account Visuals");
    }
}

internal static class AccountVaultProfileSmoke
{
    [STAThread]
    private static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "grimoire-account-profile-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            Grimoire.Utils.AppPaths.DataDirectory = root;
            File.WriteAllText(Path.Combine(root, "account-vault.json"), "{\"version\":1,\"accounts\":[]}");
            VerifyVisualCapture();
            VerifyCaptureRetryPolicy();

            AccountSummary saved = AccountVault.SaveAccount("Main", "TestCharacter", "not-a-real-password");
            Require(saved.CharacterClass == null && saved.CharacterLevel == null && !saved.HasPortrait,
                "A new account should have safe empty profile defaults.");

            byte[] portrait = CreatePortrait();
            Require(AccountVault.UpdateCharacterProfile(
                saved.Id,
                saved.Username,
                "TestCharacter",
                "ArchPaladin",
                100,
                portrait), "The character profile update was not persisted.");

            AccountSummary profile = AccountVault.GetAccounts().Single();
            Require(profile.CharacterName == "TestCharacter", "Character name was not returned.");
            Require(profile.CharacterClass == "ArchPaladin", "Character class was not returned.");
            Require(profile.CharacterLevel == 100, "Character level was not returned.");
            Require(profile.HasPortrait, "Character visual was not returned.");
            Require(profile.PortraitDataUri != null
                && profile.PortraitDataUri.StartsWith("data:image/png;base64,", StringComparison.Ordinal),
                "Character visual was not exposed as a bounded PNG data URI.");

            string vaultJson = File.ReadAllText(Path.Combine(root, "account-vault.json"));
            Require(vaultJson.Contains("\"version\": 2"), "The upgraded vault version was not saved.");
            Require(!vaultJson.Contains("not-a-real-password"), "The clear-text password leaked into the vault.");
            Require(!vaultJson.Contains(root), "A machine-specific visual path leaked into the vault.");

            bool invalidRejected = false;
            try
            {
                AccountVault.UpdateCharacterProfile(saved.Id, saved.Username, "Test", "Class", 1, new byte[] { 1, 2, 3 });
            }
            catch (ArgumentException)
            {
                invalidRejected = true;
            }
            Require(invalidRejected, "Malformed character visual bytes were accepted.");

            Require(AccountVault.RemoveAccount(saved.Id), "The account was not removed.");
            Require(!Directory.EnumerateFiles(Path.Combine(root, "Account Visuals"), "*.png").Any(),
                "Removing an account left its character visual behind.");

            string legacyId = Guid.NewGuid().ToString("N");
            File.WriteAllText(
                Path.Combine(root, "account-vault.json"),
                "{\"version\":1,\"accounts\":[{\"id\":\"" + legacyId
                    + "\",\"label\":\"Legacy\",\"username\":\"LegacyUser\",\"protectedPassword\":\"unused\"}]}");
            AccountSummary legacy = AccountVault.GetAccounts().Single();
            Require(legacy.CharacterClass == null && legacy.CharacterLevel == null && !legacy.HasPortrait,
                "A v1 vault did not load with safe profile defaults.");

            Console.WriteLine("Account Locker character profile validation passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    private static void VerifyVisualCapture()
    {
        Application.EnableVisualStyles();
        using (Form form = new Form
        {
            ClientSize = new Size(960, 550),
            FormBorderStyle = FormBorderStyle.None,
            Location = new Point(-32000, -32000),
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual
        })
        using (GradientControl gameSurface = new GradientControl { Dock = DockStyle.Fill })
        {
            form.Controls.Add(gameSurface);
            form.Show();
            Application.DoEvents();
            byte[] visual = AccountCharacterVisual.TryCapture(gameSurface);
            Require(visual != null && visual.Length > 0, "A visible game surface did not produce a portrait visual.");
            using (MemoryStream stream = new MemoryStream(visual, false))
            using (Image image = Image.FromStream(stream))
            {
                Require(image.Width == 96 && image.Height == 96, "The portrait visual is not bounded to 96 by 96 pixels.");
            }
        }
    }

    private static byte[] CreatePortrait()
    {
        using (Bitmap bitmap = new Bitmap(48, 48))
        using (Graphics graphics = Graphics.FromImage(bitmap))
        using (MemoryStream stream = new MemoryStream())
        {
            graphics.Clear(Color.FromArgb(30, 34, 44));
            graphics.FillEllipse(Brushes.MediumPurple, 5, 5, 38, 38);
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
    }

    private static void VerifyCaptureRetryPolicy()
    {
        Require(
            AccountProfileCapturePolicy.Evaluate(false, false, 19)
                == AccountProfileCaptureDisposition.Retry,
            "The profile capture paused before its bounded retry window was exhausted.");
        Require(
            AccountProfileCapturePolicy.Evaluate(false, false, 20)
                == AccountProfileCaptureDisposition.Pause,
            "Exhausting a hidden or unready capture incorrectly marked the profile complete.");
        Require(
            AccountProfileCapturePolicy.Evaluate(false, true, 1)
                == AccountProfileCaptureDisposition.Complete,
            "A successful portrait capture did not complete the profile refresh.");
        Require(
            AccountProfileCapturePolicy.Evaluate(true, false, 1)
                == AccountProfileCaptureDisposition.Complete,
            "An existing portrait did not complete the profile refresh.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class GradientControl : Control
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            for (int index = 0; index < 16; index++)
            {
                using (Brush brush = new SolidBrush(Color.FromArgb(
                    50 + index * 10,
                    20 + index * 8,
                    160 - index * 6)))
                {
                    e.Graphics.FillRectangle(brush, index * 10, 0, 12, 140);
                }
            }
        }
    }
}
