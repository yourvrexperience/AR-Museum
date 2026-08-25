import { $, esc } from "../Utils/dom.js";

export class EventsManager {
  constructor(root) {
    this.root = root;               // #view-events
    this.events = [];
    this.body    = $("#ap-events-body", root);
    this.empty   = $("#ap-events-empty", root);
    this.typeSel = $("#ap-event-type", root);
    this.poiList = $("#ap-poi-list", root);
    this.typeSel.addEventListener("change", () => this.renderEvents());
  }

  get count() { return this.events.length; }

  setEvents(rows) { this.events = rows || []; this.renderEvents(); return this.count; }

  _filtered() {
    const t = this.typeSel.value || "";
    return t ? this.events.filter((e) => String(e.type) === t) : this.events;
  }

  renderEvents() {
    const rows = this._filtered();
    this.body.innerHTML = "";

    if (!rows.length) {
      this.empty.style.display = "block";
      this.empty.innerHTML = this.events.length
        ? '<strong>No matches</strong>No events match the current filter.'
        : '<strong>No events yet</strong>Events captured during narration will stream in here.';
      return;
    }
    this.empty.style.display = "none";

    rows.forEach((ev) => {
      const tr = document.createElement("tr");
      tr.innerHTML =
        '<td class="muted">' + esc(ev.time) + '</td>' +
        '<td>' + esc(ev.type) +
          (ev.detail ? '<div class="muted" style="font-size:11px;">' + esc(ev.detail) + '</div>' : '') +
        '</td>' +
        '<td class="muted">' + esc(ev.poi || "—") + '</td>';
      this.body.appendChild(tr);
    });
  }

  setPoiStats(rows) {
    if (!this.poiList || !rows || !rows.length) return;   // add "!this.poiList ||"
    this.poiList.innerHTML = "";
    rows.forEach((p) => {
      const num = String(p.index != null ? p.index : "").padStart(2, "0");
      const div = document.createElement("div");
      div.className = "ap-poi";
      div.innerHTML =
        '<span class="num">' + esc(num) + '</span>' +
        '<span class="name">' + esc(p.name || ("POI " + num)) + '</span>' +
        '<span class="metric">' +
          (p.plays != null ? esc(p.plays) + " plays" : "—") +
          (p.completion != null ? " · " + esc(p.completion) : "") +
        '</span>';
      this.poiList.appendChild(div);
    });
  }
}
