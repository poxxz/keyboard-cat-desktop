using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;

// Dual-source official release updater:
// 1) Checks Gitee Releases API first (fast & reliable in Mainland China),
// 2) Automatically falls back to GitHub Releases API if Gitee is unreachable or has no release,
// 3) Downloads either the NSIS Setup.exe (installed builds) or portable KeyboardCat.exe with
//    automatic cross-source download fallback, MZ PE header validation, silent install/replace,
//    automatic restart, and complete cleanup of all temporary files.
internal static class UpdateManager
{
    private const string RepoOwner = "poxxz";
    private const string RepoName = "keyboard-cat-desktop";
    private const string GiteeReleasesUrl = "https://gitee.com/" + RepoOwner + "/" + RepoName + "/releases";
    private const string GitHubReleasesUrl = "https://github.com/" + RepoOwner + "/" + RepoName + "/releases";
    private const string GiteeLatestApi = "https://gitee.com/api/v5/repos/" + RepoOwner + "/" + RepoName + "/releases/latest";
    private const string GiteeListApi = "https://gitee.com/api/v5/repos/" + RepoOwner + "/" + RepoName + "/releases?page=1&per_page=10&direction=desc";
    private const string GitHubLatestApi = "https://api.github.com/repos/" + RepoOwner + "/" + RepoName + "/releases/latest";
    private const string UninstallRegPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\KeycatDesktop.B48D8B";
    private const string StageFolderName = "KeycatDesktopUpdate";

    internal sealed class CheckResult
    {
        public string CurrentVersion;
        public string LatestVersion;
        public string TagName;
        public string SourceName;
        public string ReleaseUrl;
        public string SetupUrl;
        public string PortableUrl;
        public string FallbackSetupUrl;
        public string FallbackPortableUrl;
        public bool HasUpdate;
        public string Error;
    }

    internal static string CurrentVersionText
    {
        get { return FormatVersion(GetCurrentVersion()); }
    }

    internal static string FormatVersion(Version v)
    {
        string baseVer = v.Major + "." + v.Minor + "." + Math.Max(0, v.Build);
        return v.Revision > 0 ? baseVer + " v" + v.Revision : baseVer;
    }

    private static Version GetCurrentVersion()
    {
        Version v = Assembly.GetExecutingAssembly().GetName().Version;
        if (v == null) return new Version(1, 0, 0, 0);
        return new Version(v.Major, v.Minor, Math.Max(0, v.Build), Math.Max(0, v.Revision));
    }

    private static string StageDir
    {
        get { return Path.Combine(Path.GetTempPath(), StageFolderName); }
    }

    internal static void OpenReleasePage(string url)
    {
        try
        {
            string target = string.IsNullOrEmpty(url) ? GiteeReleasesUrl : url;
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception) { }
    }

    // Best-effort cleanup of any leftover update staging files from a previous update.
    internal static void CleanupLeftovers()
    {
        ThreadPool.QueueUserWorkItem(delegate
        {
            try
            {
                string dir = StageDir;
                if (Directory.Exists(dir))
                {
                    try { Directory.Delete(dir, true); } catch (Exception) { }
                }
                string exe = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(exe))
                {
                    string folder = Path.GetDirectoryName(exe);
                    if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                    {
                        string[] leftovers = Directory.GetFiles(folder, "*.keycat-old");
                        for (int i = 0; i < leftovers.Length; ++i)
                        {
                            try { File.Delete(leftovers[i]); } catch (Exception) { }
                        }
                    }
                }
            }
            catch (Exception) { }
        });
    }

    private static void EnsureTls12()
    {
        try
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072; // TLS 1.2
        }
        catch (Exception) { }
    }

    internal static CheckResult CheckLatest()
    {
        Version current = GetCurrentVersion();
        CheckResult gitee = CheckGitee(current);
        if (string.IsNullOrEmpty(gitee.Error))
        {
            return gitee;
        }

        CheckResult github = CheckGitHub(current);
        if (string.IsNullOrEmpty(github.Error))
        {
            return github;
        }

        return new CheckResult
        {
            CurrentVersion = CurrentVersionText,
            SourceName = "Gitee / GitHub",
            ReleaseUrl = GiteeReleasesUrl,
            Error = "无法连接 Gitee 与 GitHub 更新源，请检查网络"
        };
    }

    private static CheckResult CheckGitee(Version current)
    {
        CheckResult res = new CheckResult
        {
            CurrentVersion = CurrentVersionText,
            SourceName = "Gitee",
            ReleaseUrl = GiteeReleasesUrl
        };
        try
        {
            string json = FetchJson(GiteeLatestApi, null, 8000);
            if (IsPrerelease(json))
            {
                // Fallback to release list to pick the latest non-prerelease item.
                string listJson = FetchJson(GiteeListApi, null, 8000);
                json = ExtractLatestOfficialFromArray(listJson);
            }
            if (string.IsNullOrEmpty(json))
            {
                res.Error = "Gitee 暂无正式发布版本";
                return res;
            }
            return PopulateResultFromReleaseJson(json, current, res, true);
        }
        catch (Exception)
        {
            res.Error = "连接 Gitee 失败";
            return res;
        }
    }

    private static CheckResult CheckGitHub(Version current)
    {
        CheckResult res = new CheckResult
        {
            CurrentVersion = CurrentVersionText,
            SourceName = "GitHub",
            ReleaseUrl = GitHubReleasesUrl
        };
        try
        {
            string json = FetchJson(GitHubLatestApi, "application/vnd.github+json", 10000);
            return PopulateResultFromReleaseJson(json, current, res, false);
        }
        catch (WebException ex)
        {
            HttpWebResponse errResp = ex.Response as HttpWebResponse;
            if (errResp != null && (int)errResp.StatusCode == 404)
                res.Error = "仓库暂无正式发布版本";
            else if (errResp != null && (int)errResp.StatusCode == 403)
                res.Error = "GitHub 请求频率受限，请稍后再试";
            else
                res.Error = "连接 GitHub 失败，请检查网络";
            return res;
        }
        catch (Exception)
        {
            res.Error = "检查更新时发生异常，请重试";
            return res;
        }
    }

    internal static CheckResult PopulateResultFromReleaseJson(string json, Version current, CheckResult res, bool isGitee)
    {
        string tag = MatchJsonString(json, "tag_name");
        if (string.IsNullOrEmpty(tag))
        {
            res.Error = "未获取到有效版本信息";
            return res;
        }
        res.TagName = tag;
        string html = MatchJsonString(json, "html_url");
        if (!string.IsNullOrEmpty(html))
            res.ReleaseUrl = html;
        else if (isGitee)
            res.ReleaseUrl = GiteeReleasesUrl + "/tag/" + Uri.EscapeDataString(tag);

        Version latest = ParseTagVersion(tag);
        if (latest == null)
        {
            res.Error = "版本号格式无法识别 (" + tag + ")";
            return res;
        }
        res.LatestVersion = FormatVersion(latest);
        res.HasUpdate = latest > current;

        ExtractAssets(json, res);
        // Require at least one Windows .exe asset when checking Gitee so an un-synced mirror falls back cleanly.
        if (isGitee && string.IsNullOrEmpty(res.SetupUrl) && string.IsNullOrEmpty(res.PortableUrl))
        {
            res.Error = "Gitee 发行版暂未同步 Windows 安装包";
            return res;
        }

        // Populate cross-source fallback URLs so a Gitee anti-hotlink hiccup still downloads smoothly from GitHub.
        if (isGitee)
        {
            string baseVer = latest.Major + "." + latest.Minor + "." + Math.Max(0, latest.Build);
            string ghBase = "https://github.com/" + RepoOwner + "/" + RepoName + "/releases/download/" + Uri.EscapeDataString(tag) + "/";
            res.FallbackSetupUrl = ghBase + "KeyboardCat-" + baseVer + "-Windows-Setup.exe";
            res.FallbackPortableUrl = ghBase + "KeyboardCat.exe";
        }
        return res;
    }

    private static string FetchJson(string url, string accept, int timeoutMs)
    {
        EnsureTls12();
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
        req.Method = "GET";
        req.UserAgent = "KeycatDesktop/" + CurrentVersionText;
        req.Accept = string.IsNullOrEmpty(accept) ? "application/json" : accept;
        req.Timeout = timeoutMs;
        req.ReadWriteTimeout = timeoutMs;

        using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
        using (Stream stream = resp.GetResponseStream())
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    private static bool IsPrerelease(string json)
    {
        if (string.IsNullOrEmpty(json)) return false;
        return Regex.IsMatch(json, "\"prerelease\"\\s*:\\s*true", RegexOptions.IgnoreCase);
    }

    private static string ExtractLatestOfficialFromArray(string arrayJson)
    {
        if (string.IsNullOrEmpty(arrayJson)) return null;
        int depth = 0;
        bool inStr = false;
        bool escape = false;
        int objStart = -1;
        for (int i = 0; i < arrayJson.Length; ++i)
        {
            char ch = arrayJson[i];
            if (inStr)
            {
                if (escape) { escape = false; }
                else if (ch == '\\') { escape = true; }
                else if (ch == '"') { inStr = false; }
                continue;
            }
            if (ch == '"') { inStr = true; continue; }
            if (ch == '{')
            {
                if (depth == 0) objStart = i;
                depth++;
            }
            else if (ch == '}')
            {
                depth--;
                if (depth == 0 && objStart >= 0)
                {
                    string obj = arrayJson.Substring(objStart, i - objStart + 1);
                    if (!IsPrerelease(obj)) return obj;
                    objStart = -1;
                }
            }
        }
        return null;
    }

    private static string MatchJsonString(string json, string key)
    {
        Match m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
        if (!m.Success) return null;
        return UnescapeJsonString(m.Groups[1].Value);
    }

    private static string UnescapeJsonString(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        return Regex.Unescape(raw.Replace("\\/", "/"));
    }

    // Parses tags like v1.8.0, v1.8.0.2, v1.8.0-v2, v1.8.0_v2, or v1.8.0 v2 into 4-part Version.
    internal static Version ParseTagVersion(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return null;
        Match m = Regex.Match(tag, @"(\d+)\.(\d+)(?:\.(\d+))?(?:(?:\.|[-_\s]*[vV])(\d+))?");
        if (!m.Success) return null;
        int maj = int.Parse(m.Groups[1].Value);
        int min = int.Parse(m.Groups[2].Value);
        int pat = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;
        int rev = m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : 0;
        return new Version(maj, min, pat, rev);
    }

    private static void ExtractAssets(string json, CheckResult res)
    {
        int assetsIdx = json.IndexOf("\"assets\"", StringComparison.Ordinal);
        if (assetsIdx < 0) return;
        string tail = json.Substring(assetsIdx);
        MatchCollection urls = Regex.Matches(tail, "\"browser_download_url\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
        for (int i = 0; i < urls.Count; ++i)
        {
            string url = UnescapeJsonString(urls[i].Groups[1].Value);
            string cleanUrl = url;
            int qIdx = cleanUrl.IndexOf('?');
            if (qIdx >= 0) cleanUrl = cleanUrl.Substring(0, qIdx);
            string lower = cleanUrl.ToLowerInvariant();
            if (!lower.EndsWith(".exe")) continue;
            if (lower.IndexOf("setup", StringComparison.Ordinal) >= 0)
            {
                if (string.IsNullOrEmpty(res.SetupUrl)) res.SetupUrl = url;
            }
            else
            {
                if (string.IsNullOrEmpty(res.PortableUrl)) res.PortableUrl = url;
            }
        }
    }

    private static bool IsInstalledBuild(out string installDir)
    {
        installDir = null;
        try
        {
            string exePath = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
            string exeDir = Path.GetDirectoryName(exePath);
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(UninstallRegPath, false))
            {
                if (key != null)
                {
                    string loc = key.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrEmpty(loc))
                    {
                        string normLoc = Path.GetFullPath(loc).TrimEnd('\\');
                        string normExeDir = (exeDir ?? "").TrimEnd('\\');
                        if (string.Equals(normLoc, normExeDir, StringComparison.OrdinalIgnoreCase))
                        {
                            installDir = normLoc;
                            return true;
                        }
                    }
                }
            }
        }
        catch (Exception) { }
        return false;
    }

    private static bool TryDownloadExecutable(string url, string destination, Action<int> onProgress)
    {
        if (string.IsNullOrEmpty(url)) return false;
        try
        {
            EnsureTls12();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) KeycatDesktop/" + CurrentVersionText;
            if (url.IndexOf("gitee.com", StringComparison.OrdinalIgnoreCase) >= 0)
                req.Referer = GiteeReleasesUrl;
            req.AllowAutoRedirect = true;
            req.Timeout = 18000;
            req.ReadWriteTimeout = 25000;

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream src = resp.GetResponseStream())
            using (FileStream dst = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                long total = resp.ContentLength;
                byte[] buf = new byte[16384];
                long downloaded = 0;
                int lastPct = -1;
                int read;
                while ((read = src.Read(buf, 0, buf.Length)) > 0)
                {
                    dst.Write(buf, 0, read);
                    downloaded += read;
                    if (total > 0 && onProgress != null)
                    {
                        int pct = (int)Math.Min(100, downloaded * 100 / total);
                        if (pct != lastPct) { lastPct = pct; onProgress(pct); }
                    }
                }
            }

            // Validate PE header ('MZ') and sane minimum size so HTML/captcha redirects never run.
            FileInfo fi = new FileInfo(destination);
            if (!fi.Exists || fi.Length < 50 * 1024)
            {
                TryDeleteFile(destination);
                return false;
            }
            byte[] magic = new byte[2];
            using (FileStream check = File.OpenRead(destination))
            {
                if (check.Read(magic, 0, 2) < 2 || magic[0] != 0x4D || magic[1] != 0x5A)
                {
                    TryDeleteFile(destination);
                    return false;
                }
            }
            return true;
        }
        catch (Exception)
        {
            TryDeleteFile(destination);
            return false;
        }
    }

    // Downloads the appropriate package (Setup.exe for installed builds, KeyboardCat.exe for portable builds),
    // automatically falling back from Gitee to GitHub if needed, validates it is a real PE executable,
    // spawns a self-cleaning updater script, and returns null on success or a user-facing error message.
    internal static string DownloadAndPrepareInstall(CheckResult info, Action<int> onProgress, out Action launchUpdater)
    {
        launchUpdater = null;
        if (info == null) return "缺少版本信息";

        string installDir;
        bool installed = IsInstalledBuild(out installDir);
        bool useSetup = installed && !string.IsNullOrEmpty(info.SetupUrl);
        string primaryUrl = useSetup ? info.SetupUrl : (!string.IsNullOrEmpty(info.PortableUrl) ? info.PortableUrl : info.SetupUrl);
        if (!useSetup && primaryUrl == info.SetupUrl && !string.IsNullOrEmpty(info.SetupUrl))
            useSetup = true;
        string fallbackUrl = useSetup ? info.FallbackSetupUrl : info.FallbackPortableUrl;

        if (string.IsNullOrEmpty(primaryUrl) && string.IsNullOrEmpty(fallbackUrl))
            return "该版本未找到可用的 Windows 安装文件";

        string stage = StageDir;
        try
        {
            if (Directory.Exists(stage))
            {
                try { Directory.Delete(stage, true); } catch (Exception) { }
            }
            Directory.CreateDirectory(stage);
        }
        catch (Exception)
        {
            return "无法创建临时更新目录";
        }

        string payloadFile = Path.Combine(stage, useSetup ? "KeyboardCat-Setup.exe" : "KeyboardCat-New.exe");
        bool ok = TryDownloadExecutable(primaryUrl, payloadFile, onProgress);
        if (!ok && !string.IsNullOrEmpty(fallbackUrl) && !string.Equals(fallbackUrl, primaryUrl, StringComparison.OrdinalIgnoreCase))
        {
            if (onProgress != null) onProgress(0);
            ok = TryDownloadExecutable(fallbackUrl, payloadFile, onProgress);
        }
        if (!ok)
        {
            return "自动下载受阻（可能触发防盗链或网络波动），可点击右侧前往发布页手动下载";
        }

        string currentExe = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
        string scriptPath = Path.Combine(stage, "apply_update.cmd");
        int pid = Process.GetCurrentProcess().Id;

        try
        {
            string script;
            if (useSetup)
            {
                string installedExe = Path.Combine(
                    !string.IsNullOrEmpty(installDir)
                        ? installDir
                        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\KeycatDesktop"),
                    "键盘小黑猫.exe");
                script = BuildSetupUpdaterScript(pid, payloadFile, installedExe, stage);
            }
            else
            {
                script = BuildPortableUpdaterScript(pid, payloadFile, currentExe, stage);
            }
            File.WriteAllText(scriptPath, script, new UTF8Encoding(false));
        }
        catch (Exception)
        {
            TryDeleteFile(payloadFile);
            return "准备更新脚本失败";
        }

        launchUpdater = delegate
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c \"\"" + scriptPath + "\"\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.GetTempPath()
                };
                Process.Start(psi);
            }
            catch (Exception) { }
        };
        return null;
    }

    private static string BuildSetupUpdaterScript(int pid, string setupExe, string installedExe, string stageDir)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("chcp 65001 >nul 2>&1");
        sb.AppendLine("setlocal");
        sb.AppendLine("set /a tries=0");
        sb.AppendLine(":wait_exit");
        sb.AppendLine("tasklist /FI \"PID eq " + pid + "\" 2>nul | find \"" + pid + "\" >nul");
        sb.AppendLine("if not errorlevel 1 (");
        sb.AppendLine("  set /a tries+=1");
        sb.AppendLine("  if %tries% LSS 20 (");
        sb.AppendLine("    ping -n 2 127.0.0.1 >nul");
        sb.AppendLine("    goto wait_exit");
        sb.AppendLine("  )");
        sb.AppendLine(")");
        sb.AppendLine("\"" + setupExe + "\" /S");
        sb.AppendLine("ping -n 2 127.0.0.1 >nul");
        sb.AppendLine("if exist \"" + installedExe + "\" start \"\" \"" + installedExe + "\"");
        sb.AppendLine("del /f /q \"" + setupExe + "\" >nul 2>&1");
        sb.AppendLine("cd /d \"%TEMP%\"");
        sb.AppendLine("start \"\" /b cmd /c \"ping -n 3 127.0.0.1 >nul & rd /s /q \"" + stageDir + "\" >nul 2>&1\"");
        sb.AppendLine("exit /b 0");
        return sb.ToString();
    }

    private static string BuildPortableUpdaterScript(int pid, string newExe, string targetExe, string stageDir)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("chcp 65001 >nul 2>&1");
        sb.AppendLine("setlocal");
        sb.AppendLine("set /a tries=0");
        sb.AppendLine(":wait_exit");
        sb.AppendLine("tasklist /FI \"PID eq " + pid + "\" 2>nul | find \"" + pid + "\" >nul");
        sb.AppendLine("if not errorlevel 1 (");
        sb.AppendLine("  set /a tries+=1");
        sb.AppendLine("  if %tries% LSS 20 (");
        sb.AppendLine("    ping -n 2 127.0.0.1 >nul");
        sb.AppendLine("    goto wait_exit");
        sb.AppendLine("  )");
        sb.AppendLine(")");
        sb.AppendLine("set /a retries=0");
        sb.AppendLine(":replace_loop");
        sb.AppendLine("copy /y \"" + newExe + "\" \"" + targetExe + "\" >nul 2>&1");
        sb.AppendLine("if errorlevel 1 (");
        sb.AppendLine("  set /a retries+=1");
        sb.AppendLine("  if %retries% LSS 15 (");
        sb.AppendLine("    ping -n 2 127.0.0.1 >nul");
        sb.AppendLine("    goto replace_loop");
        sb.AppendLine("  )");
        sb.AppendLine(")");
        sb.AppendLine("if exist \"" + targetExe + "\" start \"\" \"" + targetExe + "\"");
        sb.AppendLine("del /f /q \"" + newExe + "\" >nul 2>&1");
        sb.AppendLine("cd /d \"%TEMP%\"");
        sb.AppendLine("start \"\" /b cmd /c \"ping -n 3 127.0.0.1 >nul & rd /s /q \"" + stageDir + "\" >nul 2>&1\"");
        sb.AppendLine("exit /b 0");
        return sb.ToString();
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch (Exception) { }
    }
}
