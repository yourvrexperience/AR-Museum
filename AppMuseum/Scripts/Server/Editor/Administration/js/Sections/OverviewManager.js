import { $, $$, setText, fmtPct } from "../Utils/dom.js";
import { AP } from "../Utils/ap.js";

export class OverviewManager {
  constructor(root) {
    this.root = root;               // #view-overview
    this.users = [];
    this.narrations = [];           // MuseumConsultAllPOIs.php definitions
    this.area = 0;
  }

  get areaLabel() 
  { 
	return AP.getNameFloor(this.area);
  }
  
  // data: { users, narrations } — either key may be omitted to keep current.
  setData(data) {
    data = data || {};
    if (data.users != null)      this.users = data.users;
    if (data.narrations != null) this.narrations = data.narrations;
	if (data.secrets != null) this.secrets = data.secrets;
    const count = AP.areaCount(this.narrations);
    this.render();
    return count;
  }

  setArea(area) {
    this.area = area;
    this.render();
  }
  
  // Fill a set of three bars: prefix is "comp" or "vis".
  _bars(prefix, values, fmt, denom) {
    for (let i = 0; i < 3; i++) {
      const v = values[i];
      const fill = $('[data-' + prefix + '="' + i + '"]', this.root);
      const val  = $('[data-' + prefix + '-val="' + i + '"]', this.root);
      const d = (typeof denom === "function") ? denom(i) : denom;
      const pct = (v == null || !d) ? 0 : Math.max(0, Math.min(100, (v / d) * 100));
      if (fill) fill.style.width = pct + "%";
      if (val)  val.textContent = fmt(v);
    }
  }

  render() {
    const ov = AP.computeOverview(this.users, this.area, this.narrations, this.secrets);

    setText($('[data-stat="users"]', this.root),       ov.registered);
    setText($('[data-stat="active"]', this.root),      ov.active24h);
    setText($('[data-stat="narrations"]', this.root),  ov.narrations);
    setText($('[data-stat="completed"]', this.root),   ov.toursCompleted);
    setText($('[data-stat="secrets"]', this.root),  ov.secrets);
    setText($('[data-stat="discovered"]', this.root),   ov.secretsCompleted);

    // Q3 completion rate: rates are 0..1, so denom 1 => width = rate*100 (%).
    this._bars("comp", ov.completionRatePOIs, fmtPct, 1);

    // Q4 discovered secrets
    this._bars("sec", ov.completionRateSecrets, fmtPct, 1);

    // Current-area labels sprinkled through the section.
    $$("[data-area-label]", this.root).forEach((el) => {
      el.textContent = el.classList.contains("sub")
        ? ("in " + this.areaLabel)
        : ("· " + this.areaLabel);
    });

    return ov;
  }
}
