# Bekannte Fehler

Ergebnis einer Logik-Analyse des Projekts. Sortiert nach Schweregrad.

> **Stand:** Die Punkte **#1–#7 sind behoben** – Details in
> [`BEHOBENE_FEHLER.md`](BEHOBENE_FEHLER.md). Offen bleiben nur noch die
> Betriebshinweise unter **#8**.

## 1. Spalten vertauscht in AJAX-Zeilen (sicher) — ✅ BEHOBEN

**Datei:** [SimracingUtility/Views/FuelCalc/Index.cshtml](SimracingUtility/Views/FuelCalc/Index.cshtml) (Zeile ~228–231)

Tabellen-Header und serverseitig gerenderte Zeilen verwenden die Reihenfolge
`Car Class`, `Car Name`. Die per AJAX angehängte Zeile vertauscht beide:

```js
var cols = [
    data.trackName,
    data.carName,    // landet unter "Car Class"  ✗
    data.carClass,   // landet unter "Car Name"   ✗
    ...
```

**Auswirkung:** Nach einer Berechnung (AJAX = Normalfall) werden Fahrzeugklasse
und -name in der Verlaufstabelle vertauscht angezeigt. Erst nach einem
Seiten-Neuladen stimmt die Zuordnung wieder.

**Fix:** In der `cols`-Liste `data.carClass` und `data.carName` tauschen
(und den irreführenden Kommentar in Zeile ~221 korrigieren).

## 2. `Calculate`-Endpunkt überspringt die Validierung (echter Bug) — ✅ BEHOBEN

**Datei:** [SimracingUtility/Controllers/FuelCalcController.cs:143](SimracingUtility/Controllers/FuelCalcController.cs)

Der AJAX-Endpunkt rechnet und speichert bedingungslos:

```csharp
public async Task<IActionResult> Calculate([FromBody] FuelCalcViewModel model)
{
    if (model == null) return BadRequest();
    model.CalculateFuel();
    // ... sofort persistieren, kein ModelState.IsValid
```

Der reguläre `POST Index` prüft dagegen `ModelState.IsValid`. Da der AJAX-Weg der
Standardpfad ist, greifen die `[Required]`-Regeln (TrackName, CarName, CarClass)
faktisch nie — leere/ungültige Berechnungen werden in die Datenbank geschrieben.

**Fix:** Im `Calculate`-Endpunkt `ModelState.IsValid` prüfen und bei ungültigen
Daten `BadRequest(ModelState)` zurückgeben, bevor gerechnet/gespeichert wird.

## 3. Oszillation bei Tank-Grenzfällen (Modellierungsschwäche) — ✅ BEHOBEN

**Datei:** [SimracingUtility/Models/FuelCalcViewModel.cs:46](SimracingUtility/Models/FuelCalcViewModel.cs) (`CalculateFuel`, Schleife Zeile 46–54)

Die Iteration konvergiert in normalen Fällen sauber. Liegt der Spritbedarf jedoch
genau an einer Tankfüllungs-Grenze, kann sie pendeln: ein Boxenstopp mehr →
weniger Zeit → weniger Sprit → ein Stopp weniger → mehr Zeit → … Dann läuft die
Schleife bis `maxIter = 40` durch, und das Ergebnis hängt davon ab, auf welcher
Iteration sie endet. Dadurch ist im Randfall zusätzlich eine 1-Schritt-
Inkonsistenz zwischen `TotalFuelNeeded` (mit altem Stopp-Wert berechnet) und
`NumberOfPitStops`/`TotalTimeLost` (neuer Wert) möglich.

**Auswirkung:** Kein Crash, aber nicht-deterministisches Ergebnis im Grenzfall.

**Fix:** `CalculateFuel` fährt das Rennen jetzt Runde für Runde nach (direkte
Simulation) statt den Fixpunkt iterativ zu schätzen. Runden und Boxenstopps sind
damit exakt gekoppelt und deterministisch; der 1-Schritt-Versatz zwischen
`TotalFuelNeeded` und `NumberOfPitStops`/`TotalTimeLost` entfällt. Gegen eine
Hand-Simulation verifiziert (u. a. Grenzfall ohne Phantom-Stopp am Rennende).

## 4. Kleinere Punkte (Spritrechner) — ✅ BEHOBEN

- **`Laps` als Nachkommazahl:** Es wurden fraktionale Runden angezeigt (z. B.
  117,92). **Fix:** Die Simulation zählt ganze Runden (eine angefangene Runde zählt
  voll), `Laps` ist dadurch ganzzahlig.

---

# Setup-Hub (zweite Analyse-Runde)

## 5. Übersicht lädt komplette Setup-Dateien mit (Effizienz, relevant) — ✅ BEHOBEN

**Datei:** [SimracingUtility/Controllers/SetupController.cs:31](SimracingUtility/Controllers/SetupController.cs) (`Index`)

```csharp
var query = _db.Setups
    .Include(s => s.Car)
    .Include(s => s.Track)
    .AsNoTracking()
    .AsQueryable();
...
var setups = await query.OrderByDescending(s => s.CreatedAt).Take(200).ToListAsync();
```

Es werden vollständige `Setup`-Entities geladen — **inklusive `FileData` (bytea)**.
Die Übersicht ([Views/Setup/Index.cshtml](SimracingUtility/Views/Setup/Index.cshtml))
zeigt aber nur Metadaten (Name, Auto, Strecke, `FileName`, `FileSize` …) an und
nutzt `FileData` nie.

**Auswirkung:** Bei der Kartenansicht werden die kompletten Datei-Bytes von bis zu
200 Setups unnötig aus der DB in den Speicher geladen — wächst linear mit Anzahl
und Größe der Setups.

**Fix:** Auf ein DTO/anonymes Objekt **ohne `FileData`** projizieren
(`.Select(s => new SetupListItem { ... })`); die Datei erst im `Download`-Endpunkt
laden (dort geschieht es bereits korrekt).

## 6. Filter-Platzhalter „Alle" geht beim Sim-Wechsel verloren (klein, UX) — ✅ BEHOBEN

**Datei:** [SimracingUtility/wwwroot/js/setup-hub.js](SimracingUtility/wwwroot/js/setup-hub.js) (`fillSelect`)

Auf der Übersicht ([Index.cshtml](SimracingUtility/Views/Setup/Index.cshtml)) ist die
erste Option der Auto-/Strecken-Dropdowns serverseitig `Alle`. Ändert man die
Simulation, ersetzt das JS den kompletten Inhalt und setzt als Platzhalter
`-- Auto wählen --` bzw. `-- Strecke wählen --`.

**Auswirkung:** Rein kosmetisch — der Wert bleibt leer (`""`), die Filterung
funktioniert weiterhin korrekt; nur die Beschriftung passt im Filter-Kontext nicht.

**Fix:** Platzhaltertext über ein `data-`-Attribut konfigurierbar machen (Formular:
„wählen", Filter: „Alle").

## 7. Stiller Cap auf 200 Setups (klein) — ✅ BEHOBEN

**Datei:** [SimracingUtility/Controllers/SetupController.cs:43](SimracingUtility/Controllers/SetupController.cs)

`Take(200)` begrenzt die Übersicht ohne Paginierung und ohne Hinweis. Sobald mehr
als 200 (gefilterte) Setups existieren, fehlen ältere Einträge unbemerkt.

**Fix:** Paginierung ergänzen oder zumindest einen Hinweis „Es werden die neuesten
200 Setups angezeigt" rendern.

## 8. Kleinere Punkte (Setup-Hub)

- **Automatische Migration beim Start:** `db.Database.Migrate()` läuft in
  [Program.cs](SimracingUtility/Program.cs) unbedingt bei jedem Start (auch in
  Produktion). Bequem im Dev, in Produktion aber eher kontrolliert über ein
  Deployment ausführen.
- **Sim-fremder Filter-Parameter:** Wird `carId`/`trackId` einer anderen
  Simulation als `sim` übergeben (z. B. manipulierte URL), liefert `Index` eine
  leere Liste statt den Parameter zu ignorieren — verwirrend, aber harmlos.

---

# LMU-Agent / Statistik-Integration (bewusst offene Punkte)

Diese Punkte sind **absichtlich offen** und für die Übergabe dokumentiert.

## A. Stats nicht an ein Login gebunden
`/LmuStats` zeigt den zuletzt gepushten bzw. per `?owner=` gewählten Fahrer; es gibt
keine Bindung an einen eingeloggten Account. **Begründung:** Login/Views sind
Platzhalter fürs spätere Fremdsystem. Die Naht ist vorhanden (`OwnerKey` /
Header `X-User-Key`, siehe [INTEGRATION.md](INTEGRATION.md)) – das Host-System bindet
es an seine Nutzer.

## B. Ingest-Sicherheit (nur Dev)
Geteilter API-Key (`Lmu:IngestApiKey`, Default `dev-local-key`), kein HTTPS erzwungen,
`X-User-Key` wird nicht validiert. **Vor produktivem Hosting:** echtes Secret, HTTPS,
`X-User-Key` gegen den eigenen User-Store prüfen.

## C. Telemetrie-Download nur same-machine
Der Link zeigt auf den lokal laufenden Agent (`localhost:5601`). Für ein zentral
gehostetes Portal müsste der Agent die ZIPs aktiv hochladen statt lokal zu servieren.

## D. Kuratierte Team-Liste pflegebedürftig
Reale Teamnamen **ohne** Jahr/Startnummer (z. B. „RLR MSport #15"), die nicht in
[`StandardTeams`](LMU_Agent/src/LMU.Agent.Core/Services/StandardTeams.cs) stehen,
werden nicht als Stock-Livery erkannt. Bei Bedarf dort ergänzen. Saison-Varianten
mit Jahr+Nummer werden automatisch über das Muster erkannt.

## E. Event-/DriverProfile-Parser sind Platzhalter
Unverifiziertes Schema, vom Agent nicht aktiv genutzt. Entweder gegen echte Quellen
verifizieren oder entfernen.

---

**Fazit:** Der Rechenkern ist solide und durch Unit-Tests abgedeckt. Die
Spritrechner-Punkte **#1–#4** und die Setup-Hub-Punkte **#5–#7** sind behoben
(Details in [`BEHOBENE_FEHLER.md`](BEHOBENE_FEHLER.md)); offen bleiben nur die
betrieblichen Hinweise unter **#8**. Beim LMU-Agent sind alle offenen Punkte (A–E)
bewusst gewählt und oben begründet.
