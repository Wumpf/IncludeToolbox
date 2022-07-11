using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace IncludeToolbox.IncludeWhatYouUse
{
    /// <summary>
    /// Functions for downloading and versioning of the iwyu installation.
    /// </summary>
    static public class IWYUDownload
    {
        public const string DisplayRepositorURL = @"https://github.com/Wumpf/iwyu_for_vs_includetoolbox";
        private const string DownloadRepositorURL = @"https://github.com/Wumpf/iwyu_for_vs_includetoolbox/archive/master.zip";
        private const string LatestCommitQuery = @"https://api.github.com/repos/Wumpf/iwyu_for_vs_includetoolbox/git/refs/heads/master";

        private static async Task<string> GetCurrentVersionOnline()
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
                    Output.Instance.WriteLine($"Failed to query IWYU version from {DownloadRepositorURL}: {e}");
                    return "";
                }

                // Poor man's json parsing in lack of a json parser.
                var shaRegex = new Regex(@"\""sha\""\w*:\w*\""([a-z0-9]+)\""");
                return shaRegex.Match(latestCommitResponse).Groups[1].Value;
            }
        }

        public static string GetVersionFilePath(string iwyuExectuablePath)
        {
            string directory = Path.GetDirectoryName(iwyuExectuablePath);
            return Path.Combine(directory, "version");
        }

        private static string GetCurrentVersionHarddrive(string iwyuExectuablePath)
        {
            // Read current version.
            try
            {
                return File.ReadAllText(GetVersionFilePath(iwyuExectuablePath));
            }
            catch
            {
                return "";
            }
        }

        public static async Task<bool> IsNewerVersionAvailableOnline(string executablePath)
        {
            string currentVersion = GetCurrentVersionHarddrive(executablePath);
            string onlineVersion = await GetCurrentVersionOnline();
            return currentVersion != onlineVersion;
        }

        /// <summary>
        /// Callback for download progress.
        /// </summary>
        /// <param name="section">General stage.</param>
        /// <param name="status">Sub status, may be empty.</param>
        /// <param name="percentage">Progress in percent for current section. -1 is there is none.</param>
        public delegate void DownloadProgressUpdate(string section, string status, float percentage);

        /// <summary>
        /// Downloads iwyu from default download repository.
        /// </summary>
        /// <remarks>
        /// Throws an exception if anything goes wrong (and there's a lot that can!)
        /// </remarks>
        /// <exception cref="OperationCanceledException">If cancellation token is used.</exception>
        static public async Task DownloadIWYU(string executablePath, DownloadProgressUpdate onProgressUpdate, CancellationToken cancellationToken)
        {
            string targetDirectory = Path.GetDirectoryName(executablePath);
            Directory.CreateDirectory(targetDirectory);
            string targetZipFile = Path.Combine(targetDirectory, "download.zip");

            // Delete existing zip file.
            try
            {
                File.Delete(targetZipFile);
            }
            catch { }

            // Download.
            onProgressUpdate("Connecting...", "", -1.0f);

            // In contrast to GetCurrentVersionOnline we're not using HttpClient here since WebClient makes downloading files so much nicer.
            // (in HttpClient we would need to do the whole buffering + queuing and file writing ourselves)
            using (var client = new WebClient())
            {
                var cancelRegistration = cancellationToken.Register(() =>
                {
                    client.CancelAsync();
                    throw new TaskCanceledException();
                });

                client.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs e) =>
                {
                    int kbTodo = (int)System.Math.Ceiling((double)e.TotalBytesToReceive / 1024);
                    int kbDownloaded = (int)System.Math.Ceiling((double)e.BytesReceived / 1024);
                    onProgressUpdate("Downloading", kbTodo > 0 ? $"{kbTodo} / {kbDownloaded} kB" : $"{kbDownloaded} kB", e.ProgressPercentage * 0.01f);
                };

                await client.DownloadFileTaskAsync(DownloadRepositorURL, targetZipFile);

                cancelRegistration.Dispose();
            }

            // Unpacking. Looks like there is no async api, so we're just moving this to a task.
            onProgressUpdate("Unpacking...", "", -1.0f);
            await Task.Run(() =>
            {
                using (var zipArchive = new ZipArchive(File.OpenRead(targetZipFile), ZipArchiveMode.Read))
                {
                    // Don't want to have the top level folder if any,
                    string topLevelFolderName = "";

                    for (int i = 0; i < zipArchive.Entries.Count; ++i)
                    {
                        var file = zipArchive.Entries[i];

                        string targetName = file.FullName.Substring(topLevelFolderName.Length);
                        string completeFileName = Path.Combine(targetDirectory, targetName);

                        // If name is empty it should be a directory.
                        if (file.Name == "")
                        {
                            if (i == 0)    // We assume that if the first thing we encounter is a folder, it is a toplevel one.
                                topLevelFolderName = file.FullName;
                            else
                                Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                        }
                        else
                        {
                            using (var destination = File.Open(completeFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                using (var stream = file.Open())
                                    stream.CopyTo(destination);
                            }
                        }

                        if (cancellationToken.IsCancellationRequested)
                            return;
                    }
                }

            }, cancellationToken);

            // Save version.
            onProgressUpdate("Saving Version", "", -1.0f);
            string version = await GetCurrentVersionOnline();
            File.WriteAllText(GetVersionFilePath(executablePath), version);
        }

        static public IEnumerable<string> GetMappingFilesNextToIwyuPath(string executablePath)
        {
            string targetDirectory = Path.GetDirectoryName(executablePath);

            var impFiles = Directory.EnumerateFiles(targetDirectory).
                            Where(file => Path.GetExtension(file).Equals(".imp", System.StringComparison.InvariantCultureIgnoreCase));
            foreach (string dirs in Directory.EnumerateDirectories(targetDirectory))
            {
                impFiles.Concat(
                    Directory.EnumerateFiles(targetDirectory).
                        Where(file => Path.GetExtension(file).Equals(".imp", System.StringComparison.InvariantCultureIgnoreCase))
                        );
            }

            return impFiles;
        }
    }
}
