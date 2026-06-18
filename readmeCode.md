# readmeCode — Code-Referenz (alle Klassen, Methoden & Abhängigkeiten)

Dieses Dokument beschreibt **jede C#-Klasse und ihre Methoden** – wie sie funktionieren und
wovon sie abhängen – sowie die übrigen Programmiersprachen im Projekt (JavaScript,
PowerShell). Es dient als Nachschlagewerk, um den Code **vollständig zu verstehen und bei
spezifischen Rückfragen erklären** zu können.

> **Verwendete Werkzeuge / Kontext:** Für dieses Projekt wurden **Ollama mit einem
> Qwen-Modell** sowie **Claude** eingesetzt. Das Projekt hat höhere Anforderungen, als ich
> allein erfüllen kann, soll später in eine **laufende Website** eingebaut werden und muss
> daher **hohen Standards** genügen.

**Aufbau des Projekts:** Zwei .NET-10-Projekte – die Website **SimracingUtility** (ASP.NET
Core MVC, EF Core, PostgreSQL) und der lokale **LMU-Agent** (WinForms-Tray-App + Core-Library
mit SQLite-Cache + Legacy-Web-UI). Dazu Frontend-JavaScript und ein PowerShell-Publish-Skript.

**Inhalt**

1. Website (SimracingUtility) — 1.1 Controllers · 1.2 Models, Daten & Start · 1.3 Services
2. LMU-Agent — 2.1 Core (Parser, Modelle, Daten) · 2.2 Service & UI
3. Frontend & Skripte (JavaScript, PowerShell)

---

## 1. Website (SimracingUtility)

### 1.1 Controllers

### HomeController — `SimracingUtility/Controllers/HomeController.cs`
- **Zweck:** Liefert die statischen Standardseiten der Website (Startseite, Datenschutz, Fehlerseite).
- **Abhängigkeiten:** Keine injizierten Services (kein Konstruktor). Erbt von `Controller`. Verwendet `ErrorViewModel` aus `SimracingUtility.Models` und `System.Diagnostics.Activity`. Keine Routen-/Autorisierungs-Attribute auf Klassenebene.
- **Methoden/Actions:**
  - `public IActionResult Index()` — Gibt schlicht die zugehörige View ohne Modell zurück (Startseite).
  - `public IActionResult Privacy()` — Gibt die Datenschutz-View ohne Modell zurück.
  - `[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)] public IActionResult Error()` — Deaktiviert Caching per Attribut und gibt die Fehler-View mit einem `ErrorViewModel` zurück. Die `RequestId` wird aus `Activity.Current?.Id` befüllt; ist diese null, dient `HttpContext.TraceIdentifier` als Fallback.

### DownloadController — `SimracingUtility/Controllers/DownloadController.cs`
- **Zweck:** Stellt das separat erzeugte LMU-Agent-Artefakt (ZIP) zum Download bereit. Die Datei wird vom Publish-Skript des Agents nach `wwwroot/downloads/` gelegt.
- **Abhängigkeiten:** Konstruktor injiziert `IWebHostEnvironment _env` (für `WebRootPath`). Erbt von `Controller`.
- **Wichtige Felder/Konstanten:** `private const string AgentFileName = "LMU.Agent.Service.zip"` — Dateiname des Artefakts (zugleich angebotener Download-Name).
- **Methoden/Actions:**
  - `public IActionResult Index()` — Setzt `ViewBag.AgentAvailable` auf das Ergebnis von `File.Exists(GetAgentPath())` (zeigt der View, ob ein Download bereitsteht) und gibt die View zurück.
  - `public IActionResult Agent()` — Bestimmt den Pfad via `GetAgentPath()`. Existiert die Datei nicht, wird eine deutsche Hinweismeldung in `TempData["DownloadMessage"]` gelegt und per Redirect auf `Index` umgeleitet. Andernfalls wird die Datei als Stream geöffnet und per `File(stream, "application/zip", AgentFileName)` als ZIP-Download zurückgegeben.
  - `private string GetAgentPath()` — Baut den absoluten Pfad `WebRootPath/downloads/LMU.Agent.Service.zip` per `Path.Combine` zusammen.

### LmuStatsApiController — `SimracingUtility/Controllers/LmuStatsApiController.cs`
- **Zweck:** REST-Endpunkt, der die vom LMU-Agent gepushten Fahrer-Statistiken in PostgreSQL entgegennimmt (Upsert) und sie als JSON wieder ausliefert.
- **Abhängigkeiten:** Konstruktor injiziert `ApplicationDbContext _db` und `IConfiguration _config`. Erbt von `ControllerBase`. Klassenattribute `[ApiController]` und `[Route("api/lmu")]`. Verwendet die DTOs/Modelle (`LmuStatsPushDto`, `LmuStatsResponseDto`, `LmuDriver`, `LmuCategoryStat`, `LmuTrackBest`, `LmuRacedWith`, `CategoryStatsDto`, `TrackBestDto`, `RacedWithDto`). Konfig-Schlüssel: `Lmu:IngestApiKey`.
- **Methoden/Actions:**
  - `[HttpPost("stats")] public async Task<IActionResult> Push([FromBody] LmuStatsPushDto? dto)` — Nimmt einen gepushten Statistik-Datensatz entgegen.
    - **Auth:** Ist `Lmu:IngestApiKey` gesetzt, muss der Header `X-Api-Key` exakt übereinstimmen, sonst `Unauthorized()`. Kein Key konfiguriert → keine Prüfung.
    - **Validierung:** Bei `dto == null` oder leerem `DriverName` → `BadRequest("DriverName fehlt.")`.
    - **OwnerKey:** Optionaler Host-Nutzer-Identifier aus Header `X-User-Key` (getrimmt); fehlt er, leerer String.
    - **Upsert:** Bei leerem `ownerKey` Suche per `OwnerKey == "" && DriverName == dto.DriverName`, sonst nur per `OwnerKey == ownerKey` — jeweils inkl. `Categories`, `TrackBests`, `RacedWith`. Fehlt der Fahrer → neuer `LmuDriver`; existiert er → `DriverName` aktualisieren und die Kind-Sammlungen via `RemoveRange` aus dem Kontext entfernen und leeren.
    - **Befüllung:** `UpdatedAt = DateTime.UtcNow`, zwei Kategorien ("Sprint"/"Endurance") über `ToCategory`, `BestLapsByTrack`→`LmuTrackBest`, `MostRacedWith`→`LmuRacedWith` (`Kind="Driver"`), `MostRacedAgainstTeams`→`Kind="Team"`.
    - **Rückgabe:** Nach `SaveChangesAsync()` ein `Ok(...)` mit `driver`, `tracks` (Anzahl TrackBests), `racedWith` (Anzahl RacedWith).
  - `[HttpGet("stats")] public async Task<IActionResult> Get([FromQuery] string? owner, [FromQuery] string? driver)` — Liest die Statistik als JSON (inkl. der drei Kind-Sammlungen). Auswahl: bei `owner` per `OwnerKey`, sonst bei `driver` per `DriverName`, sonst der nach `UpdatedAt` zuletzt aktualisierte Fahrer. Keiner gefunden → `NotFound()`, sonst `Ok(ToResponse(d2))`.
  - `private static LmuStatsResponseDto ToResponse(LmuDriver d)` — Mappt die Entität auf das Antwort-DTO. Lokale `Cat(name)` liefert leeres `CategoryStatsDto` falls die Kategorie fehlt; lokale `Companions(kind)` filtert `RacedWith` nach `Kind`, sortiert absteigend nach `RacesShared`. Setzt zudem `OwnerKey`, `DriverName`, `UpdatedAt`, beide Kategorien, `BestLapsByTrack` (nach `Track` sortiert) und beide Companion-Listen.
  - `private static LmuCategoryStat ToCategory(string category, CategoryStatsDto s)` — Erzeugt eine `LmuCategoryStat` aus dem DTO. Überträgt alle Zähler; `LastRaceDate` wird per `DateTime.SpecifyKind(..., DateTimeKind.Utc)` als UTC markiert (Npgsql speichert `timestamptz` als UTC).

### FuelCalcController — `SimracingUtility/Controllers/FuelCalcController.cs`
- **Zweck:** Fuel-Calculator-Seite: Eingabeformular, serverseitige Berechnung, Persistenz und Anzeige der letzten Berechnungen (klassischer Form-Post und AJAX-JSON-Pfad).
- **Abhängigkeiten:** Konstruktor injiziert `IWebHostEnvironment _env` und `IRecentFuelCalcService _recentService`. Verwendet `FuelCalcViewModel`, `RecentFuelCalculation`, `System.Text.Json`, `SelectListItem`. Liest `wwwroot/data/cars.json`.
- **Methoden/Actions:**
  - `private async Task PopulateRecentCalcsAsync()` — Legt die letzten 20 Berechnungen via `_recentService.GetRecentAsync(20)` in `ViewBag.RecentFuelCalcs`.
  - `private void LoadCarsIntoViewBag()` — Lädt `cars.json` robust (drei Kandidatenpfade: `WebRootPath`, `ContentRootPath/wwwroot`, aktuelles Verzeichnis). Parst `carClasses` (String-Array → distinct) und `cars` (Objekte mit `name` → distinct) zu `SelectListItem`-Listen (`ViewBag.CarClasses`/`CarNames`) und serialisiert `{name,class}`-Objekte nach `ViewBag.CarsJson` (für die client-seitige Klassenfilterung). Defensive `ValueKind`-Prüfungen; jeder Fehler → leere Listen (kein Crash).
  - `public async Task<IActionResult> Index()` — GET: befüllt Recent-Calcs + Autodaten, gibt die View mit leerem `FuelCalcViewModel` zurück.
  - `[HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> Index(FuelCalcViewModel model)` — Klassischer Form-Post. Bei gültigem `ModelState`: `model.CalculateFuel()`, Mapping in `RecentFuelCalculation`, `CreateAsync`. Danach Recent-Calcs/Autodaten neu, View mit Modell (auch bei ungültig — dann ohne Speicherung).
  - `[HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> Calculate([FromBody] FuelCalcViewModel model)` — AJAX/JSON. `model == null` → `BadRequest()`. Validiert explizit `ModelState` (sonst greifen die `[Required]`-Regeln auf dem AJAX-Pfad nie) → `BadRequest(ModelState)`. Sonst `CalculateFuel()`, Mapping, `CreateAsync`. Rückgabe `Json(...)` mit `id`, `numberOfPitStops`, `laps`, `totalFuelNeeded`, `fuelReserveLiters`, `reserveExceedsTank`, `stints` (projiziert auf `StintNumber/Laps/FromLap/ToLap/Fuel`), Streckendaten und `createdAt` (UTC→Lokalzeit, ISO-`"o"`).

### LmuEventsController — `SimracingUtility/Controllers/LmuEventsController.cs`
- **Zweck:** Kuratierte, statische Übersicht „Was steht an in Le Mans Ultimate" (Special Events + Community-Meisterschaften), ohne Live-Abruf; Daten aus `wwwroot/data/lmu-events.json`.
- **Abhängigkeiten:** Konstruktor injiziert `IWebHostEnvironment _env`. Verwendet `LmuEventCatalog`, `System.Text.Json`. Liest `wwwroot/data/lmu-events.json`.
- **Methoden/Actions:**
  - `public IActionResult Index()` — Setzt `ViewBag.Today` (heute als `DateOnly`) und gibt die View mit dem über `LoadCatalog()` geladenen Katalog zurück.
  - `private LmuEventCatalog LoadCatalog()` — Sucht `lmu-events.json` über dieselben drei Kandidatenpfade. Keiner vorhanden → leeres `LmuEventCatalog`. Sonst Deserialisierung (`PropertyNameCaseInsensitive = true`); `null` → leeres Objekt; jeder Fehler → leeres `LmuEventCatalog` (defekte/fehlende Datei crasht die Seite nicht).

### LmuStatsController — `SimracingUtility/Controllers/LmuStatsController.cs`
- **Zweck:** Zeigt die vom LMU-Agent gepushten Statistiken an und verwaltet die selbst gepflegte SimGrid-Profil-URL (inkl. optionaler öffentlicher SimGrid-Stats).
- **Abhängigkeiten:** Konstruktor injiziert `ApplicationDbContext _db`, `IConfiguration _config`, `SimGridClient _simGrid`. Verwendet `SimGridProfile`. Konfig-Schlüssel: `Lmu:AgentTelemetryUrl`.
- **Methoden/Actions:**
  - `public async Task<IActionResult> Index()` — Setzt `ViewBag.TelemetryBase` (`Lmu:AgentTelemetryUrl`, Fallback `http://localhost:5601`). Lädt den zuletzt aktualisierten Fahrer (`OrderByDescending(UpdatedAt)`) inkl. `Categories`, `TrackBests`, `RacedWith`. Ist eine `SimGridProfileUrl` gesetzt, werden best-effort die öffentlichen Stats via `_simGrid.GetStatsAsync(...)` geladen und in `ViewBag.SimGridStats` gelegt. View mit `driver` (kann null sein).
  - `[HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> SetSimGrid(int id, string? simGridUrl)` — Speichert die SimGrid-URL am Fahrer. Fahrer per `Id`; fehlt → `NotFound()`. Leere Eingabe → `SimGridProfileUrl = null`. Sonst Validierung über `SimGridProfile.TryParse(...)`: bei Erfolg kanonische URL speichern, bei Misserfolg `TempData["SimGridError"]` + Redirect (nur `thesimgrid.com` wird akzeptiert). Nach Erfolg `SaveChangesAsync()` + Redirect.

### SetupController — `SimracingUtility/Controllers/SetupController.cs`
- **Zweck:** Setup-Hub: öffentliche Übersicht mit Filterung (Sim → Auto → Strecke) sowie Upload, Download und Löschen von Setup-Dateien (in der DB als BLOB), inkl. JSON-Endpunkten für abhängige Dropdowns.
- **Abhängigkeiten:** Konstruktor injiziert `ApplicationDbContext _db` und `UserManager<IdentityUser> _userManager`. Verwendet `Setup`, `SimCar`, `SimTrack`, `SimGame`, `SimGameInfo`, `SetupUploadViewModel`. Action-Attribute: `[AllowAnonymous]`, `[Authorize]`, `[HttpGet/Post]`, `[ValidateAntiForgeryToken]`, `[RequestSizeLimit]`.
- **Wichtige Felder/Konstanten:** `MaxFileSizeBytes = 25 * 1024 * 1024` (25 MB, erlaubt ZIPs mit Telemetrie `.ld/.ldx`); `MaxOverviewItems = 200`.
- **Methoden/Actions:**
  - `[AllowAnonymous] [HttpGet] Index(SimGame? sim, int? carId, int? trackId)` — Öffentliche Übersicht. `AsNoTracking`-Query, optionale Filter, absteigend nach `CreatedAt`, begrenzt auf `MaxOverviewItems`, **projiziert bewusst nur Metadaten** (kein `FileData`). Setzt ViewBag: `OverviewCapped`, `OverviewLimit`, Filterwerte, Dropdown-Listen, `CurrentUserId`.
  - `[Authorize] [HttpGet] Upload()` — Zeigt das Upload-Formular mit leerem ViewModel.
  - `[Authorize] [HttpPost] [ValidateAntiForgeryToken] [RequestSizeLimit(MaxFileSizeBytes + 256 KB)] Upload(SetupUploadViewModel model)` — Validiert über `ValidateUploadAsync`; bei ungültig Formular erneut. Sonst Datei → `byte[]`, baut `Setup` (Owner, Sim/Car/Track, getrimmte Felder, `FileName` via `Path.GetFileName`, `ContentType`-Fallback, `FileData`-BLOB, `CreatedAt`), `SaveChangesAsync()`, `TempData`-Erfolg, Redirect auf `Index`.
  - `[Authorize] [HttpGet] Download(int id)` — Lädt das Setup inkl. `FileData`; fehlt → `NotFound()`. Gibt `File(..., "application/octet-stream", FileName)` zurück (erzwingt Download).
  - `[Authorize] [HttpPost] [ValidateAntiForgeryToken] Delete(int id)` — Lädt das Setup; fehlt → `NotFound()`; nicht der Eigentümer → `Forbid()`; sonst Entfernen, Speichern, Redirect.
  - `[AllowAnonymous] [HttpGet] Cars(SimGame sim)` / `Tracks(SimGame sim)` — JSON-Endpunkte für die abhängigen Dropdowns: Autos/Strecken der Sim, nach Name sortiert, als `{ id, name }`.
  - `private async Task ValidateUploadAsync(...)` — Serverseitige Validierung: `Sim` Pflicht; Datei vorhanden/Größe ≤ `MaxFileSizeBytes`; Endung gegen `SimGameInfo.ExtensionsFor(Sim)`; `CarId`/`TrackId` müssen existieren UND zur Sim gehören. Fehler in `ModelState`.
  - `private static SimSelectList(...)`, `CarSelectListAsync(...)`, `TrackSelectListAsync(...)` — bauen die `SelectListItem`-Listen (Sims aus dem Enum; Autos/Strecken je Sim mit Vorauswahl).

### 1.2 Models, Daten & Start

### ErrorViewModel — `SimracingUtility/Models/ErrorViewModel.cs`
- **Zweck:** ViewModel der Fehlerseite; transportiert die Request-ID.
- **Eigenschaften:** `RequestId : string?`; `ShowRequestId : bool` (read-only, true wenn `RequestId` gesetzt).

### SimCar — `SimracingUtility/Models/SimCar.cs`
- **Zweck:** Auto einer Simulation, geseedet aus `sim_data.json`; `(Sim, Slug)` eindeutig.
- **Abhängigkeiten:** EF-Entity (DbSet `SimCars`), Navigation `Setups` (1:n), Enum `SimGame`, eindeutiger Index `(Sim, Slug)`.
- **Eigenschaften:** `Id`; `Sim : SimGame`; `Slug` (required, MaxLength 80); `Name` (required, 150); `Setups : ICollection<Setup>`.

### SimTrack — `SimracingUtility/Models/SimTrack.cs`
- **Zweck:** Strecke einer Simulation; `(Sim, Slug)` eindeutig.
- **Eigenschaften:** `Id`; `Sim`; `Slug` (required, 80); `Name` (required, 150); `Setups`.

### Setup — `SimracingUtility/Models/Setup.cs`
- **Zweck:** Hochgeladenes Setup; die Datei wird als `FileData` (PostgreSQL `bytea`) in der DB gespeichert.
- **Abhängigkeiten:** EF-Entity (DbSet `Setups`); FKs `Owner`(IdentityUser, Cascade), `Car`(Restrict), `Track`(Restrict); Indizes `(Sim,CarId,TrackId)` und `OwnerId`.
- **Eigenschaften:** `Id`; `OwnerId`(req)/`Owner?`; `Sim`; `CarId`/`Car?`; `TrackId`/`Track?`; `Name?`(150); `Description?`(2000); `LapTime?`(20); `TrackTempCelsius?`; `CreatorName?`(100); `FileName`(req,260); `ContentType`(150, Default `application/octet-stream`); `FileSize : long`; `FileData : byte[]`(req); `CreatedAt`(Default UtcNow).

### SetupUploadViewModel — `SimracingUtility/Models/SetupUploadViewModel.cs`
- **Zweck:** Eingabemodell des Upload-Formulars (getrennt vom Entity wegen `IFormFile` + Validierung).
- **Eigenschaften:** `Sim?`(req), `CarId?`(req), `TrackId?`(req), `File : IFormFile?`(req), `Name?`(150), `Description?`(2000), `LapTime?`(20), `TrackTempCelsius?`(`Range(-20,80)`), `CreatorName?`(100). Deutsche Fehlermeldungen via DataAnnotations.

### RecentFuelCalculation — `SimracingUtility/Models/RecentFuelCalculation.cs`
- **Zweck:** Persistierte Spritberechnung (Tabelle `FuelCalc`) als „zuletzt berechnet"-Historie.
- **Eigenschaften:** `Id`; `EventDurationMinutes`; `TrackName`(req,200); `PitBoxTime`; `FuelPerLap`; `TotalFuelNeeded`; `NumberOfPitStops`; `DriveThroughTime`; `TotalTimeLost`; `CarName`(req,200); `CarClass`(req,100); `FuelTankCapacity`; `TimePerLap`; `Laps : double`; `CreatedAt?`(Default UtcNow); `UpdatedAt?`.

### LmuStatsPushDto & DTOs — `SimracingUtility/Models/LmuStatsPushDto.cs`
- **LmuStatsPushDto** — Push-Payload des Agents (eigenständige DTOs, damit die Website nicht vom Agent-Projekt abhängt; case-insensitiver Feldabgleich): `DriverName`, `Sprint`/`Endurance : CategoryStatsDto`, `BestLapsByTrack : List<TrackBestDto>`, `MostRacedWith`/`MostRacedAgainstTeams : List<RacedWithDto>`.
- **CategoryStatsDto** — `TotalRaces`, `Wins`, `Podiums`, `Top5`, `Top10`, `TopHalf`, `Dnf`, `BestPosition` (int) + `LastRaceDate`.
- **TrackBestDto** — `Track`, `BestLapTime : double`.
- **RacedWithDto** — `Name`, `RacesShared : int`.
- **LmuStatsResponseDto** — Antwort der Lese-API (host-agnostisch): `OwnerKey`, `DriverName`, `UpdatedAt`, `Sprint`/`Endurance`, `BestLapsByTrack`, `MostRacedWith`, `MostRacedAgainstTeams`.

### FuelCalcViewModel — `SimracingUtility/Models/FuelCalcViewModel.cs`
- **Zweck:** ViewModel des Fuel Calculators; simuliert das Rennen Runde für Runde für Spritbedarf, Boxenstopps, Stints und Sicherheitsreserve.
- **Abhängigkeiten:** `[NotMapped]` (kein EF-Mapping); nutzt `FuelStint`.
- **Eingaben:** `EventDurationMinutes`, `TrackName`/`CarName`/`CarClass`, `PitBoxTime`, `DriveThroughTime`, `FuelPerLap`, `FuelTankCapacity`, `TimePerLap`, `FuelReserve`, `FuelReserveUnit` (`"Laps"`/`"Percent"`, Default `"Laps"`).
- **Ergebnisse:** `TotalFuelNeeded`, `NumberOfPitStops`, `TotalTimeLost`, `Laps`, `FuelReserveLiters`, `ReserveExceedsTank`, `Stints : List<FuelStint>`.
- **Methode `void CalculateFuel()`** — Kernlogik:
  1. **Guard:** `TimePerLap <= 0` oder `FuelTankCapacity <= 0` → alle Ergebnisfelder null/leer, Abbruch.
  2. `eventSeconds = EventDurationMinutes*60`; `pitCost = PitBoxTime + DriveThroughTime`; `eps = 1e-9`.
  3. Start: `laps=0`, `pitStops=0`, `clockRemaining=eventSeconds`, `fuelInTank=FuelTankCapacity` (voller Tank), `maxLaps=1_000_000` (Sicherheitsnetz). Bewusst **direkte** Runde-für-Runde-Simulation (statt iterativer Schätzung, die im Tank-Grenzfall pendelte) → koppelt Runden und Stopps exakt und ist deterministisch.
  4. Schleife (solange `clockRemaining > eps` und `laps < maxLaps`):
     - **Tank-Check vor der Runde:** Wenn `FuelPerLap > 0` und `fuelInTank < FuelPerLap - eps`: reicht ein voller Tank nicht für eine Runde (`FuelTankCapacity < FuelPerLap`) → `break`; bliebe nach dem Stopp keine Zeit (`clockRemaining - pitCost <= eps`) → `break` (kein Phantom-Stopp); sonst laufenden Stint abschließen (`FuelStint` anhängen), `clockRemaining -= pitCost`, `pitStops++`, volltanken.
     - **Runde:** `laps++`, `lapsThisStint++`, `clockRemaining -= TimePerLap`, `fuelInTank -= FuelPerLap`. Eine begonnene Runde zählt **voll**.
  5. Letzten Stint abschließen.
  6. `consumedFuel = max(0, laps*FuelPerLap)`.
  7. **Reserve** („mehr laden, gleiche Strategie"): nur falls `FuelReserve>0`, `laps>0`, `FuelPerLap>0` — `reserveLaps` = `"Percent"`: `laps*FuelReserve/100`, sonst `FuelReserve` (Runden). `FuelReserveLiters = max(0, reserveLaps*FuelPerLap)`.
  8. `endFuel = max(0, fuelInTank)`; `ReserveExceedsTank = FuelReserveLiters > endFuel + eps` (Reserve wird zusätzlich geladen, ändert die Stoppzahl nicht; passt sie nicht in den Resttank → nur Hinweis).
  9. Ergebnis schreiben: `NumberOfPitStops`, `TotalFuelNeeded = consumedFuel + FuelReserveLiters`, `TotalTimeLost = pitStops*pitCost`, `Laps`, `Stints`.

### FuelStint — `SimracingUtility/Models/FuelCalcViewModel.cs`
- **Zweck:** Ein Stint (zusammenhängende Runden auf einer Tankfüllung). **Eigenschaften:** `StintNumber`, `FromLap`, `ToLap`, `Laps`, `Fuel`.

### LmuDriver (+ LmuCategoryStat, LmuTrackBest, LmuRacedWith) — `SimracingUtility/Models/LmuDriver.cs`
- **LmuDriver** — Vom Agent gepushte Auswertung (Upsert je Fahrer). DbSet `LmuDrivers`; 1:n zu den drei Kind-Tabellen (Cascade); nicht-eindeutige Indizes auf `OwnerKey` und `DriverName`. **Eigenschaften:** `Id`; `OwnerKey`(200, leer=Einzelnutzer/Dev); `DriverName`(req,150); `UpdatedAt`; `SimGridProfileUrl?`(300, vom Nutzer gepflegt, beim Push nicht überschrieben); `Categories`/`TrackBests`/`RacedWith`.
- **LmuCategoryStat** — Kennzahlen je Kategorie. `Id`; `LmuDriverId`/`Driver?`; `Category`(req,20); `TotalRaces`, `Wins`, `Podiums`, `Top5`, `Top10`, `TopHalf`, `Dnf`, `BestPosition`; `LastRaceDate`.
- **LmuTrackBest** — Beste Runde je Strecke. `Id`; FK; `Track`(req,150); `BestLapTime : double`.
- **LmuRacedWith** — Mitstreiter/Gegner-Team. `Id`; FK; `Name`(req,150); `Kind`(req,20: `"Driver"`/`"Team"`); `RacesShared`.

### LmuEventCatalog (+ LmuSpecialEvent, LmuChampionship) — `SimracingUtility/Models/LmuEventCatalog.cs`
- **LmuEventCatalog** — In-Memory-Modell der kuratierten LMU-Events (Quelle `lmu-events.json`, kein Scraping). **Eigenschaften:** `SpecialEvents`, `Championships`.
  - `List<LmuSpecialEvent> UpcomingSpecialEvents(DateOnly today)` — Events mit `Date >= today`, aufsteigend nach Datum.
  - `List<LmuChampionship> SortedChampionships()` — Team-Events zuerst (`IsTeam` desc), dann nach `StartsOn` (null → ans Ende), dann `Name` (case-insensitiv).
- **LmuSpecialEvent** — `Title`, `Date : DateOnly`, `Track`, `Classes`, `Duration`.
- **LmuChampionship** — `Name`, `Organizer`, `StartsOn : DateOnly?`, `IsTeam : bool`, `Url`, `Note?`.

### SimGridStats — `SimracingUtility/Models/SimGridStats.cs`
- **Zweck:** Aus dem öffentlichen SimGrid-Profil gelesene Renn-Kennzahlen (best effort), **ohne PII**. **Eigenschaften:** `Starts`, `Wins`, `Podiums`, `Top5`, `FastestLaps` (alle `int?`); `HasAny : bool` (true, wenn mind. ein Wert gesetzt).

### SimGame / SimGameInfo — `SimracingUtility/Models/SimGame.cs`
- **SimGame** (Enum, als int persistiert): `iRacing=1`, `LMU=2`, `ACC=3` (mit `[Display]`-Namen).
- **SimGameInfo** (statisch): `DisplayNames` (Anzeigenamen); `AllowedExtensions` (iRacing `.sto`/`.zip`, LMU `.svm`/`.zip`, ACC `.json`/`.zip` — `.zip` für alle erlaubt, um Setup + Telemetrie zu bündeln). `DisplayName(sim)` und `ExtensionsFor(sim)` mit Fallbacks.

### ApplicationDbContext — `SimracingUtility/Data/ApplicationDbContext.cs`
- **Zweck:** EF-Core-DbContext der Website inkl. ASP.NET Identity (`IdentityDbContext`).
- **DbSets:** `RecentFuelCalculations`, `Setups`, `SimCars`, `SimTracks`, `LmuDrivers`, `LmuCategoryStats`, `LmuTrackBests`, `LmuRacedWith` (+ Identity-Tabellen).
- **`OnModelCreating`** (Fluent API): `RecentFuelCalculation` → Tabelle `"FuelCalc"`, Pflichtfelder; `SimCar`/`SimTrack` → eindeutiger Index `(Sim, Slug)`; `Setup` → FK Owner(Cascade)/Car(Restrict)/Track(Restrict), Indizes; `LmuDriver` → Längen, nicht-eindeutige Indizes (`OwnerKey`/`DriverName`), 1:n zu den drei Kind-Tabellen (Cascade); `LmuCategoryStat`/`LmuTrackBest`/`LmuRacedWith` → Längen.

### ApplicationDbContextFactory — `SimracingUtility/Data/ApplicationDbContextFactory.cs`
- **Zweck:** Design-Time-Factory (`IDesignTimeDbContextFactory`) für die EF-Tools, damit `dotnet ef` die App-Pipeline nicht ausführt.
- **`CreateDbContext(string[] args)`** — Baut Konfiguration aus `appsettings(.Development).json`, liest `DefaultConnection` (Fallback `Host=localhost;...postgres`), `UseNpgsql`, gibt neue Context-Instanz zurück.

### SimDataSeeder — `SimracingUtility/Data/SimDataSeeder.cs`
- **Zweck:** Liest `wwwroot/data/sim_data.json` und legt fehlende Autos/Strecken an (idempotent über `Sim`+`Slug`).
- **`static void Seed(db, env, logger?)`** — Datei prüfen (fehlt → Warnung + Skip), JSON parsen (`simulations`-Array), vorhandene `(Sim,Slug)` als HashSet laden, je Sim (`SimMap` mappt `iracing`/`lmu`/`acc`) `cars`/`tracks` durchgehen, neue hinzufügen, nur bei Änderungen `SaveChanges()` + Info-Log.
- **`private static IEnumerable<(string,string)> EnumerateNamed(JsonElement array)`** — liefert `(id→slug, name)`-Paare, nur wenn beide gesetzt.

### Program — `SimracingUtility/Program.cs`
- **Zweck:** Einstiegspunkt der Website.
- **`static void Main(string[] args)`** — (1) `DefaultConnection` lesen (sonst Exception), `ApplicationDbContext` mit `UseNpgsql` (EF-10-`PendingModelChangesWarning` unterdrückt). (2) Identity (`AddDefaultIdentity<IdentityUser>`, `RequireConfirmedAccount=false`). (3) `IRecentFuelCalcService`→`RecentFuelCalcService`; `AddMemoryCache()`; typed `HttpClient` für `SimGridClient` (8 s Timeout, Browser-User-Agent). (4) `AddControllersWithViews()`. (5) Pipeline (Dev: MigrationsEndPoint; sonst ExceptionHandler + HSTS). (6) Start-Scope: `db.Database.Migrate()` + `SimDataSeeder.Seed(...)`. (7) `UseHttpsRedirection/StaticFiles/Routing/Authentication/Authorization`. (8) Default-Route + `MapRazorPages` (Identity-UI), `app.Run()`.

### 1.3 Services

### IRecentFuelCalcService / RecentFuelCalcService — `SimracingUtility/Services/`
- **Zweck:** CRUD für gespeicherte Spritrechnungen (`RecentFuelCalculation`).
- **Abhängigkeiten:** `RecentFuelCalcService` injiziert `ApplicationDbContext _db`.
- **Methoden:**
  - `GetByIdAsync(int id)` — `FindAsync(id)` (oder null).
  - `GetRecentAsync(int max = 20)` — absteigend nach `CreatedAt`, `Take(max)`, `ToListAsync`.
  - `CreateAsync(entity)` — `Add` + `SaveChangesAsync` (vergibt Id), gibt die getrackte Instanz zurück.
  - `UpdateAsync(entity)` — setzt `UpdatedAt = UtcNow`, `Update`, `SaveChangesAsync` (kein Existenz-Check).
  - `DeleteAsync(int id)` — lädt per `FindAsync`; nur wenn vorhanden `Remove` + speichern (idempotent).

### SimGridProfile — `SimracingUtility/Services/SimGridProfile.cs`
- **Zweck:** Validiert eine SimGrid-Fahrerprofil-URL und extrahiert Slug + Fahrer-Id; akzeptiert ausschließlich `thesimgrid.com`.
- **Konstanten:** `BaseUrl = "https://www.thesimgrid.com/drivers/"`; `SlugRegex = ^[0-9]+(?:-[A-Za-z0-9_-]+)?$` (kompiliert).
- **`static bool TryParse(string? input, out string slug, out int driverId, out string profileUrl)`** — Try-Parse-Muster: null/leer → false. Enthält der Text `/` oder `.`, wird er als URL behandelt (ggf. `https://` voranstellen), Host muss `thesimgrid.com`/`www.thesimgrid.com` sein, im Pfad das Segment nach `drivers` ist der Kandidat; sonst gilt der Text direkt als Slug/Id. Kandidat muss `SlugRegex` matchen; führende Ziffern → `driverId` (`>0`). Bei Erfolg `slug`/`profileUrl = BaseUrl + slug` (kanonisch mit `www`, `/activities` u. Ä. verworfen).

### SimGridStatsParser — `SimracingUtility/Services/SimGridStatsParser.cs`
- **Zweck:** Liest Renn-Kennzahlen (Starts, Wins, Podiums, Top 5, Fastest Laps) aus dem SimGrid-Profil-HTML; verankert am **Label**-Text, nicht an CSS-Klassen. Best effort, fragil bei Layout-Änderungen.
- **Felder:** `Targets` = Tabelle (Label → Setter); erwartete Karten `<div>Zahl</div><div ...text-uppercase>Label</div>`.
- **`static SimGridStats Parse(string? html)`** — leeres HTML → leeres Ergebnis. Pro Label whitespace-tolerantes Pattern (Wörter per `Regex.Escape`, mit `\s+` verbunden); Regex `>([\d][\d,]*)\s*</div>\s*<div[^>]*text-uppercase[^>]*>\s*<Label>\s*</div>` (Singleline, IgnoreCase); Treffer von `,` befreien, `int.TryParse`, Setter aufrufen. Nicht gefundene Werte bleiben `null`.

### SimGridClient — `SimracingUtility/Services/SimGridClient.cs`
- **Zweck:** Holt das öffentliche SimGrid-Profil (Browser-Kennung gegen den Bot-403) und extrahiert via `SimGridStatsParser` die Stats; aggressiv gecacht, Fehler best-effort geschluckt.
- **Abhängigkeiten:** injiziert `HttpClient _http`, `IMemoryCache _cache` (User-Agent/Timeout werden bei der `HttpClient`-Registrierung in `Program.cs` gesetzt).
- **Cache:** Key `"simgrid-stats:" + profileUrl`; Erfolg 6 h, Fehlschlag/`null` 30 min.
- **`async Task<SimGridStats?> GetStatsAsync(string? profileUrl, CancellationToken ct = default)`** — leere URL → null. Cache-Hit (auch negatives `null`) → zurück. Sonst `GetAsync`; nur bei `IsSuccessStatusCode` Body lesen, parsen, nur übernehmen wenn `HasAny`. Jeder Fehler → leeres `catch` (null). Ergebnis cachen, zurückgeben.

---

## 2. LMU-Agent

### 2.1 Core (Parser, Modelle, Daten)

### IRaceResultParser / IStatisticsParser — `LMU_Agent/src/LMU.Agent.Core/Services/`
- **IRaceResultParser:** `ParseRaceResultsAsync(string savePath)` (parst alle Ergebnis-XML + persistiert), `GetLastRaceResultsAsync()`, `GetRaceResultByIdAsync(int id)`.
- **IStatisticsParser:** `CalculateStatisticsAsync()` (berechnet je Fahrer, ohne Speichern), `GetStatisticsByDriverNameAsync(string)`, `CalculateAndStoreStatisticsAsync()` (berechnet + Upsert).

### RaceResult — `LMU_Agent/src/LMU.Agent.Core/Models/RaceResult.cs`
- **Zweck:** EF-Entität eines einzelnen Fahrer-Ergebnisses; berechnete Klassifizierungs-Properties werden im Context ge-`Ignore`-d.
- **Roh-Spalten:** `Id`, `DriverName`, `SessionId` (Unix-Timestamp/Gruppierungsschlüssel), `RaceDate`, `TrackName`, `RaceMinutes` (`<Minutes>`), `RaceLaps` (`<RaceLaps>`), `TeamName`, `CarEntry` (VEH-Datei), `Position` (Klassenposition), `OverallPosition`, `FieldSize` (Teilnehmer der Klasse), `Laps`, `BestLapTime` (s), `FinishStatus`, `CarNumber`, `CarClass`, `IsPlayer` (Mensch vs. Bot).
- **Berechnet:** `IsEndurance` (true ab `RaceMinutes >= 90`; bei reinen Rundenrennen Schätzung `RaceLaps*BestLapTime >= 90*60`); `CarKey` (`TeamName` sonst `CarEntry`); `IsDnf` (false bei leer, `"None"` oder Status mit `"Finished"`; sonst true).

### Statistics — `LMU_Agent/src/LMU.Agent.Core/Models/Statistics.cs`
- **Zweck:** Aggregat-Kennzahlen je Fahrer über **alle** Rennen (ohne Sprint/Endurance-Trennung). DbSet `Statistics`.
- **Eigenschaften:** `Id`, `DriverName`, `TotalRaces`, `Wins`, `Podiums`, `Top5`, `Top10`, `TopHalf`, `Dnf`, `BestPosition`, `FastestLapTime`, `LastRaceDate`.

### UserDashboard (+ CategoryStats, TrackBestLap, CompanionCount) — `LMU_Agent/src/LMU.Agent.Core/Models/UserDashboard.cs`
- **Zweck:** Transferobjekt (kein DbSet) der an die Website gepushten Auswertung. KI-Trainingsrennen ausgeschlossen; **CategoryStats beziehen sich auf die Fahrzeug-Klasse, nicht das Gesamtfeld**.
- `CategoryStats` (`TotalRaces`, `Wins`, `Podiums`, `Top5`, `Top10`, `TopHalf`, `Dnf`, `BestPosition`, `LastRaceDate`); `TrackBestLap` (`Track`, `BestLapTime`); `CompanionCount` (`Name`, `RacesShared`); `UserDashboard` (`DriverName`, `Sprint`/`Endurance`, `BestLapsByTrack`, `MostRacedWith`, `MostRacedAgainstTeams`).

### RaceResultParser — `LMU_Agent/src/LMU.Agent.Core/Services/RaceResultParser.cs`
- **Zweck:** Liest die rFactor-2-XML aus `UserData\Log\Results`, mappt jeden Fahrer der Race-Session auf `RaceResult`, schreibt idempotent.
- **Abhängigkeiten:** `LMUAgentContext`, `System.Xml.Linq`, `System.Globalization`, `Encoding.Latin1`.
- **Methoden:**
  - `ParseRaceResultsAsync(string resultsFolder)` — fehlender Ordner → Warnung + leer. Je `*.xml` `ParseResultsFile` (Fehler geloggt, Datei übersprungen). **Upsert** nur, wenn nicht bereits `DriverName`+`RaceDate`+`Position` existiert; `SaveChangesAsync`.
  - `GetLastRaceResultsAsync()` — 10 jüngste nach `RaceDate`. `GetRaceResultByIdAsync(int)` — `FindAsync`.
  - `static IEnumerable<RaceResult> ParseResultsFile(string path)` — `XDocument.Load`; bei `XmlException` **Encoding-Fallback** (Latin-1-Read + `Parse`). Delegiert an `ParseResults`.
  - `static List<RaceResult> ParseResults(XDocument doc)` — DB-frei/testbar: `RaceDate` aus `<TimeString>`/`<DateTime>` (`ParseDate`); `trackName` aus `<TrackCourse>`/`<TrackVenue>`; wertet **nur** die `<Race>`-Session aus; multiclass: `classCounts` je `CarClass`; pro `<Driver>` ein `RaceResult` (`ClassPosition`→`Position`, `Position`→`OverallPosition`, `FieldSize`, `Laps` via `ParseLaps`, …, `isPlayer=="1"`).
  - Hilfen: `ParseLaps` (`<Laps>` sonst Anzahl `<Lap>`), `ParseDate` (exakt `yyyy/MM/dd HH:mm:ss`, dann tolerant, dann Unix, sonst `MinValue`), `ParseInt`/`ParseDouble` (Invariant).

### StatisticsParser — `LMU_Agent/src/LMU.Agent.Core/Services/StatisticsParser.cs`
- **Zweck:** Leitet die Fahrerliste aus den `RaceResults` ab und berechnet/persistiert je Fahrer eine `Statistics`.
- **Methoden:** `CalculateStatisticsAsync()` (alle Results, Gruppierung nach `DriverName`, `ComputeStatistics`, Sortierung Wins/Podiums); `static Statistics ComputeStatistics(...)` (DB-frei: `finished` = `!IsDnf && Position>0`, Zähler über Schwellen, `TopHalf` über `IsInTopHalf`, `Dnf`, `BestPosition`, `FastestLapTime`, `LastRaceDate`); `static bool IsInTopHalf(...)` (`Position <= ceil(FieldSize/2)`); `GetStatisticsByDriverNameAsync`; `CalculateAndStoreStatisticsAsync()` (**Upsert je Fahrer**).

### DashboardBuilder — `LMU_Agent/src/LMU.Agent.Core/Services/DashboardBuilder.cs`
- **Zweck:** Baut aus allen Ergebnissen das `UserDashboard` des Besitzers; filtert KI-Trainingsrennen, trennt Sprint/Endurance, ermittelt Strecken-Bestzeiten, Mitstreiter und gegnerische Custom-Teams. DB-frei.
- **Konstanten:** `EnduranceMinutes = 90` (die eigentliche Entscheidung läuft über `RaceResult.IsEndurance`); `StockLiveryPattern = \b20\d{2}\b.*#\s*\d+`.
- **Methoden:**
  - `static UserDashboard Build(all, topN=10, configuredDriver=null)` — (1) `humansPerSession` (distinct Menschen je Session); (2) `competitive` = Sessions mit **≥2 Menschen** (reine Solo-/KI-Trainings raus, Online-Rennen mit KI-Auffüllern bleiben); (3) Besitzer = `configuredDriver` falls vorhanden, sonst Mensch mit den meisten Ergebnissen; (4) Sprint/Endurance via `ComputeCategory` nach `IsEndurance`; (5) `BestLapsByTrack`, `MostRacedWith`; (6) `MostRacedAgainstTeams` (Dedup gegen Mitstreiter).
  - `static CategoryStats ComputeCategory(...)` — wie `ComputeStatistics`, kategoriebezogen, ohne `FastestLapTime`.
  - `static List<TrackBestLap> BestLapsByTrack(...)` — nur `BestLapTime>0` + Strecke gesetzt, je Strecke das Minimum, alphabetisch.
  - `static List<CompanionCount> MostRacedWith(...)` — Menschen ≠ ich in meinen Sessions, `RacesShared` = distinct gemeinsame Sessions, `Take(topN)`.
  - `static List<CompanionCount> MostRacedAgainstTeams(...)` — gegnerische **echte** Teams (≥2 verschiedene Menschen), die nicht Stock-Livery, nicht mein Team und nicht schon Mitstreiter sind.
  - `static HashSet<string> StandardLiveries(...)` — **mehrsignalige** Stock-Erkennung: (1) von Bot gefahren; (2) `StockLiveryPattern` (Jahr+#Nr.); (3) im selben Rennen von ≥2 Fahrern; (4) insgesamt von ≥8 Fahrern; (5) `StandardTeams.IsOfficial`. Übrig bleiben Custom-Teamnamen.

### StandardTeams — `LMU_Agent/src/LMU.Agent.Core/Services/StandardTeams.cs`
- **Zweck:** Kuratierte offizielle WEC-/ELMS-Teamnamen (Stock-Liverys ohne Jahr/Startnummer).
- **Felder:** `OfficialNames` (Array), `NormalizedNames` (HashSet, normalisiert).
- **Methoden:** `static bool IsOfficial(string?)` — normalisiert + Wortgrenzen-Abgleich (`" "+official+" "` als Teilfolge, damit „Team WRTesting" ≠ „Team WRT"); `static string Normalize(string)` — Tokens auf Kleinbuchstaben/Ziffern reduziert, durch einzelne Leerzeichen getrennt.

### LmuPathResolver — `LMU_Agent/src/LMU.Agent.Core/Services/LmuPathResolver.cs`
- **Zweck:** Ermittelt den LMU-Ergebnisordner – konfigurierbar mit Steam-Bibliotheks-Auto-Erkennung (Steam kann auf beliebigen Laufwerken liegen).
- **Konstanten:** `DefaultResultsPath`, `EnvVariable = "LMU_RESULTS_PATH"`, privater `ResultsRelative`.
- **Methoden:** `static string ResolveResultsPath(configured, envValue=null, autoDetect=null)` — Priorität configured → Env → `autoDetect()` (Default `AutoDetectResultsPath`) → `DefaultResultsPath` (Parameter machen es testbar); `AutoDetectResultsPath()` — erster existierender `ResultsRelative` über die Steam-Libraries; `SteamLibraryFolders()`/`SteamRootCandidates()` (ProgramFiles-Steam + `<Laufwerk>\Steam`/`\SteamLibrary`); `ParseLibraryFolders(steamRoot)` — Regex über `libraryfolders.vdf`, `\\`→`\`, fehlende Dateien übersprungen.

### TelemetryLocator — `LMU_Agent/src/LMU.Agent.Core/Services/TelemetryLocator.cs`
- **Zweck:** Findet die MoTeC-Telemetrie (`.ld`/`.ldx`) im `<Spiel>\LOG` und matcht robust (trotz Mojibake) gegen einen Streckennamen.
- **Methoden:** `GameLogFolderFromResults(resultsPath)` (über `Parent.Parent.Parent` den `LOG`-Ordner); `FindTelemetryFiles(logFolder, track)` (Endung `.ld`/`.ldx`, normalisierter Dateiname enthält normalisierten Streckennamen, sortiert); `LatestSessionFiles(files)` (lexikografisch größter Basisname = jüngste Session, alle Dateien dieses Basisnamens); `Normalize(s)` (reines ASCII-Skelett `a-z0-9`, **verwirft Nicht-ASCII** → mojibake-toleranter Abgleich).

### LMUAgentContext — `LMU_Agent/src/LMU.Agent.Core/Data/LMUAgentContext.cs`
- **Zweck:** EF-Core-DbContext über eine **SQLite**-Datei als reproduzierbarer Cache der geparsten Daten (keine Migrationen, sondern Schema-Versionierung mit Verwerfen/Neuaufbau).
- **Felder:** `const int SchemaVersion = 2`; DbSets `RaceResults`, `Statistics`; `DbDirectory` (`LocalApplicationData\LMUAgent`), `DbPath` (`…\lmu_agent.db`).
- **Methoden:** `static void PrepareDatabaseFile()` — DB-Ordner anlegen, gespeicherte Version aus `<DbPath>.schema` lesen; weicht sie von `SchemaVersion` ab und existiert die DB → `db`/`-wal`/`-shm` löschen; aktuelle Version schreiben (vor `EnsureCreated`). `OnConfiguring` — `UseSqlite($"Data Source={DbPath}")`. `OnModelCreating` — `RaceResult` (PK, Pflichtfelder; `IsDnf`/`CarKey`/`IsEndurance` `Ignore`); `Statistics` (PK, `DriverName` required).

### 2.2 Service & UI

### Program (Service) — `LMU_Agent/src/LMU.Agent.Service/Program.cs`
- **Zweck:** Einstiegspunkt der Windows-Tray-App. **`[STAThread] static void Main(...)`** — `ApplicationConfiguration.Initialize()`; **Single-Instance** über globalen Mutex `Global\LMU.Agent.Service` (läuft bereits → MessageBox + return); Generic Host: `LMUAgentContext` (Scoped), `IRaceResultParser`/`IStatisticsParser` (Scoped), `StatsPushClient` (`AddHttpClient`), `Worker` (HostedService), `host.Start()`; Ergebnis-Pfad via `LmuPathResolver`, Port aus `Telemetry:Port` (Default 5601), `TelemetryServer` starten; `Application.Run(new TrayAppContext(...))` blockiert bis „Beenden".

### Worker — `LMU_Agent/src/LMU.Agent.Service/Worker.cs`
- **Zweck:** `BackgroundService`, der periodisch einliest, cached, Statistiken neu berechnet und das Dashboard pusht. **Felder:** `PollInterval = 5 min`.
- **`ExecuteAsync(ct)`** — `LMUAgentContext.PrepareDatabaseFile()` + `EnsureCreatedAsync`; Endlosschleife: `RunCaptureAsync()` + `Task.Delay(PollInterval)`; `OperationCanceledException` = sauberer Shutdown.
- **`RunCaptureAsync()`** — Pfad via `LmuPathResolver`; fehlt der Ordner → Warnung + return. Sonst Scope: Parser + `StatsPushClient`; `ParseRaceResultsAsync`, `CalculateAndStoreStatisticsAsync`, alle `RaceResults` laden, `DashboardBuilder.Build(all, configuredDriver: Lmu:DriverName)`, `pushClient.PushAsync`. Komplett in try/catch (Fehler geloggt, Loop läuft weiter).

### TrayAppContext — `LMU_Agent/src/LMU.Agent.Service/TrayAppContext.cs`
- **Zweck:** WinForms-Tray-Oberfläche (`NotifyIcon` + Kontextmenü).
- **Methoden:** Konstruktor `(IHost, TelemetryServer, resultsPath)` — baut Menü (Status, „Ergebnis-Ordner öffnen", „Telemetrie-Ordner öffnen" via `TelemetryLocator.GameLogFolderFromResults`, „Beenden"), sichtbares `NotifyIcon`; `OpenFolder(path?)` (Explorer via `Process.Start`, sonst Warn-MessageBox); `Exit()` (Icon aus, TelemetryServer/Host stoppen+disposen, `ExitThread()`); `Dispose(bool)`.

### TelemetryServer — `LMU_Agent/src/LMU.Agent.Service/TelemetryServer.cs`
- **Zweck:** Lokaler `HttpListener`, der Telemetrie einer Strecke als ZIP ausliefert. Endpunkt `GET /telemetry?track=<Strecke>` (`&all=1` für alle Sessions).
- **Methoden:** Konstruktor `(port, resultsPath, logger)` — Prefix `http://localhost:{port}/`; `Start()` (Listener + `LoopAsync` fire-and-forget); `LoopAsync()` (`GetContextAsync`-Schleife, je Anfrage `Handle` in try/catch); `Handle(ctx)` — CORS `*`; nicht `/telemetry` → 404; `track`/`all` UTF-8-sicher via `GetQueryValue`; Dateien via `TelemetryLocator` (ohne `all` nur jüngste Session); keine → 404; sonst `ZipArchive` direkt in den Response-Stream (`CompressionLevel.Fastest`); `GetQueryValue(query, key)` (manueller UTF-8-Query-Parser); `Dispose()`.

### StatsPushClient — `LMU_Agent/src/LMU.Agent.Service/StatsPushClient.cs`
- **Zweck:** Pusht das `UserDashboard` per REST an die Website. Konfig: `Website:BaseUrl`, `Website:ApiKey`, `Lmu:UserKey`.
- **`PushAsync(dashboard, ct)`** — leere `BaseUrl` → Skip. URL `{BaseUrl}/api/lmu/stats`, POST `JsonContent.Create(dashboard)`, Header `X-Api-Key` (ApiKey) und `X-User-Key` (UserKey, optional). Erfolg/Fehler geloggt; Exceptions geschluckt (robust).

### Program (UI) / Controllers — `LMU_Agent/src/LMU.Agent.UI/`
- **Program (UI)** — Legacy-MVC-Web-UI: OpenAPI + `AddControllersWithViews`, `LMUAgentContext` + beide Parser (Scoped), `MapControllers()` (API + Stats-Seite). Gleicher SQLite-Pfad wie der Dienst.
- **ResultsController** (`[ApiController]`, `api/[controller]`) — `GetLastRaceResults()` (`[HttpGet]`), `GetRaceResult(int id)` (`[HttpGet("{id}")]`, `NotFound` falls fehlend).
- **StatisticsController** (`[ApiController]`) — `GetStatistics()`, `GetDriverStatistics(string name)` (`driver/{name}`), `RecalculateStatistics()` (`[HttpPost("recalculate")]`).
- **StatsController** (MVC-Views) — `Index()` auf `/` und `/stats`: `CalculateStatisticsAsync()` → `View(stats)` (HTML-Sichtprüfung).

---

## 3. Frontend & Skripte

### site.js — `SimracingUtility/wwwroot/js/site.js`
- **JavaScript.** Leerer Vorlagen-Platzhalter (nur Kommentare), keine Laufzeitwirkung.

### setup-hub.js — `SimracingUtility/wwwroot/js/setup-hub.js`
- **JavaScript (IIFE, Strict-Mode).** Kaskadierende Dropdowns für den Setup-Hub: bei Wechsel der Simulation werden Auto-/Streckenliste per JSON nachgeladen. Eingebunden in `Setup/Index.cshtml` (Filter) und `Setup/Upload.cshtml`.
- **Konventionen:** Container `data-setup-cascade` mit `data-cars-url`/`data-tracks-url`; Selects `data-sim-select`/`data-car-select`/`data-track-select`; optional `data-placeholder`, `data-selected`. Endpunkte `Setup/Cars`/`Setup/Tracks` (`GET ?sim=<sim>`, `Accept: application/json`, Antwort `[{id,name}]`). Nur natives `fetch`/DOM.
- **Funktionen:** `fillSelect(select, items, placeholder, selectedValue)` (Select neu befüllen, String-Vergleich für Vorauswahl); `loadList(url, sim)` (`fetch` mit `encodeURIComponent`, Fehler bei `!res.ok`); `initCascade(root)` (verdrahtet eine Instanz; innere `refresh(preselectCar, preselectTrack)` lädt Auto+Strecke; `change`-Handler am Sim-Select setzt zurück; initial mit `data-selected`-Vorauswahl); `DOMContentLoaded`-Handler initialisiert alle Container.

### FuelCalc/Index.cshtml — Inline-Script — `SimracingUtility/Views/FuelCalc/Index.cshtml`
- **Zwei Inline-IIFEs (Browser-JS).**
  - **Block 1 — Klasse→Auto-Kaskade:** liest `ViewBag.CarsJson` (`@Html.Raw`), `filterCarsByClass(cls)`, `populate(select, items, placeholder)`, `change`-Handler am Klassen-Select, Initialbefüllung.
  - **Block 2 — AJAX-Submit:** `submit`-Handler (`preventDefault`), baut `model` (typgerechte Konvertierung, `FuelReserveUnit` Default `Laps`), liest Antiforgery-Token, `fetch(POST @Url.Action("Calculate","FuelCalc"))`. Antwort: Ergebnisfelder setzen, `#reserveInfo`/`#reserveWarning` (`d-none`-Toggle), `#stintTable tbody` neu aufbauen, Verlaufstabelle oben ergänzen; `.catch` → Full-Post-Fallback (`form.submit()`).

### LmuStats / Setup-Views — Inline-Script
- **`LmuStats/Index.cshtml`:** **kein** Inline-JS — rein serverseitig gerendert; Interaktion nur über HTML-Form-Post (`SetSimGrid`), `<details>` und externe Links.
- **`Setup/Index.cshtml`:** kein Inline-`<script>`; bindet `~/js/setup-hub.js` ein und stellt die `data-*`-Marker bereit (`data-placeholder="Alle"`); einziges Inline-Verhalten: `onsubmit="return confirm('Dieses Setup wirklich löschen?')"` am Lösch-Formular.
- **`Setup/Upload.cshtml`:** kein Inline-`<script>`; lädt `_ValidationScriptsPartial` (jQuery-Validation) + `~/js/setup-hub.js` (Default-Platzhalter, da keine `data-placeholder`).

### publish.ps1 — `LMU_Agent/publish.ps1`
- **PowerShell-Build-/Packaging-Skript.** Baut `LMU.Agent.Service` als self-contained **win-x64 Single-File** und legt das ZIP nach `../SimracingUtility/wwwroot/downloads/`. Aufruf aus `LMU_Agent`: `pwsh ./publish.ps1`.
- **Ablauf (linear):** `$ErrorActionPreference = "Stop"`; Pfade relativ zu `$PSScriptRoot`; `dotnet publish ... -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish` (native SQLite-Lib selbst-entpackend → eine `.exe` ohne .NET/PowerShell für den Endnutzer); Zielordner sicherstellen; vorhandenes ZIP löschen; `Compress-Archive` des `publish`-Inhalts nach `LMU.Agent.Service.zip`; Abschlussmeldung.
