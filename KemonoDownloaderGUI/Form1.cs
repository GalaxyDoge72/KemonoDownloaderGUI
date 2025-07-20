using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using File = System.IO.File;

namespace KemonoDownloaderGUI
{
    public partial class Form1 : Form
    {
        List<string> providers = new List<string> { "Patreon", "Gumroad", "Fanbox", "Fantia" };
        string[] extensions = new string[]
            {
                ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", // Images
                ".mp4", ".mov", ".avi", ".webm", ".mkv", ".flv", // Videos
                ".mp3", ".wav", ".flac", ".m4a", // Audio
                ".zip", ".rar", ".7z", // Archives
                ".pdf", ".txt", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", // Documents
                ".psd"
            };

        private List<failedDownload> failedFileDowloads = new List<failedDownload>();
        string outputDir;
        private KemonoApiFetcher _currentFetcher;

        public Form1()
        {
            InitializeComponent();
            init();
        }
        private void init()
        {
            foreach (string provider in providers)
            {
                providerListBox.Items.Add(provider);
            }
            foreach (string extension in extensions)
            {
                checkedListBox1.Items.Add(extension, false);
            }
            clearDownloadText();
            clearSpeedText();
        }
        public void AppendLogBox(string msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => AppendLogBox(msg)));
            }
            else
            {
                logTextBox.AppendText(msg + Environment.NewLine);
                logTextBox.ScrollToCaret();
            }
        }
        public string sanitizeFilename(string filename)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"[{0}]", invalidChars);
            return Regex.Replace(filename, invalidRegStr, "_");
        }
        private string AddLongPathPrefix(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            if (path.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            string pathRoot = Path.GetPathRoot(path);
            if (!string.IsNullOrEmpty(pathRoot) && pathRoot.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
            {
                return @"\\?\UNC\" + path.Substring(2);
            }

            return @"\\?\" + path;
        }

        private string RemoveLongPathPrefix(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }
            if (path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
            {
                return @"\\" + path.Substring(8);
            }
            if (path.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase))
            {
                return path.Substring(4);
            }
            return path;
        }
        public string getUniqueFilename(string outputDir, string filename)
        {
            filename = sanitizeFilename(filename);
            string baseName = Path.GetFileNameWithoutExtension(filename);
            string extension = Path.GetExtension(filename);

            // Combine path and filename.
            string rawFilePath = Path.Combine(outputDir, filename);
            // Apply long path prefix
            string filePath = AddLongPathPrefix(rawFilePath);

            if (!File.Exists(filePath)) // Use the prefixed path
            {
                AppendLogBox($"Using filename: {filename}.");
                return filename; // Filename is unique so return.
            }

            int i = 0;
            while (true)
            {
                string newFilename = $"{baseName}_{i}{extension}";
                string rawNewFilePath = Path.Combine(outputDir, newFilename);
                string newFilePath = AddLongPathPrefix(rawNewFilePath); // Apply long path prefix

                if (!File.Exists(newFilePath)) // Use the prefixed path
                {
                    AppendLogBox($"Using filename: {newFilename}");
                    return newFilename;
                }

                i++; // Increment and try again.
            }
        }

        private async void downloadButton_Click(object sender, EventArgs e)
        {
            string service = providerListBox.Text.ToLower().Trim();
            string creatorId = idBox.Text.Trim();
            string outputDir = directoryBox.Text.Trim();
            int postLimit = 0;
            if (int.TryParse(postLimitBox.Text, out int parsedLimit))
            {
                postLimit = parsedLimit;
            }

            if (string.IsNullOrEmpty(service) || string.IsNullOrEmpty(creatorId) || string.IsNullOrEmpty(outputDir))
            {
                AppendLogBox("Service, Creator ID, and Output Directory cannot be empty.");
                return;
            }

            if (!Directory.Exists(outputDir))
            {
                try
                {
                    Directory.CreateDirectory(outputDir);
                    AppendLogBox($"Created main output directory: {outputDir}");
                }
                catch (Exception ex)
                {
                    AppendLogBox($"ERROR: Could not create main output directory '{outputDir}': {ex.Message}");
                    return;
                }
            }

            List<string> desiredExtensions = new List<string>();
            foreach (object extension in checkedListBox1.CheckedItems)
            {
                desiredExtensions.Add(extension.ToString());
            }

            if (desiredExtensions.Count == 0)
            {
                AppendLogBox($"Selected extensions cannot be none. Select some extensions and try again.");
                return;
            }

            skipPostButton.Enabled = true;

            _currentFetcher = new KemonoApiFetcher(this, desiredExtensions, postLimit);
            await _currentFetcher.StartDownloadAsync(service, creatorId, outputDir);
            _currentFetcher = null;

            skipPostButton.Enabled = false;
        }

        private void skipPostButton_Click(object sender, EventArgs e)
        {
            if (_currentFetcher != null)
            {
                AppendLogBox("Skip requested. Moving to next post...");
                _currentFetcher.SkipCurrentPost();
            }
        }

        public void updateDownloadText(long downloadedBytes, long totalBytes)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => updateDownloadText(downloadedBytes, totalBytes)));
            }
            else
            {
                string downloaded = FormatBytes(downloadedBytes);
                string total = FormatBytes(totalBytes);
                downloadedText.Text = $"Download in progress... ({downloaded} of {total})";
            }
        }
        public void clearDownloadText()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => clearDownloadText()));
            }
            else
            {
                downloadedText.Text = string.Empty;
            }
        }

        public void updateProgressBar(int percentage)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => updateProgressBar(percentage)));
            }
            else
            {
                if (percentage >= 0 && percentage <= 100)
                {
                    downloadProgressBar.Value = percentage;
                }
            }
        }
        public void resetProgressBar()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => resetProgressBar()));
            }
            else
            {
                downloadProgressBar.Value = 0;
            }
        }
        private string FormatBytes(long bytes)
        {
            string[] Suffix = { "Bytes", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = bytes;
            while (Math.Round(dblSByte / 1024) >= 1)
            {
                dblSByte /= 1024;
                i++;
            }
            return string.Format("{0:n2} {1}", dblSByte, Suffix[i]);
        }

        private void fileDialogButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                DialogResult result = dialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    directoryBox.Text = dialog.SelectedPath;
                }
            }
        }

        public void updateSpeedText(double bytesPerSecond)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => updateSpeedText(bytesPerSecond)));
            }
            else
            {
                string speedStr = FormatBytes((long)bytesPerSecond) + "/s";
                speedText.Text = $"Speed: {speedStr}";
            }
        }

        public void clearSpeedText()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => clearSpeedText()));
            }
            else
            {
                speedText.Text = string.Empty;
            }
        }

        public int getTrackbarVal()
        {
            if (string.IsNullOrEmpty(filepathLimitBox.Text))
            {
                return 5000;
            }
            int var = int.Parse(filepathLimitBox.Text);
            return var;
        }

    }
    public static class PathSanitizer
    {
        public static string SanitizeFileName(string name)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitizedName = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
            sanitizedName = System.Text.RegularExpressions.Regex.Replace(sanitizedName, @"\s+", " ").Trim();

            sanitizedName = sanitizedName.Trim('.');

            int maxSegmentLength = 25; // A common safe length for path segments
            if (sanitizedName.Length > maxSegmentLength)
            {
                sanitizedName = sanitizedName.Substring(0, maxSegmentLength);
            }

            return sanitizedName;
        }

        
        public static string SanitizeDirectoryName(string name, int maxSegmentLength)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();

            string sanitizedName = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());

            // Further sanitization specific to directory names or general cleanup
            sanitizedName = System.Text.RegularExpressions.Regex.Replace(sanitizedName, @"\s+", " ").Trim();
            sanitizedName = sanitizedName.Trim('.');

            if (sanitizedName.Length > maxSegmentLength)
            {
                sanitizedName = sanitizedName.Substring(0, maxSegmentLength);
            }

            return sanitizedName;
        }
    }
    public class KemonoFile
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("path")]
        public string Path { get; set; }
    }
    public class KemonoAttachment
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("path")]
        public string Path { get; set; }
    }

    public class KemonoPost
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("file")]
        public KemonoFile File { get; set; }
        [JsonPropertyName("attachments")]
        public List<KemonoAttachment> Attachments { get; set; }

    }
    public class IncompleteDownloadException : Exception
    {
        public IncompleteDownloadException() { }
        public IncompleteDownloadException(string message) : base(message) { }
        public IncompleteDownloadException(string message, Exception inner) : base(message, inner) { }
    }

    public class failedDownload
    {
        public string URL { get; set; }
        public string kemonoPath { get; set; }
        public string fileName { get; set; }
        public string outputDir { get; set; }
        public string originalErr { get; set; }
    }

    public class DownloadManager
    {
        private const int maxRetries = 5;
        private const int retryDelaySec = 3;
        private const int downloadTimeoutSec = 60;

        public static ConcurrentBag<failedDownload> failedDownloads { get; set; } = new ConcurrentBag<failedDownload>();
        public static ConcurrentBag<string> downloadedPaths { get; private set; } = new ConcurrentBag<string>();

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(downloadTimeoutSec)
        };

        public static async Task<bool> downloadFileAsync(string url, string kemonoPath, string filename, string outputDir, Form1 formInstance, CancellationToken cancellationToken, bool isRetryPass = false)
        {
            string filePath = Path.Combine(outputDir, filename);

            if (downloadedPaths.Any(p => p == kemonoPath))
            {
                formInstance.AppendLogBox($"WARN: File {filename} (path: {kemonoPath}) already downloaded. Skipping...");
                return true;
            }
            if (File.Exists(filePath))
            {
                formInstance.AppendLogBox($"WARN: File already exists locally, skipping download: {filename}");
                downloadedPaths.Add(kemonoPath);
                return true;
            }

            int retries = 0;
            while (retries < maxRetries)
            {
                TimeSpan currentAttemptDelay = TimeSpan.FromSeconds(retryDelaySec);

                try
                {
                    Directory.CreateDirectory(outputDir);
                    // Ensure the directory exists
                    if (!Directory.Exists(outputDir))
                    {
                        // Try to create the directory and wait until it exists
                        Directory.CreateDirectory(outputDir);
                        int waitCount = 0;
                        while (!Directory.Exists(outputDir) && waitCount < 10)
                        {
                            await Task.Delay(50); // Wait 50ms
                            waitCount++;
                        }
                        if (!Directory.Exists(outputDir))
                        {
                            formInstance.AppendLogBox($"ERROR: Could not create output directory '{outputDir}' for file '{filename}'.");
                            return false;
                        }
                    }

                    using (HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                    {
                        response.EnsureSuccessStatusCode();

                        long? totalSize = response.Content.Headers.ContentLength;
                        long downloadedSize = 0;
                        formInstance.resetProgressBar();

                        if (totalSize.HasValue)
                        {
                            formInstance.updateDownloadText(0, totalSize.Value);
                        }
                        else
                        {
                            formInstance.updateDownloadText(0, 0);
                        }

                        using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                        using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            byte[] buffer = new byte[8192];
                            int bytesRead;
                            var sw = System.Diagnostics.Stopwatch.StartNew();
                            long lastBytes = 0;
                            long lastElapsedMs = 0;

                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                                downloadedSize += bytesRead;

                                if (totalSize.HasValue && totalSize.Value > 0)
                                {
                                    formInstance.updateDownloadText(downloadedSize, totalSize.Value);
                                    int percentage = (int)((double)downloadedSize / totalSize.Value * 100);
                                    formInstance.updateProgressBar(percentage);
                                }

                                if (sw.ElapsedMilliseconds - lastElapsedMs > 500)
                                {
                                    long bytesDelta = downloadedSize - lastBytes;
                                    long msDelta = sw.ElapsedMilliseconds - lastElapsedMs;
                                    double speed = msDelta > 0 ? (bytesDelta * 1000.0 / msDelta) : 0;
                                    formInstance.updateSpeedText(speed);
                                    lastBytes = downloadedSize;
                                    lastElapsedMs = sw.ElapsedMilliseconds;
                                }
                            }
                            // Final speed update
                            double avgSpeed = sw.ElapsedMilliseconds > 0 ? (downloadedSize * 1000.0 / sw.ElapsedMilliseconds) : 0;
                            formInstance.updateSpeedText(avgSpeed);
                        }

                        if (totalSize.HasValue && totalSize.Value > 0 && downloadedSize != totalSize.Value)
                        {
                            throw new IncompleteDownloadException($"Downloaded {downloadedSize} bytes, but expected {totalSize.Value} bytes.");
                        }

                        formInstance.AppendLogBox($"SUCCESS: Downloaded file: {filename}.");
                        downloadedPaths.Add(kemonoPath);
                        formInstance.clearDownloadText();
                        formInstance.updateSpeedText(0); // Clear speed after download
                        return true;
                    }
                }
                catch (HttpRequestException httpEX)
                {
                    int statusCode = (int?)httpEX.StatusCode ?? 0;
                    if (retries < maxRetries)
                    {
                        retries++;
                        formInstance.AppendLogBox($"WARN: HTTP {statusCode} error while downloading {url}. Retrying... ({retries}/{maxRetries} in {currentAttemptDelay.TotalSeconds}s...)");
                        await Task.Delay(currentAttemptDelay);
                        continue;
                    }
                    else
                    {
                        formInstance.AppendLogBox($"HTTP {statusCode} Error downloading {url} after {maxRetries} retries: {httpEX.Message}");
                        if (!isRetryPass && !failedDownloads.Any(item => item.URL == url))
                        {
                            failedDownloads.Add(new failedDownload { URL = url, kemonoPath = kemonoPath, fileName = filename, outputDir = outputDir, originalErr = httpEX.ToString() });
                        }
                        return false;
                    }
                }
                catch (OperationCanceledException)
                {
                    formInstance.AppendLogBox($"Download of {filename} was cancelled.");
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            File.Delete(filePath);
                            formInstance.AppendLogBox($"Cleaned up incomplete file: {filePath}.");
                        }
                        catch (IOException ioEX)
                        {
                            formInstance.AppendLogBox($"ERR: Could not delete incomplete file {filePath}: {ioEX.Message}");
                        }
                    }
                    return false;
                }
                catch (IncompleteDownloadException incompleteEX)
                {
                    if (retries < maxRetries)
                    {
                        retries++;
                        currentAttemptDelay = TimeSpan.Zero;
                        formInstance.AppendLogBox($"WARN: Incomplete download for {url}. Retrying... (Attempt {retries}/{maxRetries})");
                        if (File.Exists(filePath))
                        {
                            try
                            {
                                File.Delete(filePath);
                                formInstance.AppendLogBox($"Cleaned up incomplete file: {filePath}");
                            }
                            catch (IOException ioEX)
                            {
                                formInstance.AppendLogBox($"Failed to clean up incomplete file: {filePath}: {ioEX.Message}");
                            }
                        }
                        await Task.Delay(currentAttemptDelay);
                        continue;
                    }
                    else
                    {
                        if (!isRetryPass && !failedDownloads.Any(item => item.URL == url))
                        {
                            failedDownloads.Add(new failedDownload { URL = url, kemonoPath = kemonoPath, fileName = filename, outputDir = outputDir, originalErr = incompleteEX.Message });
                        }
                        return false;
                    }
                }
                catch (IOException ioEX)
                {
                    formInstance.AppendLogBox($"ERR: File system error while saving {filename}: {ioEX.Message}");
                    if (!isRetryPass && !failedDownloads.Any(item => item.URL == url))
                    {
                        failedDownloads.Add(new failedDownload { URL = url, kemonoPath = kemonoPath, fileName = filename, outputDir = outputDir, originalErr = ioEX.ToString() });
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    formInstance.AppendLogBox($"ERR: Error occurred while downloading {filename}: {ex.Message}");
                    if (!isRetryPass && !failedDownloads.Any(item => item.URL == url))
                    {
                        failedDownloads.Add(new failedDownload { URL = url, kemonoPath = kemonoPath, fileName = filename, outputDir = outputDir, originalErr = ex.ToString() });
                    }
                    return false;
                }
            }
            if (!isRetryPass && !failedDownloads.Any(item => item.URL == url))
            {
                failedDownloads.Add(new failedDownload { URL = url, kemonoPath = kemonoPath, fileName = filename, outputDir = outputDir, originalErr = "Max retries exhausted in DownloadFileAsync" });
            }
            return false;
        }
    }

    public class KemonoApiFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly Form1 _formInstance;
        private readonly List<string> _desiredExtensions;
        private readonly int _postLimitConfig;
        private CancellationTokenSource _postSkipCts;

        public ConcurrentBag<PageFetchFailure> FailedPageFetches { get; private set; } = new ConcurrentBag<PageFetchFailure>();

        public class PageFetchFailure
        {
            public string Url { get; set; }
            public int Offset { get; set; }
            public string Error { get; set; }
            public int? StatusCode { get; set; }
        }

        public KemonoApiFetcher(Form1 formInstance, List<string> desiredExtensions, int postLimitConfig)
        {
            _formInstance = formInstance;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(KemonoConstants.PageFetchTimeoutSeconds) };
            _desiredExtensions = desiredExtensions;
            _postLimitConfig = postLimitConfig;
        }

        public void SkipCurrentPost()
        {
            _postSkipCts?.Cancel();
        }

        public async Task StartDownloadAsync(string service, string creatorId, string baseOutputDir)
        {
            _formInstance.AppendLogBox($"Starting download for creator ID: {creatorId} from {service}...");
            if (_postLimitConfig > 0)
            {
                _formInstance.AppendLogBox($"Limiting download to the first {_postLimitConfig} posts.");
            }
            else
            {
                _formInstance.AppendLogBox("No post limit set, attempting to download all available posts.");
            }

            int offset = 0;
            int totalPostsProcessed = 0;
            int totalDownloadedCount = 0;
            bool activePageFetching = true;

            while (activePageFetching)
            {
                if (_postLimitConfig > 0 && totalPostsProcessed >= _postLimitConfig)
                {
                    _formInstance.AppendLogBox($"Post limit ({_postLimitConfig}) reached. Stopping page fetching.");
                    activePageFetching = false;
                    break;
                }

                string pageUrl = $"{KemonoConstants.BaseApiUrl}{service}/user/{creatorId}/posts?o={offset}";
                _formInstance.AppendLogBox($"Fetching URL: {pageUrl}");

                List<KemonoPost> postsOnPage = null;
                int retries = 0;
                bool currentPageFetchSuccess = false;

                while (retries < KemonoConstants.MaxRetries)
                {
                    try
                    {
                        HttpResponseMessage response = await _httpClient.GetAsync(pageUrl);
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            _formInstance.AppendLogBox($"Received HTTP 404 for page {pageUrl}. Assuming end of posts or invalid creator/service.");
                            activePageFetching = false;
                            break;
                        }
                        response.EnsureSuccessStatusCode();

                        string jsonString = await response.Content.ReadAsStringAsync();
                        postsOnPage = JsonSerializer.Deserialize<List<KemonoPost>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        currentPageFetchSuccess = true;
                        break;
                    }
                    catch (HttpRequestException httpEx)
                    {
                        int statusCode = (int?)httpEx.StatusCode ?? 0;
                        retries++;
                        _formInstance.AppendLogBox($"WARN: HTTP {statusCode} Error fetching page {pageUrl}. Retrying ({retries}/{KemonoConstants.MaxRetries}) in {KemonoConstants.RetryDelaySeconds}s...");
                        await Task.Delay(TimeSpan.FromSeconds(KemonoConstants.RetryDelaySeconds));
                    }
                    catch (OperationCanceledException)
                    {
                        retries++;
                        _formInstance.AppendLogBox($"WARN: Timeout fetching page {pageUrl}. Retrying ({retries}/{KemonoConstants.MaxRetries}) immediately...");
                    }
                    catch (JsonException jsonEx)
                    {
                        _formInstance.AppendLogBox($"Failed to decode JSON from page {pageUrl}: {jsonEx.Message}");
                        FailedPageFetches.Add(new PageFetchFailure { Url = pageUrl, Offset = offset, Error = $"JSONDecodeError: {jsonEx.Message}", StatusCode = null });
                        activePageFetching = false;
                        break;
                    }
                    catch (Exception ex)
                    {
                        _formInstance.AppendLogBox($"An unexpected error occurred fetching page {pageUrl}: {ex.Message}");
                        FailedPageFetches.Add(new PageFetchFailure { Url = pageUrl, Offset = offset, Error = ex.Message, StatusCode = null });
                        activePageFetching = false;
                        break;
                    }
                }

                if (!activePageFetching) break;

                if (!currentPageFetchSuccess || postsOnPage == null)
                {
                    _formInstance.AppendLogBox($"ERROR: Could not retrieve page data for offset {offset} after all attempts. Stopping.");
                    break;
                }

                if (!postsOnPage.Any())
                {
                    _formInstance.AppendLogBox("No more posts found on this page or subsequent pages. Ending pagination.");
                    break;
                }

                _formInstance.AppendLogBox($"Found {postsOnPage.Count} posts on this page.");

                foreach (var post in postsOnPage)
                {
                    _postSkipCts = new CancellationTokenSource();
                    try
                    {
                        if (_postLimitConfig > 0 && totalPostsProcessed >= _postLimitConfig)
                        {
                            _formInstance.AppendLogBox($"Post limit ({_postLimitConfig}) reached. Not processing further posts on this page.");
                            activePageFetching = false;
                            break;
                        }

                        totalPostsProcessed++;
                        string postId = post.Id ?? $"N/A_{offset}_{postsOnPage.IndexOf(post)}";
                        string postTitle = post.Title ?? "No Title";
                        int maxSegmentLength = _formInstance.getTrackbarVal();
                        string sanitizedService = PathSanitizer.SanitizeDirectoryName(service, maxSegmentLength);
                        string sanitizedCreatorName = PathSanitizer.SanitizeDirectoryName(creatorId, maxSegmentLength);
                        string sanitizedPostTitle = PathSanitizer.SanitizeDirectoryName(postTitle, maxSegmentLength);
                        string creatorOutputDir = Path.Combine(baseOutputDir, sanitizedService, sanitizedCreatorName, sanitizedPostTitle);
                        creatorOutputDir = LimitPathLength(creatorOutputDir);
                        Directory.CreateDirectory(creatorOutputDir);
                        _formInstance.AppendLogBox($"Processing post {totalPostsProcessed}: \"{postTitle}\" (ID: {postId}) -> Saving to '{creatorOutputDir}'");

                        if (post.File?.Path != null)
                        {
                            string fullFileUrl = $"https://kemono.su{post.File.Path}";
                            string fileExt = Path.GetExtension(new Uri(fullFileUrl).LocalPath).ToLower();
                            string dlFilename = GetFilenameFromUrl(fullFileUrl);
                            string uniqueDlFilename = _formInstance.getUniqueFilename(creatorOutputDir, dlFilename);

                            if (!_desiredExtensions.Any() || _desiredExtensions.Contains(fileExt))
                            {
                                if (await DownloadManager.downloadFileAsync(fullFileUrl, post.File.Path, uniqueDlFilename, creatorOutputDir, _formInstance, _postSkipCts.Token))
                                {
                                    totalDownloadedCount++;
                                }
                            }
                            else
                            {
                                _formInstance.AppendLogBox($"Skipping main file '{dlFilename}' (Post ID: {postId}), undesired extension: {fileExt}");
                            }
                        }

                        if (post.Attachments != null)
                        {
                            foreach (var attachment in post.Attachments)
                            {
                                if (attachment.Path != null)
                                {
                                    string fullAttachmentUrl = $"https://kemono.su{attachment.Path}";
                                    string fileExt = Path.GetExtension(new Uri(fullAttachmentUrl).LocalPath).ToLower();
                                    string dlFilename = GetFilenameFromUrl(fullAttachmentUrl);
                                    string uniqueDlFilename = _formInstance.getUniqueFilename(creatorOutputDir, dlFilename);

                                    if (!_desiredExtensions.Any() || _desiredExtensions.Contains(fileExt))
                                    {
                                        if (await DownloadManager.downloadFileAsync(fullAttachmentUrl, attachment.Path, uniqueDlFilename, creatorOutputDir, _formInstance, _postSkipCts.Token))
                                        {
                                            totalDownloadedCount++;
                                        }
                                    }
                                    else
                                    {
                                        _formInstance.AppendLogBox($"Skipping attachment '{dlFilename}' (Post ID: {postId}), undesired extension: {fileExt}");
                                    }
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        continue;
                    }
                    finally
                    {
                        _postSkipCts.Dispose();
                        _postSkipCts = null;
                    }

                    if (!activePageFetching)
                    {
                        break;
                    }
                }

                offset += KemonoConstants.PageLimit;
                await Task.Delay(TimeSpan.FromSeconds(0.25));
            }

            if (FailedPageFetches.Any())
            {
                _formInstance.AppendLogBox($"\n--- {FailedPageFetches.Count} pages failed to fetch after initial attempts. ---");
                foreach (var pageFail in FailedPageFetches)
                {
                    _formInstance.AppendLogBox($"- URL: {pageFail.Url}, Error: {pageFail.Error}, Status: {pageFail.StatusCode}");
                }
            }
            else
            {
                _formInstance.AppendLogBox("\nAll pages successfully fetched or no failures occurred.");
            }

            if (DownloadManager.failedDownloads.Any())
            {
                _formInstance.AppendLogBox($"\n--- Retrying {DownloadManager.failedDownloads.Count} persistently failed file downloads ---");
                var filesStillFailingRetryPass = new ConcurrentBag<failedDownload>();
                foreach (var item in DownloadManager.failedDownloads.ToList())
                {
                    _formInstance.AppendLogBox($"Retrying file: {item.fileName} from {item.URL}");
                    _formInstance.AppendLogBox($"Original error: {item.originalErr}");

                    string filePath = Path.Combine(item.outputDir, item.fileName);
                    if (File.Exists(filePath))
                    {
                        try { File.Delete(filePath); _formInstance.AppendLogBox($"Cleaned up incomplete file: {filePath}."); }
                        catch (IOException ioEx) { _formInstance.AppendLogBox($"ERR: Could not delete incomplete file {filePath}: {ioEx.Message}"); }
                    }

                    string uniqueRetryFilename = _formInstance.getUniqueFilename(item.outputDir, item.fileName);
                    if (await DownloadManager.downloadFileAsync(item.URL, item.kemonoPath, uniqueRetryFilename, item.outputDir, _formInstance, CancellationToken.None, isRetryPass: true))
                    {
                        totalDownloadedCount++;
                    }
                    else
                    {
                        filesStillFailingRetryPass.Add(item);
                    }
                }
                DownloadManager.failedDownloads = filesStillFailingRetryPass;

                if (DownloadManager.failedDownloads.Any())
                {
                    _formInstance.AppendLogBox($"\n{DownloadManager.failedDownloads.Count} files still failed after the retry pass.");
                }
                else
                {
                    _formInstance.AppendLogBox("\nAll previously failed files were successfully downloaded in retry pass.");
                }
            }

            _formInstance.AppendLogBox("\n--- Download Process Finished ---");
            _formInstance.AppendLogBox($"Total posts processed from API: {totalPostsProcessed}");
            _formInstance.AppendLogBox($"Total files successfully downloaded (including retries): {totalDownloadedCount}");
            _formInstance.AppendLogBox($"Total unique Kemono file paths in downloaded_paths set: {DownloadManager.downloadedPaths.Count}");

            if (DownloadManager.failedDownloads.Any())
            {
                _formInstance.AppendLogBox($"Number of files that could NOT be downloaded: {DownloadManager.failedDownloads.Count}");
            }
            if (FailedPageFetches.Any())
            {
                _formInstance.AppendLogBox($"Number of pages that could NOT be fetched (or were 404/unretryable): {FailedPageFetches.Count}");
            }

            int emptyDirCount = DeleteEmptyDirectories(baseOutputDir);
            if (emptyDirCount > 0)
            {
                _formInstance.AppendLogBox($"\nDeleted {emptyDirCount} empty directories in '{baseOutputDir}'.");
            }
            else
            {
                _formInstance.AppendLogBox("\nNo empty directories found to delete.");
            }
        }

        private int DeleteEmptyDirectories(string rootDir)
        {
            int deletedCount = 0;
            try
            {
                foreach (var dir in Directory.GetDirectories(rootDir, "*", SearchOption.AllDirectories)
                                         .OrderByDescending(d => d.Length))
                {
                    if (IsDirectoryEmpty(dir))
                    {
                        try
                        {
                            Directory.Delete(dir, false);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            _formInstance.AppendLogBox($"Failed to delete empty directory '{dir}': {ex.Message}");
                        }
                    }
                }
                if (IsDirectoryEmpty(rootDir))
                {
                    try
                    {
                        Directory.Delete(rootDir, false);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _formInstance.AppendLogBox($"Failed to delete empty root directory '{rootDir}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _formInstance.AppendLogBox($"Error during empty directory cleanup: {ex.Message}");
            }
            return deletedCount;
        }

        private bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        private string GetFilenameFromUrl(string url)
        {
            Uri uri = new Uri(url);
            string filename = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrEmpty(filename) || filename == ".")
            {
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                if (!string.IsNullOrEmpty(queryParams["filename"]))
                {
                    return Uri.UnescapeDataString(queryParams["filename"]);
                }
                return "downloaded_file";
            }
            return filename;
        }

        // Add this helper to KemonoApiFetcher (or a shared utility class)
        private string LimitPathLength(string path, int maxLength = 260)
        {
            if (path.Length <= maxLength)
                return path;

            // Try to preserve the filename and extension
            string directory = Path.GetDirectoryName(path);
            string filename = Path.GetFileName(path);
            int allowedDirLength = maxLength - filename.Length - 1; // 1 for separator

            if (allowedDirLength < 1)
                throw new PathTooLongException("Cannot construct a valid path under the limit.");

            if (directory.Length > allowedDirLength)
                directory = directory.Substring(0, allowedDirLength);

            return Path.Combine(directory, filename);
        }
    }

    public static class KemonoConstants
    {
        public const int PageFetchTimeoutSeconds = 60;
        public const int MaxRetries = 5;
        public const int RetryDelaySeconds = 3;
        public const int PageLimit = 50;
        public const string BaseApiUrl = "https://kemono.su/api/v1/";
    }
}