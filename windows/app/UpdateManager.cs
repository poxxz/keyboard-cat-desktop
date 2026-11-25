using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;

// Checks GitHub Releases for the latest official (non-draft, non-prerelease) version,
// downloads either the NSIS installer or portable exe, applies the update seamlessly,
// restarts the app, and removes all temporary / leftover installer files automatically.
internal static class UpdateManager
{
    private const string RepoOwner = "poxxz";
    private const string RepoName = "keyboard-cat-desktop";
    private const string LatestReleaseApi = "https://api.github.com/repos/" + RepoOwner + "/" + RepoName + "/releases/latest";
    private const string UninstallRegPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\KeycatDesktop.B48D8B";
    private const string StageFolderName = "KeycatDesktopUpdate";

    internal sealed class CheckResult
    {
        public string CurrentVersion;
        public string LatestVersion;
        public string TagName;
        public string ReleaseUrl;
        public string SetupUrl;
        public string PortableUrl;
        public bool HasUpdate;
        public string Error;
    }

    internal static string CurrentVersionText
    {
        get
        {
            Version v = GetCurrentVersion();
            return v.Major + "." + v.Minor + "." + Math.Max(0, v.Build);
        }
    }

    private static Version GetCurrentVersion()
    {
        Version v = Assembly.GetExecutingAssembly().GetName().Version;
        if (v == null) return new Version(1, 0, 0);
        return new Version(v.Major, v.Minor, Math.Max(0, v.Build));
    }

    private static string StageDir
    {
        get { return Path.Combine(Path.GetTempPath(), StageFolderName); }
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
        CheckResult res = new CheckResult
        {
            CurrentVersion = CurrentVersionText,
            ReleaseUrl = "https://github.com/" + RepoOwner + "/" + RepoName + "/releases"
        };

        try
        {
            EnsureTls12();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(LatestReleaseApi);
            req.Method = "GET";
            req.UserAgent = "KeycatDesktop/" + res.CurrentVersion;
            req.Accept = "application/vnd.github+json";
            req.Timeout = 12000;
            req.ReadWriteTimeout = 12000;

            string json;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream stream = resp.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                json = reader.ReadToEnd();
            }

            string tag = MatchJsonString(json, "tag_name");
            if (string.IsNullOrEmpty(tag))
            {
                res.Error = "未获取到有效版本信息";
                return res;
            }
            res.TagName = tag;
            string html = MatchJsonString(json, "html_url");
            if (!string.IsNullOrEmpty(html)) res.ReleaseUrl = html;

            Version latest = ParseTagVersion(tag);
            if (latest == null)
            {
                res.Error = "版本号格式无法识别 (" + tag + ")";
                return res;
            }
            res.LatestVersion = latest.Major + "." + latest.Minor + "." + Math.Max(0, latest.Build);
            res.HasUpdate = latest > current;

            ExtractAssets(json, res);
            return res;
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

    private static string MatchJsonString(string json, string key)
    {
        Match m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
        if (!m.Success) return null;
        return Regex.Unescape(m.Groups[1].Value);
    }

    private static Version ParseTagVersion(string tag)
    {
        Match m = Regex.Match(tag, @"(\d+)\.(\d+)(?:\.(\d+))?");
        if (!m.Success) return null;
        int maj = int.Parse(m.Groups[1].Value);
        int min = int.Parse(m.Groups[2].Value);
        int pat = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;
        return new Version(maj, min, pat);
    }

    private static void ExtractAssets(string json, CheckResult res)
    {
        int assetsIdx = json.IndexOf("\"assets\"", StringComparison.Ordinal);
        if (assetsIdx < 0) return;
        string tail = json.Substring(assetsIdx);
        MatchCollection urls = Regex.Matches(tail, "\"browser_download_url\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
        for (int i = 0; i < urls.Count; ++i)
        {
            string url = Regex.Unescape(urls[i].Groups[1].Value);
            string lower = url.ToLowerInvariant();
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

    // Downloads the appropriate package (Setup.exe for installed builds, KeyboardCat.exe for portable builds),
    // validates it is a real PE executable, spawns a self-cleaning updater script, and returns null on success
    // or a user-facing Chinese error message on failure.
    internal static string DownloadAndPrepareInstall(CheckResult info, Action<int> onProgress, out Action launchUpdater)
    {
        launchUpdater = null;
        if (info == null) return "缺少版本信息";

        string installDir;
        bool installed = IsInstalledBuild(out installDir);
        bool useSetup = installed && !string.IsNullOrEmpty(info.SetupUrl);
        string downloadUrl = useSetup ? info.SetupUrl : (!string.IsNullOrEmpty(info.PortableUrl) ? info.PortableUrl : info.SetupUrl);
        if (!useSetup && downloadUrl == info.SetupUrl && !string.IsNullOrEmpty(info.SetupUrl))
            useSetup = true;

        if (string.IsNullOrEmpty(downloadUrl))
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
        try
        {
            EnsureTls12();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(downloadUrl);
            req.Method = "GET";
            req.UserAgent = "KeycatDesktop/" + CurrentVersionText;
            req.AllowAutoRedirect = true;
            req.Timeout = 20000;
            req.ReadWriteTimeout = 30000;

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream src = resp.GetResponseStream())
            using (FileStream dst = new FileStream(payloadFile, FileMode.Create, FileAccess.Write, FileShare.None))
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

            // Validate PE header ('MZ') and sane minimum size so corrupted/HTML responses never run.
            FileInfo fi = new FileInfo(payloadFile);
            if (!fi.Exists || fi.Length < 50 * 1024)
            {
                TryDeleteFile(payloadFile);
                return "下载文件不完整，请重试";
            }
            byte[] magic = new byte[2];
            using (FileStream check = File.OpenRead(payloadFile))
            {
                if (check.Read(magic, 0, 2) < 2 || magic[0] != 0x4D || magic[1] != 0x5A)
                {
                    TryDeleteFile(payloadFile);
                    return "下载文件校验失败，请重试";
                }
            }
        }
        catch (Exception)
        {
            TryDeleteFile(payloadFile);
            return "下载安装包失败，请检查网络后重试";
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
            // Write UTF-8 (with chcp 65001) so Chinese paths like 键盘小黑猫.exe work reliably on Windows cmd.exe.
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
        // Silent NSIS install updates the app, shortcuts and registry while keeping preferences.
        sb.AppendLine("\"" + setupExe + "\" /S");
        sb.AppendLine("ping -n 2 127.0.0.1 >nul");
        sb.AppendLine("if exist \"" + installedExe + "\" start \"\" \"" + installedExe + "\"");
        // Clean up downloaded installer and entire temporary staging directory.
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
        // Remove downloaded binary and entire temporary staging folder.
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
