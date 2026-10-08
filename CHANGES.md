# MMHIS viewer rework

## What changed

### Playback and images
- Frames are served by plain HTTP endpoints instead of being base64-encoded into the Blazor render tree:
  - `GET api/runs/{runId}` returns the frame list (ld, logmeter, lat, lon, available cameras), gzip-compressed.
  - `GET api/runs/{runId}/frames/{ld}/{camera}?w=` returns one image. `w` omitted = original file streamed untouched; `w=1920|1280|900|600` = resized server-side and cached in memory.
  - Real frames are sent `Cache-Control: immutable` (7 days), so revisiting a stretch costs nothing. Missing frames return the placeholder with `no-cache`, so they show up once a share comes back.
  - Only paths stored in `mmhis_fen` for that run can be served. There is no path parameter.
- Playback runs in the browser (`wwwroot/js/mmhis.js`). It preloads 6 frames ahead and waits for each image instead of skipping. Nothing crosses the SignalR connection per frame.
- Contrast and brightness are CSS filters: instant, with no server work. The old slider multiplied RGB, which acted as brightness.
- Run and point IDs (`ld`) travel to the browser as strings. They can exceed 2^53, which JavaScript numbers silently round.
- Each frame carries its own logmeter and coordinates (ordered by capture order, `mmhis_dian.ld`), so the image, log mile, and map marker always agree. That covers direction-B runs where logmiles decrease.

### Data
- `MmhisController`, `PageModel`, and `DataModel` are replaced by `Services/`:
  - `RunCatalog` keeps every `mmhis_damu` row in memory (refreshed every 30 min), so the dropdowns don't hit SQL.
  - `RunService` loads a run with 2 queries (points + images) and caches it for 20 min sliding. It previously re-enumerated `IQueryable`s about 15 times per load.
  - The map double-click lookup searches a box that doubles in size (up to about 5.7 km), then picks the **newest** run among points within 15 m of the closest one.
- `DbContext` is registered with `AddDbContextFactory` (a short-lived context per operation). It no longer reads `appsettings.json` from disk on every construction.

### UI
- New layout: a side panel (collapsible on desktop, a drawer on phones), the viewer, and a map pane with Map / Street View tabs.
- The playback bar is always visible (it used to be hover-only), with a scrubber, start/end log miles, a frame count, and speed.
- Keyboard: Space play/pause, Shift+Space reverse, ←/→ step (Shift = 10), Home/End, 1–6 cameras, F full screen.
- The run is drawn on the map. Clicking the line jumps to that point, and an option keeps the marker in view.
- Street View faces the direction of travel and updates when paused. It used to always face north.
- **Copy link** button. The address bar always holds a shareable deep link in the existing format `/{route}/{section}/{direction}/{logmile}/{year}`.
- Deep links now win over the last-visited state, which used to override them, and the year in the link is honored.
- Changing year or direction on the same road keeps your log mile, so captures are easy to compare.
- Display preferences (camera, image size, speed, contrast, brightness) are remembered per browser.
- Overpass font (OFL) is self-hosted in `wwwroot/fonts`, so there is no external font dependency.

### Removed
Template pages (Counter, FetchData, WeatherForecast, NavMenu, SurveyPrompt), the 9.4k-line orphaned jQuery `script.js` at the project root, about 150 lines of commented-out `UseFileServer` blocks, Bootstrap, Open Iconic, Radzen, unused `Resources.resx`, and these packages: EntityFramework 6, ImageResizer, Microsoft.SqlServer.Types, CodeGeneration.Design, Newtonsoft.Json, Radzen.Blazor.

## Deploying
- Same target (**net6.0**, EF Core 5.0.10) and same publish profiles. No server runtime change is needed.
- `appsettings.json` has a new **optional** `Imagery` section (share roots tried when a stored path is missing, plus the resize cache size). The defaults match the old hard-coded values, so an existing server config keeps working without it.
- `css/site.css` and `js/mmhis.js` are cache-busted with `asp-append-version`.
- Basemap choices now use current Esri ids (`topo-vector`, `gray-vector`, `dark-gray-vector`). The retired Oceans and National Geographic basemaps were dropped.

## Worth doing next
- **Upgrade to .NET 10 LTS.** .NET 6 and EF Core 5 are out of support. When upgrading EF Core past 6, SqlClient defaults to `Encrypt=True`, so add `TrustServerCertificate=True` (or a trusted cert) to the connection string.
- `appsettings.json` is still tracked in git despite `.gitignore`, and it contains the Google Maps and ArcGIS keys. Run `git rm --cached` on it. Also make sure the Google key is HTTP-referrer restricted, since Street View exposes it to the browser by design.
- The site is published to `http://`. On HTTPS, Copy link uses the modern clipboard API (a fallback is in place for HTTP).
