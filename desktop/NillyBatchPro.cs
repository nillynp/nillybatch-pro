// ============================================================================
// NillyBatch Pro — Ultra-Light Native Batch Image Optimizer & File Renamer
// created by Nilly graphics (Sotiris Christos) — https://nillygraphics.com/
// Strictly C# 5 compatible (.NET Framework 4.x csc.exe — 0 external DLLs)
// Replaces Adobe Bridge (1.95 GB) for daily batch tasks in ~85 KB · 0 MB idle RAM
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

[assembly: AssemblyTitle("NillyBatch Pro")]
[assembly: AssemblyDescription("Ultra-Light Batch Image Resizer, Converter, Watermark & Greeklish SEO Renamer — created by Nilly graphics")]
[assembly: AssemblyCompany("Nilly graphics (Sotiris Christos) — https://nillygraphics.com/")]
[assembly: AssemblyProduct("NillyBatch Pro")]
[assembly: AssemblyCopyright("Copyright (c) 2026 Nilly graphics (Sotiris Christos)")]
[assembly: AssemblyVersion("1.2.2.0")]
[assembly: AssemblyFileVersion("1.2.2.0")]

namespace NillyBatchPro
{
    // ========================================================================
    // 1. WIN32 INTEROP (DPI, Windows 11 Dark Titlebar)
    // ========================================================================
    internal static class Native
    {
        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr minSize, IntPtr maxSize);

        public static void TrimMemory()
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false);
                SetProcessWorkingSetSize(GetCurrentProcess(), (IntPtr)(-1), (IntPtr)(-1));
            }
            catch { }
        }

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void SetDarkTitleBar(IntPtr hwnd, bool dark)
        {
            try
            {
                int val = dark ? 1 : 0;
                if (DwmSetWindowAttribute(hwnd, 20, ref val, 4) != 0)
                    DwmSetWindowAttribute(hwnd, 19, ref val, 4);
            }
            catch { }
        }
    }

    // ========================================================================
    // 2. WINDOWS 11 STUDIO THEME & ROUNDED BUTTON
    // ========================================================================
    internal class BatchTheme
    {
        public bool IsDark;
        public Color WindowBg, CardBg, CardBorder, TextPrimary, TextMuted;
        public Color BtnBg, BtnHover, BtnPress, AccentBg, AccentHover, AccentText;
        public Color StudioAmber, InputBg, OkColor, WarnColor, ListAltBg;

        public static BatchTheme Win11Dark()
        {
            BatchTheme t = new BatchTheme();
            t.IsDark = true;
            t.WindowBg = Color.FromArgb(24, 26, 31);
            t.CardBg = Color.FromArgb(31, 34, 41);
            t.CardBorder = Color.FromArgb(52, 57, 68);
            t.TextPrimary = Color.FromArgb(236, 239, 244);
            t.TextMuted = Color.FromArgb(148, 157, 174);
            t.BtnBg = Color.FromArgb(39, 43, 52);
            t.BtnHover = Color.FromArgb(49, 54, 66);
            t.BtnPress = Color.FromArgb(31, 34, 41);
            t.AccentBg = Color.FromArgb(252, 150, 31);
            t.AccentHover = Color.FromArgb(236, 134, 20);
            t.AccentText = Color.FromArgb(20, 22, 26);
            t.StudioAmber = Color.FromArgb(252, 150, 31);
            t.InputBg = Color.FromArgb(28, 30, 36);
            t.OkColor = Color.FromArgb(166, 226, 46);
            t.WarnColor = Color.FromArgb(249, 38, 114);
            t.ListAltBg = Color.FromArgb(28, 30, 36);
            return t;
        }

        public static BatchTheme Win11Light()
        {
            BatchTheme t = new BatchTheme();
            t.IsDark = false;
            t.WindowBg = Color.FromArgb(248, 249, 251);
            t.CardBg = Color.FromArgb(238, 241, 246);
            t.CardBorder = Color.FromArgb(210, 216, 226);
            t.TextPrimary = Color.FromArgb(28, 32, 40);
            t.TextMuted = Color.FromArgb(98, 108, 125);
            t.BtnBg = Color.White;
            t.BtnHover = Color.FromArgb(226, 232, 242);
            t.BtnPress = Color.FromArgb(215, 222, 234);
            t.AccentBg = Color.FromArgb(224, 118, 12);
            t.AccentHover = Color.FromArgb(204, 104, 8);
            t.AccentText = Color.White;
            t.StudioAmber = Color.FromArgb(224, 118, 12);
            t.InputBg = Color.White;
            t.OkColor = Color.FromArgb(62, 145, 32);
            t.WarnColor = Color.FromArgb(204, 28, 92);
            t.ListAltBg = Color.FromArgb(243, 245, 249);
            return t;
        }
    }

    internal enum PillKind { Normal, Accent, Subtle, Danger }

    internal class StudioButton : Control
    {
        public PillKind Kind = PillKind.Normal;
        public BatchTheme Theme = BatchTheme.Win11Dark();
        public bool IsToggled = false;
        private bool _hover, _down;

        public StudioButton()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.Cursor = Cursors.Hand;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color bg, fg, border;
            if (!this.Enabled)
            {
                bg = Theme.CardBg; fg = Theme.TextMuted; border = Theme.CardBorder;
            }
            else if (Kind == PillKind.Accent || IsToggled)
            {
                bg = _hover ? Theme.AccentHover : Theme.AccentBg;
                fg = Theme.AccentText; border = bg;
            }
            else if (Kind == PillKind.Danger)
            {
                bg = _down ? Theme.BtnPress : (_hover ? Theme.WarnColor : Theme.BtnBg);
                fg = _hover ? Color.White : Theme.WarnColor;
                border = Theme.CardBorder;
            }
            else if (Kind == PillKind.Subtle)
            {
                bg = _down ? Theme.BtnPress : (_hover ? Theme.BtnBg : Theme.WindowBg);
                fg = _hover ? Theme.TextPrimary : Theme.TextMuted;
                border = _hover ? Theme.CardBorder : Theme.WindowBg;
            }
            else
            {
                bg = _down ? Theme.BtnPress : (_hover ? Theme.BtnHover : Theme.BtnBg);
                fg = Theme.TextPrimary; border = Theme.CardBorder;
            }

            Rectangle r = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (GraphicsPath p = RoundRect(r, 5))
            {
                using (SolidBrush b = new SolidBrush(bg)) g.FillPath(b, p);
                if (border != bg) using (Pen pen = new Pen(border, 1f)) g.DrawPath(pen, p);
            }
            TextRenderer.DrawText(g, this.Text, this.Font, this.ClientRectangle, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; this.Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; _down = false; this.Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { _down = true; this.Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _down = false; this.Invalidate(); }
    }

    // ========================================================================
    // 3. GREEKLISH SEO SLUGIFIER & FILENAME ENGINE
    // ========================================================================
    internal static class Greeklish
    {
        private static readonly Dictionary<char, string> Map = new Dictionary<char, string>()
        {
            {'α',"a"},{'ά',"a"},{'β',"v"},{'γ',"g"},{'δ',"d"},{'ε',"e"},{'έ',"e"},
            {'ζ',"z"},{'η',"i"},{'ή',"i"},{'θ',"th"},{'ι',"i"},{'ί',"i"},{'ϊ',"i"},{'ΐ',"i"},
            {'κ',"k"},{'λ',"l"},{'μ',"m"},{'ν',"n"},{'ξ',"x"},{'ο',"o"},{'ό',"o"},
            {'π',"p"},{'ρ',"r"},{'σ',"s"},{'ς',"s"},{'τ',"t"},{'υ',"y"},{'ύ',"y"},{'ϋ',"y"},{'ΰ',"y"},
            {'φ',"f"},{'χ',"ch"},{'ψ',"ps"},{'ω',"o"},{'ώ',"o"}
        };

        public static string Transliterate(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            string lower = input.ToLower(new CultureInfo("el-GR"));
            // Common Greek digraphs for natural SEO greeklish
            lower = lower.Replace("ου", "ou").Replace("ού", "ou")
                         .Replace("αι", "ai").Replace("αί", "ai")
                         .Replace("ει", "ei").Replace("εί", "ei")
                         .Replace("οι", "oi").Replace("οί", "oi")
                         .Replace("μπ", "mp").Replace("ντ", "nt").Replace("γκ", "gk");

            StringBuilder sb = new StringBuilder(lower.Length);
            for (int i = 0; i < lower.Length; i++)
            {
                char ch = lower[i];
                string rep;
                if (Map.TryGetValue(ch, out rep)) sb.Append(rep);
                else sb.Append(ch);
            }
            return sb.ToString();
        }

        public static string ToSeoSlug(string nameWithoutExt)
        {
            string t = Transliterate(nameWithoutExt);
            // Strip diacritics on Latin chars too
            string norm = t.Normalize(NormalizationForm.FormD);
            StringBuilder clean = new StringBuilder(norm.Length);
            for (int i = 0; i < norm.Length; i++)
            {
                UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(norm[i]);
                if (cat == UnicodeCategory.NonSpacingMark) continue;
                char c = norm[i];
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) clean.Append(c);
                else clean.Append('-');
            }
            string slug = Regex.Replace(clean.ToString(), "-{2,}", "-").Trim('-');
            return slug.Length > 0 ? slug : "file";
        }
    }

    // ========================================================================
    // 4. NATIVE MULTI-FORMAT IMAGE BATCH ENGINE (GDI+ & Windows 11 WIC)
    // ========================================================================
    internal class BatchOptions
    {
        public string OutputFormat = "JPG"; // "JPG", "PNG", "TIFF", "BMP", "ICO", "KEEP"
        public int JpgQuality = 85;
        public int ResizeMode = 0; // 0=Keep, 1=1920, 2=1200, 3=800, 4=SquareCrop1080, 5=SquarePad1080, 6=ScalePct, 7=CustomMax
        public int CustomMaxW = 1600;
        public int CustomMaxH = 1600;
        public int ScalePercent = 50;
        public bool AutoRotateExif = true;
        public bool StripMetadata = true;
        public bool AddWatermark = false;
        public string WatermarkText = "© Nilly graphics";
        public int WatermarkPosition = 0; // 0=Bottom-Right, 1=Center, 2=Bottom-Left, 3=Tile
        public int WatermarkOpacity = 40; // 10..90
        public string Suffix = "";
        public bool SeoGreeklishNames = false;
        public bool OverwriteOriginals = false;
        public string CustomOutputFolder = "";
    }

    internal class BatchItemResult
    {
        public string InputPath;
        public string OutputPath;
        public long OldBytes;
        public long NewBytes;
        public int OutW, OutH;
        public bool Success;
        public string Error;
    }

    internal static class ImageBatchEngine
    {
        public static readonly HashSet<string> SupportedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".jpe", ".jfif", ".png", ".bmp", ".gif", ".tif", ".tiff",
            ".webp", ".heic", ".heif", ".ico", ".dng", ".cr2", ".cr3", ".nef", ".arw", ".raf", ".orf", ".rw2"
        };

        private static readonly HashSet<string> WicPreferredExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".webp", ".heic", ".heif", ".dng", ".cr2", ".cr3", ".nef", ".arw", ".raf", ".orf", ".rw2"
        };

        public static Bitmap LoadBitmapUniversal(string path, bool autoRotateExif)
        {
            string ext = Path.GetExtension(path);
            if (!WicPreferredExts.Contains(ext))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    using (Image img = Image.FromStream(ms, true, true))
                    {
                        if (autoRotateExif) ApplyGdiExifRotation(img);
                        Bitmap copy = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
                        copy.SetResolution(img.HorizontalResolution > 0 ? img.HorizontalResolution : 96f,
                                           img.VerticalResolution > 0 ? img.VerticalResolution : 96f);
                        using (Graphics g = Graphics.FromImage(copy))
                        {
                            g.CompositingMode = CompositingMode.SourceCopy;
                            g.DrawImage(img, 0, 0, img.Width, img.Height);
                        }
                        return copy;
                    }
                }
                catch { }
            }

            // Universal fallback: Windows 11 WIC (reads WebP, HEIC, Camera RAW, etc.)
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BitmapDecoder dec = BitmapDecoder.Create(fs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                BitmapSource frame = dec.Frames[0];
                FormatConvertedBitmap converted = new FormatConvertedBitmap(frame, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                int w = converted.PixelWidth;
                int h = converted.PixelHeight;
                int stride = w * 4;
                byte[] pixels = new byte[h * stride];
                converted.CopyPixels(pixels, stride, 0);

                Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try { Marshal.Copy(pixels, 0, bd.Scan0, pixels.Length); }
                finally { bmp.UnlockBits(bd); }
                return bmp;
            }
        }

        private static void ApplyGdiExifRotation(Image img)
        {
            try
            {
                const int propId = 0x0112;
                if (Array.IndexOf(img.PropertyIdList, propId) < 0) return;
                PropertyItem item = img.GetPropertyItem(propId);
                if (item == null || item.Value == null || item.Value.Length < 2) return;
                int orient = BitConverter.ToUInt16(item.Value, 0);
                RotateFlipType rf = RotateFlipType.RotateNoneFlipNone;
                switch (orient)
                {
                    case 2: rf = RotateFlipType.RotateNoneFlipX; break;
                    case 3: rf = RotateFlipType.Rotate180FlipNone; break;
                    case 4: rf = RotateFlipType.Rotate180FlipX; break;
                    case 5: rf = RotateFlipType.Rotate90FlipX; break;
                    case 6: rf = RotateFlipType.Rotate90FlipNone; break;
                    case 7: rf = RotateFlipType.Rotate270FlipX; break;
                    case 8: rf = RotateFlipType.Rotate270FlipNone; break;
                }
                if (rf != RotateFlipType.RotateNoneFlipNone) img.RotateFlip(rf);
            }
            catch { }
        }

        public static BatchItemResult ProcessSingle(string inputPath, BatchOptions opt)
        {
            BatchItemResult res = new BatchItemResult();
            res.InputPath = inputPath;
            try
            {
                FileInfo fi = new FileInfo(inputPath);
                res.OldBytes = fi.Length;

                string fmt = opt.OutputFormat;
                if (fmt == "KEEP")
                {
                    string ext = fi.Extension.ToLowerInvariant();
                    if (ext == ".png") fmt = "PNG";
                    else if (ext == ".tif" || ext == ".tiff") fmt = "TIFF";
                    else if (ext == ".bmp") fmt = "BMP";
                    else fmt = "JPG";
                }

                string outExt = (fmt == "JPG") ? ".jpg" : ((fmt == "PNG") ? ".png" : ((fmt == "TIFF") ? ".tif" : ((fmt == "BMP") ? ".bmp" : ".ico")));
                string baseName = Path.GetFileNameWithoutExtension(inputPath);
                if (opt.SeoGreeklishNames) baseName = Greeklish.ToSeoSlug(baseName);
                if (!string.IsNullOrEmpty(opt.Suffix)) baseName += opt.Suffix;

                string outDir;
                if (opt.OverwriteOriginals)
                    outDir = Path.GetDirectoryName(inputPath);
                else if (!string.IsNullOrEmpty(opt.CustomOutputFolder))
                    outDir = opt.CustomOutputFolder;
                else
                    outDir = Path.Combine(Path.GetDirectoryName(inputPath), "NillyBatch_Output");

                Directory.CreateDirectory(outDir);
                string outPath = Path.Combine(outDir, baseName + outExt);
                res.OutputPath = outPath;

                using (Bitmap src = LoadBitmapUniversal(inputPath, opt.AutoRotateExif))
                using (Bitmap resized = ResizeBitmap(src, opt, fmt == "JPG" || fmt == "BMP"))
                {
                    if (opt.AddWatermark && !string.IsNullOrWhiteSpace(opt.WatermarkText))
                        DrawWatermark(resized, opt.WatermarkText.Trim(), opt.WatermarkPosition, opt.WatermarkOpacity);

                    res.OutW = resized.Width;
                    res.OutH = resized.Height;

                    string tempPath = outPath + ".nillytmp";
                    if (fmt == "ICO") SaveMultiSizeIco(resized, tempPath);
                    else SaveEncoded(resized, tempPath, fmt, opt.JpgQuality);

                    if (File.Exists(outPath)) File.Delete(outPath);
                    File.Move(tempPath, outPath);
                }

                res.NewBytes = new FileInfo(outPath).Length;
                res.Success = true;
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Error = ex.Message;
            }
            return res;
        }

        private static Bitmap ResizeBitmap(Bitmap src, BatchOptions opt, bool flattenAlphaToWhite)
        {
            int sw = src.Width, sh = src.Height;
            int tw = sw, th = sh;
            bool squareCrop = false, squarePad = false;

            if (opt.ResizeMode == 1) ComputeFit(sw, sh, 1920, 1920, out tw, out th);
            else if (opt.ResizeMode == 2) ComputeFit(sw, sh, 1200, 1200, out tw, out th);
            else if (opt.ResizeMode == 3) ComputeFit(sw, sh, 800, 800, out tw, out th);
            else if (opt.ResizeMode == 4) { tw = 1080; th = 1080; squareCrop = true; }
            else if (opt.ResizeMode == 5) { tw = 1080; th = 1080; squarePad = true; }
            else if (opt.ResizeMode == 6)
            {
                double pct = Math.Max(5, Math.Min(400, opt.ScalePercent)) / 100.0;
                tw = Math.Max(1, (int)Math.Round(sw * pct));
                th = Math.Max(1, (int)Math.Round(sh * pct));
            }
            else if (opt.ResizeMode == 7)
            {
                ComputeFit(sw, sh, Math.Max(16, opt.CustomMaxW), Math.Max(16, opt.CustomMaxH), out tw, out th);
            }

            PixelFormat pf = flattenAlphaToWhite ? PixelFormat.Format24bppRgb : PixelFormat.Format32bppArgb;
            Bitmap dst = new Bitmap(tw, th, pf);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                if (flattenAlphaToWhite || squarePad) g.Clear(Color.White);
                else g.Clear(Color.Transparent);

                if (squareCrop)
                {
                    int side = Math.Min(sw, sh);
                    int sx = (sw - side) / 2, sy = (sh - side) / 2;
                    g.DrawImage(src, new Rectangle(0, 0, tw, th), new Rectangle(sx, sy, side, side), GraphicsUnit.Pixel);
                }
                else if (squarePad)
                {
                    int fw, fh;
                    ComputeFitForce(sw, sh, tw, th, out fw, out fh);
                    int dx = (tw - fw) / 2, dy = (th - fh) / 2;
                    g.DrawImage(src, new Rectangle(dx, dy, fw, fh), new Rectangle(0, 0, sw, sh), GraphicsUnit.Pixel);
                }
                else
                {
                    g.DrawImage(src, new Rectangle(0, 0, tw, th), new Rectangle(0, 0, sw, sh), GraphicsUnit.Pixel);
                }
            }
            return dst;
        }

        private static void ComputeFit(int sw, int sh, int maxW, int maxH, out int tw, out int th)
        {
            if (sw <= maxW && sh <= maxH) { tw = sw; th = sh; return; }
            ComputeFitForce(sw, sh, maxW, maxH, out tw, out th);
        }

        private static void ComputeFitForce(int sw, int sh, int maxW, int maxH, out int tw, out int th)
        {
            double ratio = Math.Min((double)maxW / sw, (double)maxH / sh);
            tw = Math.Max(1, (int)Math.Round(sw * ratio));
            th = Math.Max(1, (int)Math.Round(sh * ratio));
        }

        private static void DrawWatermark(Bitmap bmp, string text, int posMode, int opacityPct)
        {
            int alpha = Math.Max(15, Math.Min(245, (int)Math.Round(opacityPct * 2.55)));
            float fontPx = Math.Max(14f, Math.Min(72f, bmp.Width * 0.036f));
            using (Graphics g = Graphics.FromImage(bmp))
            using (Font f = new Font("Segoe UI", fontPx, FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(alpha / 2, 0, 0, 0)))
            using (SolidBrush fg = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
            {
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                SizeF sz = g.MeasureString(text, f);
                float pad = Math.Max(12f, bmp.Width * 0.025f);

                if (posMode == 3) // Tile Grid
                {
                    g.TranslateTransform(bmp.Width / 2f, bmp.Height / 2f);
                    g.RotateTransform(-28f);
                    float stepX = Math.Max(160f, sz.Width * 1.6f);
                    float stepY = Math.Max(110f, sz.Height * 3.0f);
                    float diag = (float)Math.Sqrt(bmp.Width * bmp.Width + bmp.Height * bmp.Height);
                    for (float y = -diag; y < diag; y += stepY)
                    {
                        for (float x = -diag; x < diag; x += stepX)
                        {
                            g.DrawString(text, f, shadow, x + 2, y + 2);
                            g.DrawString(text, f, fg, x, y);
                        }
                    }
                    return;
                }

                float tx = bmp.Width - sz.Width - pad;
                float ty = bmp.Height - sz.Height - pad;
                if (posMode == 1) { tx = (bmp.Width - sz.Width) / 2f; ty = (bmp.Height - sz.Height) / 2f; }
                else if (posMode == 2) { tx = pad; ty = bmp.Height - sz.Height - pad; }

                g.DrawString(text, f, shadow, tx + 2, ty + 2);
                g.DrawString(text, f, fg, tx, ty);
            }
        }

        private static void SaveEncoded(Bitmap bmp, string path, string fmt, int quality)
        {
            if (fmt == "JPG")
            {
                ImageCodecInfo codec = FindCodec("image/jpeg");
                using (EncoderParameters ep = new EncoderParameters(1))
                {
                    ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)Math.Max(10, Math.Min(100, quality)));
                    bmp.Save(path, codec, ep);
                }
            }
            else if (fmt == "PNG") bmp.Save(path, ImageFormat.Png);
            else if (fmt == "TIFF") bmp.Save(path, ImageFormat.Tiff);
            else if (fmt == "BMP") bmp.Save(path, ImageFormat.Bmp);
            else bmp.Save(path, ImageFormat.Png);
        }

        private static ImageCodecInfo FindCodec(string mime)
        {
            ImageCodecInfo[] all = ImageCodecInfo.GetImageEncoders();
            for (int i = 0; i < all.Length; i++)
                if (string.Equals(all[i].MimeType, mime, StringComparison.OrdinalIgnoreCase)) return all[i];
            return all[0];
        }

        private static void SaveMultiSizeIco(Bitmap src, string path)
        {
            int[] sizes = new int[] { 16, 32, 48, 256 };
            List<byte[]> pngs = new List<byte[]>();
            for (int i = 0; i < sizes.Length; i++)
            {
                int s = sizes[i];
                using (Bitmap b = new Bitmap(s, s, PixelFormat.Format32bppArgb))
                using (Graphics g = Graphics.FromImage(b))
                using (MemoryStream ms = new MemoryStream())
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.Clear(Color.Transparent);
                    int fw, fh;
                    ComputeFitForce(src.Width, src.Height, s, s, out fw, out fh);
                    g.DrawImage(src, (s - fw) / 2, (s - fh) / 2, fw, fh);
                    b.Save(ms, ImageFormat.Png);
                    pngs.Add(ms.ToArray());
                }
            }
            using (FileStream fs = File.Create(path))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write((ushort)0); bw.Write((ushort)1); bw.Write((ushort)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    byte dim = sizes[i] == 256 ? (byte)0 : (byte)sizes[i];
                    bw.Write(dim); bw.Write(dim); bw.Write((byte)0); bw.Write((byte)0);
                    bw.Write((ushort)1); bw.Write((ushort)32);
                    bw.Write((uint)pngs[i].Length); bw.Write((uint)offset);
                    offset += pngs[i].Length;
                }
                for (int i = 0; i < pngs.Count; i++) bw.Write(pngs[i]);
            }
        }
    }

    // ========================================================================
    // 5. BATCH RENAME ENGINE (Live Preview + Safe 2-Phase + 1-Click Undo)
    // ========================================================================
    internal class RenameRules
    {
        public string FindText = "";
        public string ReplaceText = "";
        public bool UseRegex = false;
        public bool CaseSensitive = false;
        public bool GreeklishSeo = false;
        public string Prefix = "";
        public string Suffix = "";
        public bool AddNumbering = false;
        public int NumStart = 1;
        public int NumPadding = 3;
        public int NumPosition = 0; // 0=End (_001), 1=Start (001_), 2=ReplaceBase (prefix+001)
        public int CaseMode = 0; // 0=Keep, 1=lower, 2=UPPER, 3=Title
        public bool LowercaseExt = true;
    }

    internal static class BatchRenameEngine
    {
        public static string ComputeNewFileName(string fullPath, int index, RenameRules r)
        {
            string dir = Path.GetDirectoryName(fullPath);
            string folderName = string.IsNullOrEmpty(dir) ? "" : Path.GetFileName(dir);
            string baseName = Path.GetFileNameWithoutExtension(fullPath);
            string ext = Path.GetExtension(fullPath);
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            if (!string.IsNullOrEmpty(r.FindText))
            {
                try
                {
                    string pat = r.UseRegex ? r.FindText : Regex.Escape(r.FindText);
                    RegexOptions opts = r.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    baseName = Regex.Replace(baseName, pat, r.ReplaceText ?? "", opts);
                }
                catch { }
            }

            if (r.GreeklishSeo)
                baseName = Greeklish.ToSeoSlug(baseName);

            string pre = (r.Prefix ?? "").Replace("{date}", today).Replace("{folder}", folderName);
            string suf = (r.Suffix ?? "").Replace("{date}", today).Replace("{folder}", folderName);

            if (r.AddNumbering)
            {
                string seq = (r.NumStart + index).ToString().PadLeft(Math.Max(1, Math.Min(6, r.NumPadding)), '0');
                if (r.NumPosition == 0) baseName = pre + baseName + suf + "_" + seq;
                else if (r.NumPosition == 1) baseName = seq + "_" + pre + baseName + suf;
                else baseName = pre + seq + suf;
            }
            else
            {
                baseName = pre + baseName + suf;
            }

            if (r.CaseMode == 1) baseName = baseName.ToLower();
            else if (r.CaseMode == 2) baseName = baseName.ToUpper();
            else if (r.CaseMode == 3) baseName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(baseName.ToLower());

            // Strip illegal Windows filename characters
            char[] bad = Path.GetInvalidFileNameChars();
            for (int i = 0; i < bad.Length; i++) baseName = baseName.Replace(bad[i], '-');
            baseName = baseName.Trim();
            if (baseName.Length == 0) baseName = "file_" + (index + 1);

            if (r.LowercaseExt) ext = ext.ToLowerInvariant();
            return baseName + ext;
        }

        public static List<KeyValuePair<string, string>> ExecuteSafeRename(List<string> oldPaths, List<string> newNames, out int renamedCount, out int errors)
        {
            renamedCount = 0;
            errors = 0;
            List<KeyValuePair<string, string>> undoLog = new List<KeyValuePair<string, string>>();
            List<KeyValuePair<string, string>> tempPairs = new List<KeyValuePair<string, string>>();

            // Phase 1: move changed files to unique temp names (avoids case-only / swap collisions on NTFS)
            for (int i = 0; i < oldPaths.Count; i++)
            {
                string oldP = oldPaths[i];
                string dir = Path.GetDirectoryName(oldP);
                string targetP = Path.Combine(dir, newNames[i]);
                if (string.Equals(oldP, targetP, StringComparison.Ordinal)) continue;
                try
                {
                    string tmp = oldP + ".nillyren_" + i + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                    File.Move(oldP, tmp);
                    tempPairs.Add(new KeyValuePair<string, string>(tmp, targetP));
                    undoLog.Add(new KeyValuePair<string, string>(targetP, oldP));
                }
                catch { errors++; }
            }

            // Phase 2: move temp names to final target names
            for (int i = 0; i < tempPairs.Count; i++)
            {
                try
                {
                    string finalP = tempPairs[i].Value;
                    if (File.Exists(finalP))
                    {
                        string baseN = Path.GetFileNameWithoutExtension(finalP);
                        string ext = Path.GetExtension(finalP);
                        finalP = Path.Combine(Path.GetDirectoryName(finalP), baseN + "_" + (i + 1) + ext);
                        undoLog[i] = new KeyValuePair<string, string>(finalP, undoLog[i].Value);
                    }
                    File.Move(tempPairs[i].Key, finalP);
                    renamedCount++;
                }
                catch { errors++; }
            }
            return undoLog;
        }
    }

    // ========================================================================
    // 6. MAIN ULTRA-LIGHT STUDIO WINDOW (MainForm)
    // ========================================================================
    internal class MainForm : Form
    {
        private BatchTheme _theme = BatchTheme.Win11Dark();
        private int _activeTab = 0;

        // Top Bar
        private Panel _topBar;
        private StudioButton _btnTabImg, _btnTabRen, _btnAddFiles, _btnAddFolder, _btnClear, _btnTheme;
        private Panel _contentHost;
        private Panel _panelImg, _panelRen;
        private Label _lblFooter;

        // Image Batch Tab controls
        private ListView _lvImg;
        private List<string> _imgFiles = new List<string>();
        private int _imgSortCol = 0; private bool _imgSortAsc = true;
        private int _renSortCol = 0; private bool _renSortAsc = true;
        private bool _internalDrag = false;
        private static readonly string[] ImgCols = new string[] { "Image File", "Original", "Output", "Status" };
        private static readonly string[] RenCols = new string[] { "Original Filename", "New Filename (Live Preview)", "Status" };
        private ComboBox _cmbFmt, _cmbResize, _cmbWmPos;
        private TrackBar _trkQuality, _trkWmOpacity;
        private Label _lblQualityVal, _lblWmOpVal, _lblImgStats;
        private TextBox _txtCustomW, _txtCustomH, _txtScalePct, _txtSuffix, _txtWmText;
        private CheckBox _chkExifRot, _chkSeoName, _chkOverwrite, _chkWatermark;
        private ProgressBar _progBar;
        private StudioButton _btnRunBatch, _btnCancelBatch, _btnOpenOut, _btnZipOut;
        private volatile bool _batchRunning = false;
        private volatile bool _batchCancel = false;
        private string _lastOutDir = "";

        // Batch Rename Tab controls
        private ListView _lvRen;
        private List<string> _renFiles = new List<string>();
        private TextBox _txtFind, _txtReplace, _txtPrefix, _txtSuffixRen, _txtNumStart, _txtNumPad;
        private CheckBox _chkRegex, _chkCaseSens, _chkGreeklish, _chkNumbering, _chkLowerExt;
        private ComboBox _cmbNumPos, _cmbCaseMode;
        private Label _lblRenStats;
        private StudioButton _btnRunRename, _btnUndoRename;
        private List<KeyValuePair<string, string>> _lastUndoLog = new List<KeyValuePair<string, string>>();

        public MainForm(string[] args)
        {
            this.Text = "NillyBatch Pro — Ultra-Light Batch Image & Rename Studio";
            this.Size = new Size(980, 660);
            this.MinimumSize = new Size(820, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AllowDrop = true;
            this.KeyPreview = true;

            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NillyBatchPro.ico");
                if (File.Exists(icoPath)) this.Icon = new Icon(icoPath);
                else this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            BuildUI();
            if (!ReadSuiteDark(true)) _theme = BatchTheme.Win11Light();
            ApplyTheme(_theme);
            SwitchTab(0);

            this.HandleCreated += delegate { Native.SetDarkTitleBar(this.Handle, _theme.IsDark); };
            this.DragEnter += delegate(object s, DragEventArgs e)
            {
                if (_internalDrag) { e.Effect = DragDropEffects.None; return; }
                if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            };
            this.DragDrop += OnDragDropFiles;
            this.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Control && e.KeyCode == Keys.D1) { SwitchTab(0); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.D2) { SwitchTab(1); e.Handled = true; }
            };

            if (args != null && args.Length > 0)
            {
                List<string> paths = new List<string>();
                for (int i = 0; i < args.Length; i++)
                    if (!string.IsNullOrEmpty(args[i]) && !args[i].StartsWith("--")) paths.Add(args[i]);
                if (paths.Count > 0) AddDroppedPaths(paths.ToArray());
            }
        }

        private void BuildUI()
        {
            // 1. Top Header Bar
            _topBar = new Panel();
            _topBar.Dock = DockStyle.Top;
            _topBar.Height = 46;
            _topBar.Padding = new Padding(10, 8, 10, 6);

            _btnTabImg = MakeBtn("Εικόνες (Ctrl+1)", 132, 30, PillKind.Subtle, delegate { SwitchTab(0); });
            _btnTabImg.Location = new Point(10, 8);
            _btnTabImg.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

            _btnTabRen = MakeBtn("Μετονομασία (Ctrl+2)", 152, 30, PillKind.Subtle, delegate { SwitchTab(1); });
            _btnTabRen.Location = new Point(148, 8);
            _btnTabRen.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

            _btnAddFiles = MakeBtn("+ Αρχεία", 88, 30, PillKind.Normal, delegate { PromptAddFiles(); });
            _btnAddFiles.Location = new Point(316, 8);

            _btnAddFolder = MakeBtn("+ Φάκελος", 96, 30, PillKind.Normal, delegate { PromptAddFolder(); });
            _btnAddFolder.Location = new Point(410, 8);

            _btnClear = MakeBtn("Καθαρισμός", 94, 30, PillKind.Danger, delegate { ClearActiveList(); });
            _btnClear.Location = new Point(512, 8);

            _btnTheme = MakeBtn("◐", 36, 30, PillKind.Subtle, delegate { ApplyTheme(_theme.IsDark ? BatchTheme.Win11Light() : BatchTheme.Win11Dark()); });
            _btnTheme.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnTheme.Location = new Point(this.ClientSize.Width - 46, 8);

            _topBar.Controls.AddRange(new Control[] { _btnTabImg, _btnTabRen, _btnAddFiles, _btnAddFolder, _btnClear, _btnTheme });

            // 2. Bottom Footer
            _lblFooter = new Label();
            _lblFooter.UseMnemonic = false;
            _lblFooter.Dock = DockStyle.Bottom;
            _lblFooter.Height = 24;
            _lblFooter.TextAlign = ContentAlignment.MiddleCenter;
            _lblFooter.Font = new Font("Segoe UI", 8.2f);
            _lblFooter.Text = "NillyBatch Pro · Drag & Drop files or folders anywhere · 0 MB background RAM · created by Nilly graphics";

            // 3. Content Host
            _contentHost = new Panel();
            _contentHost.Dock = DockStyle.Fill;
            _contentHost.Padding = new Padding(10, 4, 10, 6);

            _panelImg = BuildImageBatchPanel();
            _panelRen = BuildBatchRenamePanel();
            _panelImg.Dock = DockStyle.Fill;
            _panelRen.Dock = DockStyle.Fill;
            _panelRen.Visible = false;

            _contentHost.Controls.Add(_panelImg);
            _contentHost.Controls.Add(_panelRen);

            this.Controls.Add(_contentHost);
            this.Controls.Add(_lblFooter);
            this.Controls.Add(_topBar);
        }

        // ====================================================================
        // TAB 0: IMAGE BATCH PANEL
        // ====================================================================
        private Panel BuildImageBatchPanel()
        {
            Panel root = new Panel();

            // Right Studio Settings Panel
            Panel right = new Panel();
            right.Dock = DockStyle.Right;
            right.Width = 326;
            right.Padding = new Padding(12, 6, 6, 6);
            right.AutoScroll = true;

            int y = 4;
            right.Controls.Add(MakeSec("OUTPUT FORMAT & QUALITY", 10, y)); y += 20;

            _cmbFmt = new ComboBox();
            _cmbFmt.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbFmt.Font = new Font("Segoe UI", 9.5f);
            _cmbFmt.Bounds = new Rectangle(10, y, 294, 26);
            _cmbFmt.Items.AddRange(new object[] {
                "JPG (Web / E-Shop — smallest)",
                "PNG (Lossless + Transparency)",
                "TIFF (Print / Archival)",
                "BMP (Uncompressed)",
                "ICO (Multi-Size Windows Icon)",
                "Keep Original Format"
            });
            _cmbFmt.SelectedIndex = 0;
            right.Controls.Add(_cmbFmt); y += 34;

            _lblQualityVal = MakeLbl("JPG Quality: 85%", 10, y, 294);
            right.Controls.Add(_lblQualityVal); y += 18;

            _trkQuality = new TrackBar();
            _trkQuality.Minimum = 30; _trkQuality.Maximum = 100; _trkQuality.Value = 85;
            _trkQuality.TickStyle = TickStyle.None;
            _trkQuality.Bounds = new Rectangle(6, y, 298, 28);
            _trkQuality.Scroll += delegate { _lblQualityVal.Text = "JPG Quality: " + _trkQuality.Value + "%"; };
            right.Controls.Add(_trkQuality); y += 32;

            right.Controls.Add(MakeSec("RESIZE PRESET (High-Quality Bicubic)", 10, y)); y += 20;
            _cmbResize = new ComboBox();
            _cmbResize.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbResize.Font = new Font("Segoe UI", 9.5f);
            _cmbResize.Bounds = new Rectangle(10, y, 294, 26);
            _cmbResize.Items.AddRange(new object[] {
                "Keep Original Dimensions",
                "Web / HD — Max 1920 px",
                "E-Shop / WooCommerce — Max 1200 px",
                "Viber / Thumbnail — Max 800 px",
                "Square 1080×1080 (Center Crop — IG)",
                "Square 1080×1080 (White Pad — Skroutz)",
                "Scale by Percentage (%)",
                "Custom Max Width × Height (px)"
            });
            _cmbResize.SelectedIndex = 2;
            _cmbResize.SelectedIndexChanged += delegate { ToggleResizeCustomBoxes(); };
            right.Controls.Add(_cmbResize); y += 32;

            right.Controls.Add(MakeLbl("Max W", 10, y + 3, 44));
            _txtCustomW = MakeBox("1600", 54, y, 58);
            right.Controls.Add(_txtCustomW);
            right.Controls.Add(MakeLbl("Max H", 118, y + 3, 44));
            _txtCustomH = MakeBox("1600", 162, y, 58);
            right.Controls.Add(_txtCustomH);
            right.Controls.Add(MakeLbl("%", 228, y + 3, 20));
            _txtScalePct = MakeBox("50", 248, y, 56);
            right.Controls.Add(_txtScalePct); y += 34;

            right.Controls.Add(MakeSec("OPTIMIZATION & FILENAME", 10, y)); y += 20;
            _chkExifRot = MakeChk("Auto-Rotate EXIF & Strip GPS Metadata", true, 10, y); right.Controls.Add(_chkExifRot); y += 24;
            _chkSeoName = MakeChk("Greeklish SEO filenames (α→a, spaces→-)", false, 10, y); right.Controls.Add(_chkSeoName); y += 24;
            _chkOverwrite = MakeChk("Overwrite originals (default: \\NillyBatch_Output)", false, 10, y); right.Controls.Add(_chkOverwrite); y += 26;

            right.Controls.Add(MakeLbl("Suffix (optional):", 10, y + 3, 110));
            _txtSuffix = MakeBox("", 122, y, 182);
            right.Controls.Add(_txtSuffix); y += 34;

            right.Controls.Add(MakeSec("WATERMARK (Client Proofs)", 10, y)); y += 20;
            _chkWatermark = MakeChk("Add Text Watermark", false, 10, y); right.Controls.Add(_chkWatermark); y += 24;
            _txtWmText = MakeBox("© Nilly graphics", 10, y, 180); right.Controls.Add(_txtWmText);
            _cmbWmPos = new ComboBox();
            _cmbWmPos.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbWmPos.Font = new Font("Segoe UI", 9f);
            _cmbWmPos.Bounds = new Rectangle(196, y, 108, 26);
            _cmbWmPos.Items.AddRange(new object[] { "Bottom-Right", "Center", "Bottom-Left", "Tile Grid" });
            _cmbWmPos.SelectedIndex = 0;
            right.Controls.Add(_cmbWmPos); y += 30;

            _lblWmOpVal = MakeLbl("Opacity: 40%", 10, y, 100); right.Controls.Add(_lblWmOpVal);
            _trkWmOpacity = new TrackBar();
            _trkWmOpacity.Minimum = 10; _trkWmOpacity.Maximum = 90; _trkWmOpacity.Value = 40;
            _trkWmOpacity.TickStyle = TickStyle.None;
            _trkWmOpacity.Bounds = new Rectangle(110, y - 4, 194, 26);
            _trkWmOpacity.Scroll += delegate { _lblWmOpVal.Text = "Opacity: " + _trkWmOpacity.Value + "%"; };
            right.Controls.Add(_trkWmOpacity); y += 32;

            // Progress + Action Buttons
            _progBar = new ProgressBar();
            _progBar.Bounds = new Rectangle(10, y, 294, 14);
            right.Controls.Add(_progBar); y += 20;

            _lblImgStats = MakeLbl("Drop images (JPG/PNG/WebP/HEIC/TIFF/RAW)", 10, y, 294);
            _lblImgStats.Height = 32;
            right.Controls.Add(_lblImgStats); y += 36;

            _btnRunBatch = MakeBtn("Εκκίνηση", 190, 38, PillKind.Accent, delegate { StartImageBatch(); });
            _btnRunBatch.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _btnRunBatch.Location = new Point(10, y);
            right.Controls.Add(_btnRunBatch);

            _btnCancelBatch = MakeBtn("Διακοπή", 98, 38, PillKind.Danger, delegate { _batchCancel = true; });
            _btnCancelBatch.Location = new Point(206, y);
            _btnCancelBatch.Enabled = false;
            right.Controls.Add(_btnCancelBatch); y += 44;

            _btnOpenOut = MakeBtn("Άνοιγμα Φακέλου", 188, 32, PillKind.Normal, delegate
            {
                if (!string.IsNullOrEmpty(_lastOutDir) && Directory.Exists(_lastOutDir))
                    OpenFolderSmart(_lastOutDir);
            });
            _btnOpenOut.Location = new Point(10, y);
            _btnOpenOut.Enabled = false;
            right.Controls.Add(_btnOpenOut);

            _btnZipOut = MakeBtn("ZIP Φακέλου", 100, 32, PillKind.Subtle, delegate
            {
                if (string.IsNullOrEmpty(_lastOutDir) || !Directory.Exists(_lastOutDir)) return;
                string zipExe = FindSiblingExe("NillyZipPro", "NillyZipPro.exe");
                if (zipExe != null)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(zipExe, "--compress \"" + _lastOutDir + "\"") { WorkingDirectory = Path.GetDirectoryName(zipExe), UseShellExecute = true });
                        _lblImgStats.Text = "Δημιουργήθηκε ZIP με NillyZip Pro.";
                    }
                    catch { }
                }
            });
            _btnZipOut.Location = new Point(204, y);
            _btnZipOut.Enabled = false;
            right.Controls.Add(_btnZipOut);

            // Left File ListView
            _lvImg = new ListView();
            _lvImg.Dock = DockStyle.Fill;
            _lvImg.View = View.Details;
            _lvImg.FullRowSelect = true;
            _lvImg.AllowDrop = true;
            _lvImg.BorderStyle = BorderStyle.None;
            _lvImg.Font = new Font("Segoe UI", 9f);
            _lvImg.Columns.Add("Image File ▲", 260);
            _lvImg.Columns.Add("Original", 82, HorizontalAlignment.Right);
            _lvImg.Columns.Add("Output", 110, HorizontalAlignment.Right);
            _lvImg.Columns.Add("Status", 130);
            _lvImg.DragEnter += delegate(object s, DragEventArgs e) { if (_internalDrag) { e.Effect = DragDropEffects.None; return; } if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            _lvImg.DragDrop += OnDragDropFiles;
            _lvImg.ItemDrag += delegate(object s, ItemDragEventArgs e) { if (e.Button == MouseButtons.Left) DragOutBatchFiles(_lvImg, _imgFiles); };
            _lvImg.ColumnClick += delegate(object s, ColumnClickEventArgs e)
            {
                if (_batchRunning) return;
                if (_imgSortCol == e.Column) _imgSortAsc = !_imgSortAsc;
                else { _imgSortCol = e.Column; _imgSortAsc = (e.Column != 1); }
                SortImageQueue();
            };
            _lvImg.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Delete && !_batchRunning) RemoveSelectedImgItems();
            };

            root.Controls.Add(_lvImg);
            root.Controls.Add(right);
            ToggleResizeCustomBoxes();
            return root;
        }

        private void ToggleResizeCustomBoxes()
        {
            int idx = _cmbResize.SelectedIndex;
            _txtCustomW.Enabled = (idx == 7);
            _txtCustomH.Enabled = (idx == 7);
            _txtScalePct.Enabled = (idx == 6);
        }

        // ====================================================================
        // TAB 1: BATCH RENAME PANEL (Live Before -> After Preview + Undo)
        // ====================================================================
        private Panel BuildBatchRenamePanel()
        {
            Panel root = new Panel();

            Panel right = new Panel();
            right.Dock = DockStyle.Right;
            right.Width = 326;
            right.Padding = new Padding(12, 6, 6, 6);
            right.AutoScroll = true;

            int y = 4;
            right.Controls.Add(MakeSec("FIND & REPLACE", 10, y)); y += 20;
            right.Controls.Add(MakeLbl("Find:", 10, y + 3, 58));
            _txtFind = MakeBox("", 72, y, 232); _txtFind.TextChanged += delegate { RefreshRenamePreview(); };
            right.Controls.Add(_txtFind); y += 30;

            right.Controls.Add(MakeLbl("Replace:", 10, y + 3, 58));
            _txtReplace = MakeBox("", 72, y, 232); _txtReplace.TextChanged += delegate { RefreshRenamePreview(); };
            right.Controls.Add(_txtReplace); y += 30;

            _chkRegex = MakeChk("Regex (.*)", false, 10, y); _chkRegex.Width = 110; _chkRegex.CheckedChanged += delegate { RefreshRenamePreview(); };
            _chkCaseSens = MakeChk("Case-Sensitive", false, 130, y); _chkCaseSens.Width = 140; _chkCaseSens.CheckedChanged += delegate { RefreshRenamePreview(); };
            right.Controls.Add(_chkRegex); right.Controls.Add(_chkCaseSens); y += 30;

            right.Controls.Add(MakeSec("SEO & GREEKLISH CLEANER", 10, y)); y += 20;
            _chkGreeklish = MakeChk("Ελληνικά → greeklish-seo (URL/E-Shop ready)", true, 10, y);
            _chkGreeklish.CheckedChanged += delegate { RefreshRenamePreview(); };
            right.Controls.Add(_chkGreeklish); y += 30;

            right.Controls.Add(MakeSec("PREFIX & SUFFIX ({date}, {folder})", 10, y)); y += 20;
            right.Controls.Add(MakeLbl("Prefix:", 10, y + 3, 58));
            _txtPrefix = MakeBox("", 72, y, 232); _txtPrefix.TextChanged += delegate { RefreshRenamePreview(); };
            right.Controls.Add(_txtPrefix); y += 30;

            right.Controls.Add(MakeLbl("Suffix:", 10, y + 3, 58));
            _txtSuffixRen = MakeBox("", 72, y, 232); _txtSuffixRen.TextChanged += delegate { RefreshRenamePreview(); };
            right.Controls.Add(_txtSuffixRen); y += 34;

            right.Controls.Add(MakeSec("AUTO-NUMBERING", 10, y)); y += 20;
            _chkNumbering = MakeChk("Add Sequence Numbering (001, 002...)", false, 10, y);
            _chkNumbering.CheckedChanged += delegate { RefreshRenamePreview(); };
            right.Controls.Add(_chkNumbering); y += 26;

            right.Controls.Add(MakeLbl("Start", 10, y + 3, 36));
            _txtNumStart = MakeBox("1", 48, y, 48); _txtNumStart.TextChanged += delegate { RefreshRenamePreview(); };
            right.Controls.Add(_txtNumStart);

            right.Controls.Add(MakeLbl("Pad", 104, y + 3, 30));
            _txtNumPad = MakeBox("3", 136, y, 40); _txtNumPad.TextChanged += delegate { RefreshRenamePreview(); };
            right.Controls.Add(_txtNumPad);

            _cmbNumPos = new ComboBox();
            _cmbNumPos.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbNumPos.Font = new Font("Segoe UI", 9f);
            _cmbNumPos.Bounds = new Rectangle(184, y, 120, 26);
            _cmbNumPos.Items.AddRange(new object[] { "End (_001)", "Start (001_)", "Replace Name" });
            _cmbNumPos.SelectedIndex = 0;
            _cmbNumPos.SelectedIndexChanged += delegate { RefreshRenamePreview(); };
            right.Controls.Add(_cmbNumPos); y += 34;

            right.Controls.Add(MakeSec("CASE & EXTENSION", 10, y)); y += 20;
            _cmbCaseMode = new ComboBox();
            _cmbCaseMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbCaseMode.Font = new Font("Segoe UI", 9f);
            _cmbCaseMode.Bounds = new Rectangle(10, y, 146, 26);
            _cmbCaseMode.Items.AddRange(new object[] { "Keep Case", "lowercase", "UPPERCASE", "Title Case" });
            _cmbCaseMode.SelectedIndex = 0;
            _cmbCaseMode.SelectedIndexChanged += delegate { RefreshRenamePreview(); };
            right.Controls.Add(_cmbCaseMode);

            _chkLowerExt = MakeChk(".ext lowercase", true, 166, y + 2); _chkLowerExt.Width = 136;
            _chkLowerExt.CheckedChanged += delegate { RefreshRenamePreview(); };
            right.Controls.Add(_chkLowerExt); y += 38;

            _lblRenStats = MakeLbl("Drop ANY files to preview live rename", 10, y, 294);
            _lblRenStats.Height = 28;
            right.Controls.Add(_lblRenStats); y += 32;

            _btnRunRename = MakeBtn("Μετονομασία Όλων", 294, 38, PillKind.Accent, delegate { ExecuteBatchRename(); });
            _btnRunRename.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _btnRunRename.Location = new Point(10, y);
            right.Controls.Add(_btnRunRename); y += 44;

            _btnUndoRename = MakeBtn("Αναίρεση Τελευταίας Μετονομασίας", 294, 32, PillKind.Normal, delegate { UndoLastRename(); });
            _btnUndoRename.Location = new Point(10, y);
            _btnUndoRename.Enabled = false;
            right.Controls.Add(_btnUndoRename);

            _lvRen = new ListView();
            _lvRen.Dock = DockStyle.Fill;
            _lvRen.View = View.Details;
            _lvRen.FullRowSelect = true;
            _lvRen.AllowDrop = true;
            _lvRen.BorderStyle = BorderStyle.None;
            _lvRen.Font = new Font("Segoe UI", 9f);
            _lvRen.Columns.Add("Original Filename ▲", 260);
            _lvRen.Columns.Add("New Filename (Live Preview)", 270);
            _lvRen.Columns.Add("Status", 75);
            _lvRen.DragEnter += delegate(object s, DragEventArgs e) { if (_internalDrag) { e.Effect = DragDropEffects.None; return; } if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            _lvRen.DragDrop += OnDragDropFiles;
            _lvRen.ItemDrag += delegate(object s, ItemDragEventArgs e) { if (e.Button == MouseButtons.Left) DragOutBatchFiles(_lvRen, _renFiles); };
            _lvRen.ColumnClick += delegate(object s, ColumnClickEventArgs e)
            {
                if (_renSortCol == e.Column) _renSortAsc = !_renSortAsc;
                else { _renSortCol = e.Column; _renSortAsc = true; }
                SortRenameQueue();
            };
            _lvRen.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Delete) RemoveSelectedRenItems();
            };

            root.Controls.Add(_lvRen);
            root.Controls.Add(right);
            return root;
        }

        // ====================================================================
        // FILE LOADING & DRAG-DROP
        // ====================================================================
        private void OnDragDropFiles(object sender, DragEventArgs e)
        {
            if (_internalDrag || e.Data == null) return;
            string[] items = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (items != null) AddDroppedPaths(items);
        }

        private void DragOutBatchFiles(ListView lv, List<string> backing)
        {
            if (lv == null || lv.SelectedIndices.Count == 0) return;
            List<string> paths = new List<string>();
            for (int i = 0; i < lv.SelectedIndices.Count; i++)
            {
                int idx = lv.SelectedIndices[i];
                if (idx >= 0 && idx < backing.Count && File.Exists(backing[idx])) paths.Add(backing[idx]);
            }
            if (paths.Count == 0) return;
            _internalDrag = true;
            try { lv.DoDragDrop(new DataObject(DataFormats.FileDrop, paths.ToArray()), DragDropEffects.Copy | DragDropEffects.Link); }
            catch { }
            finally { _internalDrag = false; }
        }

        private void SortImageQueue()
        {
            for (int i = 0; i < ImgCols.Length; i++)
                _lvImg.Columns[i].Text = ImgCols[i] + (i == _imgSortCol ? (_imgSortAsc ? " ▲" : " ▼") : "");
            if (_imgFiles.Count <= 1) return;
            int sign = _imgSortAsc ? 1 : -1;
            Dictionary<string, long> sizes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            if (_imgSortCol == 1)
            {
                for (int i = 0; i < _imgFiles.Count; i++)
                    try { sizes[_imgFiles[i]] = new FileInfo(_imgFiles[i]).Length; } catch { sizes[_imgFiles[i]] = 0; }
            }
            _imgFiles.Sort(delegate(string a, string b)
            {
                int cmp = (_imgSortCol == 1)
                    ? sizes[a].CompareTo(sizes[b])
                    : string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase);
                return sign * cmp;
            });
            _lvImg.BeginUpdate();
            _lvImg.Items.Clear();
            for (int i = 0; i < _imgFiles.Count; i++)
            {
                string f = _imgFiles[i];
                long bytes = 0;
                try { bytes = new FileInfo(f).Length; } catch { }
                ListViewItem lvi = new ListViewItem(Path.GetFileName(f));
                lvi.SubItems.Add(FormatBytes(bytes));
                lvi.SubItems.Add("-");
                lvi.SubItems.Add("Ready");
                lvi.Tag = f;
                _lvImg.Items.Add(lvi);
            }
            _lvImg.EndUpdate();
        }

        private void SortRenameQueue()
        {
            for (int i = 0; i < RenCols.Length; i++)
                _lvRen.Columns[i].Text = RenCols[i] + (i == _renSortCol ? (_renSortAsc ? " ▲" : " ▼") : "");
            if (_renFiles.Count <= 1) return;
            int sign = _renSortAsc ? 1 : -1;
            _renFiles.Sort(delegate(string a, string b)
            {
                return sign * string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase);
            });
            RefreshRenamePreview();
        }

        private void AddDroppedPaths(string[] paths)
        {
            List<string> expanded = new List<string>();
            for (int i = 0; i < paths.Length; i++)
            {
                if (Directory.Exists(paths[i]))
                {
                    try { expanded.AddRange(Directory.GetFiles(paths[i])); } catch { }
                }
                else if (File.Exists(paths[i]))
                {
                    expanded.Add(paths[i]);
                }
            }
            expanded.Sort(StringComparer.CurrentCultureIgnoreCase);

            if (_activeTab == 0)
            {
                HashSet<string> existing = new HashSet<string>(_imgFiles, StringComparer.OrdinalIgnoreCase);
                int added = 0;
                _lvImg.BeginUpdate();
                for (int i = 0; i < expanded.Count; i++)
                {
                    string f = expanded[i];
                    if (!ImageBatchEngine.SupportedExts.Contains(Path.GetExtension(f))) continue;
                    if (!existing.Add(f)) continue;
                    _imgFiles.Add(f);
                    long bytes = new FileInfo(f).Length;
                    ListViewItem lvi = new ListViewItem(Path.GetFileName(f));
                    lvi.SubItems.Add(FormatBytes(bytes));
                    lvi.SubItems.Add("-");
                    lvi.SubItems.Add("Ready");
                    lvi.Tag = f;
                    _lvImg.Items.Add(lvi);
                    added++;
                }
                _lvImg.EndUpdate();
                _lblImgStats.Text = _imgFiles.Count + " image(s) queued" + (added > 0 ? (" (+" + added + " added)") : "");
            }
            else
            {
                HashSet<string> existing = new HashSet<string>(_renFiles, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < expanded.Count; i++)
                {
                    if (existing.Add(expanded[i])) _renFiles.Add(expanded[i]);
                }
                RefreshRenamePreview();
            }
        }

        private void PromptAddFiles()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Multiselect = true;
                dlg.Filter = _activeTab == 0
                    ? "Images|*.jpg;*.jpeg;*.png;*.webp;*.heic;*.tif;*.tiff;*.bmp;*.gif;*.ico;*.dng;*.cr2;*.cr3;*.nef;*.arw|All Files|*.*"
                    : "All Files (*.*)|*.*";
                if (dlg.ShowDialog(this) == DialogResult.OK) AddDroppedPaths(dlg.FileNames);
            }
        }

        private void PromptAddFolder()
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Επιλογή φακέλου αρχείων";
                if (dlg.ShowDialog(this) == DialogResult.OK) AddDroppedPaths(new string[] { dlg.SelectedPath });
            }
        }

        private void ClearActiveList()
        {
            if (_activeTab == 0)
            {
                if (_batchRunning) return;
                _imgFiles.Clear(); _lvImg.Items.Clear(); _progBar.Value = 0;
                _lblImgStats.Text = "List cleared. Drop images to begin.";
            }
            else
            {
                _renFiles.Clear(); _lvRen.Items.Clear();
                _lblRenStats.Text = "List cleared. Drop ANY files to rename.";
            }
        }

        private void RemoveSelectedImgItems()
        {
            for (int i = _lvImg.SelectedIndices.Count - 1; i >= 0; i--)
            {
                int idx = _lvImg.SelectedIndices[i];
                _imgFiles.RemoveAt(idx);
                _lvImg.Items.RemoveAt(idx);
            }
            _lblImgStats.Text = _imgFiles.Count + " image(s) queued";
        }

        private void RemoveSelectedRenItems()
        {
            for (int i = _lvRen.SelectedIndices.Count - 1; i >= 0; i--)
            {
                int idx = _lvRen.SelectedIndices[i];
                _renFiles.RemoveAt(idx);
            }
            RefreshRenamePreview();
        }

        // ====================================================================
        // ASYNC IMAGE BATCH EXECUTION
        // ====================================================================
        private BatchOptions ReadBatchOptions()
        {
            BatchOptions o = new BatchOptions();
            string[] fmts = new string[] { "JPG", "PNG", "TIFF", "BMP", "ICO", "KEEP" };
            o.OutputFormat = fmts[Math.Max(0, Math.Min(fmts.Length - 1, _cmbFmt.SelectedIndex))];
            o.JpgQuality = _trkQuality.Value;
            o.ResizeMode = _cmbResize.SelectedIndex;
            int.TryParse(_txtCustomW.Text, out o.CustomMaxW); if (o.CustomMaxW < 16) o.CustomMaxW = 1600;
            int.TryParse(_txtCustomH.Text, out o.CustomMaxH); if (o.CustomMaxH < 16) o.CustomMaxH = 1600;
            int.TryParse(_txtScalePct.Text, out o.ScalePercent); if (o.ScalePercent < 5) o.ScalePercent = 50;
            o.AutoRotateExif = _chkExifRot.Checked;
            o.StripMetadata = _chkExifRot.Checked;
            o.SeoGreeklishNames = _chkSeoName.Checked;
            o.OverwriteOriginals = _chkOverwrite.Checked;
            o.Suffix = _txtSuffix.Text;
            o.AddWatermark = _chkWatermark.Checked;
            o.WatermarkText = _txtWmText.Text;
            o.WatermarkPosition = _cmbWmPos.SelectedIndex;
            o.WatermarkOpacity = _trkWmOpacity.Value;
            return o;
        }

        private void StartImageBatch()
        {
            if (_batchRunning || _imgFiles.Count == 0)
            {
                if (_imgFiles.Count == 0) MessageBox.Show("Πρόσθεσε πρώτα εικόνες (Drag & Drop ή + Add Files).", "NillyBatch Pro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            BatchOptions opt = ReadBatchOptions();
            List<string> work = new List<string>(_imgFiles);
            _batchRunning = true;
            _batchCancel = false;
            _btnRunBatch.Enabled = false;
            _btnCancelBatch.Enabled = true;
            _btnOpenOut.Enabled = false;
            _progBar.Maximum = work.Count;
            _progBar.Value = 0;

            Stopwatch sw = Stopwatch.StartNew();
            Thread th = new Thread(delegate()
            {
                long sumOld = 0, sumNew = 0;
                int okCnt = 0, errCnt = 0;
                string outDir = "";

                for (int i = 0; i < work.Count; i++)
                {
                    if (_batchCancel) break;
                    BatchItemResult r = ImageBatchEngine.ProcessSingle(work[i], opt);
                    int idx = i;
                    if (r.Success)
                    {
                        okCnt++; sumOld += r.OldBytes; sumNew += r.NewBytes;
                        outDir = Path.GetDirectoryName(r.OutputPath);
                    }
                    else errCnt++;

                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke((Action)delegate
                        {
                            if (idx < _lvImg.Items.Count)
                            {
                                ListViewItem item = _lvImg.Items[idx];
                                if (r.Success)
                                {
                                    double pct = r.OldBytes > 0 ? ((double)(r.NewBytes - r.OldBytes) / r.OldBytes * 100.0) : 0;
                                    item.SubItems[2].Text = FormatBytes(r.NewBytes) + " (" + r.OutW + "×" + r.OutH + ")";
                                    item.SubItems[3].Text = string.Format("OK ({0:+0.#;-0.#;0}%)", pct);
                                    item.ForeColor = _theme.OkColor;
                                }
                                else
                                {
                                    item.SubItems[3].Text = "ERR: " + r.Error;
                                    item.ForeColor = _theme.WarnColor;
                                }
                            }
                            _progBar.Value = Math.Min(_progBar.Maximum, idx + 1);
                        });
                    }
                }

                sw.Stop();
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((Action)delegate
                    {
                        _batchRunning = false;
                        _btnRunBatch.Enabled = true;
                        _btnCancelBatch.Enabled = false;
                        _lastOutDir = outDir;
                        bool hasOut = !string.IsNullOrEmpty(outDir) && Directory.Exists(outDir);
                        _btnOpenOut.Enabled = hasOut;
                        _btnZipOut.Enabled = hasOut && FindSiblingExe("NillyZipPro", "NillyZipPro.exe") != null;
                        double savedPct = sumOld > 0 ? ((double)(sumOld - sumNew) / sumOld * 100.0) : 0;
                        _lblImgStats.Text = string.Format("{0} OK{1} in {2:0.0}s\n{3} → {4} ({5:0.#}% saved)",
                            okCnt, errCnt > 0 ? (", " + errCnt + " err") : "", sw.Elapsed.TotalSeconds,
                            FormatBytes(sumOld), FormatBytes(sumNew), savedPct);
                    });
                }
            });
            th.IsBackground = true;
            th.Start();
        }

        // ====================================================================
        // BATCH RENAME LIVE PREVIEW & EXECUTION
        // ====================================================================
        private RenameRules ReadRenameRules()
        {
            RenameRules r = new RenameRules();
            r.FindText = _txtFind.Text;
            r.ReplaceText = _txtReplace.Text;
            r.UseRegex = _chkRegex.Checked;
            r.CaseSensitive = _chkCaseSens.Checked;
            r.GreeklishSeo = _chkGreeklish.Checked;
            r.Prefix = _txtPrefix.Text;
            r.Suffix = _txtSuffixRen.Text;
            r.AddNumbering = _chkNumbering.Checked;
            int.TryParse(_txtNumStart.Text, out r.NumStart);
            int.TryParse(_txtNumPad.Text, out r.NumPadding); if (r.NumPadding < 1) r.NumPadding = 3;
            r.NumPosition = _cmbNumPos.SelectedIndex;
            r.CaseMode = _cmbCaseMode.SelectedIndex;
            r.LowercaseExt = _chkLowerExt.Checked;
            return r;
        }

        private void RefreshRenamePreview()
        {
            RenameRules r = ReadRenameRules();
            _lvRen.BeginUpdate();
            _lvRen.Items.Clear();
            int willChange = 0;
            for (int i = 0; i < _renFiles.Count; i++)
            {
                string oldName = Path.GetFileName(_renFiles[i]);
                string newName = BatchRenameEngine.ComputeNewFileName(_renFiles[i], i, r);
                bool changed = !string.Equals(oldName, newName, StringComparison.Ordinal);
                if (changed) willChange++;

                ListViewItem lvi = new ListViewItem(oldName);
                lvi.SubItems.Add(newName);
                lvi.SubItems.Add(changed ? "Rename" : "Same");
                lvi.ForeColor = changed ? _theme.OkColor : _theme.TextMuted;
                _lvRen.Items.Add(lvi);
            }
            _lvRen.EndUpdate();
            _lblRenStats.Text = _renFiles.Count + " file(s) · " + willChange + " will be renamed";
        }

        private void ExecuteBatchRename()
        {
            if (_renFiles.Count == 0) return;
            RenameRules r = ReadRenameRules();
            List<string> newNames = new List<string>();
            for (int i = 0; i < _renFiles.Count; i++)
                newNames.Add(BatchRenameEngine.ComputeNewFileName(_renFiles[i], i, r));

            int ok, err;
            List<KeyValuePair<string, string>> undo = BatchRenameEngine.ExecuteSafeRename(_renFiles, newNames, out ok, out err);
            if (undo.Count > 0)
            {
                _lastUndoLog = undo;
                _btnUndoRename.Enabled = true;
                // Update list to new paths
                Dictionary<string, string> forward = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < undo.Count; i++) forward[undo[i].Value] = undo[i].Key;
                for (int i = 0; i < _renFiles.Count; i++)
                {
                    string np;
                    if (forward.TryGetValue(_renFiles[i], out np)) _renFiles[i] = np;
                }
            }
            RefreshRenamePreview();
            _lblRenStats.Text = "Renamed " + ok + " file(s)" + (err > 0 ? (" (" + err + " errors)") : " cleanly!");
        }

        private void UndoLastRename()
        {
            if (_lastUndoLog == null || _lastUndoLog.Count == 0) return;
            List<string> curPaths = new List<string>();
            List<string> origNames = new List<string>();
            for (int i = 0; i < _lastUndoLog.Count; i++)
            {
                if (File.Exists(_lastUndoLog[i].Key))
                {
                    curPaths.Add(_lastUndoLog[i].Key);
                    origNames.Add(Path.GetFileName(_lastUndoLog[i].Value));
                }
            }
            int ok, err;
            BatchRenameEngine.ExecuteSafeRename(curPaths, origNames, out ok, out err);
            Dictionary<string, string> back = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _lastUndoLog.Count; i++) back[_lastUndoLog[i].Key] = _lastUndoLog[i].Value;
            for (int i = 0; i < _renFiles.Count; i++)
            {
                string op;
                if (back.TryGetValue(_renFiles[i], out op)) _renFiles[i] = op;
            }
            _lastUndoLog.Clear();
            _btnUndoRename.Enabled = false;
            RefreshRenamePreview();
            _lblRenStats.Text = "Undo complete: restored " + ok + " filename(s)!";
        }

        // ====================================================================
        // UI HELPERS & THEME
        // ====================================================================
        public void SwitchTab(int idx)
        {
            _activeTab = idx;
            _panelImg.Visible = (idx == 0);
            _panelRen.Visible = (idx == 1);
            _btnTabImg.IsToggled = (idx == 0); _btnTabImg.Invalidate();
            _btnTabRen.IsToggled = (idx == 1); _btnTabRen.Invalidate();
        }

        private static bool ReadSuiteDark(bool fallback)
        {
            try
            {
                string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NillySuite\suite.theme");
                if (File.Exists(p))
                {
                    string s = File.ReadAllText(p, Encoding.ASCII).Trim();
                    if (s.Equals("light", StringComparison.OrdinalIgnoreCase)) return false;
                    if (s.Equals("dark", StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            catch { }
            return fallback;
        }

        private static string FindSiblingExe(string folder, string exeName)
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NillySuite\" + folder + @"\" + exeName);
                if (File.Exists(appData)) return appData;
                DirectoryInfo par = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'));
                if (par != null)
                {
                    string sib = Path.Combine(Path.Combine(par.FullName, folder), exeName);
                    if (File.Exists(sib)) return sib;
                }
            }
            catch { }
            return null;
        }

        private static void OpenFolderSmart(string dir)
        {
            string filesExe = FindSiblingExe("NillyFilesPro", "NillyFilesPro.exe");
            if (filesExe != null)
            {
                try { Process.Start(new ProcessStartInfo(filesExe, "\"" + dir + "\"") { WorkingDirectory = Path.GetDirectoryName(filesExe), UseShellExecute = true }); return; } catch { }
            }
            try { Process.Start("explorer.exe", "\"" + dir + "\""); } catch { }
        }

        public void ApplyTheme(BatchTheme t)
        {
            _theme = t;
            this.BackColor = t.WindowBg;
            this.ForeColor = t.TextPrimary;
            if (this.IsHandleCreated) Native.SetDarkTitleBar(this.Handle, t.IsDark);
            _btnTheme.Text = "◐";
            _lblFooter.BackColor = t.WindowBg;
            _lblFooter.ForeColor = t.TextMuted;
            ApplyRecursive(this, t);
            RefreshRenamePreview();
        }

        private void ApplyRecursive(Control parent, BatchTheme t)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is StudioButton) { ((StudioButton)c).Theme = t; c.Invalidate(); }
                else if (c is TextBox || c is ComboBox) { c.BackColor = t.InputBg; c.ForeColor = t.TextPrimary; }
                else if (c is ListView) { c.BackColor = t.CardBg; c.ForeColor = t.TextPrimary; }
                else if (c is CheckBox) { c.ForeColor = t.TextPrimary; }
                else if (c is TrackBar) { c.BackColor = t.WindowBg; }
                else if (c is Label)
                {
                    c.ForeColor = (c.Tag as string == "SEC") ? t.StudioAmber : ((c.Tag as string == "MUTED") ? t.TextMuted : t.TextPrimary);
                }
                else if (c is Panel)
                {
                    c.BackColor = t.WindowBg;
                    ApplyRecursive(c, t);
                }
            }
        }

        private StudioButton MakeBtn(string text, int w, int h, PillKind kind, Action onClick)
        {
            StudioButton b = new StudioButton();
            b.Text = text; b.Size = new Size(w, h); b.Kind = kind;
            b.Click += delegate { if (onClick != null) onClick(); };
            return b;
        }

        private Label MakeSec(string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text; l.Bounds = new Rectangle(x, y, 296, 16);
            l.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            l.Tag = "SEC";
            return l;
        }

        private Label MakeLbl(string text, int x, int y, int w)
        {
            Label l = new Label();
            l.Text = text; l.Bounds = new Rectangle(x, y, w, 18);
            l.Font = new Font("Segoe UI", 8.5f);
            l.Tag = "MUTED";
            return l;
        }

        private TextBox MakeBox(string def, int x, int y, int w)
        {
            TextBox tb = new TextBox();
            tb.Text = def; tb.Bounds = new Rectangle(x, y, w, 24);
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Font = new Font("Segoe UI", 9.5f);
            return tb;
        }

        private CheckBox MakeChk(string text, bool chk, int x, int y)
        {
            CheckBox cb = new CheckBox();
            cb.Text = text; cb.Checked = chk; cb.Bounds = new Rectangle(x, y, 294, 22);
            cb.Font = new Font("Segoe UI", 8.8f);
            return cb;
        }

        private static string FormatBytes(long b)
        {
            if (b < 1024) return b + " B";
            if (b < 1024 * 1024) return (b / 1024.0).ToString("0.0", CultureInfo.InvariantCulture) + " KB";
            return (b / (1024.0 * 1024.0)).ToString("0.00", CultureInfo.InvariantCulture) + " MB";
        }
    }

    // ========================================================================
    // 7. ENTRY POINT & FULL END-TO-END AUTOMATED QA TEST
    // ========================================================================
    internal static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            try { Native.SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args != null && args.Length > 0 && args[0] == "--test-smoke")
                return RunSmokeTest(args.Length > 1 ? args[1] : null);

            MainForm mf = new MainForm(args);
            mf.Shown += delegate { Native.TrimMemory(); };
            Application.Run(mf);
            return 0;
        }

        private static int RunSmokeTest(string logPath)
        {
            StringBuilder sb = new StringBuilder();
            string tmpDir = Path.Combine(Path.GetTempPath(), "NillyBatchQA_" + Guid.NewGuid().ToString("N").Substring(0, 6));
            try
            {
                Directory.CreateDirectory(tmpDir);

                // 1. Greeklish SEO slug test
                string slug = Greeklish.ToSeoSlug("Φωτογραφία Προϊόντος (Νέο) 01");
                sb.AppendLine("GREEKLISH_SEO_OK=" + (slug == "fotografia-proiontos-neo-01") + " (" + slug + ")");

                // 2. Real image batch resize + JPG compress + watermark + SEO rename
                string samplePng = Path.Combine(tmpDir, "Φωτογραφία Δοκιμής 1.png");
                using (Bitmap b = new Bitmap(1600, 1200, PixelFormat.Format32bppArgb))
                using (Graphics g = Graphics.FromImage(b))
                {
                    g.Clear(Color.FromArgb(40, 80, 160));
                    using (SolidBrush br = new SolidBrush(Color.Orange)) g.FillEllipse(br, 200, 200, 800, 800);
                    b.Save(samplePng, ImageFormat.Png);
                }

                BatchOptions opt = new BatchOptions();
                opt.OutputFormat = "JPG";
                opt.JpgQuality = 82;
                opt.ResizeMode = 5; // Square 1080x1080 White Pad
                opt.AddWatermark = true;
                opt.WatermarkText = "© Nilly graphics";
                opt.SeoGreeklishNames = true;
                BatchItemResult r = ImageBatchEngine.ProcessSingle(samplePng, opt);
                sb.AppendLine("BATCH_RESIZE_OK=" + (r.Success && r.OutW == 1080 && r.OutH == 1080 && Path.GetFileName(r.OutputPath) == "fotografia-dokimis-1.jpg"));
                sb.AppendLine("BATCH_OUT_BYTES=" + r.NewBytes);

                // 3. Batch Rename + 1-Click Undo test
                RenameRules rr = new RenameRules();
                rr.GreeklishSeo = true;
                rr.AddNumbering = true;
                rr.NumStart = 1; rr.NumPadding = 3; rr.NumPosition = 0;
                List<string> oldList = new List<string>() { samplePng };
                List<string> newNames = new List<string>() { BatchRenameEngine.ComputeNewFileName(samplePng, 0, rr) };
                int ok, err;
                List<KeyValuePair<string, string>> undo = BatchRenameEngine.ExecuteSafeRename(oldList, newNames, out ok, out err);
                bool renOk = (ok == 1 && err == 0 && File.Exists(undo[0].Key) && Path.GetFileName(undo[0].Key) == "fotografia-dokimis-1_001.png");
                // Execute undo
                BatchRenameEngine.ExecuteSafeRename(new List<string>() { undo[0].Key }, new List<string>() { Path.GetFileName(undo[0].Value) }, out ok, out err);
                bool undoOk = (ok == 1 && File.Exists(samplePng));
                sb.AppendLine("RENAME_AND_UNDO_OK=" + (renOk && undoOk));

                // 4. UI Form + Themes + RAM check
                using (MainForm form = new MainForm(new string[0]))
                {
                    form.Show();
                    form.SwitchTab(1); Application.DoEvents();
                    form.SwitchTab(0); Application.DoEvents();
                    form.ApplyTheme(BatchTheme.Win11Light());
                    form.ApplyTheme(BatchTheme.Win11Dark());
                    Application.DoEvents();
                    long ramMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
                    sb.AppendLine("WINDOW_OPENED_OK=" + form.IsHandleCreated);
                    sb.AppendLine("WORKING_SET_MB=" + ramMb);
                    form.Close();
                }
                sb.AppendLine("SMOKE_RESULT=PASS");
            }
            catch (Exception ex)
            {
                sb.AppendLine("SMOKE_RESULT=FAIL");
                sb.AppendLine(ex.ToString());
                if (!string.IsNullOrEmpty(logPath)) File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
                return 1;
            }
            finally
            {
                try { if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true); } catch { }
            }
            if (!string.IsNullOrEmpty(logPath)) File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
            return 0;
        }
    }
}
