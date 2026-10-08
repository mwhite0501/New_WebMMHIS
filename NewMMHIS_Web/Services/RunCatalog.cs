using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NewMMHIS_Web.Models;

namespace NewMMHIS_Web.Services
{
    /// <summary>One imagery run: a route/section/direction captured in a given year (a row in mmhis_damu).</summary>
    public sealed record RunInfo(
        long Id,
        string Route,
        string Section,
        string Direction,
        string Year,
        string County,
        string District,
        string Note);

    /// <summary>
    /// In-memory copy of every run in mmhis_damu. The dropdowns cascade off this instead of
    /// hitting SQL on every change. Refreshed every <see cref="RefreshInterval"/>.
    /// </summary>
    public sealed class RunCatalog
    {
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(30);

        private readonly IDbContextFactory<mmhisContext> _dbFactory;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private CatalogSnapshot _snapshot;
        private DateTime _loadedUtc;

        public RunCatalog(IDbContextFactory<mmhisContext> dbFactory) => _dbFactory = dbFactory;

        public async Task<CatalogSnapshot> GetAsync(bool forceRefresh = false)
        {
            var current = _snapshot;
            if (!forceRefresh && current != null && DateTime.UtcNow - _loadedUtc < RefreshInterval)
                return current;

            await _gate.WaitAsync();
            try
            {
                if (!forceRefresh && _snapshot != null && DateTime.UtcNow - _loadedUtc < RefreshInterval)
                    return _snapshot;

                await using var db = _dbFactory.CreateDbContext();
                var rows = await db.MmhisDamus.AsNoTracking()
                    .Select(r => new RunInfo(r.Ld, r.Route, r.Section, r.MmhisDirection, r.TheYear, r.County, r.District, r.Note))
                    .ToListAsync();

                _snapshot = new CatalogSnapshot(rows);
                _loadedUtc = DateTime.UtcNow;
                return _snapshot;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>Finds a run by id, refreshing the catalog once if it was added since the last load.</summary>
        public async Task<RunInfo> FindByIdAsync(long id)
        {
            var snap = await GetAsync();
            if (snap.TryGet(id, out var run)) return run;
            snap = await GetAsync(forceRefresh: true);
            return snap.TryGet(id, out run) ? run : null;
        }
    }

    public sealed class CatalogSnapshot
    {
        private readonly List<RunInfo> _runs;
        private readonly Dictionary<long, RunInfo> _byId;

        public CatalogSnapshot(List<RunInfo> runs)
        {
            _runs = runs;
            _byId = new Dictionary<long, RunInfo>(runs.Count);
            foreach (var r in runs) _byId.TryAdd(r.Id, r);
            Routes = NaturalDistinct(runs.Select(r => r.Route));
        }

        public IReadOnlyList<string> Routes { get; }

        public bool TryGet(long id, out RunInfo run) => _byId.TryGetValue(id, out run);

        public IReadOnlyList<string> SectionsFor(string route) =>
            NaturalDistinct(_runs.Where(r => r.Route == route).Select(r => r.Section));

        public IReadOnlyList<string> DirectionsFor(string route, string section) =>
            NaturalDistinct(_runs.Where(r => r.Route == route && r.Section == section).Select(r => r.Direction));

        /// <summary>Newest first.</summary>
        public IReadOnlyList<string> YearsFor(string route, string section, string direction) =>
            _runs.Where(r => r.Route == route && r.Section == section && r.Direction == direction)
                 .Select(r => r.Year)
                 .Where(y => !string.IsNullOrWhiteSpace(y))
                 .Distinct()
                 .OrderByDescending(y => y, StringComparer.Ordinal)
                 .ToList();

        public RunInfo Find(string route, string section, string direction, string year) =>
            _runs.FirstOrDefault(r => r.Route == route && r.Section == section && r.Direction == direction && r.Year == year);

        /// <summary>First run on a route + section; used for the county/district readout.</summary>
        public RunInfo FirstFor(string route, string section) =>
            _runs.FirstOrDefault(r => r.Route == route && r.Section == section);

        // Shorter strings first, then alphabetical: "2", "10", "10S", "540" instead of "10", "10S", "2", "540".
        private static IReadOnlyList<string> NaturalDistinct(IEnumerable<string> values) =>
            values.Where(v => !string.IsNullOrWhiteSpace(v))
                  .Distinct()
                  .OrderBy(v => v.Length)
                  .ThenBy(v => v, StringComparer.Ordinal)
                  .ToList();
    }
}
