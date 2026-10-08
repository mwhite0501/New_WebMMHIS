using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace NewMMHIS_Web.Services
{
    /// <summary>Bound from the optional "Imagery" section of appsettings.json; defaults match the old hard-coded values.</summary>
    public sealed class ImageryOptions
    {
        private static readonly string[] DefaultRoots = { @"\\mmhisdata-01\", @"\\sirapav-01\" };

        /// <summary>Servers tried, in order, when a stored path no longer exists as written.</summary>
        /// <remarks>No initializer: the config binder appends to arrays rather than replacing them.</remarks>
        public string[] Roots { get; set; }

        public string[] EffectiveRoots => Roots is { Length: > 0 } ? Roots : DefaultRoots;

        /// <summary>Memory budget for resized frames shared across users.</summary>
        public int ResizeCacheMegabytes { get; set; } = 256;
    }

    /// <summary>Finds frame files on the shares and produces reduced-size copies on request.</summary>
    public sealed class FrameImageService : IDisposable
    {
        /// <summary>Widths the client may ask for. 0 = original file, streamed untouched.</summary>
        public static readonly int[] AllowedWidths = { 0, 1920, 1280, 900, 600 };

        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };

        private readonly ImageryOptions _options;
        private readonly MemoryCache _resized;
        private readonly ImageCodecInfo _jpegCodec;
        private readonly EncoderParameters _jpegParams;

        public FrameImageService(IOptions<ImageryOptions> options, IWebHostEnvironment env)
        {
            _options = options.Value;
            PlaceholderPath = Path.Combine(env.WebRootPath, "Images", "No_Image_Available.jpg");
            _resized = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = Math.Max(16, _options.ResizeCacheMegabytes) * 1024L * 1024L,
            });
            _jpegCodec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            _jpegParams = new EncoderParameters(1);
            _jpegParams.Param[0] = new EncoderParameter(Encoder.Quality, 82L);
        }

        public string PlaceholderPath { get; }

        /// <summary>
        /// Turns a path from mmhis_fen into a file that exists, or null. Tries the path as stored,
        /// then the same share + folder under each configured root (old server names get remapped).
        /// </summary>
        public string Resolve(string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return null;
            var path = storedPath.Replace('/', '\\').Trim();
            if (!ImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)) return null;

            if (File.Exists(path)) return path;

            var relative = StripServer(path);
            if (string.IsNullOrEmpty(relative)) return null;

            foreach (var root in _options.EffectiveRoots)
            {
                var candidate = Path.Combine(root, relative);
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        /// <summary>JPEG bytes at the requested width (never upscaled). Cached by path + width.</summary>
        public byte[] GetResized(string path, int width)
        {
            var key = (path, width);
            if (_resized.TryGetValue(key, out byte[] cached)) return cached;

            byte[] bytes = Resize(path, width);
            _resized.Set(key, bytes, new MemoryCacheEntryOptions
            {
                Size = bytes.Length,
                SlidingExpiration = TimeSpan.FromMinutes(15),
            });
            return bytes;
        }

        private byte[] Resize(string path, int width)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024);
            using var source = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false);

            if (source.Width <= width)
            {
                stream.Position = 0;
                using var copy = new MemoryStream();
                stream.CopyTo(copy);
                return copy.ToArray();
            }

            int height = Math.Max(1, (int)Math.Round(source.Height * (width / (double)source.Width)));
            using var target = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(target))
            using (var wrap = new ImageAttributes())
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                g.SmoothingMode = SmoothingMode.None;
                wrap.SetWrapMode(WrapMode.TileFlipXY); // avoids a dark fringe on the edges
                g.DrawImage(source, new Rectangle(0, 0, width, height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, wrap);
            }

            using var output = new MemoryStream();
            target.Save(output, _jpegCodec, _jpegParams);
            return output.ToArray();
        }

        // "\\server\share\folder\file.jpg" -> "share\folder\file.jpg"; relative paths pass through.
        private static string StripServer(string path)
        {
            if (!path.StartsWith(@"\\")) return path.TrimStart('\\');
            int serverEnd = path.IndexOf('\\', 2);
            return serverEnd < 0 ? path.TrimStart('\\') : path.Substring(serverEnd + 1).TrimStart('\\');
        }

        public void Dispose()
        {
            _resized.Dispose();
            _jpegParams.Dispose();
        }
    }
}
