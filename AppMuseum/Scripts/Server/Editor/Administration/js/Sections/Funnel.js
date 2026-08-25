import { esc } from "../Utils/dom.js";

/* =====================================================================
   Funnel — the route drop-off funnel (Events section).

   Fetches `mode=funnel` and draws one bar per step: "Started" (visitors who
   began the route) then POI 1, POI 2, … (visitors who reached at least that
   stop). Bars shrink as people drop off; the largest single-step loss is
   highlighted and summarised.

     const f = new Funnel(document.querySelector("#ap-funnel"),
                          { eventsUrl: "MuseumConsultAllEvents.php" });
     f.load({ area, from, to });

   The endpoint returns { starts, steps:[{poi, reached}] } already computed,
   so this module only formats and draws.
   ===================================================================== */
export class Funnel {
  constructor(root, opts = {}) {
    this.root      = root;
    this.eventsUrl = opts.eventsUrl || "MuseumConsultAllEvents.php";
    this.headers   = opts.headers || {};
    this.starts = 0;
    this.steps  = [];
    this._filters = {};
  }

  configure(opts = {}) {
    if (opts.eventsUrl != null) this.eventsUrl = opts.eventsUrl;
    if (opts.headers   != null) this.headers   = opts.headers;
  }

  async load(filters) {
    this._filters = filters || {};
    this._state("Loading…");
    try {
      const resp = await fetch(this._url("funnel", this._filters), {
        headers: this.headers, cache: "no-store"
      });
      if (!resp.ok) throw new Error("HTTP " + resp.status);
      const data = await resp.json();
      this.setData(data);
      return data;
    } catch (e) {
      console.error("Funnel.load:", e);
      this._state("Couldn't load funnel data.");
      throw e;
    }
  }

  setData(data) {
    this.starts = (data && data.starts) || 0;
    this.steps  = (data && Array.isArray(data.steps)) ? data.steps : [];
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

  render() {
    if (!this.steps.length && !this.starts) {
      this._state("No route activity in this range yet.");
      return;
    }

    // Rows: an optional "Started" baseline, then one per POI step.
    const rows = [];
    if (this.starts > 0) rows.push({ label: "Started", value: this.starts, start: true });
    this.steps.forEach((s) => rows.push({ label: "POI " + this._pad(s.poi), value: s.reached }));

    const base    = Math.max(1, ...rows.map((r) => r.value));            // bar scaling
    const pctBase = this.starts > 0 ? this.starts : (this.steps[0] ? this.steps[0].reached : 0);

    // Largest single-step drop (the leak).
    let leakIdx = -1, leakDrop = 0;
    for (let i = 1; i < rows.length; i++) {
      const d = rows[i - 1].value - rows[i].value;
      if (d > leakDrop) { leakDrop = d; leakIdx = i; }
    }

    let html = '<div class="ap-funnel">';
    rows.forEach((r, i) => {
      const w   = Math.round((r.value / base) * 100);
      const pct = pctBase ? Math.round((r.value / pctBase) * 100) : null;
      html +=
        '<div class="ap-funnel-row' + (r.start ? ' is-start' : '') + (i === leakIdx ? ' is-leak' : '') + '">' +
          '<span class="ap-funnel-label">' + esc(r.label) + '</span>' +
          '<span class="ap-funnel-track"><span class="ap-funnel-fill" style="width:' + w + '%"></span></span>' +
          '<span class="ap-funnel-count">' + r.value + '</span>' +
          '<span class="ap-funnel-pct">' + (pct == null ? "" : pct + "%") + '</span>' +
        '</div>';
    });
    html += '</div>';

    if (leakIdx > 0) {
      const before = rows[leakIdx - 1], after = rows[leakIdx];
      const dpct = before.value ? Math.round((leakDrop / before.value) * 100) : 0;
      html += '<p class="ap-funnel-note">Biggest drop-off: ' + esc(before.label) + ' → ' +
              esc(after.label) + ' (−' + leakDrop + ', −' + dpct + '%)</p>';
    }

    this.root.innerHTML = html;
  }

  _state(msg) { this.root.innerHTML = '<div class="ap-empty">' + esc(msg) + '</div>'; }
  _pad(n)     { return String(n).padStart(2, "0"); }
}
