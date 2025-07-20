using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Security.AccessControl;
using System.Security.Policy;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;
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
                ".mp3", ".wav", ".flac", // Audio
                ".zip", ".rar", ".7z", // Archives
                ".pdf", ".txt", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" // Documents
            };

        private List<failedDownload> failedFileDowloads = new List<failedDownload>();
        string outputDir;

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
        }
        public void AppendLogBox(string msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => AppendLogBox(msg)));
            }
            else
            {
                logTextBox.AppendText(msg);
                logTextBox.ScrollToCaret();
            }
        }
        public string sanitizeFilename(string filename)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"[{0}]", invalidChars);
            return Regex.Replace(filename, invalidRegStr, "_");
        }
        public string getUniqueFilename(string outputDir, string filename)
        {
            filename = sanitizeFilename(filename);
            string baseName = Path.GetFileNameWithoutExtension(filename);
            string extension = Path.GetExtension(filename);

            string filePath = Path.Combine(outputDir, filename);

            if (!File.Exists(filePath))
            {
                AppendLogBox($"Using filename: {filename}.");
                return filename; // Filename is unique so return.
            }

            int i = 0;
            while (true)
            {
                string newFilename = $"{baseName}_{i}{extension}";
                string newFilePath = Path.Combine(outputDir, newFilename);

                if (!File.Exists(newFilePath))
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

            // Example for desired extensions (e.g., from a checklist box)
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

            KemonoApiFetcher fetcher = new KemonoApiFetcher(this, desiredExtensions, postLimit);
            await fetcher.StartDownloadAsync(service, creatorId, outputDir);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select where to save the downloads...";
                dialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                DialogResult result = dialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    outputDir = dialog.SelectedPath;
                    directoryBox.Text = dialog.SelectedPath;
                }
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
                if (percentage >= 0 &&  percentage <= 100)
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
        public string URL {  get; set; }
        public string kemonoPath { get; set; }
        public string fileName { get; set; }
        public string outputDir { get; set; }
        public string originalErr {  get; set; }
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

        public static async Task<bool> downloadFileAsync(string url, string kemonoPath, string filename, string outputDir, Form1 formInstance, bool isRetryPass = false)
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

                    using (HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
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
                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                downloadedSize += bytesRead;
                                
                                if (totalSize.HasValue)
                                {
                                    formInstance.updateDownloadText(downloadedSize, totalSize.Value);
                                }

                                if (totalSize.HasValue && totalSize.Value > 0)
                                {
                                    int percentage = (int)((double)downloadedSize / totalSize.Value * 100);
                                    formInstance.updateProgressBar(percentage); // Update the progress bar
                                }
                            }
                        }

                        if (totalSize.HasValue && totalSize.Value > 0 && downloadedSize != totalSize.Value)
                        {
                            throw new IncompleteDownloadException($"Downloaded {downloadedSize} bytes, but expected {totalSize.Value} bytes.");
                        }

                        formInstance.AppendLogBox($"SUCCESS: Downloaded file: {filename}.");
                        downloadedPaths.Add(kemonoPath);
                        formInstance.clearDownloadText();
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
                            failedDownloads.Add(new failedDownload
                            {
                                URL = url,
                                kemonoPath = kemonoPath,
                                fileName = filename,
                                outputDir = outputDir,
                                originalErr = httpEX.ToString()
                            });
                        }
                        return false;
                    }
                }
                catch (OperationCanceledException)
                {
                    if (retries < maxRetries)
                    {
                        retries++;
                        currentAttemptDelay = TimeSpan.Zero;
                        formInstance.AppendLogBox($"WARN: Timeout/Cancelled downloading {url}. Retrying ({retries}/{maxRetries}) immediately...)");

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
                        await Task.Delay(currentAttemptDelay);
                        continue;
                    }
                    else
                    {
                        formInstance.AppendLogBox($"Timeout or Cancellation of download {url} after {maxRetries} retries.");
                        if (!isRetryPass && !failedDownloads.Any(item => item.URL == url))
                        {
                            failedDownloads.Add(new failedDownload
                            {
                                URL = url,
                                kemonoPath = kemonoPath,
                                fileName = filename,
                                outputDir = outputDir,
                                originalErr = "Timeout or Operation Cancelled"
                            });
                        }
                        return false;
                    }
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
                            failedDownloads.Add(new failedDownload
                            {
                                URL = url,
                                kemonoPath = kemonoPath,
                                fileName = filename,
                                outputDir = outputDir,
                                originalErr = incompleteEX.Message
                            });
                        }
                        return false;
                    }
                }
                catch (IOException ioEX)
                {
                    formInstance.AppendLogBox($"ERR: File system error while saving {filename}: {ioEX.Message}");
                    if (!isRetryPass && !failedDownloads.Any(item => item.URL == url))
                    {
                        failedDownloads.Add(new failedDownload
                        {
                            URL = url,
                            kemonoPath = kemonoPath,
                            fileName = filename,
                            outputDir = outputDir,
                            originalErr = ioEX.ToString()
                        });
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    formInstance.AppendLogBox($"ERR: Error occurred while downloading {filename}: {ex.Message}");
                    if (!isRetryPass && !failedDownloads.Any(item => item.URL == url))
                    {
                        failedDownloads.Add(new failedDownload
                        {
                            URL = url,
                            kemonoPath = kemonoPath,
                            fileName = filename,
                            outputDir = outputDir,
                            originalErr = ex.ToString()
                        });
                    }
                    return false;
                }
            }
            if (!isRetryPass && !failedDownloads.Any(item => item.URL == url))
            {
                failedDownloads.Add(new failedDownload
                {
                    URL = url,
                    kemonoPath = kemonoPath,
                    fileName = filename,
                    outputDir = outputDir,
                    originalErr = "Max retries exhausted in DownloadFileAsync"
                });
            }
            return false;
        }
    }

    public class KemonoApiFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly Form1 _formInstance; // Reference to your Form1 for logging
        private readonly List<string> _desiredExtensions; // To filter media types
        private readonly int _postLimitConfig; // Max number of posts to process

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
            HashSet<string> downloadedPaths = new HashSet<string>(); // To track already processed kemono paths

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
                        response.EnsureSuccessStatusCode(); // Throws for 4xx/5xx responses

                        string jsonString = await response.Content.ReadAsStringAsync();
                        // Using System.Text.Json
                        postsOnPage = JsonSerializer.Deserialize<List<KemonoPost>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        // If using Newtonsoft.Json: postsOnPage = JsonConvert.DeserializeObject<List<KemonoPost>>(jsonString);

                        currentPageFetchSuccess = true;
                        break; // Success
                    }
                    catch (HttpRequestException httpEx)
                    {
                        int statusCode = (int?)httpEx.StatusCode ?? 0;
                        retries++;
                        _formInstance.AppendLogBox($"WARN: HTTP {statusCode} Error fetching page {pageUrl}. Retrying ({retries}/{KemonoConstants.MaxRetries}) in {KemonoConstants.RetryDelaySeconds}s...");
                        await Task.Delay(TimeSpan.FromSeconds(KemonoConstants.RetryDelaySeconds));
                    }
                    catch (OperationCanceledException) // Catches Timeout
                    {
                        retries++;
                        _formInstance.AppendLogBox($"WARN: Timeout fetching page {pageUrl}. Retrying ({retries}/{KemonoConstants.MaxRetries}) immediately...");
                        // No delay for immediate retry
                    }
                    catch (JsonException jsonEx) // For System.Text.Json, or JsonSerializationException for Newtonsoft.Json
                    {
                        _formInstance.AppendLogBox($"Failed to decode JSON from page {pageUrl}: {jsonEx.Message}");
                        // Log to failedPageFetches similar to Python
                        FailedPageFetches.Add(new PageFetchFailure { Url = pageUrl, Offset = offset, Error = $"JSONDecodeError: {jsonEx.Message}", StatusCode = null });
                        activePageFetching = false;
                        break;
                    }
                    catch (Exception ex)
                    {
                        _formInstance.AppendLogBox($"An unexpected error occurred fetching page {pageUrl}: {ex.Message}");
                        // Log to failedPageFetches similar to Python
                        FailedPageFetches.Add(new PageFetchFailure { Url = pageUrl, Offset = offset, Error = ex.Message, StatusCode = null });
                        activePageFetching = false;
                        break;
                    }
                }

                if (!activePageFetching)
                {
                    break;
                }

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
                    if (_postLimitConfig > 0 && totalPostsProcessed >= _postLimitConfig)
                    {
                        _formInstance.AppendLogBox($"Post limit ({_postLimitConfig}) reached. Not processing further posts on this page.");
                        activePageFetching = false;
                        break;
                    }

                    totalPostsProcessed++;
                    string postId = post.Id ?? $"N/A_{offset}_{postsOnPage.IndexOf(post)}"; // Similar to Python's N/A_{page_counter}_{post_index}
                    string postTitle = post.Title ?? "No Title";
                    string sanitizedTitle = _formInstance.sanitizeFilename(postTitle); // Re-use your sanitizeFilename for folder names
                    string postFolderName = sanitizedTitle; // Simplified, you can add ID if preferred
                    string postOutputDir = Path.Combine(baseOutputDir, postFolderName);
                    Directory.CreateDirectory(postOutputDir);

                    _formInstance.AppendLogBox($"Processing post {totalPostsProcessed}: \"{postTitle}\" (ID: {postId}) -> Saving to '{postOutputDir}'");

                    // --- Main file download logic ---
                    if (post.File?.Path != null) // Check if 'file' object and 'path' property exist
                    {
                        string fullFileUrl = $"https://kemono.su{post.File.Path}";
                        string fileExt = Path.GetExtension(new Uri(fullFileUrl).LocalPath).ToLower();
                        string dlFilename = GetFilenameFromUrl(fullFileUrl); // You'll need to implement this
                        string uniqueDlFilename = _formInstance.getUniqueFilename(postOutputDir, dlFilename);

                        if (!_desiredExtensions.Any() || _desiredExtensions.Contains(fileExt))
                        {
                            if (await DownloadManager.downloadFileAsync(fullFileUrl, post.File.Path, uniqueDlFilename, postOutputDir, _formInstance))
                            {
                                totalDownloadedCount++;
                            }
                        }
                        else
                        {
                            _formInstance.AppendLogBox($"Skipping main file '{dlFilename}' (Post ID: {postId}), undesired extension: {fileExt}");
                        }
                    }

                    // --- Attachments download logic ---
                    if (post.Attachments != null)
                    {
                        foreach (var attachment in post.Attachments)
                        {
                            if (attachment.Path != null)
                            {
                                string fullAttachmentUrl = $"https://kemono.su{attachment.Path}";
                                string fileExt = Path.GetExtension(new Uri(fullAttachmentUrl).LocalPath).ToLower();
                                string dlFilename = GetFilenameFromUrl(fullAttachmentUrl);
                                string uniqueDlFilename = _formInstance.getUniqueFilename(postOutputDir, dlFilename);

                                if (!_desiredExtensions.Any() || _desiredExtensions.Contains(fileExt))
                                {
                                    if (await DownloadManager.downloadFileAsync(fullAttachmentUrl, attachment.Path, uniqueDlFilename, postOutputDir, _formInstance))
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

                    if (!activePageFetching)
                    {
                        break;
                    }
                }

                offset += KemonoConstants.PageLimit;
                await Task.Delay(TimeSpan.FromSeconds(1)); // Small delay between pages
            }

            // --- Retry persistently failed page fetches ---
            // This is complex and might require re-calling parts of StartDownloadAsync for a specific page.
            // For simplicity in this conceptual example, we'll just report them.
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


            // --- Retry persistently failed file downloads ---
            if (DownloadManager.failedDownloads.Any())
            {
                _formInstance.AppendLogBox($"\n--- Retrying {DownloadManager.failedDownloads.Count} persistently failed file downloads ---");
                var filesStillFailingRetryPass = new ConcurrentBag<failedDownload>();
                foreach (var item in DownloadManager.failedDownloads.ToList()) // Iterate over a copy
                {
                    _formInstance.AppendLogBox($"Retrying file: {item.fileName} from {item.URL}");
                    _formInstance.AppendLogBox($"Original error: {item.originalErr}");

                    // Clear the file if it exists before retrying, as in Python
                    string filePath = Path.Combine(item.outputDir, item.fileName);
                    if (File.Exists(filePath))
                    {
                        try { File.Delete(filePath); _formInstance.AppendLogBox($"Cleaned up incomplete file: {filePath}."); }
                        catch (IOException ioEx) { _formInstance.AppendLogBox($"ERR: Could not delete incomplete file {filePath}: {ioEx.Message}"); }
                    }

                    string uniqueRetryFilename = _formInstance.getUniqueFilename(item.outputDir, item.fileName);
                    if (await DownloadManager.downloadFileAsync(item.URL, item.kemonoPath, uniqueRetryFilename, item.outputDir, _formInstance, isRetryPass: true))
                    {
                        totalDownloadedCount++;
                    }
                    else
                    {
                        filesStillFailingRetryPass.Add(item); // Add back to list if still fails
                    }
                }
                // Update the global failedDownloads list (you might want to clear and re-add or create a new instance)
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
        }

        // Helper function to extract filename from URL, similar to Python's get_filename_from_url
        private string GetFilenameFromUrl(string url)
        {
            // This is a simplified version. The Python script has more robust logic for query params.
            // You can port that regex logic more directly if needed.
            Uri uri = new Uri(url);
            string filename = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrEmpty(filename) || filename == ".")
            {
                // Attempt to get from query string, similar to Python's re.search(r'filename=([^&]+)', query)
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                if (!string.IsNullOrEmpty(queryParams["filename"]))
                {
                    return Uri.UnescapeDataString(queryParams["filename"]);
                }
                return "downloaded_file";
            }
            return filename;
        }
    }

    
    public static class KemonoConstants
    {
        public const int PageFetchTimeoutSeconds = 60; // Adjust as needed
        public const int MaxRetries = 5; // Adjust as needed
        public const int RetryDelaySeconds = 3; // Adjust as needed
        public const int PageLimit = 50; // Adjust as needed
        public const string BaseApiUrl = "https://kemono.su/api/v1/";
    }
}
