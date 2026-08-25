/* =====================================================================
   Entry point.

   Boots the Unity WebGL player and the admin panel, then exposes the
   public facade on window.MuseumAdmin so it can be driven from anywhere
   (console, Unity SendMessage bridge, other scripts):

     MuseumAdmin.configure({ usersUrl, headers });
     MuseumAdmin.refresh();            // fetch -> render
     MuseumAdmin.loadFromText(text);   // render from a string (testing)
     MuseumAdmin.setHeader(title, subtitle);
     MuseumAdmin.setConnection(on, label);
     MuseumAdmin.setUsers([...]); MuseumAdmin.setEvents([...]);
     MuseumAdmin.setPoiStats([...]);
     MuseumAdmin.app                   // the GlobalManager instance
   ===================================================================== */
import { initUnity } from "./unity.js";
import { GlobalManager } from "./GlobalManager.js";

initUnity();

const app = new GlobalManager();

window.MuseumAdmin = {
  app: app,
  configure:     (o)     => app.configure(o),
  refresh:       ()      => app.refresh(),
  loadFromText:  (t, p, s)  => app.loadFromText(t, p, s),
  setHeader:     (t, s)  => app.setHeader(t, s),
  setConnection: (on, l) => app.setConnection(on, l),
  setUsers:      (rows)  => { const n = app.users.setRows(rows);   app.setTabCount("users", n);  return n; },
  setEvents:     (rows)  => { const n = app.events.setEvents(rows); app.setTabCount("events", n); return n; },
  setPoiStats:   (rows)  => app.events.setPoiStats(rows)
};

MuseumAdmin.configure({
	usersUrl: "https://www.yourvrexperience.com/mygames/template6dof/base/UserConsultAll.php",
	poisUrl: "https://www.yourvrexperience.com/mygames/template6dof/base/MuseumConsultAllPOIs.php",
	secretsUrl: "https://www.yourvrexperience.com/mygames/template6dof/base/MuseumConsultAllSecrets.php",
	eventsUrl: "https://www.yourvrexperience.com/mygames/template6dof/base/MuseumConsultAllEvents.php"
});	