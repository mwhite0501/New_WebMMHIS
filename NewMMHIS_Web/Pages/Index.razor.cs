using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NewMMHIS_Web.Services;

namespace NewMMHIS_Web.Pages
{
    /// <summary>
    /// Route picker and glue. Playback, images, map and Street View all run in the browser
    /// (wwwroot/js/mmhis.js); this component only resolves which run to show.
    /// </summary>
    public partial class Index : IAsyncDisposable
    {
        [Inject] private RunCatalog Catalog { get; set; }
        [Inject] private RunService Runs { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private IConfiguration Configuration { get; set; }
        [Inject] private ILogger<Index> Logger { get; set; }

        // Deep link: /{route}/{section}/{direction}/{logmile}/{year} (same shape as the original app).
        [Parameter] public string RouteId { get; set; }
        [Parameter] public string SectionId { get; set; }
        [Parameter] public string DirectionId { get; set; }
        [Parameter] public string SearchText { get; set; }
        [Parameter] public string YearId { get; set; }

        private static readonly string[] CountyNames =
        {
            "", "Arkansas", "Ashley", "Baxter", "Benton", "Boone", "Bradley", "Calhoun", "Carroll",
            "Chicot", "Clark", "Clay", "Cleburne", "Cleveland", "Columbia", "Conway", "Craighead",
            "Crawford", "Crittenden", "Cross", "Dallas", "Desha", "Drew", "Faulkner", "Franklin",
            "Fulton", "Garland", "Grant", "Greene", "Hempstead", "Hot Spring", "Howard", "Independence",
            "Izard", "Jackson", "Jefferson", "Johnson", "Lafayette", "Lawrence", "Lee", "Lincoln",
            "Little River", "Logan", "Lonoke", "Madison", "Marion", "Miller", "Mississippi", "Monroe",
            "Montgomery", "Nevada", "Newton", "Ouachita", "Perry", "Phillips", "Pike", "Poinsett",
            "Polk", "Pope", "Prairie", "Pulaski", "Randolph", "Saline", "Scott", "Searcy", "Sebastian",
            "Sevier", "Sharp", "St. Francis", "Stone", "Union", "Van Buren", "Washington", "White",
            "Woodruff", "Yell",
        };

        private CatalogSnapshot catalog;
        private string route, section, direction, year, logmileText;
        private IReadOnlyList<string> sections = Array.Empty<string>();
        private IReadOnlyList<string> directions = Array.Empty<string>();
        private IReadOnlyList<string> years = Array.Empty<string>();
        private RunInfo facts;      // county/district for the current route + section
        private RunInfo loadedRun;  // what the viewer is showing
        private string status;
        private bool statusIsError;
        private bool busy;
        private bool drawerOpen;      // phones: side panel slid in
        private bool panelCollapsed;  // desktop: side panel hidden
        private DotNetObjectReference<Index> selfRef;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                catalog = await Catalog.GetAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Loading the run catalog failed");
                SetStatus("Couldn't reach the MMHIS database. Reload the page to try again.", isError: true);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            selfRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("mmhis.init", selfRef, new
            {
                streetViewKey = Configuration["Keys:GoogleMapsJavascript"],
            });

            if (catalog == null) return;

            if (!string.IsNullOrWhiteSpace(RouteId))
            {
                await OpenAsync(RouteId, SectionId, DirectionId, YearId, SearchText, fromLink: true);
            }
            else
            {
                var saved = ReadSavedState(await JS.InvokeAsync<string>("mmhis.getSavedState"));
                if (saved != null)
                    await OpenAsync(saved.RouteId, saved.SectionId, saved.DirectionId, saved.YearId, saved.SearchText, fromLink: false);
            }
        }

        // ---- Dropdown cascade: each change re-picks everything below it ----------------------------

        private void OnRouteChanged(ChangeEventArgs e) => Cascade(e.Value?.ToString());
        private void OnSectionChanged(ChangeEventArgs e) => Cascade(route, e.Value?.ToString());
        private void OnDirectionChanged(ChangeEventArgs e) => Cascade(route, section, e.Value?.ToString());
        private void OnYearChanged(ChangeEventArgs e) => year = e.Value?.ToString();

        private void Cascade(string newRoute, string wantSection = null, string wantDirection = null, string wantYear = null)
        {
            status = null;
            route = string.IsNullOrEmpty(newRoute) ? null : newRoute;
            sections = route == null ? Array.Empty<string>() : catalog.SectionsFor(route);
            section = Pick(sections, wantSection);
            directions = section == null ? Array.Empty<string>() : catalog.DirectionsFor(route, section);
            direction = Pick(directions, wantDirection);
            years = direction == null ? Array.Empty<string>() : catalog.YearsFor(route, section, direction);
            year = Pick(years, wantYear);
            facts = section == null ? null : catalog.FirstFor(route, section);
        }

        private static string Pick(IReadOnlyList<string> options, string wanted) =>
            wanted != null && options.Contains(wanted) ? wanted : options.FirstOrDefault();

        // ---- Loading a run ------------------------------------------------------------------------

        private async Task ShowImageryAsync()
        {
            var run = catalog?.Find(route, section, direction, year);
            if (run == null)
            {
                SetStatus("There's no imagery for that route, section, direction and date.", isError: true);
                return;
            }

            double? startLogmile = null;
            if (!string.IsNullOrWhiteSpace(logmileText))
            {
                if (!TryParseMiles(logmileText, out var miles))
                {
                    SetStatus("Enter the log mile as a number, like 12.5.", isError: true);
                    return;
                }
                startLogmile = miles;
            }
            else if (loadedRun != null && loadedRun.Route == run.Route && loadedRun.Section == run.Section)
            {
                // Switching year or direction on the same road keeps your place, so captures are easy to compare.
                startLogmile = await JS.InvokeAsync<double?>("mmhis.currentLogmile");
            }

            if (await LoadAsync(run, startLogmile: startLogmile, startLd: null, fit: true))
                logmileText = null;
        }

        /// <summary>Applies a deep link or the last visited state, then loads it.</summary>
        private async Task OpenAsync(string r, string s, string d, string y, string logmile, bool fromLink)
        {
            if (!catalog.Routes.Contains(r))
            {
                if (fromLink) SetStatus($"Route {r} isn't in the imagery catalog.", isError: true);
                StateHasChanged();
                return;
            }

            Cascade(r, s, d, y);
            logmileText = TryParseMiles(logmile, out var miles) && miles > 0 ? miles.ToString(CultureInfo.InvariantCulture) : null;
            StateHasChanged();
            await ShowImageryAsync();
            StateHasChanged();
        }

        private async Task<bool> LoadAsync(RunInfo run, double? startLogmile, long? startLd, bool fit)
        {
            busy = true;
            status = null;
            StateHasChanged();

            try
            {
                // IDs go to the browser as strings: ld values can exceed 2^53, which JavaScript numbers round.
                var result = await JS.InvokeAsync<LoadResult>("mmhis.loadRun", new
                {
                    id = run.Id.ToString(CultureInfo.InvariantCulture),
                    route = run.Route,
                    section = run.Section,
                    direction = run.Direction,
                    year = run.Year,
                    note = string.IsNullOrWhiteSpace(run.Note) ? null : run.Note.Trim(),
                    startLogmile,
                    startLd = startLd?.ToString(CultureInfo.InvariantCulture),
                    fit,
                });

                if (result?.Ok != true)
                {
                    SetStatus(result?.Error ?? "The imagery didn't load. Try again.", isError: true);
                    return false;
                }

                loadedRun = run;
                drawerOpen = false;
                return true;
            }
            catch (JSException ex)
            {
                Logger.LogWarning(ex, "Loading run {RunId} failed in the browser", run.Id);
                SetStatus("The imagery didn't load. Try again.", isError: true);
                return false;
            }
            finally
            {
                busy = false;
                StateHasChanged();
            }
        }

        /// <summary>Called by the map on double-click.</summary>
        [JSInvokable]
        public async Task<bool> OnMapDoubleClick(double latitude, double longitude)
        {
            if (catalog == null) return false;

            SetStatus("Finding the nearest imagery…", isError: false);
            busy = true;
            StateHasChanged();

            try
            {
                var hit = await Runs.FindNearestAsync(latitude, longitude);
                var run = hit == null ? null : await Catalog.FindByIdAsync(hit.RunId);
                if (run == null)
                {
                    busy = false;
                    SetStatus("There's no imagery within about 5 km of that spot.", isError: true);
                    return false;
                }

                catalog = await Catalog.GetAsync();
                Cascade(run.Route, run.Section, run.Direction, run.Year);
                logmileText = null;
                return await LoadAsync(run, startLogmile: null, startLd: hit.PointLd, fit: false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Nearest-imagery lookup failed at {Lat},{Lon}", latitude, longitude);
                busy = false;
                SetStatus("The nearest-imagery lookup failed. Try again.", isError: true);
                return false;
            }
            finally
            {
                StateHasChanged();
            }
        }

        private async Task ResetAsync()
        {
            await JS.InvokeVoidAsync("mmhis.reset");
            Cascade(null);
            logmileText = null;
            loadedRun = null;
            status = null;
        }

        // One button: slides the drawer on phones, collapses the panel on desktop for a bigger viewer.
        private async Task TogglePanelAsync()
        {
            if (await JS.InvokeAsync<bool>("mmhis.isNarrow")) drawerOpen = !drawerOpen;
            else panelCollapsed = !panelCollapsed;
        }

        // ---- Helpers ------------------------------------------------------------------------------

        private void SetStatus(string message, bool isError)
        {
            status = message;
            statusIsError = isError;
        }

        private static bool TryParseMiles(string text, out double miles) =>
            double.TryParse(text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out miles) && miles >= 0;

        private static string CountyName(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "Not recorded";
            return int.TryParse(code, out var n) && n > 0 && n < CountyNames.Length
                ? $"{CountyNames[n]} ({code.Trim()})"
                : code;
        }

        private static SavedState ReadSavedState(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var state = JsonSerializer.Deserialize<SavedState>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return string.IsNullOrWhiteSpace(state?.RouteId) ? null : state;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (selfRef == null) return; // prerender pass: never reached the browser
            try
            {
                await JS.InvokeVoidAsync("mmhis.detach");
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            selfRef?.Dispose();
        }

        /// <summary>Shape written to localStorage["lastVisitedState"]; unchanged from the original app.</summary>
        private sealed class SavedState
        {
            public string RouteId { get; set; }
            public string SectionId { get; set; }
            public string DirectionId { get; set; }
            public string SearchText { get; set; }
            public string YearId { get; set; }
        }

        private sealed class LoadResult
        {
            public bool Ok { get; set; }
            public string Error { get; set; }
        }
    }
}
