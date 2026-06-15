# Behobene Fehler

Protokoll der im LMU-Agent behobenen Fehler – was war kaputt und wie wurde es
gelöst. (Die offenen Punkte der Website stehen weiterhin in
[`BEKANNTE_FEHLER.md`](BEKANNTE_FEHLER.md).)

## LMU-Agent: Build-Blocker

### 1. Core ohne EF-Core-Pakete
**Problem:** `LMU.Agent.Core` nutzte `DbContext`, `DbSet`, `UseSqlite`, hatte aber
keine `PackageReference` → 12 Compilerfehler.
**Fix:** `Microsoft.EntityFrameworkCore.Sqlite` und `…Design` (10.0.9) ergänzt.

### 2. StatisticsParser: nicht existierende `FastestLapTime`
**Problem:** `driverResults.Min(r => r.FastestLapTime)` – `RaceResult` hat kein
solches Feld, sondern eine `LapTimes`-Liste.
**Fix:** Schnellste Runde aus allen `LapTimes` berechnet
(`SelectMany(r => r.LapTimes).Select(l => l.Time).Min()`), mit Leer-Absicherung.

### 3. StatisticsParser: `double` → `DateTime`
**Problem:** `LastRaceDate = driverResults.Max(r => r.Time)` wies einen `double`
(Rundenzeit) einer `DateTime`-Eigenschaft zu; `RaceResult` hatte gar kein Datum.
**Fix:** Feld `RaceResult.RaceDate` eingeführt und `LastRaceDate = Max(r.RaceDate)`.

### 4. Service: Zielframework-Mismatch
**Problem:** `LMU.Agent.Service` zielte auf `net8.0`, referenzierte aber das
`net10.0`-Core-Projekt → `NU1201`.
**Fix:** Service auf `net10.0` gehoben, Pakete auf 10.0.9 angeglichen.

### 5. Service: veraltetes `ServiceBase`
**Problem:** `LMUAgentService : ServiceBase` (`System.ServiceProcess`,
.NET-Framework-Stil) ist im SDK nicht mehr verfügbar (`CS1069`) und war zudem
nirgends registriert; parallel existierte eine Konsolen-`Program.cs`.
**Fix:** Auf den **Generic Host** umgestellt: `Program.cs` mit
`AddWindowsService()` + `AddHostedService<Worker>()`; neuer
`Worker : BackgroundService`; alte `ServiceBase`-Klasse gelöscht.

### 6. UI: fehlende Core-Referenz
**Problem:** `LMU.Agent.UI` referenzierte `LMU.Agent.Core` nicht → alle Parser-
und Modelltypen unbekannt (25 Fehler).
**Fix:** `ProjectReference` auf Core ergänzt.

### 7. UI: doppelte Endpunkte und Tippfehler
**Problem:** `Program.cs` definierte Minimal-APIs, die die MVC-Controller
duplizierten, rief das nicht existierende `GetEventsAsync` auf und enthielt den
`WeatherForecast`-Template-Rest.
**Fix:** Auf die Controller konsolidiert (`AddControllers()`/`MapControllers()`),
Minimal-API-Dubletten und Template-Reste entfernt.

### 8. Kaputte Solution-Dateien
**Problem:** `LMU.Agent.slnx` referenzierte `LMU.Agent.Services` (falscher Name);
die Root-`LMU_Agent.sln` zeigte auf falsche relative Pfade.
**Fix:** `.slnx`-Pfad korrigiert, redundante Root-`.sln` entfernt.

## LMU-Agent: Laufzeit & Logik

### 9. `GETUTCDATE()` auf SQLite
**Problem:** Default-Wert `HasDefaultValueSql("GETUTCDATE()")` ist SQL-Server-
Syntax und schlägt auf SQLite fehl.
**Fix:** Auf `CURRENT_TIMESTAMP` geändert.

### 10. Duplikate bei jedem Lauf (fehlende Idempotenz)
**Problem:** Alle Parser machten bedingungslos `Add(...)`; wiederholtes Einlesen
derselben Dateien erzeugte Duplikate, die DB wuchs unbegrenzt und Statistiken
wurden mehrfach gezählt.
**Fix:** Upsert anhand natürlicher Schlüssel (Events: Name+Date, Profile: Name,
RaceResults: Fahrer+Datum+Position, Statistics: DriverName). Dadurch ist das
Schreiben idempotent; der Worker liest nun gefahrlos periodisch neu ein.

### 11. Aufräumen
Leeren Platzhalter `Class1.cs` entfernt.
