import { $, $$, setText } from "./Utils/dom.js";
import { AP } from "./Utils/ap.js";
import { DateRangeFilter } from "./Utils/DateRangeFilter.js";
import { OverviewManager } from "./Sections/OverviewManager.js";
import { UsersManager } from "./Sections/UsersManager.js";
import { EventsManager } from "./Sections/EventsManager.js";
import { PoiEngagement } from "./Sections/PoiEngagement.js";
import { Funnel } from "./Sections/Funnel.js";
import { AiQuestions } from "./Sections/AiQuestions.js";

export class GlobalManager {
  constructor() {
    this.usersUrl = "UserConsutlAll.php";        // user + progress endpoint
    this.poisUrl  = "MuseumConsultAllPOIs.php";  // narration definitions endpoint
	this.secretsUrl = "MuseumConsultAllSecrets.php";  // secrets definitions endpoint	
	this.eventsUrl = "MuseumConsultAllEvents.php"; // events analysis endpoint	
    this.headers  = {};                          // add an admin token here

    this.statusEl   = $("#ap-status");
    this.refreshBtn = $("#ap-refresh");
    this.titleEl    = $(".ap-title");
    this.subtitleEl = $(".ap-subtitle");

    // Section managers
    this.overview = new OverviewManager($("#view-overview"));
    this.users    = new UsersManager($("#view-users"));
    this.events   = new EventsManager($("#view-events"));
	this.poiEngagement = new PoiEngagement($("#ap-poi-engagement"), { eventsUrl: this.eventsUrl, headers: this.headers });
	this.funnel = new Funnel($("#ap-funnel"), { eventsUrl: this.eventsUrl, headers: this.headers });
	this.aiQuestions = new AiQuestions($("#ap-ai-questions"), { eventsUrl: this.eventsUrl, headers: this.headers });
	this.eventsFilter = new DateRangeFilter($("#ap-events-filter"), { onChange: () => this._syncEvents() });
	
    // Shared selected area — known across the whole system.
    this.area = 0;
    this.areaSelect = $("#ap-area");
    this.areaSelect.addEventListener("change", () => this._onAreaChange());
	
    this._initTabs();
    this.refreshBtn.addEventListener("click", () => this.refresh());
  }

  // ---- header ------------------------------------------------------
  setHeader(title, subtitle) {
    if (title != null)    setText(this.titleEl, title);
    if (subtitle != null) setText(this.subtitleEl, subtitle);
  }

  setConnection(online, label) {
    this.statusEl.classList.toggle("is-online", !!online);
    this.statusEl.classList.toggle("is-offline", !online);
    $(".txt", this.statusEl).textContent = label || (online ? "Connected" : "Not connected");
  }

  // ---- tab bar -----------------------------------------------------
  _initTabs() {
    this.tabs  = $$(".ap-tab");
    this.views = $$(".ap-view");
    this.tabs.forEach((tab) =>
      tab.addEventListener("click", () => this.showTab(tab.dataset.view)));
  }

  showTab(view) {
    this.tabs.forEach((t) => t.classList.toggle("is-active", t.dataset.view === view));
    this.views.forEach((v) => v.classList.toggle("is-active", v.id === "view-" + view));
    if (view === "events") this._syncEvents();   // reflect current area + range
  }

  // Current Events query = selected area + date range. Single source of truth
  // for every Events dashboard.
  _eventsFilters() {
    return Object.assign({}, this.eventsFilter.getRange(), { area: this.area });
  }

  // Reload the Events dashboards, but only when the area/range actually changed
  // (so re-opening the tab with no change doesn't refetch).
  _syncEvents() {
    const f = this._eventsFilters();
    const sig = [f.from, f.to, f.area].join("|");
    if (sig === this._eventsSig) return;
    this._eventsSig = sig;
    this.poiEngagement.load(f);
	this.funnel.load(f);
	this.aiQuestions.load(f);
    // funnel / AI questions get added here as they're built.
  }
  
  setTabCount(view, n) {
    const tab = this.tabs.find((t) => t.dataset.view === view);
    if (!tab) return;
    const badge = $(".ap-tab-count", tab);
    if (!badge) return;
    if (n == null) { badge.style.display = "none"; }
    else { badge.textContent = String(n); badge.style.display = "inline-block"; }
  }

  // ---- data flow ---------------------------------------------------
  configure(opts) {
    opts = opts || {};
    if (opts.usersUrl != null) this.usersUrl = opts.usersUrl;
    if (opts.poisUrl != null) this.poisUrl  = opts.poisUrl;
	if (opts.secretsUrl != null) this.secretsUrl  = opts.secretsUrl;
	if (opts.eventsUrl != null) this.eventsUrl = opts.eventsUrl;
    if (opts.headers != null) this.headers  = opts.headers;
	
	this.poiEngagement.configure({ eventsUrl: this.eventsUrl, headers: this.headers });
	this.funnel.configure({ eventsUrl: this.eventsUrl, headers: this.headers });
	this.aiQuestions.configure({ eventsUrl: this.eventsUrl, headers: this.headers });
  }

  // Distribute parsed data to the sections + tab headers.
  //   users:      parsed user/progress records
  //   narrations: parsed narration definitions (drives the area count)
  _distribute(users, narrations, secrets) {
    const areaCount = AP.areaCount(narrations);
    if (this.area >= areaCount) this.area = 0;
    this._populateAreaSelect(areaCount);
    this.overview.setData({ users, narrations, secrets, area: this.area });
    this.users.setData({ users, narrations });   // Users section derives its own summaries
    this.setTabCount("users", users.length);     // current info in the tab header
    return { registered: users.length, areaCount, narrations: narrations ? narrations.length : 0, secrets: secrets ? secrets.length : 0 };
  }

  getArea() { return this.area; }
  
  _onAreaChange() {
    this.area = parseInt(this.areaSelect.value, 10) || 0;
    this._broadcastArea();
  }
  
  _broadcastArea() {
    this.overview.setArea(this.area);
	this._syncEvents();
  }
  
  _populateAreaSelect(count) {
    const sel = this.areaSelect;
    sel.innerHTML = "";
    if (!count) {
      const o = document.createElement("option");
      o.value = "0"; o.textContent = "No data"; o.disabled = true; o.selected = true;
      sel.appendChild(o);
      return;
    }
    if (this.area >= count) this.area = 0;
    for (let a = 0; a < count; a++) {
      const opt = document.createElement("option");
      opt.value = String(a);
      opt.textContent = AP.getNameFloor(a);
      if (a === this.area) opt.selected = true;
      sel.appendChild(opt);
    }
  }
  
  loadFromText(usersText, poisText, secretsText) {
    const users = AP.parseUsers(usersText);
    const narrations = poisText ? AP.parseNarrations(poisText) : [];
	const secrets = secretsText ? AP.parseSecrets(secretsText) : [];
    const r = this._distribute(users, narrations, secrets);
    this.setConnection(true, "Loaded · " + r.registered + " users");
    return r;
  }

  _fetchText(url) {
    return fetch(url, { headers: this.headers, cache: "no-store" }).then((resp) => {
      if (!resp.ok) throw new Error("HTTP " + resp.status + " for " + url);
      return resp.text();
    });
  }

  async refresh() {
    this.refreshBtn.classList.add("is-loading");
    try {
      // Both endpoints in parallel.
      const [usersText, poisText, secretsText] = await Promise.all([
        this._fetchText(this.usersUrl),
        this._fetchText(this.poisUrl),
		this._fetchText(this.secretsUrl),
      ]);
      const users = AP.parseUsers(usersText);
      const narrations = AP.parseNarrations(poisText);
	  const secrets = AP.parseSecrets(secretsText);
      const r = this._distribute(users, narrations, secrets);
      this.setConnection(true,
        "Connected · " + r.registered + " users · " + r.narrations + " narrations");
      return r;
    } catch (e) {
      this.setConnection(false, "Fetch failed");
      console.error("GlobalManager.refresh:", e);
      throw e;
    } finally {
      setTimeout(() => this.refreshBtn.classList.remove("is-loading"), 400);
    }
  }
}
