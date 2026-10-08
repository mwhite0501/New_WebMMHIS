using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace NewMMHIS_Web.Services
{
    /// <summary>
    /// Plain HTTP endpoints the browser player uses. Images go over HTTP (cacheable, preloadable)
    /// instead of being base64-encoded into the Blazor render tree.
    /// </summary>
    public static class ImageryEndpoints
    {
        public static void MapImageryEndpoints(this IEndpointRouteBuilder endpoints)
        {
            // Frame list for a run: parallel arrays keep the payload small for 10k+ frame runs.
            endpoints.MapGet("api/runs/{runId:long}", async (long runId, RunService runs, HttpContext http, ILoggerFactory logs) =>
            {
                var frames = await runs.GetFramesAsync(runId);
                if (frames == null)
                {
                    logs.CreateLogger("MMHIS.Imagery").LogWarning("No mmhis_dian points with lu = {RunId}", runId);
                    return Results.NotFound(new { error = "No points recorded for this run.", runId = runId.ToString(CultureInfo.InvariantCulture) });
                }

                http.Response.Headers.CacheControl = "private, max-age=300";
                return Results.Json(new
                {
                    // ld values can exceed 2^53, so they travel as strings to survive JavaScript.
                    id = frames.RunId.ToString(CultureInfo.InvariantCulture),
                    cameras = frames.AvailableCameras.ToArray(),
                    ld = frames.Ld.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToArray(),
                    m = frames.Logmeter.Select(v => Math.Round(v, 2)).ToArray(),
                    lat = frames.Latitude.Select(v => v.HasValue ? Math.Round(v.Value, 6) : (double?)null).ToArray(),
                    lon = frames.Longitude.Select(v => v.HasValue ? Math.Round(v.Value, 6) : (double?)null).ToArray(),
                });
            });

            // One frame image. w = 0 streams the original file; other allowed widths are resized server-side.
            endpoints.MapGet("api/runs/{runId:long}/frames/{ld:long}/{camera}", async (
                long runId, long ld, string camera, int? w,
                RunService runs, FrameImageService images, HttpContext http) =>
            {
                camera = camera?.ToLowerInvariant();
                if (!Cameras.IsValid(camera)) return Results.BadRequest();
                int width = w ?? 0;
                if (Array.IndexOf(FrameImageService.AllowedWidths, width) < 0) return Results.BadRequest();

                var frames = await runs.GetFramesAsync(runId);
                if (frames == null || !frames.IndexByLd.ContainsKey(ld)) return Results.NotFound();

                string path = frames.ResolvedPaths.TryGetValue((ld, camera), out var hit) ? hit : null;
                if (path == null)
                {
                    path = await Task.Run(() => images.Resolve(frames.RawPath(ld, camera)));
                    if (path != null) frames.ResolvedPaths[(ld, camera)] = path;
                }

                bool missing = path == null;
                if (missing) path = images.PlaceholderPath;

                // Real frames never change; a missing one might show up once a share is back online.
                http.Response.Headers.CacheControl = missing ? "no-cache" : "public, max-age=604800, immutable";
                http.Response.Headers["X-Frame-Missing"] = missing ? "1" : "0";

                try
                {
                    if (width == 0)
                        return Results.File(path, ContentTypeFor(path));

                    var bytes = await Task.Run(() => images.GetResized(path, width));
                    return Results.File(bytes, "image/jpeg");
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is OutOfMemoryException)
                {
                    // Unreadable or corrupt file (GDI+ reports bad JPEGs as OutOfMemory).
                    http.Response.Headers.CacheControl = "no-cache";
                    http.Response.Headers["X-Frame-Missing"] = "1";
                    return Results.File(images.PlaceholderPath, "image/jpeg");
                }
            });
        }

        private static string ContentTypeFor(string path) =>
            Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".bmp" => "image/bmp",
                _ => "image/jpeg",
            };
    }
}
