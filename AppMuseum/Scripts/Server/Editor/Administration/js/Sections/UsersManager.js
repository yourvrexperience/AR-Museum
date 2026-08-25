import { $, $$, esc, fmtPct, fmtAgo } from "../Utils/dom.js";
import { AP } from "../Utils/ap.js";

// Profile filter value -> age index.
const PROFILE_AGE = { children: 0, adults: 1, experts: 2 };

export class UsersManager {
  constructor(root) {
    this.root = root;               // #view-users
    this.users = [];                // parsed user records
    this.narrations = [];           // narration definitions (drives area count)
    this.summaries = [];            // AP.userSummary(...) per user
	this.expanded = new Set();      // user ids currently expanded
	this.expandedCells = new Set(); // "uid|area|kind" cells drilled into

    this.body    = $("#ap-users-body", root);
    this.empty   = $("#ap-users-empty", root);
    this.search  = $("#ap-user-search", root);
    this.profile = $("#ap-user-profile", root);

	this.search.addEventListener("input",  () => this.render());
	this.profile.addEventListener("change", () => this.render());

	// Delegated: click a POIs/Secrets value inside a detail row to drill in.
	this.body.addEventListener("click", (e) => {
	      const cell = e.target.closest(".ap-udetail-cell");
	      if (!cell || !this.body.contains(cell)) return;
	      e.stopPropagation();
	      this._toggleCell(cell.dataset.uid, cell.dataset.area, cell.dataset.kind);
	    });
  }

  get count() { return this.users.length; }

  // data: { users, narrations } — either key may be omitted to keep current.
  setData(data) {
    data = data || {};
    if (data.users != null)      this.users = data.users;
    if (data.narrations != null) this.narrations = data.narrations;
    this.summaries = AP.userSummaries(this.users, this.narrations);
    this.render();
    return this.users.length;
  }

  _filtered() {
    const q = (this.search.value || "").trim().toLowerCase();
    const pAge = PROFILE_AGE[this.profile.value];   // undefined => all
    return this.summaries.filter((s) => {
      if (q) {
        const hay = ((s.name || "") + " " + (s.email || "")).toLowerCase();
        if (hay.indexOf(q) < 0) return false;
      }
      if (pAge != null && s.selectedAge !== pAge) return false;
      return true;
    });
  }

  render() {
    const rows = this._filtered();
    this.body.innerHTML = "";

    if (!rows.length) {
      this.empty.style.display = "block";
      this.empty.innerHTML = this.summaries.length
        ? '<strong>No matches</strong>No users match the current filters.'
        : '<strong>No users loaded</strong>Registered visitors appear here once you connect the backend.';
      return;
    }
    this.empty.style.display = "none";

    rows.forEach((s) => {
      const open = this.expanded.has(s.id);

      // ---- summary row ----
      const tr = document.createElement("tr");
      tr.className = "ap-user-row" + (open ? " is-open" : "");
      tr.innerHTML =
        '<td class="ap-user-email">' +
          '<span class="ap-caret" aria-hidden="true">' + this._caret() + '</span>' +
          '<span class="ap-email-txt">' + esc(s.email || "—") + '</span></td>' +
        '<td class="muted">' + esc(fmtAgo(s.lastlogin)) + '</td>' +
        '<td>' + this._pctCell(s.overallPoiPct) + '</td>' +
        '<td>' + this._pctCell(s.overallSecretPct) + '</td>';
      tr.addEventListener("click", () => this._toggle(s.id));
      this.body.appendChild(tr);

      // ---- detail row ----
      const dtr = document.createElement("tr");
      dtr.className = "ap-user-detail" + (open ? "" : " is-hidden");
      const td = document.createElement("td");
      td.colSpan = 4;
      td.innerHTML = this._detailHtml(s);
      dtr.appendChild(td);
      this.body.appendChild(dtr);
    });
  }

  _toggle(id) {
    if (this.expanded.has(id)) this.expanded.delete(id);
    else this.expanded.add(id);
    this.render();
  }

  _caret() {
    return '<svg width="9" height="9" viewBox="0 0 12 12" fill="none">' +
      '<path d="M4 2 L8 6 L4 10" stroke="currentColor" stroke-width="1.6" ' +
      'stroke-linecap="round" stroke-linejoin="round"/></svg>';
  }

  // Compact cell: percentage + a thin progress bar.
  _pctCell(rate) {
    const pct = rate == null ? 0 : Math.round(rate * 100);
    return '<div class="ap-pctcell">' +
      '<span class="ap-pctval">' + fmtPct(rate) + '</span>' +
      '<span class="ap-pctbar"><span class="ap-pctbar-fill" style="width:' + pct + '%"></span></span>' +
    '</div>';
  }

  _detailHtml(s) {
      let rows = "";
      s.perArea.forEach((a) => {
        const poiOpen = this.expandedCells.has(this._cellKey(s.id, a.area, "poi"));
        const secOpen = this.expandedCells.has(this._cellKey(s.id, a.area, "secret"));

        rows +=
          '<tr>' +
            '<td>' + esc(a.name) + '</td>' +
            this._cell(s.id, a.area, "poi",    poiOpen, a.poiItems.length,    this._detailPct(a.poiPct, a.poiDone, a.poiTotal)) +
            this._cell(s.id, a.area, "secret", secOpen, a.secretItems.length, this._detailPct(a.secretPct, a.secretDone, a.secretTotal)) +
          '</tr>';

        if (poiOpen && a.poiItems.length)    rows += this._subRow(a.poiItems, "POI", "Completed");
        if (secOpen && a.secretItems.length) rows += this._subRow(a.secretItems, "Secret", "Discovered");
      });
      if (!rows) rows = '<tr><td colspan="3" class="muted">No areas defined.</td></tr>';

      return '<div class="ap-udetail">' +
        '<div class="ap-udetail-age">Age selected: <strong>' + esc(s.selectedAgeName) + '</strong></div>' +
        '<table class="ap-udetail-table">' +
          '<thead><tr><th>Area</th><th>POIs</th><th>Secrets</th></tr></thead>' +
          '<tbody>' + rows + '</tbody>' +
        '</table></div>';
    }

    _cellKey(uid, area, kind) { return uid + "|" + area + "|" + kind; }

    _cell(uid, area, kind, open, count, inner) {
      if (!count) return '<td>' + inner + '</td>';
      return '<td class="ap-udetail-cell' + (open ? ' is-open' : '') + '"' +
        ' data-uid="' + esc(uid) + '" data-area="' + area + '" data-kind="' + kind + '">' +
        inner + '</td>';
    }

    _toggleCell(uid, area, kind) {
      const key = this._cellKey(uid, area, kind);
      if (this.expandedCells.has(key)) this.expandedCells.delete(key);
      else this.expandedCells.add(key);
      this.render();
    }

    // items: [{ id, done }]. Two chip groups: done vs. left.
    _subRow(items, label, doneWord) {
      const chips = (list) => list.length
        ? list.map((it) => '<span class="ap-chip">' + label + ' ' + (it.id + 1) + '</span>').join('')
        : '<span class="muted">none</span>';
      const done = items.filter((it) => it.done);
      const left = items.filter((it) => !it.done);
      return '<tr class="ap-udetail-sub"><td colspan="3">' +
        '<div class="ap-breakdown">' +
          '<div class="ap-breakdown-row is-done"><span class="ap-breakdown-lbl">' + doneWord + '</span>' +
            '<span class="ap-chips">' + chips(done) + '</span></div>' +
          '<div class="ap-breakdown-row is-left"><span class="ap-breakdown-lbl">Left</span>' +
            '<span class="ap-chips">' + chips(left) + '</span></div>' +
        '</div></td></tr>';
    }

  _detailPct(rate, done, total) {
    if (rate == null) return '<span class="muted">—</span>';
    return fmtPct(rate) +
      ' <span class="muted">(' + done + '/' + total + ')</span>';
  }
}
