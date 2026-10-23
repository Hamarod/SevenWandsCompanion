using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

namespace SevenwandsCompanion.Services
{
    // Mise à jour automatique depuis les releases GitHub (zip Windows publié par le workflow
    // build.yml). Au démarrage : compare le tag de la dernière release à la version de l'appli,
    // propose la mise à jour, télécharge/extrait le zip puis délègue le remplacement des
    // fichiers à un script PowerShell lancé juste avant la fermeture (une appli ne peut pas
    // écraser ses propres fichiers verrouillés). Les données utilisateur (AppData) ne sont
    // jamais touchées : elles sont hors du dossier de l'exécutable.
    public static class UpdateService
    {
        private const string LatestReleaseUrl = "https://api.github.com/repos/Hamarod/SevenWandsCompanion/releases/latest";
        private const string PendingMarkerFile = "update_pending.txt";
        private const string ErrorFile = "update_error.txt";

        private sealed record ReleaseInfo(Version Version, string Tag, string DownloadUrl);

        public static Version CurrentVersion
            => Normalize(typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 0, 0, 0));

        private static Version Normalize(Version v)
            => new(Math.Max(v.Major, 0), Math.Max(v.Minor, 0), Math.Max(v.Build, 0), Math.Max(v.Revision, 0));

        // Point d'entrée appelé au démarrage, une fois la navigation initiale terminée.
        public static async Task CheckAtStartupAsync(Page host)
        {
            await ReportPreviousUpdateResultAsync(host);

#if WINDOWS && !DEBUG
            ReleaseInfo? release;
            try
            {
                release = await FetchLatestReleaseAsync();
            }
            catch (Exception ex)
            {
                // Hors ligne, quota API dépassé... : on ne dérange pas l'utilisateur.
                Debug.WriteLine($"Update check failed: {ex.Message}");
                return;
            }

            if (release == null || release.Version <= CurrentVersion) return;

            bool accept = await host.DisplayAlertAsync(
                "Mise à jour disponible",
                $"La version {release.Tag} est disponible (version actuelle : {CurrentVersion}).\n\n" +
                "L'application va télécharger la mise à jour, se fermer puis redémarrer automatiquement. " +
                "Vos données ne seront pas modifiées. Le téléchargement peut prendre un moment.\n\nMettre à jour maintenant ?",
                "Mettre à jour",
                "Plus tard");
            if (!accept) return;

            try
            {
                await DownloadAndApplyAsync(release);
            }
            catch (Exception ex)
            {
                await host.DisplayAlertAsync(
                    "Mise à jour impossible",
                    $"La mise à jour vers {release.Tag} n'a pas pu être réalisée.\n\nRaison : {ex.Message}\n\n" +
                    "L'application reste utilisable dans sa version actuelle.",
                    "OK");
            }
#endif
        }

        // Au démarrage suivant une tentative : détecte l'échec même si le script PowerShell a
        // planté sans rien écrire (marqueur présent mais version inchangée).
        private static async Task ReportPreviousUpdateResultAsync(Page host)
        {
            try
            {
                string markerPath = Path.Combine(FileSystem.AppDataDirectory, PendingMarkerFile);
                string errorPath = Path.Combine(FileSystem.AppDataDirectory, ErrorFile);
                if (!File.Exists(markerPath)) return;

                string target = (await File.ReadAllTextAsync(markerPath)).Trim();
                string? error = File.Exists(errorPath) ? (await File.ReadAllTextAsync(errorPath)).Trim() : null;
                File.Delete(markerPath);
                if (File.Exists(errorPath)) File.Delete(errorPath);

                bool reachedTarget = Version.TryParse(target, out var targetVersion) && CurrentVersion >= Normalize(targetVersion);
                if (reachedTarget && string.IsNullOrEmpty(error))
                {
                    CleanTempFiles();
                    return;
                }

                string reason = !string.IsNullOrEmpty(error)
                    ? error
                    : "le script de mise à jour s'est arrêté avant d'avoir remplacé les fichiers (PowerShell indisponible, bloqué par un antivirus ou fermé prématurément).";
                await host.DisplayAlertAsync(
                    "Mise à jour non réalisée",
                    $"La mise à jour vers la version {target} n'a pas pu se terminer.\n\nRaison : {reason}\n\n" +
                    "Vous pouvez la réessayer au prochain démarrage, ou télécharger la dernière version manuellement sur GitHub.",
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReportPreviousUpdateResult error: {ex.Message}");
            }
        }

#if WINDOWS
        private static async Task<ReleaseInfo?> FetchLatestReleaseAsync()
        {
            using var http = CreateHttpClient(TimeSpan.FromSeconds(10));
            using var response = await http.GetAsync(LatestReleaseUrl);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            string tag = root.GetProperty("tag_name").GetString() ?? "";
            if (!Version.TryParse(tag.TrimStart('v', 'V'), out var version)) return null;

            string? downloadUrl = null;
            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? "";
                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }

            return downloadUrl == null ? null : new ReleaseInfo(Normalize(version), tag, downloadUrl);
        }

        private static HttpClient CreateHttpClient(TimeSpan timeout)
        {
            var http = new HttpClient { Timeout = timeout };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("SevenWandsCompanion-Updater");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return http;
        }

        private static async Task DownloadAndApplyAsync(ReleaseInfo release)
        {
            string appDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            string exePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Impossible de déterminer l'emplacement de l'exécutable.");

            EnsureDirectoryWritable(appDir);

            string workDir = Path.Combine(Path.GetTempPath(), "SevenWandsUpdate");
            if (Directory.Exists(workDir)) Directory.Delete(workDir, true);
            Directory.CreateDirectory(workDir);

            string zipPath = Path.Combine(workDir, "update.zip");
            try
            {
                using var http = CreateHttpClient(TimeSpan.FromMinutes(10));
                using var response = await http.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                await using var source = await response.Content.ReadAsStreamAsync();
                await using var target = File.Create(zipPath);
                await source.CopyToAsync(target);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"téléchargement échoué ({ex.Message}).", ex);
            }

            string extractDir = Path.Combine(workDir, "extracted");
            try
            {
                ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"archive invalide ou illisible ({ex.Message}).", ex);
            }

            // Le zip peut contenir les fichiers à la racine ou dans un sous-dossier (ex: win-x64) :
            // on repère le dossier qui contient l'exécutable.
            string exeName = Path.GetFileName(exePath);
            string? exeInZip = Directory.EnumerateFiles(extractDir, exeName, SearchOption.AllDirectories).FirstOrDefault();
            if (exeInZip == null)
                throw new InvalidOperationException($"'{exeName}' est introuvable dans l'archive téléchargée.");
            string sourceRoot = Path.GetDirectoryName(exeInZip)!;

            string scriptPath = Path.Combine(workDir, "apply_update.ps1");
            await File.WriteAllTextAsync(scriptPath, UpdateScript);

            string errorPath = Path.Combine(FileSystem.AppDataDirectory, ErrorFile);
            if (File.Exists(errorPath)) File.Delete(errorPath);
            await File.WriteAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, PendingMarkerFile), release.Version.ToString());

            var psi = new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            foreach (var arg in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-WindowStyle", "Hidden", "-File", scriptPath,
                "-ProcessId", Environment.ProcessId.ToString(), "-Source", sourceRoot, "-Target", appDir,
                "-ExePath", exePath, "-ErrorFile", errorPath })
            {
                psi.ArgumentList.Add(arg);
            }

            try
            {
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                File.Delete(Path.Combine(FileSystem.AppDataDirectory, PendingMarkerFile));
                throw new InvalidOperationException($"impossible de lancer PowerShell ({ex.Message}).", ex);
            }

            Application.Current?.Quit();
        }

        private static void EnsureDirectoryWritable(string dir)
        {
            string probe = Path.Combine(dir, ".update_write_test");
            try
            {
                File.WriteAllText(probe, "");
                File.Delete(probe);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"le dossier d'installation n'est pas modifiable ({dir}) : {ex.Message}. " +
                    "Déplacez l'application dans un dossier où vous avez les droits d'écriture.", ex);
            }
        }

        // Attend la fermeture de l'appli, remplace les fichiers (avec nouvelles tentatives si un
        // fichier est encore verrouillé), écrit la raison dans le fichier d'erreur en cas
        // d'échec, puis relance l'appli (nouvelle ou ancienne version).
        private const string UpdateScript = @"
param(
    [int]$ProcessId,
    [string]$Source,
    [string]$Target,
    [string]$ExePath,
    [string]$ErrorFile
)
$ErrorActionPreference = 'Stop'
try {
    Wait-Process -Id $ProcessId -Timeout 60 -ErrorAction SilentlyContinue
    if (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue) {
        throw ""l'application ne s'est pas fermée dans le délai imparti.""
    }
    $attempt = 0
    while ($true) {
        try {
            Copy-Item -Path (Join-Path $Source '*') -Destination $Target -Recurse -Force
            break
        } catch {
            $attempt++
            if ($attempt -ge 5) { throw }
            Start-Sleep -Seconds 2
        }
    }
} catch {
    try { Set-Content -Path $ErrorFile -Value (""copie des fichiers impossible : "" + $_.Exception.Message) -Encoding UTF8 } catch {}
}
Start-Process -FilePath $ExePath
";
#endif

        private static void CleanTempFiles()
        {
            try
            {
                string workDir = Path.Combine(Path.GetTempPath(), "SevenWandsUpdate");
                if (Directory.Exists(workDir)) Directory.Delete(workDir, true);
            }
            catch { /* nettoyage best-effort */ }
        }
    }
}
