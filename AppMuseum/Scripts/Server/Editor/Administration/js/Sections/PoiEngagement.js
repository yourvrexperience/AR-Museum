import { esc, fmtPct } from "../Utils/dom.js";

/* =====================================================================
   PoiEngagement — the per-POI engagement table (Events section).

   Self-contained: give it a container element and (optionally) the events
   endpoint URL, then call load() to fetch `mode=poi_engagement` and render.

     const t = new PoiEngagement(document.querySelector("#ap-poi-engagement"),
                                 { eventsUrl: "MuseumConsultAllEvents.php" });
     t.load();                       // fetch + render
     t.load({ from, to, age, ... }); // same, with server-side filters
     t.setData(json);                // render from an object you already have
     t.ensureLoaded();               // load once (lazy, e.g. on tab open)

   The endpoint returns pre-computed rows, so there's nothing to aggregate
   here — this module only formats, sorts, and renders.
   ===================================================================== */
export class PoiEngagement {
  constructor(root, opts = {}) {
    this.root      = root;                                   // container element
    this.eventsUrl = opts.eventsUrl || "MuseumConsultAllEvents.php";
    this.headers   = opts.headers || {};

    this.pois     = [];
    this.expanded = new Set();     // poi numbers with the detail row open
    this.sortKey  = "poi";
    this.sortDir  = 1;             // 1 asc, -1 desc
    this.loaded   = false;
    this.loading  = false;
    this._filters = {};

    // Delegated: header click sorts, row click expands.
    this.root.addEventListener("click", (e) => {
      const th = e.target.closest("th[data-sort]");
      if (th && this.root.contains(th)) { this._sortBy(th.dataset.sort); return; }
      const row = e.target.closest(".ap-poieng-row");
      if (row && this.root.contains(row)) this._toggle(parseInt(row.dataset.poi, 10));
    });
  }

  configure(opts = {}) {
    if (opts.eventsUrl != null) this.eventsUrl = opts.eventsUrl;
    if (opts.headers   != null) this.headers   = opts.headers;
  }

  // ---- data ----------------------------------------------------------
  async load(filters) {
    this._filters = filters || {};
    this.loading = true;
    this._state("Loading…");
    try {
      const resp = await fetch(this._url("poi_engagement", this._filters), {
        headers: this.headers, cache: "no-store"
      });
      if (!resp.ok) throw new Error("HTTP " + resp.status);
      const data = await resp.json();
      this.setData(data);
      this.loaded = true;
      return data;
    } catch (e) {
      console.error("PoiEngagement.load:", e);
      this._state("Couldn't load engagement data.");
      throw e;
    } finally {
      this.loading = false;
    }
  }

  ensureLoaded() {
    if (!this.loaded && !this.loading) return this.load(this._filters);
  }

  setData(data) {
    this.pois = (data && Array.isArray(data.pois)) ? data.pois : [];
    this.render();
  }

  _url(mode, filters) {
    const p = new URLSearchParams({ mode });
    Object.keys(filters || {}).forEach((k) => {
      if (filters[k] != null && filters[k] !== "") p.set(k, filters[k]);
    });
    const sep = this.eventsUrl.indexOf("?") >= 0 ? "&" : "?";
    return this.eventsUrl + sep + p.toString();
  }

  // ---- sorting / expanding ------------------------------------------
  _sortBy(key) {
    if (this.sortKey === key) this.sortDir = -this.sortDir;
    else { this.sortKey = key; this.sortDir = 1; }
    this.render();
  }

  _toggle(poi) {
    if (this.expanded.has(poi)) this.expanded.delete(poi);
    else this.expanded.add(poi);
    this.render();
  }

  _sorted() {
    const k = this.sortKey, dir = this.sortDir;
    return this.pois.slice().sort((a, b) => {
      const av = a[k], bv = b[k];
      if (av == null && bv == null) return 0;
      if (av == null) return 1;                 // nulls always last
      if (bv == null) return -1;
      return (av < bv ? -1 : av > bv ? 1 : 0) * dir;
    });
  }

  // ---- rendering -----------------------------------------------------
  render() {
    if (!this.pois.length) { this._state("No POI activity in this range yet."); return; }

    const arrow = (key) => this.sortKey === key ? (this.sortDir === 1 ? " ▲" : " ▼") : "";
    const th = (key, label) =>
      '<th data-sort="' + key + '" class="ap-sortable' + (this.sortKey === key ? ' is-sorted' : '') + '">' +
        esc(label) + arrow(key) + '</th>';

    let body = "";
    this._sorted().forEach((p) => {
      const open = this.expanded.has(p.poi);
      body +=
        '<tr class="ap-poieng-row' + (open ? ' is-open' : '') + '" data-poi="' + p.poi + '">' +
          '<td>' + this._pad(p.poi) + '</td>' +
          '<td>' + (p.visits != null ? p.visits : "—") + '</td>' +
          '<td>' + this._secs(p.avgListen) + '</td>' +
          '<td>' + fmtPct(p.skipRate) + '</td>' +
          '<td>' + (p.replays != null ? p.replays : "—") + '</td>' +
        '</tr>';
      if (open) {
        body +=
          '<tr class="ap-poieng-detail"><td colspan="5">' +
            '<div class="ap-poieng-more">' +
              this._kv("Avg. skip point", this._secs(p.avgSkipTime)) +
              this._kv("Avg. pauses",   p.avgPaused    != null ? p.avgPaused    : "—") +
              this._kv("Avg. restarts", p.avgRestarted != null ? p.avgRestarted : "—") +
            '</div></td></tr>';
      }
    });

    this.root.innerHTML =
      '<div class="ap-poieng">' +
        '<table class="ap-table ap-poieng-table">' +
          '<thead><tr>' +
            th("poi", "POI") + th("visits", "Visits") + th("avgListen", "Listen") +
            th("skipRate", "Skip") + th("replays", "Replays") +
          '</tr></thead>' +
          '<tbody>' + body + '</tbody>' +
        '</table>' +
      '</div>';
  }

  _state(msg) { this.root.innerHTML = '<div class="ap-empty">' + esc(msg) + '</div>'; }
  _pad(n)     { return String(n).padStart(2, "0"); }
  _secs(v)    { return v == null ? "—" : v + "s"; }
  _kv(k, v)   { return '<span class="ap-kv"><span class="k">' + esc(k) + '</span>' +
                       '<span class="v">' + esc(v) + '</span></span>'; }
}
