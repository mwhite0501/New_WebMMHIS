using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NewMMHIS_Web.Models;

namespace NewMMHIS_Web.Services
{
    /// <summary>Camera codes as stored in mmhis_fen.field_name.</summary>
    public static class Cameras
    {
        public const string Front = "f";
        public static readonly string[] All = { "f", "fr", "fl", "rr", "rl", "p" };
        public static bool IsValid(string code) => Array.IndexOf(All, code) >= 0;
    }

    /// <summary>
    /// Every frame of one run, in capture order (mmhis_dian.ld). Each frame carries its own
    /// logmeter and coordinates, so the image, logmile and map marker always agree.
    /// </summary>
    public sealed class RunFrames
    {
        public long RunId { get; init; }
        public long[] Ld { get; init; }
        public float[] Logmeter { get; init; }
        public float?[] Latitude { get; init; }
        public float?[] Longitude { get; init; }

        /// <summary>Raw image path per camera, indexed like <see cref="Ld"/>. Null where the camera has no frame.</summary>
        public Dictionary<string, string[]> PathsByCamera { get; init; }

        public Dictionary<long, int> IndexByLd { get; init; }

        public IEnumerable<string> AvailableCameras => Cameras.All.Where(PathsByCamera.ContainsKey);

        /// <summary>Resolved on-disk paths, filled lazily by the image endpoint. Only hits are cached.</summary>
        internal ConcurrentDictionary<(long ld, string camera), string> ResolvedPaths { get; } = new();

        public string RawPath(long ld, string camera) =>
            IndexByLd.TryGetValue(ld, out var i) && PathsByCamera.TryGetValue(camera, out var paths) ? paths[i] : null;
    }

    public sealed record NearestPoint(long RunId, long PointLd);

    public sealed class RunService
    {
        private static readonly TimeSpan RunCacheSliding = TimeSpan.FromMinutes(20);

        private readonly IDbContextFactory<mmhisContext> _dbFactory;
        private readonly IMemoryCache _cache;

        public RunService(IDbContextFactory<mmhisContext> dbFactory, IMemoryCache cache)
        {
            _dbFactory = dbFactory;
            _cache = cache;
        }

        /// <summary>Loads (or returns the cached) frames for a run. Null if the run has no points.</summary>
        public async Task<RunFrames> GetFramesAsync(long runId)
        {
            var key = ("run-frames", runId);
            var lazy = _cache.GetOrCreate(key, entry =>
            {
                entry.SlidingExpiration = RunCacheSliding;
                return new Lazy<Task<RunFrames>>(() => LoadFramesAsync(runId));
            });

            try
            {
                var frames = await lazy.Value;
                if (frames == null) _cache.Remove(key);
                return frames;
            }
            catch
            {
                _cache.Remove(key); // don't cache failures (e.g. SQL timeout)
                throw;
            }
        }

        private async Task<RunFrames> LoadFramesAsync(long runId)
        {
            await using var db = _dbFactory.CreateDbContext();

            var points = await db.MmhisDians.AsNoTracking()
                .Where(d => d.Lu == runId)
                .Select(d => new { d.Ld, d.Logmeter0, d.Latitude, d.Longitude })
                .ToListAsync();

            if (points.Count == 0) return null;

            points = points.GroupBy(p => p.Ld).Select(g => g.First()).OrderBy(p => p.Ld).ToList();

            var images = await db.MmhisFens.AsNoTracking()
                .Where(f => f.Lt == runId)
                .Select(f => new { f.Lu, f.FieldName, f.FieldValue })
                .ToListAsync();

            var indexByLd = new Dictionary<long, int>(points.Count);
            for (int i = 0; i < points.Count; i++) indexByLd[points[i].Ld] = i;

            var paths = new Dictionary<string, string[]>();
            foreach (var img in images)
            {
                var camera = img.FieldName?.Trim().ToLowerInvariant();
                if (camera == null || !Cameras.IsValid(camera) || string.IsNullOrWhiteSpace(img.FieldValue)) continue;
                if (!indexByLd.TryGetValue(img.Lu, out var idx)) continue;

                if (!paths.TryGetValue(camera, out var arr))
                    paths[camera] = arr = new string[points.Count];
                arr[idx] ??= img.FieldValue.Trim();
            }

            return new RunFrames
            {
                RunId = runId,
                Ld = points.Select(p => p.Ld).ToArray(),
                Logmeter = points.Select(p => p.Logmeter0).ToArray(),
                Latitude = points.Select(p => p.Latitude).ToArray(),
                Longitude = points.Select(p => p.Longitude).ToArray(),
                PathsByCamera = paths,
                IndexByLd = indexByLd,
            };
        }

        /// <summary>
        /// Nearest imagery to a map click. Searches a box that doubles in size until it finds points
        /// (up to ~5.7 km), then prefers the newest run among points within 15 m of the closest one,
        /// so overlapping years of the same road resolve to the latest capture.
        /// </summary>
        public async Task<NearestPoint> FindNearestAsync(double latitude, double longitude)
        {
            await using var db = _dbFactory.CreateDbContext();
            double metersPerDegLon = 111_320 * Math.Cos(latitude * Math.PI / 180);

            for (double box = 0.0001; box <= 0.052; box *= 2)
            {
                float latMin = (float)(latitude - box), latMax = (float)(latitude + box);
                float lonMin = (float)(longitude - box), lonMax = (float)(longitude + box);

                var candidates = await (
                        from p in db.MmhisDians.AsNoTracking()
                        where p.Latitude >= latMin && p.Latitude <= latMax
                           && p.Longitude >= lonMin && p.Longitude <= lonMax
                        join r in db.MmhisDamus.AsNoTracking() on p.Lu equals r.Ld
                        select new { RunId = r.Ld, PointLd = p.Ld, p.Latitude, p.Longitude, r.TheYear })
                    .Take(5000)
                    .ToListAsync();

                if (candidates.Count == 0) continue;

                var scored = candidates
                    .Where(c => c.Latitude.HasValue && c.Longitude.HasValue)
                    .Select(c =>
                    {
                        double dy = (c.Latitude.Value - latitude) * 110_574;
                        double dx = (c.Longitude.Value - longitude) * metersPerDegLon;
                        return new { c.RunId, c.PointLd, c.TheYear, Distance = Math.Sqrt(dx * dx + dy * dy) };
                    })
                    .ToList();
                if (scored.Count == 0) continue;

                double nearest = scored.Min(s => s.Distance);
                var best = scored
                    .Where(s => s.Distance <= nearest + 15)
                    .OrderByDescending(s => s.TheYear, StringComparer.Ordinal)
                    .ThenBy(s => s.Distance)
                    .First();

                return new NearestPoint(best.RunId, best.PointLd);
            }

            return null;
        }
    }
}
