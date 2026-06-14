# Bekannte Fehler

Ergebnis einer Logik-Analyse des Projekts. Sortiert nach Schweregrad.

## 1. Spalten vertauscht in AJAX-Zeilen (sicher)

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

## 2. `Calculate`-Endpunkt überspringt die Validierung (echter Bug)

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

## 3. Oszillation bei Tank-Grenzfällen (Modellierungsschwäche)

**Datei:** [SimracingUtility/Models/FuelCalcViewModel.cs:46](SimracingUtility/Models/FuelCalcViewModel.cs) (`CalculateFuel`, Schleife Zeile 46–54)

Die Iteration konvergiert in normalen Fällen sauber. Liegt der Spritbedarf jedoch
genau an einer Tankfüllungs-Grenze, kann sie pendeln: ein Boxenstopp mehr →
weniger Zeit → weniger Sprit → ein Stopp weniger → mehr Zeit → … Dann läuft die
Schleife bis `maxIter = 40` durch, und das Ergebnis hängt davon ab, auf welcher
Iteration sie endet. Dadurch ist im Randfall zusätzlich eine 1-Schritt-
Inkonsistenz zwischen `TotalFuelNeeded` (mit altem Stopp-Wert berechnet) und
`NumberOfPitStops`/`TotalTimeLost` (neuer Wert) möglich.

**Auswirkung:** Kein Crash, aber nicht-deterministisches Ergebnis im Grenzfall.

## 4. Kleinere Punkte

- **`Laps` als Nachkommazahl:** Es werden fraktionale Runden angezeigt (z. B.
  117,92). Für eine Spritplanung ist eher die aufgerundete Rundenzahl sinnvoll —
  Modellierungsfrage, kein Crash.
- **`SetupController`** ([SetupController.cs](SimracingUtility/Controllers/SetupController.cs))
  ist ein leeres Gerüst mit leeren `try/catch`-Blöcken — tote Funktionalität.

---

**Fazit:** Der eigentliche Rechenkern ist solide und durch Unit-Tests abgedeckt.
Die klar behebbaren Fehler liegen drumherum — **#1** ist der einzige direkt für
den Nutzer sichtbare Fehler, **#2** der relevanteste für die Datenqualität.
