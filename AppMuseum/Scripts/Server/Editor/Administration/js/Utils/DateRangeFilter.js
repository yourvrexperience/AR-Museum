/* =====================================================================
   DateRangeFilter — a small, reusable date-range control.

   Renders a preset dropdown (All time / Last 7·30·90 days / Custom) with
   two date inputs revealed for "Custom". It owns no data and knows nothing
   about the endpoint — on any change it calls onChange({ from, to }) with
   epoch seconds (or null for an open bound), and the caller decides what to
   reload:

     new DateRangeFilter(container, { onChange: (r) => table.load(r) });

   `from` is the start of the first day, `to` is the end of the last day,
   both in the browser's local time (which is how staff think about "a day").
   ===================================================================== */

const DAY = 86400;

function toEpoch(y, mo, d, h, mi, s) {
  return Math.floor(new Date(y, mo - 1, d, h, mi, s, 0).getTime() / 1000);
}
function startOfDay(dateStr) {           // "YYYY-MM-DD" -> local 00:00:00
  const [y, m, d] = dateStr.split("-").map(Number);
  return toEpoch(y, m, d, 0, 0, 0);
}
function endOfDay(dateStr) {             // "YYYY-MM-DD" -> local 23:59:59
  const [y, m, d] = dateStr.split("-").map(Number);
  return toEpoch(y, m, d, 23, 59, 59);
}
function todayStr() {
  const t = new Date();
  const p = (n) => String(n).padStart(2, "0");
  return t.getFullYear() + "-" + p(t.getMonth() + 1) + "-" + p(t.getDate());
}

export class DateRangeFilter {
  constructor(root, opts = {}) {
    this.root = root;
    this.onChange = opts.onChange || function () {};
    this.value = { from: null, to: null };   // null = open bound (all time)
    this._render();
  }

  getRange() { return { from: this.value.from, to: this.value.to }; }

  _render() {
    const max = todayStr();
    this.root.innerHTML =
      '<div class="ap-daterange">' +
        '<select class="ap-select" data-role="preset">' +
          '<option value="all">All time</option>' +
          '<option value="7">Last 7 days</option>' +
          '<option value="30">Last 30 days</option>' +
          '<option value="90">Last 90 days</option>' +
          '<option value="custom">Custom…</option>' +
        '</select>' +
        '<div class="ap-daterange-custom" data-role="custom" hidden>' +
          '<input class="ap-input" type="date" data-role="from" max="' + max + '" aria-label="From date">' +
          '<span class="ap-daterange-sep">–</span>' +
          '<input class="ap-input" type="date" data-role="to" max="' + max + '" aria-label="To date">' +
        '</div>' +
      '</div>';

    this.preset    = this.root.querySelector('[data-role="preset"]');
    this.custom    = this.root.querySelector('[data-role="custom"]');
    this.fromInput = this.root.querySelector('[data-role="from"]');
    this.toInput   = this.root.querySelector('[data-role="to"]');

    this.preset.addEventListener("change", () => this._onPreset());
    this.fromInput.addEventListener("change", () => this._onCustom());
    this.toInput.addEventListener("change", () => this._onCustom());
  }

  _onPreset() {
    const v = this.preset.value;
    if (v === "custom") {
      this.custom.hidden = false;
      this._onCustom();                     // emit only if both/either date set
      return;
    }
    this.custom.hidden = true;
    if (v === "all") {
      this.value = { from: null, to: null };
    } else {
      const now = Math.floor(Date.now() / 1000);
      this.value = { from: now - parseInt(v, 10) * DAY, to: null };
    }
    this.onChange(this.getRange());
  }

  _onCustom() {
    if (this.preset.value !== "custom") return;
    let f = this.fromInput.value, t = this.toInput.value;
    if (!f && !t) return;                   // nothing chosen yet
    if (f && t && startOfDay(f) > startOfDay(t)) { const tmp = f; f = t; t = tmp; }  // tolerate reversed
    this.value = { from: f ? startOfDay(f) : null, to: t ? endOfDay(t) : null };
    this.onChange(this.getRange());
  }
}
