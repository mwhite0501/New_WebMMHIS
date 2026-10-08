/*
 * MMHIS browser player.
 *
 * Owns everything that changes per frame so nothing per-frame crosses the Blazor connection:
 *   - fetches a run's frame list once (api/runs/{id}) and images over plain HTTP (cacheable)
 *   - plays forward/backward, waiting for each image instead of skipping, and preloads ahead
 *   - updates the log mile plate, scrubber, coordinates and map marker directly
 *   - draws the run on the ArcGIS map and keeps Street View in step when paused
 *   - writes the shareable URL and localStorage["lastVisitedState"] when the position settles
 *
 * Blazor calls: init, loadRun, reset, currentLogmile, getSavedState, detach, isNarrow.
 * Blazor is called back for: OnMapDoubleClick(lat, lon).
 */
(function () {
    'use strict';

    const CAMERAS = ['f', 'fl', 'fr', 'rl', 'rr', 'p'];  // order of keys 1-6
    const SAVED_STATE_KEY = 'lastVisitedState';           // same key/shape as the original app
    const PREFS_KEY = 'mmhis.display';
    const METERS_TO_MILES = 0.000621371;
    const PRELOAD_AHEAD = 6;
    const MAX_IN_FLIGHT = 4;
    const CACHE_LIMIT = 40;
    const SETTLE_MS = 450;

    const s = {
        dotnet: null,
        run: null,          // { id, route, section, direction, year, note }
        frames: null,       // { id, cameras[], ld[], m[], lat[], lon[] }
        ldIndex: null,      // Map ld (string) -> index. IDs stay strings: they can exceed 2^53.
        idx: 0,
        preferredCamera: 'f',
        camera: 'f',
        width: 0,
        speed: 220,
        contrast: 100,
        brightness: 100,
        follow: true,
        dir: 0,             // 1 playing forward, -1 backward, 0 paused
        playToken: 0,
        showToken: 0,
        inFlight: 0,
        cache: new Map(),   // request url -> Promise<{ url, missing }>
        liveUrls: new Set(),// object URLs still referenced by the cache
        shownUrl: null,
        settleTimer: 0,
        scrubTimer: 0,
        loadingTimer: 0,
        streetKey: null,
        streetLast: null,
        tab: 'map',
        wheelAccum: 0,
        initialized: false,
    };

    const el = {};
    const map = { view: null, routeLayer: null, marker: null, Graphic: null, Polyline: null, Point: null, lastFollow: 0 };

    // ---------------------------------------------------------------- setup

    function init(dotnetRef, options) {
        s.dotnet = dotnetRef;
        s.streetKey = (options && options.streetViewKey) || null;
        if (s.initialized) return;   // reconnects re-run init; DOM listeners are already attached
        s.initialized = true;

        [
            'viewer', 'frame', 'frame-img', 'frame-note', 'frame-missing', 'frame-loading', 'milemarker', 'mm-value',
            'scrubber', 'scrub-start', 'scrub-end', 'frame-count', 'speed', 'coords', 'copy-link',
            'img-size', 'contrast', 'contrast-out', 'brightness', 'brightness-out', 'display-reset',
            'tab-map', 'tab-street', 'map-body', 'streetview', 'streetview-frame', 'follow', 'map-unavailable',
        ].forEach(id => { el[camel(id)] = document.getElementById(id); });
        el.buttons = Array.from(document.querySelectorAll('.transport [data-action]'));
        el.cameraButtons = Array.from(document.querySelectorAll('.cameras [data-camera]'));

        loadPrefs();
        wireViewer();
        wireDisplay();
        wireTabs();
        wireKeyboard();
        initMap();
        if (s.streetKey) el.tabStreet.hidden = false;
        setControlsEnabled(false);
    }

    function detach() {
        pause();
        clearTimeout(s.settleTimer);
        clearTimeout(s.scrubTimer);
        clearTimeout(s.loadingTimer);
    }

    // ---------------------------------------------------------------- run loading

    async function loadRun(req) {
        pause();
        const runId = String(req.id);
        const startLd = req.startLd == null ? null : String(req.startLd);
        const sameRun = s.frames && String(s.frames.id) === runId;
        setLoading(true);
        try {
            if (!sameRun) {
                const res = await fetch(url(`api/runs/${encodeURIComponent(runId)}`), { credentials: 'same-origin' });
                if (res.status === 404) return { ok: false, error: `Run ${runId} has no points in mmhis_dian, so there are no frames to show.` };
                if (!res.ok) return { ok: false, error: `Loading the frame list failed (HTTP ${res.status}).` };
                const data = await res.json();
                if (!data.ld || data.ld.length === 0) return { ok: false, error: 'That run has no frames recorded.' };
                data.id = String(data.id);
                data.ld = data.ld.map(String);
                s.frames = data;
                s.ldIndex = new Map(data.ld.map((ld, i) => [ld, i]));
                clearImageCache();
            }

            s.run = { id: runId, route: req.route, section: req.section, direction: req.direction, year: req.year, note: req.note };
            const n = s.frames.ld.length;

            let start = 0;
            if (startLd != null && s.ldIndex.has(startLd)) start = s.ldIndex.get(startLd);
            else if (req.startLogmile != null) start = nearestByLogmile(req.startLogmile);

            applyCameraAvailability();
            el.frameNote.textContent = req.note || '';
            el.frameNote.hidden = !req.note;
            el.scrubber.max = String(n - 1);
            el.scrubStart.textContent = 'Start ' + miles(0).toFixed(3);
            el.scrubEnd.textContent = 'End ' + miles(n - 1).toFixed(3);
            el.viewer.classList.remove('is-empty');
            el.milemarker.hidden = false;
            el.copyLink.disabled = false;
            setControlsEnabled(true);
            drawRoute(req.fit && !sameRun);

            await show(start);
            preload(start, 1);
            settleSoon();
            return { ok: true };
        } catch (err) {
            console.error('mmhis: loadRun failed', err);
            return { ok: false, error: 'The frame list didn\'t load. Check your connection and try again.' };
        } finally {
            setLoading(false);
        }
    }

    function reset() {
        pause();
        s.run = null;
        s.frames = null;
        s.ldIndex = null;
        s.idx = 0;
        clearImageCache();
        el.frameImg.removeAttribute('src');
        el.viewer.classList.add('is-empty');
        el.milemarker.hidden = true;
        el.frameNote.hidden = true;
        setMissing(null);
        el.scrubber.value = '0';
        el.scrubber.max = '0';
        el.scrubber.style.setProperty('--pct', '0%');
        el.scrubStart.textContent = el.scrubEnd.textContent = el.frameCount.textContent = el.coords.textContent = '';
        el.copyLink.disabled = true;
        setControlsEnabled(false);
        if (map.routeLayer) map.routeLayer.removeAll();
        if (map.marker) map.marker.visible = false;
        try { localStorage.removeItem(SAVED_STATE_KEY); } catch (e) { /* storage blocked */ }
        history.replaceState(history.state, '', document.baseURI);
    }

    function currentLogmile() {
        return s.frames ? round3(miles(s.idx)) : null;
    }

    function getSavedState() {
        try { return localStorage.getItem(SAVED_STATE_KEY); } catch (e) { return null; }
    }

    // ---------------------------------------------------------------- frames & images

    function frameUrl(i, camera) {
        const q = s.width ? `?w=${s.width}` : '';
        return url(`api/runs/${s.frames.id}/frames/${s.frames.ld[i]}/${camera || s.camera}${q}`);
    }

    function getImage(src) {
        let entry = s.cache.get(src);
        if (entry) {
            s.cache.delete(src);   // re-insert to mark as recently used
            s.cache.set(src, entry);
            return entry;
        }
        s.inFlight++;
        entry = fetch(src, { credentials: 'same-origin' })
            .then(async res => {
                if (!res.ok) throw new Error('HTTP ' + res.status);
                const blob = await res.blob();
                const objectUrl = URL.createObjectURL(blob);
                s.liveUrls.add(objectUrl);
                const img = new Image();
                img.src = objectUrl;
                try { await img.decode(); } catch (e) { /* still displayable */ }
                return { url: objectUrl, missing: res.headers.get('X-Frame-Missing') === '1' };
            })
            .finally(() => { s.inFlight--; });
        entry.catch(() => s.cache.delete(src));
        s.cache.set(src, entry);
        trimCache();
        return entry;
    }

    function trimCache() {
        while (s.cache.size > CACHE_LIMIT) {
            const [oldest, promise] = s.cache.entries().next().value;
            s.cache.delete(oldest);
            promise.then(release).catch(() => { });
        }
    }

    function clearImageCache() {
        for (const p of s.cache.values()) p.then(release).catch(() => { });
        s.cache.clear();
    }

    // Frees an evicted image unless it's on screen; show() frees that one when it's replaced.
    function release(result) {
        s.liveUrls.delete(result.url);
        if (result.url !== s.shownUrl) URL.revokeObjectURL(result.url);
    }

    function preload(from, dir) {
        if (!s.frames) return;
        const n = s.frames.ld.length;
        for (let k = 1; k <= PRELOAD_AHEAD && s.inFlight < MAX_IN_FLIGHT; k++) {
            const i = from + dir * k;
            if (i < 0 || i >= n) break;
            const src = frameUrl(i);
            if (!s.cache.has(src)) getImage(src);
        }
    }

    /** Moves to frame i. Readouts update immediately; the image swaps in once it has loaded. */
    async function show(i, opts) {
        if (!s.frames) return false;
        const n = s.frames.ld.length;
        i = Math.max(0, Math.min(n - 1, i));
        const token = ++s.showToken;
        s.idx = i;
        updateReadouts();
        updateMarker();
        if (opts && opts.deferImage) return true;

        // Spinner only for slow single-frame moves; during playback the last frame just holds.
        clearTimeout(s.loadingTimer);
        if (s.dir === 0) s.loadingTimer = setTimeout(() => { if (token === s.showToken) setLoading(true); }, 250);
        try {
            const result = await getImage(frameUrl(i));
            if (token !== s.showToken) return false;
            const previous = s.shownUrl;
            s.shownUrl = result.url;
            el.frameImg.src = result.url;
            setMissing(result.missing ? 'No image from this camera at this point' : null);
            if (previous && previous !== result.url && !s.liveUrls.has(previous)) URL.revokeObjectURL(previous);
            return true;
        } catch (err) {
            if (token === s.showToken) setMissing('This frame didn\'t load. Step again to retry.');
            return false;
        } finally {
            if (token === s.showToken) {
                clearTimeout(s.loadingTimer);
                setLoading(false);
            }
        }
    }

    function setMissing(message) {
        el.frameMissing.hidden = !message;
        if (message) el.frameMissing.textContent = message;
    }

    // ---------------------------------------------------------------- playback

    function play(dir) {
        if (!s.frames) return;
        if (s.dir === dir) { pause(); return; }
        s.dir = dir;
        const token = ++s.playToken;
        updatePlayButtons();
        loop(dir, token);
    }

    async function loop(dir, token) {
        const n = s.frames.ld.length;
        while (s.playToken === token && s.frames) {
            const next = s.idx + dir;
            if (next < 0 || next >= n) { pause(); break; }
            const started = performance.now();
            preload(next, dir);
            await show(next);
            if (s.playToken !== token) break;
            const wait = s.speed - (performance.now() - started);
            if (wait > 0) await sleep(wait);
        }
    }

    function pause() {
        if (s.dir === 0) return;
        s.dir = 0;
        s.playToken++;
        updatePlayButtons();
        settleSoon();
    }

    function step(delta) {
        if (!s.frames) return;
        pause();
        show(s.idx + delta);
        preload(s.idx, Math.sign(delta) || 1);
        settleSoon();
    }

    function seek(i) {
        if (!s.frames) return;
        show(i);
        preload(i, s.dir || 1);
        settleSoon();
    }

    function updatePlayButtons() {
        const fwd = el.buttons.find(b => b.dataset.action === 'forward');
        const rev = el.buttons.find(b => b.dataset.action === 'reverse');
        fwd.setAttribute('aria-pressed', String(s.dir === 1));
        fwd.setAttribute('aria-label', s.dir === 1 ? 'Pause' : 'Play');
        rev.setAttribute('aria-pressed', String(s.dir === -1));
        rev.setAttribute('aria-label', s.dir === -1 ? 'Pause' : 'Play backward');
    }

    // ---------------------------------------------------------------- readouts

    function miles(i) { return s.frames.m[i] * METERS_TO_MILES; }

    function nearestByLogmile(target) {
        const m = s.frames.m;
        let best = 0, bestDiff = Infinity;
        for (let i = 0; i < m.length; i++) {
            const d = Math.abs(m[i] * METERS_TO_MILES - target);
            if (d < bestDiff) { bestDiff = d; best = i; }
        }
        return best;
    }

    function updateReadouts() {
        const i = s.idx, n = s.frames.ld.length;
        el.mmValue.textContent = miles(i).toFixed(3);
        el.scrubber.value = String(i);
        el.scrubber.style.setProperty('--pct', n > 1 ? (i / (n - 1) * 100) + '%' : '0%');
        el.frameCount.textContent = `Frame ${(i + 1).toLocaleString()} of ${n.toLocaleString()}`;
        const lat = s.frames.lat[i], lon = s.frames.lon[i];
        el.coords.textContent = lat != null && lon != null ? `${lat.toFixed(6)}, ${lon.toFixed(6)}` : 'No coordinates for this frame';
    }

    // ---------------------------------------------------------------- settle: URL, saved state, Street View

    function settleSoon() {
        clearTimeout(s.settleTimer);
        s.settleTimer = setTimeout(settle, SETTLE_MS);
    }

    function settle() {
        if (!s.run || !s.frames || s.dir !== 0) return;
        const lm = round3(miles(s.idx)).toFixed(3);
        const path = [s.run.route, s.run.section, s.run.direction, lm, s.run.year].map(v => encodeURIComponent(v ?? '')).join('/');
        history.replaceState(history.state, '', new URL(path, document.baseURI).toString());
        try {
            localStorage.setItem(SAVED_STATE_KEY, JSON.stringify({
                RouteId: s.run.route, SectionId: s.run.section, DirectionId: s.run.direction,
                SearchText: lm, YearId: s.run.year, Log: lm,
            }));
        } catch (e) { /* storage blocked */ }
        updateStreetView();
    }

    function updateStreetView() {
        if (!s.streetKey || s.tab !== 'street' || !s.frames) return;
        const i = s.idx, lat = s.frames.lat[i], lon = s.frames.lon[i];
        if (lat == null || lon == null) return;
        if (s.streetLast && distanceMeters(s.streetLast, [lat, lon]) < 8) return;
        s.streetLast = [lat, lon];
        const heading = Math.round(headingAt(i));
        el.streetviewFrame.src = 'https://www.google.com/maps/embed/v1/streetview' +
            `?key=${encodeURIComponent(s.streetKey)}&location=${lat},${lon}&heading=${heading}&pitch=0&fov=90`;
    }

    /** Direction of travel at frame i, from the neighbouring frames' coordinates. */
    function headingAt(i) {
        const { lat, lon } = s.frames, n = lat.length;
        const a = i < n - 1 ? i : i - 1, b = a + 1;
        if (a < 0 || lat[a] == null || lat[b] == null) return 0;
        const toRad = Math.PI / 180;
        const y = Math.sin((lon[b] - lon[a]) * toRad) * Math.cos(lat[b] * toRad);
        const x = Math.cos(lat[a] * toRad) * Math.sin(lat[b] * toRad) -
            Math.sin(lat[a] * toRad) * Math.cos(lat[b] * toRad) * Math.cos((lon[b] - lon[a]) * toRad);
        return (Math.atan2(y, x) / toRad + 360) % 360;
    }

    // ---------------------------------------------------------------- viewer controls

    function wireViewer() {
        el.buttons.forEach(btn => btn.addEventListener('click', () => {
            switch (btn.dataset.action) {
                case 'step-back': step(-1); break;
                case 'step-forward': step(1); break;
                case 'forward': play(1); break;
                case 'reverse': play(-1); break;
                case 'fullscreen': toggleFullscreen(); break;
            }
        }));

        // Scrubbing: readouts and marker follow the thumb; images load at most every 90 ms.
        el.scrubber.addEventListener('input', () => {
            if (!s.frames) return;
            pause();
            const i = Number(el.scrubber.value);
            show(i, { deferImage: true });
            clearTimeout(s.scrubTimer);
            s.scrubTimer = setTimeout(() => seek(Number(el.scrubber.value)), 90);
        });
        el.scrubber.addEventListener('change', () => seek(Number(el.scrubber.value)));

        el.speed.addEventListener('change', () => { s.speed = Number(el.speed.value); savePrefs(); });

        el.cameraButtons.forEach(btn => btn.addEventListener('click', () => setCamera(btn.dataset.camera)));

        // Mouse wheel / trackpad over the image steps through frames (up = forward, as before).
        el.frame.addEventListener('wheel', e => {
            if (!s.frames) return;
            e.preventDefault();
            s.wheelAccum += e.deltaMode === 1 ? e.deltaY * 16 : e.deltaY;
            const steps = Math.trunc(s.wheelAccum / 40);
            if (steps !== 0) {
                s.wheelAccum -= steps * 40;
                step(-steps);
            }
        }, { passive: false });

        el.copyLink.addEventListener('click', copyLink);
    }

    function setCamera(camera) {
        if (!CAMERAS.includes(camera)) return;
        if (s.frames && !s.frames.cameras.includes(camera)) return;
        s.preferredCamera = camera;
        s.camera = camera;
        markCamera();
        savePrefs();
        if (s.frames) { show(s.idx); preload(s.idx, s.dir || 1); }
    }

    function applyCameraAvailability() {
        const available = s.frames.cameras;
        el.cameraButtons.forEach(btn => {
            const ok = available.includes(btn.dataset.camera);
            btn.disabled = !ok;
            btn.title = ok ? '' : 'No images from this camera on this run';
        });
        s.camera = available.includes(s.preferredCamera) ? s.preferredCamera
            : available.includes('f') ? 'f' : (available[0] || 'f');
        markCamera();
    }

    function markCamera() {
        el.cameraButtons.forEach(btn => btn.setAttribute('aria-pressed', String(btn.dataset.camera === s.camera)));
    }

    function setControlsEnabled(on) {
        el.buttons.forEach(b => { if (b.dataset.action !== 'fullscreen') b.disabled = !on; });
        el.scrubber.disabled = !on;
        if (!on) el.cameraButtons.forEach(b => { b.disabled = false; b.title = ''; });
    }

    function setLoading(on) {
        el.frameLoading.hidden = !on;
    }

    function toggleFullscreen() {
        const doc = document;
        if (doc.fullscreenElement || doc.webkitFullscreenElement) {
            (doc.exitFullscreen || doc.webkitExitFullscreen).call(doc);
        } else {
            const v = el.viewer;
            (v.requestFullscreen || v.webkitRequestFullscreen).call(v);
        }
    }

    async function copyLink() {
        settle();
        const link = location.href;
        let ok = false;
        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(link);
                ok = true;
            }
        } catch (e) { /* fall through */ }
        if (!ok) {
            // Clipboard API needs HTTPS; fall back for http:// intranet hosts.
            const ta = document.createElement('textarea');
            ta.value = link;
            ta.setAttribute('readonly', '');
            ta.style.position = 'fixed';
            ta.style.opacity = '0';
            document.body.appendChild(ta);
            ta.select();
            try { ok = document.execCommand('copy'); } catch (e) { ok = false; }
            ta.remove();
        }
        const label = el.copyLink.textContent;
        el.copyLink.textContent = ok ? 'Link copied' : 'Copy failed';
        el.copyLink.classList.toggle('is-done', ok);
        setTimeout(() => { el.copyLink.textContent = label; el.copyLink.classList.remove('is-done'); }, 1600);
    }

    // ---------------------------------------------------------------- display settings

    function wireDisplay() {
        el.imgSize.addEventListener('change', () => {
            s.width = Number(el.imgSize.value);
            savePrefs();
            if (s.frames) { show(s.idx); preload(s.idx, s.dir || 1); }
        });
        el.contrast.addEventListener('input', () => { s.contrast = Number(el.contrast.value); applyFilter(); });
        el.brightness.addEventListener('input', () => { s.brightness = Number(el.brightness.value); applyFilter(); });
        el.contrast.addEventListener('change', savePrefs);
        el.brightness.addEventListener('change', savePrefs);
        el.displayReset.addEventListener('click', () => {
            s.contrast = 100; s.brightness = 100;
            el.contrast.value = '100'; el.brightness.value = '100';
            applyFilter(); savePrefs();
        });
        applyFilter();
    }

    function applyFilter() {
        el.contrastOut.textContent = s.contrast + '%';
        el.brightnessOut.textContent = s.brightness + '%';
        el.frameImg.style.filter = (s.contrast === 100 && s.brightness === 100)
            ? '' : `contrast(${s.contrast}%) brightness(${s.brightness}%)`;
    }

    function loadPrefs() {
        let p = null;
        try { p = JSON.parse(localStorage.getItem(PREFS_KEY) || 'null'); } catch (e) { p = null; }
        if (p) {
            if (CAMERAS.includes(p.camera)) s.preferredCamera = s.camera = p.camera;
            if (Number.isFinite(p.width)) s.width = p.width;
            if (Number.isFinite(p.speed)) s.speed = p.speed;
            if (Number.isFinite(p.contrast)) s.contrast = p.contrast;
            if (Number.isFinite(p.brightness)) s.brightness = p.brightness;
            if (typeof p.follow === 'boolean') s.follow = p.follow;
        }
        setSelect(el.imgSize, s.width, v => { s.width = v; });
        setSelect(el.speed, s.speed, v => { s.speed = v; });
        el.contrast.value = String(s.contrast);
        el.brightness.value = String(s.brightness);
        el.follow.checked = s.follow;
        markCamera();
    }

    function setSelect(select, value, fallback) {
        const match = Array.from(select.options).some(o => Number(o.value) === value);
        if (match) select.value = String(value);
        else fallback(Number(select.value));
    }

    function savePrefs() {
        try {
            localStorage.setItem(PREFS_KEY, JSON.stringify({
                camera: s.preferredCamera, width: s.width, speed: s.speed,
                contrast: s.contrast, brightness: s.brightness, follow: s.follow,
            }));
        } catch (e) { /* storage blocked */ }
    }

    // ---------------------------------------------------------------- keyboard

    function wireKeyboard() {
        document.addEventListener('keydown', e => {
            if (e.defaultPrevented || e.ctrlKey || e.metaKey || e.altKey) return;
            const t = e.target;
            if (t && (t.closest('input, select, textarea, iframe, [contenteditable="true"]') || t.closest('#viewDiv'))) return;
            if (!s.frames && e.key !== 'f' && e.key !== 'F') return;

            switch (e.key) {
                case ' ':
                    if (t && t.closest('button')) return;  // let Space activate a focused button
                    e.preventDefault();
                    play(e.shiftKey ? -1 : 1);
                    break;
                case 'ArrowRight': e.preventDefault(); step(e.shiftKey ? 10 : 1); break;
                case 'ArrowLeft': e.preventDefault(); step(e.shiftKey ? -10 : -1); break;
                case 'Home': e.preventDefault(); pause(); seek(0); break;
                case 'End': e.preventDefault(); pause(); seek(s.frames.ld.length - 1); break;
                case 'f': case 'F': toggleFullscreen(); break;
                default:
                    if (e.key >= '1' && e.key <= '6') setCamera(CAMERAS[Number(e.key) - 1]);
            }
        });
    }

    // ---------------------------------------------------------------- map tabs

    function wireTabs() {
        [el.tabMap, el.tabStreet].forEach(tab => tab.addEventListener('click', () => selectTab(tab.dataset.tab)));
        el.follow.addEventListener('change', () => { s.follow = el.follow.checked; savePrefs(); updateMarker(true); });
    }

    function selectTab(name) {
        s.tab = name;
        el.tabMap.setAttribute('aria-selected', String(name === 'map'));
        el.tabStreet.setAttribute('aria-selected', String(name === 'street'));
        el.mapBody.hidden = name !== 'map';
        el.streetview.hidden = name !== 'street';
        if (name === 'street') { s.streetLast = null; updateStreetView(); }
    }

    // ---------------------------------------------------------------- ArcGIS map

    function initMap() {
        if (typeof window.require !== 'function') {
            el.mapUnavailable.hidden = false;
            return;
        }
        window.require([
            'esri/Map', 'esri/views/MapView', 'esri/layers/FeatureLayer', 'esri/layers/GraphicsLayer',
            'esri/widgets/Measurement', 'esri/Graphic', 'esri/geometry/Polyline', 'esri/geometry/Point',
        ], (EsriMap, MapView, FeatureLayer, GraphicsLayer, Measurement, Graphic, Polyline, Point) => {
            map.Graphic = Graphic; map.Polyline = Polyline; map.Point = Point;

            const routeLayer = new GraphicsLayer({ title: 'Current run' });
            const markerLayer = new GraphicsLayer({ title: 'Current frame' });
            const esriMap = new EsriMap({ basemap: 'streets-navigation-vector', layers: [routeLayer, markerLayer] });
            const view = new MapView({
                container: 'viewDiv',
                map: esriMap,
                zoom: 7,
                center: [-92.2746, 34.7514],
                constraints: { snapToZoom: false },
            });
            map.view = view;
            map.routeLayer = routeLayer;

            map.marker = new Graphic({
                geometry: new Point({ longitude: -92.2746, latitude: 34.7514 }),
                symbol: { type: 'simple-marker', style: 'circle', size: 14, color: '#e22526', outline: { color: '#ffffff', width: 2.5 } },
                visible: false,
            });
            markerLayer.add(map.marker);

            // Basemap and overlay pickers
            document.getElementById('basemap-select').addEventListener('change', e => { esriMap.basemap = e.target.value; });
            let overlay = null;
            document.getElementById('feature-layer-select').addEventListener('change', e => {
                if (overlay) esriMap.remove(overlay);
                overlay = null;
                if (!e.target.value) return;
                overlay = new FeatureLayer({ url: e.target.value, outFields: ['*'] });
                esriMap.add(overlay, 0);   // under the run line and marker
            });

            // Measurement tools
            const measurement = new Measurement({ view });
            view.ui.add(measurement, 'bottom-right');
            const toolbar = document.getElementById('toolbarDiv');
            view.ui.add(toolbar, 'top-left');
            const distanceBtn = document.getElementById('distance');
            const areaBtn = document.getElementById('area');
            const clearBtn = document.getElementById('clear');
            const clearMeasurements = () => {
                distanceBtn.classList.remove('active');
                areaBtn.classList.remove('active');
                measurement.clear();
            };
            distanceBtn.addEventListener('click', () => {
                measurement.activeTool = 'distance';
                distanceBtn.classList.add('active'); areaBtn.classList.remove('active');
            });
            areaBtn.addEventListener('click', () => {
                measurement.activeTool = 'area';
                areaBtn.classList.add('active'); distanceBtn.classList.remove('active');
            });
            clearBtn.addEventListener('click', clearMeasurements);
            view.on('context-menu', e => { e.stopPropagation(); clearMeasurements(); });

            // Double-click anywhere: nearest imagery (server lookup)
            view.on('double-click', e => {
                e.stopPropagation();
                if (!s.dotnet || measurement.activeTool) return;
                pause();
                setLoading(true);
                s.dotnet.invokeMethodAsync('OnMapDoubleClick', e.mapPoint.latitude, e.mapPoint.longitude)
                    .catch(err => console.error('mmhis: map lookup failed', err))
                    .finally(() => setLoading(false));
            });

            // Single click on the run line: jump along the current run (no server call)
            view.on('click', async e => {
                if (!s.frames || measurement.activeTool) return;
                const hit = await view.hitTest(e, { include: [routeLayer] });
                if (!hit.results.length) return;
                pause();
                seek(nearestFrameTo(e.mapPoint.latitude, e.mapPoint.longitude));
            });

            view.when(() => { if (s.frames) { drawRoute(false); updateMarker(true); } })
                .catch(() => { el.mapUnavailable.hidden = false; });
        }, () => { el.mapUnavailable.hidden = false; });
    }

    function drawRoute(fit) {
        if (!map.view || !s.frames) return;
        map.routeLayer.removeAll();
        const { lat, lon } = s.frames;
        const stepBy = Math.max(1, Math.ceil(lat.length / 4000));   // keep the line light on long runs
        const path = [];
        for (let i = 0; i < lat.length; i += stepBy) {
            if (lat[i] != null && lon[i] != null) path.push([lon[i], lat[i]]);
        }
        const last = lat.length - 1;
        if (lat[last] != null && lon[last] != null) path.push([lon[last], lat[last]]);
        if (path.length < 2) return;

        const line = new map.Polyline({ paths: [path], spatialReference: { wkid: 4326 } });
        map.routeLayer.addMany([
            new map.Graphic({ geometry: line, symbol: { type: 'simple-line', color: [255, 255, 255, 0.95], width: 7, cap: 'round', join: 'round' } }),
            new map.Graphic({ geometry: line, symbol: { type: 'simple-line', color: '#00704a', width: 4, cap: 'round', join: 'round' } }),
        ]);
        if (fit && line.extent) map.view.goTo(line.extent.clone().expand(1.25)).catch(() => { });
    }

    function updateMarker(force) {
        if (!map.view || !map.marker || !s.frames) return;
        const lat = s.frames.lat[s.idx], lon = s.frames.lon[s.idx];
        if (lat == null || lon == null) return;
        const point = new map.Point({ longitude: lon, latitude: lat });
        map.marker.geometry = point;
        map.marker.visible = true;

        if (!s.follow) return;
        const now = performance.now();
        if (!force && now - map.lastFollow < 700) return;
        const screen = map.view.toScreen(point);
        const w = map.view.width, h = map.view.height, pad = 0.15;
        if (!screen || screen.x < w * pad || screen.x > w * (1 - pad) || screen.y < h * pad || screen.y > h * (1 - pad)) {
            map.lastFollow = now;
            map.view.goTo({ center: [lon, lat] }, { duration: 350 }).catch(() => { });
        }
    }

    function nearestFrameTo(latitude, longitude) {
        const { lat, lon } = s.frames;
        const kx = Math.cos(latitude * Math.PI / 180);
        let best = s.idx, bestD = Infinity;
        for (let i = 0; i < lat.length; i++) {
            if (lat[i] == null) continue;
            const dy = lat[i] - latitude, dx = (lon[i] - longitude) * kx;
            const d = dx * dx + dy * dy;
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    // ---------------------------------------------------------------- utilities

    function url(path) { return new URL(path, document.baseURI).toString(); }
    function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }
    function round3(v) { return Math.round(v * 1000) / 1000; }
    function camel(id) { return id.replace(/-([a-z])/g, (_, c) => c.toUpperCase()); }
    function distanceMeters(a, b) {
        const dy = (a[0] - b[0]) * 110574, dx = (a[1] - b[1]) * 111320 * Math.cos(a[0] * Math.PI / 180);
        return Math.sqrt(dx * dx + dy * dy);
    }

    function isNarrow() { return window.matchMedia('(max-width: 900px)').matches; }

    window.mmhis = { init, loadRun, reset, currentLogmile, getSavedState, detach, isNarrow };
})();
