using Community.VisualStudio.Toolkit;
using IncludeToolbox.Commands;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.IncludeWhatYouUse
{
    /// <summary>
    /// Functions for downloading and versioning of the iwyu installation.
    /// </summary>
    public class IWYUDownload
    {
        public static readonly string DisplayRepositorURL = @"https://github.com/Agrael1/BuildIWYU";
        private static readonly string DownloadRepositorURL = @"https://github.com/Agrael1/BuildIWYU/archive/main.zip";
        private static readonly string LatestCommitQuery = @"https://api.github.com/repos/Agrael1/BuildIWYU/git/refs/heads/main";

        public static string GetDefaultFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iwyu");
        }
        public static string GetDefaultExecutablePath()
        {
            return Path.Combine(GetDefaultFolder(), "include-what-you-use.exe");
        }
        public static string GetDefaultMappingPath()
        {
            return Path.Combine(GetDefaultFolder(), "msvc.imp");
        }
        public static string GetDefaultMappingChecked()
        {
            var path = GetDefaultMappingPath();
            return File.Exists(path) ? path : "";
        }


        static public string GetVersionFilePath()
        {
            return Path.Combine(GetDefaultFolder(), "version");
        }

        public delegate void OnChangeDelegate(string Section, string Status, float percent);



        public event OnChangeDelegate OnProgress;
        WebClient client;

        protected void OnProgressEvent(string Section, string Status, float percent)
        {
            OnProgress?.Invoke(Section, Status, percent);
        }



        public static async Task<bool> DownloadAsync(IVsThreadedWaitDialogFactory dialogFactory, IWYUOptions settings)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!await VS.MessageBox.ShowConfirmAsync($"Can't locate include-what-you-use. Do you want to download it from '{IWYUDownload.DisplayRepositorURL}'?"))
                return false;

            var downloader = new IWYUDownload();

            dialogFactory.CreateInstance(out IVsThreadedWaitDialog2 xdialog);
            IVsThreadedWaitDialog4 dialog = xdialog as IVsThreadedWaitDialog4;

            downloader.OnProgress += (string section, string status, float percentage) =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                dialog.UpdateProgress(
                szUpdatedWaitMessage: section,
                szProgressText: status,
                szStatusBarText: $"Downloading include-what-you-use - {section} - {status}",
                iCurrentStep: (int)(percentage * 100),
                iTotalSteps: 100,
                fDisableCancel: false,
                pfCanceled: out bool canceled);
            };

            dialog.StartWaitDialogWithCallback(
                szWaitCaption: "Include Toolbox - Downloading include-what-you-use",
                szWaitMessage: "", // comes in later.
                szProgressText: null,
                varStatusBmpAnim: null,
                szStatusBarText: "Downloading include-what-you-use",
                fIsCancelable: true,
                iDelayToShowDialog: 0,
                fShowProgress: true,
                iTotalSteps: 100,
                iCurrentStep: 0,
                new CancelCallback(() => { downloader.Cancel(); }));

            await downloader.DownloadIWYUAsync();

            if (dialog.EndWaitDialog()) return false;
            settings.Executable = GetDefaultExecutablePath();

            settings.Downloaded();
            await settings.SaveAsync();

            return true;
        }

        private static async Task<string> GetVersionOnlineAsync()
        {
            using (var httpClient = new HttpClient())
            {
                // User agent is always required for github api.
                // https://developer.github.com/v3/#user-agent-required
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("IncludeToolbox");

                string latestCommitResponse;
                try
                {
                    latestCommitResponse = await httpClient.GetStringAsync(LatestCommitQuery);
                }
                catch (HttpRequestException e)
                {
                    _ = Output.WriteLineAsync($"Failed to query IWYU version from {DownloadRepositorURL}: {e}");
                    return "";
                }

                // Poor man's json parsing in lack of a json parser.
                var shaRegex = new Regex(@"\""sha\""\w*:\w*\""([a-z0-9]+)\""");
                return shaRegex.Match(latestCommitResponse).Groups[1].Value;
            }
        }

        private static string GetCurrentVersionHarddrive()
        {
            // Read current version.
            try
            {
                return File.ReadAllText(GetVersionFilePath());
            }
            catch
            {
                return "";
            }
        }

        public static async Task<bool> IsNewerVersionAvailableOnlineAsync()
        {
            string currentVersion = GetCurrentVersionHarddrive();
            string onlineVersion = await GetVersionOnlineAsync();
            return currentVersion != onlineVersion;
        }




        async Task DownloadAsync(string targetZipFile)
        {
            using (client = new WebClient())
            {
                client.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs e) =>
                {
                    int kbTodo = (int)System.Math.Ceiling((double)e.TotalBytesToReceive / 1024);
                    int kbDownloaded = (int)System.Math.Ceiling((double)e.BytesReceived / 1024);
                    OnProgressEvent("Downloading", kbTodo > 0 ? $"{kbTodo} / {kbDownloaded} kB" : $"{kbDownloaded} kB", e.ProgressPercentage * 0.01f);
                };

                await client.DownloadFileTaskAsync(DownloadRepositorURL, targetZipFile);
                client = null;
            }
        }

        /// <summary>
        /// Downloads iwyu from default download repository.
        /// </summary>
        public async Task DownloadIWYUAsync()
        {
            string targetDirectory = GetDefaultFolder();
            Directory.CreateDirectory(targetDirectory);
            DirectoryInfo di = new DirectoryInfo(targetDirectory);

            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.EnumerateDirectories())
            {
                dir.Delete(true);
            }

            string targetZipFile = Path.Combine(targetDirectory, "download.zip");

            // Download.
            OnProgressEvent("Connecting...", "", -1.0f);

            try
            {
                await DownloadAsync(targetZipFile);
            }
            catch (Exception e)
            {
                _ = Output.WriteLineAsync("Failed to download IWYU with error:" + e.Message);
                return;
            }

            // Unpacking. Looks like there is no async api, so we're just moving this to a task.
            OnProgressEvent("Unpacking...", "", -1.0f);

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(targetZipFile))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries.Where(e =>
                    {
                        if (e.Name == "LICENSE") return true;
                        string a = Path.GetExtension(e.FullName);
                        return a == ".exe" || a == ".txt" || a == ".imp" || a == ".md";
                    }))
                    {
                        entry.ExtractToFile(Path.Combine(GetDefaultFolder(), entry.Name));
                    }
                }
            }
            catch (Exception e)
            {
                _ = Output.WriteLineAsync("Failed to unpack IWYU with error:" + e.Message);
                File.Delete(targetZipFile);
                return;
            }

            // Save version.
            OnProgressEvent("Saving Version", "", -1.0f);
            string version = await GetVersionOnlineAsync();
            File.WriteAllText(GetVersionFilePath(), version);

            OnProgressEvent("Clean Up", "", -1.0f);
            File.Delete(targetZipFile);
        }
        public void Cancel()
        {
            client?.CancelAsync();
        }
    }
}
