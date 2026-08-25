import { esc, fmtAgo } from "../Utils/dom.js";
import { AP } from "../Utils/ap.js";

/* =====================================================================
   AiQuestions — the AI Q&A explorer (Events section).

   The only "raw" Events dashboard: it lists visitor questions to the AI
   guide (preamble already stripped server-side), newest first, with the
   answer revealed on click. It paginates with keyset pagination (pass the
   last id back as `before`) and has its own text search on top of the
   shared area + date filters.

     const q = new AiQuestions(document.querySelector("#ap-ai-questions"),
                               { eventsUrl: "MuseumConsultAllEvents.php" });
     q.load({ area, from, to });     // (re)load page 1 for these filters

   The shell (search box / list / footer) is built once so the search input
   keeps focus; only the list + footer re-render.
   ===================================================================== */
export class AiQuestions {
  constructor(root, opts = {}) {
    this.root      = root;
    this.eventsUrl = opts.eventsUrl || "MuseumConsultAllEvents.php";
    this.headers   = opts.headers || {};
    this.limit     = opts.limit || 25;

    this.items      = [];
    this._base      = {};          // area/from/to from GlobalManager
    this._search    = "";
    this._before    = null;        // keyset cursor (last id of last page)
    this._exhausted = false;
    this._expanded  = new Set();   // question ids with the answer open

    this.root.innerHTML =
      '<div class="ap-aiq">' +
        '<input class="ap-input ap-aiq-search" type="search" placeholder="Search questions…">' +
        '<div class="ap-aiq-list"></div>' +
        '<div class="ap-aiq-footer"></div>' +
      '</div>';
    this.searchInput = this.root.querySelector(".ap-aiq-search");
    this.list        = this.root.querySelector(".ap-aiq-list");
    this.footer      = this.root.querySelector(".ap-aiq-footer");

    // debounced server-side search
    this.searchInput.addEventListener("input", () => {
      clearTimeout(this._searchTimer);
      this._searchTimer = setTimeout(() => {
        this._search = this.searchInput.value.trim();
        this._reload();
      }, 300);
    });

    // delegated: "Load more" + expand a question
    this.root.addEventListener("click", (e) => {
      if (e.target.closest(".ap-loadmore")) { this._fetch(true); return; }
      const q = e.target.closest(".ap-aiq-q");
      if (q && this.list.contains(q)) {
        this._toggle(parseInt(q.closest(".ap-aiq-item").dataset.id, 10));
      }
    });
  }

  configure(opts = {}) {
    if (opts.eventsUrl != null) this.eventsUrl = opts.eventsUrl;
    if (opts.headers   != null) this.headers   = opts.headers;
  }

  // Called by GlobalManager with the shared area/date filters.
  load(filters) {
    this._base = filters || {};
    return this._reload();
  }

  _reload() {
    this._before = null;
    this._exhausted = false;
    this.items = [];
    return this._fetch(false);
  }

  async _fetch(append) {
    if (append) this._setFooter("Loading…"); else this._state("Loading…");
    try {
      const params = Object.assign({ mode: "ai_questions", limit: this.limit }, this._base);
      if (this._search) params.search = this._search;
      if (append && this._before != null) params.before = this._before;

      const resp = await fetch(this._url(params), { headers: this.headers, cache: "no-store" });
      if (!resp.ok) throw new Error("HTTP " + resp.status);
      const data = await resp.json();

      const got = Array.isArray(data.items) ? data.items : [];
      this.items = append ? this.items.concat(got) : got;
      this._before = data.nextBefore != null ? data.nextBefore : null;
      this._exhausted = got.length < this.limit || this._before == null;
      this.render();
      return data;
    } catch (e) {
      console.error("AiQuestions.load:", e);
      if (append) this._setFooter("Couldn't load more.");
      else this._state("Couldn't load questions.");
      throw e;
    }
  }

  _toggle(id) {
    if (this._expanded.has(id)) this._expanded.delete(id);
    else this._expanded.add(id);
    this.render();
  }

  render() {
    if (!this.items.length) { this._state("No questions found."); this._setFooter(""); return; }

    let html = "";
    this.items.forEach((it) => {
      const open = this._expanded.has(it.id);
      const meta = [fmtAgo(it.date), (it.language || "").toUpperCase(), AP.getNameAge(it.age)]
        .filter(Boolean).join(" · ");
      html +=
        '<div class="ap-aiq-item' + (open ? " is-open" : "") + '" data-id="' + it.id + '">' +
          '<div class="ap-aiq-q">' +
            '<span class="ap-caret">' + this._caret() + '</span>' +
            '<span class="ap-aiq-qtext">' + esc(it.question || "(empty question)") + '</span>' +
          '</div>' +
          '<div class="ap-aiq-meta">' + esc(meta) + '</div>' +
          (open ? '<div class="ap-aiq-a">' + esc(it.answer || "") + '</div>' : '') +
        '</div>';
    });
    this.list.innerHTML = html;

    this._setFooter(this._exhausted
      ? '<span class="muted">' + this.items.length + ' shown</span>'
      : '<button class="ap-loadmore" type="button">Load more</button>');
  }

  _url(params) {
    const p = new URLSearchParams();
    Object.keys(params).forEach((k) => {
      if (params[k] != null && params[k] !== "") p.set(k, params[k]);
    });
    const sep = this.eventsUrl.indexOf("?") >= 0 ? "&" : "?";
    return this.eventsUrl + sep + p.toString();
  }

  _state(msg)      { this.list.innerHTML = '<div class="ap-empty">' + esc(msg) + '</div>'; }
  _setFooter(html) { this.footer.innerHTML = html; }
  _caret() {
    return '<svg width="9" height="9" viewBox="0 0 12 12" fill="none">' +
      '<path d="M4 2 L8 6 L4 10" stroke="currentColor" stroke-width="1.6" ' +
      'stroke-linecap="round" stroke-linejoin="round"/></svg>';
  }
}
