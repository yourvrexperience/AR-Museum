/* =====================================================================
   AP — pure parsing + analytics for UserConsutlAll.php.
   No DOM access here, so it can be unit-tested in isolation.

   Response format (delimiters from ConfigurationUserManagement.php):
     records  separated by  <line>   (a leading <line> precedes record 1)
     fields   separated by  <par>
     profile  introduced by <udata>PROFILE<par>...

   Per record:  id <par> email <par> nickname <par> registerdate <par>
                lastlogin <par> admin <par> level <par> validated <par>
                platform  [ <udata> PROFILE <par> profile_id <par> name <par>
                address <par> description <par> DATA <par> data2 <par> data3
                <par> data4 <par> data5 <par> autorun ]

   DATA holds the progress JSON: { "Maps": [ {ID,POIs[],Secrets[],...}, ... ] }
   Maps are ordered area-major, age-minor with a FIXED 3 ages
   (0 children, 1 adult, 2 expert):  mapIndex = area*3 + age.
   ===================================================================== */
export const AP = (function () {
  var SEP  = { line: "<line>", par: "<par>", udata: "<udata>", block: "<block>" };
  var AGES = 3;
  var DAY  = 86400;

  // ---- route predicates (single source of truth — tweak here) --------
  function poiVisited(v)     { return typeof v === "number" && v > 0; }
  function routeStarted(m)   { return !!m && Array.isArray(m.POIs) && m.POIs.some(poiVisited); }
  function routeCompleted(m) { return !!m && Array.isArray(m.POIs) && m.POIs.length > 0 && m.POIs.every(poiVisited); }
  function poisVisited(m)    { return (m && Array.isArray(m.POIs)) ? m.POIs.filter(poiVisited).length : 0; }

  function secretDiscovered(v)     { return typeof v === "boolean" && v == true; }
  function secretsStarted(m)   { return !!m && Array.isArray(m.Secrets) && m.Secrets.some(secretDiscovered); }
  function secretsCompleted(m) { return !!m && Array.isArray(m.Secrets) && m.Secrets.length > 0 && m.Secrets.every(secretDiscovered); }
  function secretsVisited(m)    { return (m && Array.isArray(m.Secrets)) ? m.Secrets.filter(secretDiscovered).length : 0; }

  function getNameFloor(floor) {
	switch (floor) {
		case 0:
			return "Area Base"; 
		case 1:
			return "Area History"; 
		case 2:
			return "Area Sciences"; 			
		default:
			return "Area " + (floor + 1); 
	}
  }	

  function getNameAge(age) {
    switch (age) {
      case 0:  return "Children";
      case 1:  return "Adults";
      case 2:  return "Experts";
      default: return "—";
    }
  }

  function toInt(x) { var n = parseInt(x, 10); return isNaN(n) ? null : n; }

  // ---- parsing -------------------------------------------------------
  function parseMaps(dataStr) {
    if (!dataStr) return [];
    try {
      var obj = JSON.parse(dataStr);
      if (obj && Array.isArray(obj.Maps)) return obj.Maps;
    } catch (e) { /* malformed data -> treat as no progress */ }
    return [];
  }

  function parseUsers(text) {
    var users = [];
    if (!text) return users;
    var recs = text.split(SEP.line);
    for (var i = 0; i < recs.length; i++) {
      var raw = recs[i];
      if (!raw || !raw.trim()) continue;                 // skip leading empty chunk
      var halves = raw.split(SEP.udata);
      var u = halves[0].split(SEP.par);
      var user = {
        id: u[0], email: u[1], name: u[2],
        registerdate: toInt(u[3]), lastlogin: toInt(u[4]),
        admin: u[5], level: u[6], validated: u[7], platform: u[8],
        profile: null, maps: []
      };
      if (halves.length > 1) {
        var p = halves[1].split(SEP.par);                // PROFILE, id, name, addr, desc, DATA, d2..d5, autorun
        if (p[0] === "PROFILE") {
          // DATA is field index 5; exactly 5 trailing fields follow it.
          // Rejoin the middle in case the JSON ever contained the separator.
          var dataStr = p.slice(5, p.length - 5).join(SEP.par);
          user.profile = { id: p[1], name: p[2], address: p[3], description: p[4], autorun: p[p.length - 1] };
          user.maps = parseMaps(dataStr);
        }
      }
      users.push(user);
    }
    return users;
  }

  // ---- museum narration definitions (MuseumConsultAllPOIs.php) -------
  // Response: <line>-separated JSON objects { "Positions": [ {ID,Position} ] }
  // (a leading <line> precedes the first record). Each record is one
  // narration; its point count is Positions.length.
  function parseJsonLoose(s) {
    try { return JSON.parse(s); }
    catch (e) {
      try { return JSON.parse(s.replace(/\\"/g, '"')); }  // tolerate escaped quotes
      catch (e2) { return null; }
    }
  }

  function parseNarrations(text) {
    var out = [];
    if (!text) return out;
    var recs = text.split(SEP.line);
    var idx = 0;
    for (var i = 0; i < recs.length; i++) {
      var raw = recs[i];
      if (!raw || !raw.trim()) continue;                 // skip leading empty chunk
      var obj = parseJsonLoose(raw);
      var positions = (obj && Array.isArray(obj.Positions)) ? obj.Positions : [];
      out.push({ index: idx++, points: positions.length, positions: positions });
    }
    return out;
  }

  function parseSecrets(text) {
    var out = [];
    if (!text) return out;
    var recs = text.split(SEP.line);
    var idx = 0;
    for (var i = 0; i < recs.length; i++) {
      var raw = recs[i];
      if (!raw || !raw.trim()) continue;                 // skip leading empty chunk
      var obj = parseJsonLoose(raw);
      var secrets = (obj && Array.isArray(obj.Secrets)) ? obj.Secrets : [];
      out.push({ index: idx++, points: secrets.length, secrets: secrets });
    }
    return out;
  }

  // Total narrations + points per narration (+ derived area count).
  function narrationStats(narrations) {
    narrations = narrations || [];
    return {
      total: narrations.length,
      points: narrations.map(function (n) { return n.points; }),
      areaCount: areaCount(narrations)
    };
  }

  // ---- analytics -----------------------------------------------------
  // Authoritative: areas = number of narrations / fixed age count.
  function areaCount(narrations) {
    return Math.floor((narrations && narrations.length ? narrations.length : 0) / AGES);
  }

  function mapFor(user, area, age) {
    var idx = (AGES * age) + area;
    return (user.maps && idx < user.maps.length) ? user.maps[idx] : null;
  }

  function computeOverview(users, area, defs, secrets, nowSecs) {
    if (nowSecs == null) nowSecs = Math.floor(Date.now() / 1000);
    area = area || 0;

    var registered = users.length, active = 0, narrationsPlayed = 0, secretsPlayed = 0, i, m;
    for (i = 0; i < users.length; i++) {
      var ll = users[i].lastlogin;
      if (ll != null && (nowSecs - ll) >= 0 && (nowSecs - ll) <= DAY) active++;
    }

    var startersPOIs = [0, 0, 0], finishersPOIs = [0, 0, 0];
	var startersSecrets = [0, 0, 0], finishersSecrets = [0, 0, 0];
    for (var age = 0; age < AGES; age++) {
      for (i = 0; i < users.length; i++) {
        var mp = mapFor(users[i], area, age);
		// POIs
        if (routeStarted(mp))
		{
			startersPOIs[age]++;
			narrationsPlayed++;
		}
        if (routeCompleted(mp))
		{ 
			finishersPOIs[age]++;
		}		
		// Secrets
		if (secretsStarted(mp))
		{
			startersSecrets[age]++;
			secretsPlayed++;
		}
        if (secretsCompleted(mp))
		{ 
			finishersSecrets[age]++;
		}
      }
    }
    
	// Finish rate per age for POIs = finishersPOIs / startersPOIs  (null when nobody started)
    var completionRatePOIs = [0, 1, 2].map(function (a) {
      return startersPOIs[a] ? finishersPOIs[a] / startersPOIs[a] : null;
    });

	// Finish rate per age for Secrets = finishersSecrets / startersSecrets  (null when nobody started)
    var completionRateSecrets = [0, 1, 2].map(function (a) {
      return startersSecrets[a] ? finishersSecrets[a] / startersSecrets[a] : null;
    });

    return {
      areaCount: areaCount(defs),
      totalNarrations: defs ? defs.length : 0,
      pointsPerNarration: defs ? defs.map(function (n) { return n.points; }) : [],
      registered: registered,
      active24h: active,
      narrations: narrationsPlayed, // POIs actually narrated (engagement)
	  secrets: secretsPlayed,		// Secrets played
      area: area,
      startersPOIs: startersPOIs,
      finishersPOIs: finishersPOIs,         // Q4: visitors by profile (completed tours)
      completionRatePOIs: completionRatePOIs,  // Q3: finish rate by profile for POIs
	  completionRateSecrets: completionRateSecrets,  // Q3: finish rate by profile for Secrets
      toursCompleted: finishersPOIs[0] + finishersPOIs[1] + finishersPOIs[2],
	  secretsCompleted: finishersSecrets[0] + finishersSecrets[1] + finishersSecrets[2],
    };
  }

  // ---- per-user summaries (Users section) ---------------------------
  // A visitor normally picks ONE age for the whole visit, so we treat the
  // first age showing any progress (POIs or secrets, in any area) as the
  // selected age. Area count comes from the narration definitions, so this
  // scales to any number of areas.
  function ageHasProgress(user, age, areas) {
    for (var a = 0; a < areas; a++) {
      var mp = mapFor(user, a, age);
      if (routeStarted(mp) || secretsStarted(mp)) return true;
    }
    return false;
  }

  function selectedAge(user, areas) {
    for (var age = 0; age < AGES; age++)
      if (ageHasProgress(user, age, areas)) return age;
    return null;
  }

  // All percentages are for the selected age, across every area.
  function userSummary(user, narrations) {
    var areas = areaCount(narrations);
    var age = selectedAge(user, areas);

    var perArea = [];
    var poiDoneAll = 0, poiTotalAll = 0, secDoneAll = 0, secTotalAll = 0;

    for (var a = 0; a < areas; a++) {
      var mp = (age == null) ? null : mapFor(user, a, age);
      var poiTotal = (mp && Array.isArray(mp.POIs))    ? mp.POIs.length    : 0;
      var poiDone  = poisVisited(mp);
      var secTotal = (mp && Array.isArray(mp.Secrets)) ? mp.Secrets.length : 0;
	  var secDone  = secretsVisited(mp);

      // Per-item status (id is the 0-based index / endpoint ID).
      var poiItems = [];
      if (mp && Array.isArray(mp.POIs))
      	for (var pi = 0; pi < mp.POIs.length; pi++)
	      poiItems.push({ id: pi, done: poiVisited(mp.POIs[pi]) });
	  
	  var secretItems = [];
	  if (mp && Array.isArray(mp.Secrets))
	    for (var si = 0; si < mp.Secrets.length; si++)
	      secretItems.push({ id: si, done: secretDiscovered(mp.Secrets[si]) });

	  perArea.push({
	          area: a,
	          name: getNameFloor(a),
	          poiDone: poiDone, poiTotal: poiTotal,
	          poiPct: poiTotal ? poiDone / poiTotal : null,
	          poiItems: poiItems,
	          secretDone: secDone, secretTotal: secTotal,
	          secretPct: secTotal ? secDone / secTotal : null,
	          secretItems: secretItems
	        });

      poiDoneAll += poiDone; poiTotalAll += poiTotal;
      secDoneAll += secDone; secTotalAll += secTotal;
    }

    return {
      id: user.id,
      email: user.email,
      name: user.name,
      lastlogin: user.lastlogin,
      selectedAge: age,
      selectedAgeName: (age == null) ? "—" : getNameAge(age),
      overallPoiPct:    poiTotalAll ? poiDoneAll / poiTotalAll : null,
      overallSecretPct: secTotalAll ? secDoneAll / secTotalAll : null,
      perArea: perArea
    };
  }

  function userSummaries(users, narrations) {
    return (users || []).map(function (u) { return userSummary(u, narrations); });
  }

  return {
	getNameFloor: getNameFloor,
    getNameAge: getNameAge,
    parseUsers: parseUsers,
    parseNarrations: parseNarrations,
	parseSecrets: parseSecrets,
    narrationStats: narrationStats,
    computeOverview: computeOverview,
    userSummary: userSummary,
    userSummaries: userSummaries,
    selectedAge: selectedAge,
    areaCount: areaCount,                   // now takes narration definitions    
    predicatesPOIs: { poiVisited: poiVisited, routeStarted: routeStarted, routeCompleted: routeCompleted },
	predicatesSecrets: { secretDiscovered: secretDiscovered, secretsStarted: secretsStarted, secretsCompleted: secretsCompleted }
  };
})();

