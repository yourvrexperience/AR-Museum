/* Shared DOM + formatting helpers used across the panel managers. */

export const $ = (sel, root) => (root || document).querySelector(sel);

export const $$ = (sel, root) =>
  Array.prototype.slice.call((root || document).querySelectorAll(sel));

export function esc(s) {
  return String(s == null ? "" : s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

export function setText(el, v) {
  if (el) el.textContent = (v == null ? "—" : v);
}

export function fmtPct(rate) {           // rate is 0..1 or null
  return rate == null ? "—" : Math.round(rate * 100) + "%";
}

export function fmtAgo(epochSecs) {      // epoch seconds -> "3d ago" / date
  if (epochSecs == null) return "—";
  const now = Math.floor(Date.now() / 1000);
  const d = now - epochSecs;
  if (d < 0) return "—";
  if (d < 60) return "just now";
  if (d < 3600) return Math.floor(d / 60) + "m ago";
  if (d < 86400) return Math.floor(d / 3600) + "h ago";
  if (d < 7 * 86400) return Math.floor(d / 86400) + "d ago";
  return new Date(epochSecs * 1000).toISOString().slice(0, 10);   // YYYY-MM-DD
}
