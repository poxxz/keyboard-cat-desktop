using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

internal static class SkinManager
{
    private const int MaxImportedDimension = 1024;
    private const int MaxZipEntries = 48;
    private const long MaxZipEntryBytes = 10L * 1024 * 1024;
    private const long MaxZipTotalBytes = 32L * 1024 * 1024;

    public static string SkinsRootDir
    {
        get
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KeycatDesktop", "skins");
        }
    }

    public static void EnsureRootDir()
    {
        try { Directory.CreateDirectory(SkinsRootDir); }
        catch (Exception) { }
    }

    public static List<CustomSkin> LoadAll()
    {
        List<CustomSkin> list = new List<CustomSkin>();
        EnsureRootDir();
        if (!Directory.Exists(SkinsRootDir)) return list;

        string[] dirs;
        try { dirs = Directory.GetDirectories(SkinsRootDir); }
        catch (Exception) { return list; }

        Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < dirs.Length; ++i)
        {
            CustomSkin skin = LoadFromDirectory(dirs[i]);
            if (skin != null) list.Add(skin);
        }
        return list;
    }

    public static CustomSkin LoadFromDirectory(string dirPath)
    {
        if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath)) return null;
        string manifestPath = Path.Combine(dirPath, "skin.xml");
        if (!File.Exists(manifestPath)) return null;
        try
        {
            XElement root = XElement.Load(manifestPath);
            string folderName = Path.GetFileName(dirPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return CustomSkin.FromXml(root, dirPath, folderName);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static CustomSkin GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        string safeId = CustomSkin.SanitizeId(id, "");
        if (string.IsNullOrEmpty(safeId)) return null;
        string dir = Path.Combine(SkinsRootDir, safeId);
        return LoadFromDirectory(dir);
    }

    public static string GenerateUniqueId(string preferredPrefix)
    {
        EnsureRootDir();
        string prefix = CustomSkin.SanitizeId(preferredPrefix, "skin");
        for (int i = 0; i < 64; ++i)
        {
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            string candidate = prefix + "-" + suffix;
            if (!Directory.Exists(Path.Combine(SkinsRootDir, candidate))) return candidate;
        }
        return "skin-" + Guid.NewGuid().ToString("N").Substring(0, 10);
    }

    public static CustomSkin CreateNew(string name, CatStyle baseCat)
    {
        EnsureRootDir();
        string id = GenerateUniqueId("custom");
        string dir = Path.Combine(SkinsRootDir, id);
        Directory.CreateDirectory(dir);
        CustomSkin skin = new CustomSkin
        {
            Id = id,
            Name = string.IsNullOrEmpty(name) ? "新建贴图皮肤" : name.Trim(),
            DirectoryPath = dir,
            BaseCat = baseCat
        };
        Save(skin);
        return skin;
    }

    public static CustomSkin Duplicate(CustomSkin source)
    {
        if (source == null) return null;
        EnsureRootDir();
        string id = GenerateUniqueId("copy");
        string dir = Path.Combine(SkinsRootDir, id);
        Directory.CreateDirectory(dir);

        if (!string.IsNullOrEmpty(source.DirectoryPath) && Directory.Exists(source.DirectoryPath))
        {
            string[] files = Directory.GetFiles(source.DirectoryPath, "*.png");
            for (int i = 0; i < files.Length; ++i)
            {
                string dest = Path.Combine(dir, Path.GetFileName(files[i]));
                try { File.Copy(files[i], dest, true); } catch (Exception) { }
            }
        }

        XElement xml = source.ToXml();
        CustomSkin clone = CustomSkin.FromXml(xml, dir, id);
        clone.Id = id;
        clone.Name = (source.Name ?? "自定义皮肤") + " 副本";
        Save(clone);
        return clone;
    }

    public static bool Save(CustomSkin skin)
    {
        if (skin == null || string.IsNullOrEmpty(skin.DirectoryPath)) return false;
        try
        {
            Directory.CreateDirectory(skin.DirectoryPath);
            string manifestPath = Path.Combine(skin.DirectoryPath, "skin.xml");
            string tmpPath = manifestPath + ".tmp";
            skin.ToXml().Save(tmpPath);
            if (File.Exists(manifestPath)) File.Replace(tmpPath, manifestPath, null);
            else File.Move(tmpPath, manifestPath);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool Delete(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        string safeId = CustomSkin.SanitizeId(id, "");
        if (string.IsNullOrEmpty(safeId)) return false;
        string dir = Path.Combine(SkinsRootDir, safeId);
        if (!Directory.Exists(dir)) return false;
        try
        {
            Directory.Delete(dir, true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // Decodes PNG/JPG/BMP/GIF/ICO safely into a clean lossless PNG inside the skin folder.
    // Returns null on success, or a user-facing Chinese error message.
    public static string ImportImageToSlot(CustomSkin skin, SkinSlot slot, string sourceFilePath)
    {
        if (skin == null || string.IsNullOrEmpty(skin.DirectoryPath)) return "请先选择或新建一个皮肤";
        if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath)) return "找不到指定的图片文件";

        BitmapSource decoded;
        try
        {
            byte[] raw = File.ReadAllBytes(sourceFilePath);
            if (raw == null || raw.Length < 8) return "图片文件为空或已损坏";
            using (MemoryStream ms = new MemoryStream(raw))
            {
                BitmapDecoder decoder = BitmapDecoder.Create(
                    ms, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
                if (decoder.Frames == null || decoder.Frames.Count == 0)
                    return "无法解析该图片文件";
                decoded = decoder.Frames[0];
            }
        }
        catch (Exception)
        {
            return "不支持的图片格式或文件已损坏（请使用 PNG / JPG / BMP）";
        }

        if (decoded.PixelWidth <= 0 || decoded.PixelHeight <= 0)
            return "图片尺寸无效";

        BitmapSource normalized = NormalizeBitmapForSkin(decoded);
        string targetFileName = CustomSkin.SlotDefaultFileName(slot);
        string targetPath = Path.Combine(skin.DirectoryPath, targetFileName);
        try
        {
            Directory.CreateDirectory(skin.DirectoryPath);
            SaveBitmapAsPng(normalized, targetPath);
        }
        catch (Exception)
        {
            return "保存贴图文件失败，请检查磁盘空间或权限";
        }

        SkinLayerConfig layer = skin.GetLayer(slot);
        bool wasEmpty = !layer.HasFile;
        layer.FileName = targetFileName;

        // If user imports HeadBlink/HeadAngry after customizing Head alignment, copy Head's transform so expressions align cleanly.
        if (wasEmpty && (slot == SkinSlot.HeadBlink || slot == SkinSlot.HeadAngry))
        {
            SkinLayerConfig headLayer = skin.GetLayer(SkinSlot.Head);
            if (headLayer.HasFile && Math.Abs(layer.OffsetX) < 0.01 && Math.Abs(layer.OffsetY) < 0.01 &&
                Math.Abs(layer.Scale - 1.0) < 0.01 && Math.Abs(layer.Rotation) < 0.01)
            {
                layer.OffsetX = headLayer.OffsetX;
                layer.OffsetY = headLayer.OffsetY;
                layer.Scale = headLayer.Scale;
                layer.Rotation = headLayer.Rotation;
                layer.FlipX = headLayer.FlipX;
                layer.Opacity = headLayer.Opacity;
            }
        }

        skin.ReloadImages();
        Save(skin);
        return null;
    }

    public static void RemoveSlotImage(CustomSkin skin, SkinSlot slot)
    {
        if (skin == null) return;
        SkinLayerConfig layer = skin.GetLayer(slot);
        string oldFile = layer.FileName;
        layer.FileName = "";
        skin.ReloadImages();
        Save(skin);

        if (!string.IsNullOrEmpty(oldFile) && !string.IsNullOrEmpty(skin.DirectoryPath))
        {
            bool stillUsed = false;
            for (int i = 0; i < CustomSkin.AllSlots.Length; ++i)
            {
                SkinLayerConfig other = skin.GetLayer(CustomSkin.AllSlots[i]);
                if (string.Equals(other.FileName, oldFile, StringComparison.OrdinalIgnoreCase))
                {
                    stillUsed = true;
                    break;
                }
            }
            if (!stillUsed)
            {
                string fullPath = Path.Combine(skin.DirectoryPath, oldFile);
                try { if (File.Exists(fullPath)) File.Delete(fullPath); } catch (Exception) { }
            }
        }
    }

    // Exports a self-contained .kcat (ZIP) archive with skin.xml, PNG textures, and a 256x256 preview.png.
    public static string ExportSkinPack(CustomSkin skin, string targetZipPath)
    {
        if (skin == null || string.IsNullOrEmpty(skin.DirectoryPath)) return "请先选择要导出的皮肤";
        if (string.IsNullOrEmpty(targetZipPath)) return "导出路径无效";

        Save(skin);
        string tempZip = targetZipPath + ".tmp";
        try
        {
            if (File.Exists(tempZip)) File.Delete(tempZip);
            using (FileStream fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                // 1. skin.xml
                ZipArchiveEntry xmlEntry = archive.CreateEntry("skin.xml", CompressionLevel.Optimal);
                using (Stream s = xmlEntry.Open())
                {
                    skin.ToXml().Save(s);
                }

                // 2. All referenced PNG textures
                HashSet<string> added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < CustomSkin.AllSlots.Length; ++i)
                {
                    SkinLayerConfig layer = skin.GetLayer(CustomSkin.AllSlots[i]);
                    if (!layer.HasFile || !added.Add(layer.FileName)) continue;
                    string src = Path.Combine(skin.DirectoryPath, layer.FileName);
                    if (!File.Exists(src)) continue;
                    ZipArchiveEntry imgEntry = archive.CreateEntry(layer.FileName, CompressionLevel.Optimal);
                    using (Stream dst = imgEntry.Open())
                    using (FileStream srcStream = File.OpenRead(src))
                    {
                        srcStream.CopyTo(dst);
                    }
                }

                // 3. Rendered 256x256 portrait preview.png
                BitmapSource preview = RenderPortraitToBitmap(skin, 256, 256);
                if (preview != null)
                {
                    ZipArchiveEntry prevEntry = archive.CreateEntry("preview.png", CompressionLevel.Optimal);
                    using (Stream dst = prevEntry.Open())
                    {
                        PngBitmapEncoder enc = new PngBitmapEncoder();
                        enc.Frames.Add(BitmapFrame.Create(preview));
                        enc.Save(dst);
                    }
                }
            }

            if (File.Exists(targetZipPath)) File.Delete(targetZipPath);
            File.Move(tempZip, targetZipPath);
            return null;
        }
        catch (Exception)
        {
            try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch (Exception) { }
            return "导出皮肤包失败，请检查目标路径是否可写";
        }
    }

    // Safely imports a .kcat or .zip skin pack (or even a plain zip of named PNGs like head.png/body.png).
    public static string ImportSkinPack(string zipPath, out CustomSkin importedSkin)
    {
        importedSkin = null;
        if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath)) return "找不到指定的皮肤包文件";

        EnsureRootDir();
        string newId = GenerateUniqueId("pack");
        string destDir = Path.Combine(SkinsRootDir, newId);
        Directory.CreateDirectory(destDir);

        try
        {
            string fullDestRoot = Path.GetFullPath(destDir);
            if (!fullDestRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                fullDestRoot += Path.DirectorySeparatorChar;

            long totalBytes = 0;
            int count = 0;
            bool hasManifest = false;
            Dictionary<string, string> extractedPngs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                if (archive.Entries.Count == 0)
                {
                    SafeDeleteDir(destDir);
                    return "皮肤包内容为空";
                }
                if (archive.Entries.Count > MaxZipEntries)
                {
                    SafeDeleteDir(destDir);
                    return "皮肤包内文件数量超出安全限制";
                }

                for (int i = 0; i < archive.Entries.Count; ++i)
                {
                    ZipArchiveEntry entry = archive.Entries[i];
                    if (string.IsNullOrEmpty(entry.Name)) continue; // directory entry
                    string fileName = Path.GetFileName(entry.Name);
                    string ext = Path.GetExtension(fileName);
                    bool isXml = string.Equals(fileName, "skin.xml", StringComparison.OrdinalIgnoreCase);
                    bool isPng = string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase);
                    if (!isXml && !isPng) continue;
                    if (string.Equals(fileName, "preview.png", StringComparison.OrdinalIgnoreCase)) continue;

                    if (entry.Length > MaxZipEntryBytes)
                    {
                        SafeDeleteDir(destDir);
                        return "皮肤包中有单个文件超过 10MB 限制";
                    }

                    string outPath = Path.GetFullPath(Path.Combine(destDir, fileName));
                    if (!outPath.StartsWith(fullDestRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        SafeDeleteDir(destDir);
                        return "检测到不安全的压缩包路径，已中止导入";
                    }

                    using (Stream src = entry.Open())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        byte[] buf = new byte[16384];
                        int read;
                        long entryBytes = 0;
                        while ((read = src.Read(buf, 0, buf.Length)) > 0)
                        {
                            entryBytes += read;
                            totalBytes += read;
                            if (entryBytes > MaxZipEntryBytes || totalBytes > MaxZipTotalBytes)
                            {
                                SafeDeleteDir(destDir);
                                return "皮肤包解压后体积超出安全限制";
                            }
                            ms.Write(buf, 0, read);
                        }
                        byte[] data = ms.ToArray();
                        if (isPng)
                        {
                            // Validate real decodable PNG before writing
                            using (MemoryStream check = new MemoryStream(data))
                            {
                                BitmapDecoder dec = BitmapDecoder.Create(check, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
                                if (dec.Frames.Count == 0) continue;
                            }
                            File.WriteAllBytes(outPath, data);
                            extractedPngs[fileName] = outPath;
                        }
                        else if (isXml)
                        {
                            File.WriteAllBytes(outPath, data);
                            hasManifest = true;
                        }
                        count++;
                    }
                }
            }

            if (count == 0)
            {
                SafeDeleteDir(destDir);
                return "未在皮肤包中找到有效的 skin.xml 或 PNG 贴图";
            }

            CustomSkin skin;
            if (hasManifest)
            {
                skin = LoadFromDirectory(destDir);
                if (skin == null)
                {
                    SafeDeleteDir(destDir);
                    return "skin.xml 格式无效，无法解析";
                }
                skin.Id = newId;
            }
            else
            {
                // Auto-assemble a skin from bare PNGs inside the zip!
                skin = new CustomSkin
                {
                    Id = newId,
                    Name = Path.GetFileNameWithoutExtension(zipPath),
                    DirectoryPath = destDir,
                    BaseCat = CatStyle.Black
                };
                AutoMapExtractedPngs(skin, extractedPngs);
            }

            skin.ReloadImages();
            Save(skin);
            importedSkin = skin;
            return null;
        }
        catch (Exception)
        {
            SafeDeleteDir(destDir);
            return "读取皮肤包失败，请确认文件为有效的 .kcat 或 .zip 格式";
        }
    }

    private static void AutoMapExtractedPngs(CustomSkin skin, Dictionary<string, string> files)
    {
        foreach (string name in files.Keys)
        {
            string lower = name.ToLowerInvariant();
            if (lower == "head.png" || lower == "face.png") skin.GetLayer(SkinSlot.Head).FileName = name;
            else if (lower == "head_blink.png" || lower == "blink.png" || lower == "sleep.png") skin.GetLayer(SkinSlot.HeadBlink).FileName = name;
            else if (lower == "head_angry.png" || lower == "angry.png") skin.GetLayer(SkinSlot.HeadAngry).FileName = name;
            else if (lower == "body.png" || lower == "torso.png") skin.GetLayer(SkinSlot.Body).FileName = name;
            else if (lower == "tail.png") skin.GetLayer(SkinSlot.Tail).FileName = name;
            else if (lower == "paw_left.png" || lower == "paw.png" || lower == "arm_left.png") skin.GetLayer(SkinSlot.PawLeft).FileName = name;
            else if (lower == "paw_right.png" || lower == "arm_right.png") skin.GetLayer(SkinSlot.PawRight).FileName = name;
            else if (lower == "accessory.png" || lower == "hat.png" || lower == "glasses.png") skin.GetLayer(SkinSlot.Accessory).FileName = name;
        }
    }

    // Exports crisp 4x transparent PNG painting templates with faint base-cat silhouette & pivot guides.
    public static string ExportTemplatePack(CatStyle baseCat, string targetFolder)
    {
        if (string.IsNullOrEmpty(targetFolder)) return "未选择导出目录";
        try
        {
            Directory.CreateDirectory(targetFolder);
            SkinSlot[] templateSlots = new SkinSlot[]
            {
                SkinSlot.Head, SkinSlot.Body, SkinSlot.Tail, SkinSlot.PawLeft, SkinSlot.Accessory
            };
            string[] names = new string[]
            {
                "head_template.png", "body_template.png", "tail_template.png",
                "paw_template.png", "accessory_template.png"
            };
            for (int i = 0; i < templateSlots.Length; ++i)
            {
                BitmapSource bmp = RenderSlotTemplateBitmap(baseCat, templateSlots[i], true);
                SaveBitmapAsPng(bmp, Path.Combine(targetFolder, names[i]));
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("键盘小黑猫 · PNG 贴图皮肤制作说明");
            sb.AppendLine("========================================");
            sb.AppendLine();
            sb.AppendLine("1. 模板说明（均为透明背景 4x 高清 PNG）：");
            sb.AppendLine("   - head_template.png       : 头部常态 (448x448)，闭眼 head_blink.png 与生气 head_angry.png 同尺寸");
            sb.AppendLine("   - body_template.png       : 身体躯干 (480x560)，支持自动前倾/蹲伏/趴睡压扁形变");
            sb.AppendLine("   - tail_template.png       : 尾巴 (384x288)，十字准星在左下方 (10%, 72%) 为摆动支点");
            sb.AppendLine("   - paw_template.png        : 爪尖贴图 (128x128)，搭配动态弯曲手臂使用；若做整臂贴图建议竖向绘制");
            sb.AppendLine("   - accessory_template.png  : 头饰/挂件 (384x384)，默认锚点在耳朵上方，也可拖到眼睛做墨镜");
            sb.AppendLine();
            sb.AppendLine("2. 绘制技巧：");
            sb.AppendLine("   - 在 PS / Procreate / SAI / Aseprite 中将模板图层设为底稿，新建图层绘制，导出前隐藏模板层。");
            sb.AppendLine("   - 不需要画全套！哪怕只导入一张 accessory.png（帽子/蝴蝶结）或一张 head.png，其余部位会自动沿用底模猫咪。");
            sb.AppendLine("   - 画像素画皮肤时，可在皮肤工坊「动画与风格」中开启“像素画锐利模式 (NearestNeighbor)”。");
            sb.AppendLine("   - 导入后可在皮肤工坊右侧微调偏移/缩放/旋转/镜像，也可在预览区直接拖拽移动、滚轮缩放。");
            sb.AppendLine();
            sb.AppendLine("3. 分享皮肤：");
            sb.AppendLine("   - 点击「导出 .kcat」即可打包为单文件皮肤包发给朋友一键导入。");
            File.WriteAllText(Path.Combine(targetFolder, "README_皮肤制作说明.txt"), sb.ToString(), new UTF8Encoding(false));
            return null;
        }
        catch (Exception)
        {
            return "导出模板失败，请检查目标目录权限";
        }
    }

    // Rasterizes the skin's BaseCat into real editable PNG files inside the skin folder.
    public static string ExtractBaseCatToPngs(CustomSkin skin, SkinSlot? singleSlot)
    {
        if (skin == null || string.IsNullOrEmpty(skin.DirectoryPath)) return "请先选择皮肤";
        try
        {
            Directory.CreateDirectory(skin.DirectoryPath);
            SkinSlot[] targets = singleSlot.HasValue
                ? new SkinSlot[] { singleSlot.Value }
                : new SkinSlot[] { SkinSlot.Head, SkinSlot.HeadBlink, SkinSlot.HeadAngry, SkinSlot.Body, SkinSlot.Tail, SkinSlot.PawLeft };

            for (int i = 0; i < targets.Length; ++i)
            {
                SkinSlot slot = targets[i];
                if (slot == SkinSlot.Accessory) continue;
                BitmapSource bmp = RenderSlotTemplateBitmap(skin.BaseCat, slot, false);
                string fileName = CustomSkin.SlotDefaultFileName(slot);
                SaveBitmapAsPng(bmp, Path.Combine(skin.DirectoryPath, fileName));
                SkinLayerConfig layer = skin.GetLayer(slot);
                layer.FileName = fileName;
                layer.ResetTransform(slot);
            }
            if (!singleSlot.HasValue)
            {
                skin.EyeMode = CustomEyeMode.PngOnly;
            }
            skin.ReloadImages();
            Save(skin);
            return null;
        }
        catch (Exception)
        {
            return "从底模导出 PNG 失败";
        }
    }

    // Generates a crisp starter transparent PNG accessory directly into accessory.png so users can test PNG layering in 1 click.
    public static string GenerateStarterAccessoryPng(CustomSkin skin, string presetKey)
    {
        if (skin == null || string.IsNullOrEmpty(skin.DirectoryPath)) return "请先选择皮肤";
        try
        {
            Directory.CreateDirectory(skin.DirectoryPath);
            const int size = 384;
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.PushTransform(new ScaleTransform(size / 96.0, size / 96.0));
                DrawStarterAccessoryVector(dc, presetKey);
                dc.Pop();
            }
            RenderTargetBitmap rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();

            string fileName = CustomSkin.SlotDefaultFileName(SkinSlot.Accessory);
            SaveBitmapAsPng(rtb, Path.Combine(skin.DirectoryPath, fileName));

            SkinLayerConfig layer = skin.GetLayer(SkinSlot.Accessory);
            layer.FileName = fileName;
            layer.ResetTransform(SkinSlot.Accessory);
            if (presetKey == "sunglasses")
            {
                layer.OffsetY = 58;
                layer.Scale = 0.88;
            }
            else if (presetKey == "headphones")
            {
                layer.OffsetY = 38;
                layer.Scale = 1.24;
            }
            else if (presetKey == "bow")
            {
                layer.OffsetX = -24;
                layer.OffsetY = 18;
                layer.Scale = 0.56;
                layer.Rotation = -16;
            }
            else if (presetKey == "crown")
            {
                layer.OffsetY = 20;
                layer.Scale = 0.62;
            }
            else if (presetKey == "wizard")
            {
                layer.OffsetY = 18;
                layer.Scale = 1.05;
            }
            skin.ReloadImages();
            Save(skin);
            return null;
        }
        catch (Exception)
        {
            return "生成示例贴图失败";
        }
    }

    private static void DrawStarterAccessoryVector(DrawingContext dc, string presetKey)
    {
        if (presetKey == "bow")
        {
            // Sakura pink ribbon bow centered around (48, 56)
            LinearGradientBrush pink = MakeGrad(0xFF9EB8, 0xE85C87);
            LinearGradientBrush darkPink = MakeGrad(0xE85C87, 0xC63A67);
            Pen outline = MakePen(0x7C2240, 2.2);
            dc.DrawGeometry(pink, outline, Geometry.Parse("M 46,56 C 32,38 14,34 12,52 C 10,68 28,74 46,60 Z"));
            dc.DrawGeometry(pink, outline, Geometry.Parse("M 50,56 C 64,38 82,34 84,52 C 86,68 68,74 50,60 Z"));
            dc.DrawGeometry(darkPink, outline, Geometry.Parse("M 43,62 L 32,84 L 44,78 L 48,64 Z"));
            dc.DrawGeometry(darkPink, outline, Geometry.Parse("M 53,62 L 64,84 L 52,78 L 48,64 Z"));
            dc.DrawRoundedRectangle(MakeGrad(0xFFBBD0, 0xE25682), outline, new Rect(40, 48, 16, 16), 6, 6);
            dc.DrawEllipse(MakeBrush(0xFFE3EC), null, new Point(26, 48), 5.5, 2.8);
            dc.DrawEllipse(MakeBrush(0xFFE3EC), null, new Point(70, 48), 5.5, 2.8);
        }
        else if (presetKey == "crown")
        {
            LinearGradientBrush gold = MakeGrad(0xFFE57F, 0xE09612);
            Pen outline = MakePen(0x7A4B08, 2.2);
            dc.DrawGeometry(gold, outline, Geometry.Parse(
                "M 18,74 L 14,34 L 34,50 L 48,22 L 62,50 L 82,34 L 78,74 Z"));
            dc.DrawRoundedRectangle(MakeGrad(0xFFCC4D, 0xC97D0C), outline, new Rect(16, 66, 64, 12), 4, 4);
            dc.DrawEllipse(MakeBrush(0xFF4D6D), outline, new Point(48, 56), 5.5, 6.5);
            dc.DrawEllipse(MakeBrush(0x52C4FF), outline, new Point(29, 59), 4.0, 4.5);
            dc.DrawEllipse(MakeBrush(0x52C4FF), outline, new Point(67, 59), 4.0, 4.5);
            dc.DrawEllipse(MakeBrush(0xFFF6C2), outline, new Point(48, 20), 4.2, 4.2);
            dc.DrawEllipse(MakeBrush(0xFFF6C2), outline, new Point(14, 32), 3.6, 3.6);
            dc.DrawEllipse(MakeBrush(0xFFF6C2), outline, new Point(82, 32), 3.6, 3.6);
        }
        else if (presetKey == "sunglasses")
        {
            LinearGradientBrush frame = MakeGrad(0x3B3532, 0x181514);
            LinearGradientBrush lens = MakeGrad(0x2B2F3A, 0x111318);
            Pen edge = MakePen(0x100E0E, 2.0);
            Pen glint = MakePen(0xFFFFFF, 2.4);
            dc.DrawRoundedRectangle(frame, edge, new Rect(8, 34, 36, 26), 8, 8);
            dc.DrawRoundedRectangle(frame, edge, new Rect(52, 34, 36, 26), 8, 8);
            dc.DrawRoundedRectangle(lens, null, new Rect(11, 37, 30, 20), 6, 6);
            dc.DrawRoundedRectangle(lens, null, new Rect(55, 37, 30, 20), 6, 6);
            dc.DrawRoundedRectangle(frame, edge, new Rect(41, 40, 14, 6), 2, 2);
            dc.PushOpacity(0.55);
            dc.DrawLine(glint, new Point(16, 42), new Point(23, 52));
            dc.DrawLine(glint, new Point(23, 41), new Point(27, 47));
            dc.DrawLine(glint, new Point(60, 42), new Point(67, 52));
            dc.DrawLine(glint, new Point(67, 41), new Point(71, 47));
            dc.Pop();
        }
        else if (presetKey == "headphones")
        {
            Pen bandOuter = MakePen(0x6D5236, 8.5);
            Pen bandInner = MakePen(0xFFF3D1, 5.2);
            Pen outline = MakePen(0x6D5236, 2.0);
            Geometry arc = Geometry.Parse("M 13,56 C 11,16 85,16 83,56");
            dc.DrawGeometry(null, bandOuter, arc);
            dc.DrawGeometry(null, bandInner, arc);
            LinearGradientBrush cup = MakeGrad(0xFFCF5C, 0xE69516);
            Brush pad = MakeBrush(0x5B4738);
            dc.DrawRoundedRectangle(pad, outline, new Rect(12, 46, 10, 28), 5, 5);
            dc.DrawRoundedRectangle(cup, outline, new Rect(4, 48, 12, 24), 6, 6);
            dc.DrawRoundedRectangle(pad, outline, new Rect(74, 46, 10, 28), 5, 5);
            dc.DrawRoundedRectangle(cup, outline, new Rect(80, 48, 12, 24), 6, 6);
        }
        else
        {
            // Wizard hat
            LinearGradientBrush hat = MakeGrad(0x463B8A, 0x241D52);
            LinearGradientBrush band = MakeGrad(0xFFD55A, 0xDF9216);
            Pen outline = MakePen(0x161133, 2.2);
            dc.DrawEllipse(hat, outline, new Point(48, 72), 42, 11);
            dc.DrawGeometry(hat, outline, Geometry.Parse("M 22,70 Q 34,30 56,10 Q 52,32 74,70 Q 48,78 22,70 Z"));
            dc.DrawGeometry(band, outline, Geometry.Parse("M 24,62 Q 48,70 72,62 L 74,70 Q 48,78 22,70 Z"));
            // Tiny golden star
            dc.DrawGeometry(MakeBrush(0xFFE97D), null, Geometry.Parse(
                "M 46,35 L 48.5,41 L 55,41.5 L 50,45.5 L 51.5,52 L 46,48.5 L 40.5,52 L 42,45.5 L 37,41.5 L 43.5,41 Z"));
            dc.DrawEllipse(MakeBrush(0xFFF3B0), null, new Point(36, 52), 2.0, 2.0);
            dc.DrawEllipse(MakeBrush(0xFFF3B0), null, new Point(60, 48), 1.6, 1.6);
        }
    }

    private static SolidColorBrush MakeBrush(uint rgb)
    {
        SolidColorBrush b = new SolidColorBrush(Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
        b.Freeze();
        return b;
    }

    private static LinearGradientBrush MakeGrad(uint top, uint bot)
    {
        LinearGradientBrush b = new LinearGradientBrush(
            Color.FromRgb((byte)(top >> 16), (byte)(top >> 8), (byte)top),
            Color.FromRgb((byte)(bot >> 16), (byte)(bot >> 8), (byte)bot),
            new Point(0.2, 0), new Point(0.8, 1));
        b.Freeze();
        return b;
    }

    private static Pen MakePen(uint rgb, double width)
    {
        Pen p = new Pen(MakeBrush(rgb), width);
        p.StartLineCap = PenLineCap.Round;
        p.EndLineCap = PenLineCap.Round;
        p.LineJoin = PenLineJoin.Round;
        p.Freeze();
        return p;
    }

    public static BitmapSource RenderSlotTemplateBitmap(CatStyle baseCat, SkinSlot slot, bool asGuide)
    {
        Size logical = CatArt.GetSlotExportLogicalSize(slot);
        const int scale = 4;
        int pw = Math.Max(32, (int)Math.Round(logical.Width * scale));
        int ph = Math.Max(32, (int)Math.Round(logical.Height * scale));

        DrawingVisual dv = new DrawingVisual();
        using (DrawingContext dc = dv.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(scale, scale));
            if (asGuide)
            {
                dc.PushOpacity(0.32);
                CatArt.DrawBaseSlotForPng(dc, baseCat, slot);
                dc.Pop();

                Pen borderPen = new Pen(new SolidColorBrush(Color.FromArgb(140, 224, 140, 24)), 0.8);
                borderPen.DashStyle = new DashStyle(new double[] { 3, 2 }, 0);
                borderPen.Freeze();
                dc.DrawRectangle(null, borderPen, new Rect(1, 1, logical.Width - 2, logical.Height - 2));

                SkinLayerConfig defLayer = SkinLayerConfig.CreateDefault(slot);
                double px = logical.Width * defLayer.PivotX;
                double py = logical.Height * defLayer.PivotY;
                Pen cross = new Pen(new SolidColorBrush(Color.FromArgb(190, 224, 92, 36)), 0.9);
                cross.Freeze();
                dc.DrawLine(cross, new Point(px - 6, py), new Point(px + 6, py));
                dc.DrawLine(cross, new Point(px, py - 6), new Point(px, py + 6));
            }
            else
            {
                CatArt.DrawBaseSlotForPng(dc, baseCat, slot);
            }
            dc.Pop();
        }

        RenderTargetBitmap rtb = new RenderTargetBitmap(pw, ph, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);
        rtb.Freeze();
        return rtb;
    }

    public static BitmapSource RenderPortraitToBitmap(CustomSkin skin, int width, int height)
    {
        try
        {
            DrawingGroup art = new DrawingGroup();
            using (DrawingContext dc = art.Open())
            {
                CustomSkinRenderer.DrawPortrait(dc, skin);
            }
            art.Freeze();
            Rect bounds = art.Bounds;
            if (bounds.IsEmpty || bounds.Width < 1 || bounds.Height < 1) return null;

            double pad = Math.Min(width, height) * 0.08;
            double scale = Math.Min((width - pad * 2) / bounds.Width, (height - pad * 2) / bounds.Height);
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.PushTransform(new MatrixTransform(
                    scale, 0, 0, scale,
                    (width - scale * bounds.Width) * 0.5 - scale * bounds.X,
                    (height - scale * bounds.Height) * 0.5 - scale * bounds.Y));
                dc.DrawDrawing(art);
                dc.Pop();
            }
            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static BitmapSource NormalizeBitmapForSkin(BitmapSource source)
    {
        int w = source.PixelWidth;
        int h = source.PixelHeight;
        BitmapSource current = source;

        if (w > MaxImportedDimension || h > MaxImportedDimension)
        {
            double factor = Math.Min((double)MaxImportedDimension / w, (double)MaxImportedDimension / h);
            int nw = Math.Max(1, (int)Math.Round(w * factor));
            int nh = Math.Max(1, (int)Math.Round(h * factor));
            DrawingVisual dv = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(dv, BitmapScalingMode.HighQuality);
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.DrawImage(source, new Rect(0, 0, nw, nh));
            }
            RenderTargetBitmap resized = new RenderTargetBitmap(nw, nh, 96, 96, PixelFormats.Pbgra32);
            resized.Render(dv);
            resized.Freeze();
            current = resized;
        }
        else if (current.Format != PixelFormats.Pbgra32 && current.Format != PixelFormats.Bgra32)
        {
            FormatConvertedBitmap converted = new FormatConvertedBitmap(current, PixelFormats.Pbgra32, null, 0);
            converted.Freeze();
            current = converted;
        }
        return current;
    }

    private static void SaveBitmapAsPng(BitmapSource bmp, string filePath)
    {
        string tmp = filePath + ".tmp";
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        using (FileStream fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            encoder.Save(fs);
        }
        if (File.Exists(filePath)) File.Replace(tmp, filePath, null);
        else File.Move(tmp, filePath);
    }

    private static void SafeDeleteDir(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch (Exception) { }
    }
}
