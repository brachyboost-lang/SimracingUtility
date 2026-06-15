# SimracingUtility

Eine ASP.NET-Core-Webanwendung für Sim-Racing mit zwei Kernbereichen:

1. **Spritrechner (Fuel Calculator)** – berechnet für ein zeitbasiertes Rennen die
   nötige Tankmenge, die Anzahl der Boxenstopps und die gefahrenen Runden und
   speichert jede Berechnung.
2. **Setup-Hub** – angemeldete Nutzer können Fahrzeug-Setups für **iRacing**,
   **Le Mans Ultimate (LMU)** und **Assetto Corsa Competizione (ACC)** hochladen,
   verwalten und teilen; die Übersicht ist öffentlich, der Download nur für
   angemeldete Nutzer.

Zusätzlich wird der **LMU-Agent** – ein eigenständiges Begleitprojekt – über die
Website zum Download bereitgestellt (siehe [LMU-Agent (Download)](#lmu-agent-download)).

## Funktionsumfang

- **Spritrechner** – Eingabe von Strecke, Renndauer, Verbrauch/Runde, Rundenzeit,
  Tankgröße sowie Boxen-/Durchfahrtszeiten; daraus werden Boxenstopps, Runden und
  Gesamt-Spritbedarf berechnet.
- **Fahrzeugauswahl (Spritrechner)** – Fahrzeugklasse und -name werden aus
  [`cars.json`](SimracingUtility/wwwroot/data/cars.json) geladen; die Fahrzeugliste
  filtert sich per JavaScript nach gewählter Klasse.
- **Verlauf** – Jede Berechnung wird in der Datenbank abgelegt und in einer Tabelle
  ("Recent Calculations") angezeigt; eingereichte Berechnungen erscheinen ohne
  Seiten-Neuladen via AJAX.
- **Setup-Hub** – Upload von Setup-Dateien (mit Validierung), Übersicht mit
  Filterung nach Simulation → Auto → Strecke, Download und Löschen eigener Setups.
  Details siehe Abschnitt [Setup-Hub](#setup-hub).
- **Benutzerverwaltung** – ASP.NET Core Identity (Registrierung/Login) ist
  eingebunden.
- **LMU-Agent-Download** – die Startseite und der Menüpunkt „LMU-Agent" führen zu
  einer Download-Seite für den lokalen Le-Mans-Ultimate-Dienst.

## Technologie-Stack

- **.NET 10** / ASP.NET Core MVC + Razor Pages
- **Entity Framework Core 10** mit **PostgreSQL** (Provider: Npgsql)
- **ASP.NET Core Identity** zur Authentifizierung
- **Bootstrap 5** + jQuery Validation (Frontend)
- **Docker** (Dockerfile für Linux-Container vorhanden)
- **xUnit (v3)** für Unit-Tests

## Projektstruktur

| Pfad | Inhalt |
|------|--------|
| `SimracingUtility/Program.cs` | Anwendungs-Setup, DI, Middleware, Routing, Auto-Migrate + Seeding |
| `SimracingUtility/Controllers/FuelCalcController.cs` | Spritrechner: Anzeige, Speichern, AJAX-Endpunkt `Calculate` |
| `SimracingUtility/Controllers/HomeController.cs` | Startseite & Fehlerseite |
| `SimracingUtility/Controllers/SetupController.cs` | **Setup-Hub**: Index/Upload/Download/Delete + JSON-Endpunkte `Cars`/`Tracks` |
| `SimracingUtility/Controllers/DownloadController.cs` | **LMU-Agent-Download**: Landing-Page + Auslieferung des Agent-ZIP |
| `SimracingUtility/Models/FuelCalcViewModel.cs` | Eingabe-/Ergebnismodell **inkl. Berechnungslogik** (`CalculateFuel`) |
| `SimracingUtility/Models/RecentFuelCalculation.cs` | EF-Entität, gemappt auf Tabelle `FuelCalc` |
| `SimracingUtility/Models/Setup.cs` | EF-Entität eines Setups (Datei als `bytea`, FKs zu User/Auto/Strecke) |
| `SimracingUtility/Models/SimCar.cs`, `SimTrack.cs` | Seedbare Stammdaten (Auto/Strecke je Simulation) |
| `SimracingUtility/Models/SimGame.cs` | Enum der Simulationen + erlaubte Datei-Endungen pro Sim |
| `SimracingUtility/Models/SetupUploadViewModel.cs` | Formularmodell für den Upload (inkl. `IFormFile` + Validierung) |
| `SimracingUtility/Services/RecentFuelCalcService.cs` | Datenzugriff (CRUD + "letzte N" laden) |
| `SimracingUtility/Data/ApplicationDbContext.cs` | EF-Kontext (Identity + FuelCalc + Setups/SimCars/SimTracks) |
| `SimracingUtility/Data/SimDataSeeder.cs` | Seedet Autos/Strecken aus `sim_data.json` (idempotent) |
| `SimracingUtility/Migrations/` | EF-Core-Migrationen (PostgreSQL) |
| `SimracingUtility/Views/FuelCalc/Index.cshtml` | Formular, Verlaufstabelle und Client-Skripte |
| `SimracingUtility/Views/Setup/Index.cshtml` | Setup-Übersicht mit Filter + Kartenansicht |
| `SimracingUtility/Views/Setup/Upload.cshtml` | Upload-Formular |
| `SimracingUtility/Pages/RecentCalculations/` | Razor Pages (Scaffolding für CRUD der Berechnungen) |
| `SimracingUtility/wwwroot/data/cars.json` | Fahrzeugklassen/-modelle (nur Spritrechner) |
| `SimracingUtility/wwwroot/data/sim_data.json` | Datenbasis Setup-Hub: Sims → Autos & Strecken |
| `SimracingUtility/wwwroot/js/setup-hub.js` | Abhängige Dropdowns (Sim → Auto/Strecke) |
| `SimracingUtility.Tests/` | xUnit-Tests für die Berechnungslogik |

## Die Berechnung

Die Logik in `FuelCalcViewModel.CalculateFuel()` löst ein zirkuläres Problem
(Boxenstopps kosten Zeit → weniger Runden → weniger Sprit → evtl. weniger Stopps)
**iterativ**:

1. Verfügbare Rennzeit = Renndauer − bisherige Stopps × (Boxenzeit + Durchfahrtszeit)
2. Runden = verfügbare Zeit ÷ Rundenzeit; Sprit = Runden × Verbrauch/Runde
3. Benötigte Boxenstopps = ⌈Sprit ÷ Tankgröße⌉ − 1
4. Wiederholen, bis sich der Spritwert stabilisiert (max. 40 Iterationen)

Bei ungültigen Eingaben (Rundenzeit oder Tankgröße ≤ 0) werden alle Ergebnisse auf 0 gesetzt.

## Setup-Hub

Bereich zum Hochladen, Filtern und Teilen von Fahrzeug-Setups für drei
Simulationen. Die Setup-Datei wird als `bytea` direkt in PostgreSQL gespeichert
(Setup-Dateien sind nur wenige KB groß).

### Datenmodell

```
Setup ──► OwnerId  (FK → AspNetUsers / IdentityUser)
      ──► CarId    (FK → SimCar)
      ──► TrackId  (FK → SimTrack)
      + Sim, Name, Description, LapTime, TrackTempCelsius, CreatorName
      + FileName, ContentType, FileSize, FileData (bytea), CreatedAt

SimCar / SimTrack: Id, Sim, Slug, Name   (eindeutig je Sim+Slug)
```

`SimCar` und `SimTrack` werden beim Anwendungsstart aus
[`sim_data.json`](SimracingUtility/wwwroot/data/sim_data.json) geseedet
([`SimDataSeeder`](SimracingUtility/Data/SimDataSeeder.cs), idempotent über Sim+Slug).
Die Datei ist hierarchisch aufgebaut: `simulations[] → { id, name, cars[], tracks[] }`,
wobei `cars`/`tracks` jeweils Objekte mit `id` (Slug) und `name` sind.

### Endpunkte (`SetupController`)

| Route | Methode | Zugriff | Zweck |
|-------|---------|---------|-------|
| `/Setup` | GET | öffentlich | Übersicht mit Filter Sim → Auto → Strecke |
| `/Setup/Upload` | GET | angemeldet | Upload-Formular |
| `/Setup/Upload` | POST | angemeldet | Setup speichern (Validierung + AntiForgery) |
| `/Setup/Download/{id}` | GET | angemeldet | Datei-Download (`application/octet-stream`) |
| `/Setup/Delete/{id}` | POST | Eigentümer | eigenes Setup löschen |
| `/Setup/Cars?sim=` | GET | öffentlich | JSON-Autoliste für abhängige Dropdowns |
| `/Setup/Tracks?sim=` | GET | öffentlich | JSON-Streckenliste für abhängige Dropdowns |

### Validierung & Sicherheit

- **Auth:** Upload/Download/Delete mit `[Authorize]`; alle POSTs mit
  `[ValidateAntiForgeryToken]`.
- **Dateityp:** Endung muss zur Simulation passen — iRacing `.sto`,
  LMU `.svm`, ACC `.json` (definiert in [`SimGameInfo`](SimracingUtility/Models/SimGame.cs)).
- **Dateigröße:** max. 5 MB (`RequestSizeLimit`).
- **Dateiname:** über `Path.GetFileName` von Pfadanteilen bereinigt.
- **Integrität:** Auto und Strecke müssen existieren *und* zur gewählten Sim
  gehören (verhindert FK-Manipulation).
- **Eigentum:** Löschen nur durch den Uploader (`OwnerId`-Prüfung, sonst `Forbid`).

### Frontend

- [`Views/Setup/Upload.cshtml`](SimracingUtility/Views/Setup/Upload.cshtml) –
  Upload-Formular mit abhängigen Auswahlfeldern.
- [`Views/Setup/Index.cshtml`](SimracingUtility/Views/Setup/Index.cshtml) –
  Filterleiste + Karten mit Metadaten, Download- und Lösch-Button.
- [`wwwroot/js/setup-hub.js`](SimracingUtility/wwwroot/js/setup-hub.js) – lädt bei
  Änderung der Simulation Auto-/Streckenliste per `fetch` nach
  (gesteuert über `data-`-Attribute, wiederverwendbar für Formular und Filter).

## LMU-Agent (Download)

Der **LMU-Agent** ist ein eigenständiges Projekt im Ordner
[`LMU_Agent/`](LMU_Agent/README.md) mit eigener Dokumentation. Die Website bindet
ihn nur über einen Download ein:

| Route | Zweck |
|-------|-------|
| `/Download` | Landing-Page mit Beschreibung und Installationsanleitung |
| `/Download/Agent` | Auslieferung des Agent-ZIP (`application/zip`) |

Das ausgelieferte ZIP liegt unter `wwwroot/downloads/LMU.Agent.Service.zip` und
wird **nicht eingecheckt**, sondern vom Publish-Skript des Agents erzeugt:

```powershell
# im Ordner LMU_Agent
pwsh ./publish.ps1
```

Das Skript baut den Dienst self-contained und legt das ZIP direkt im
Download-Ordner der Website ab. Fehlt das Artefakt, zeigt die Seite einen Hinweis
statt eines toten Links.

## Lokal starten

Voraussetzung: .NET 10 SDK und eine erreichbare **PostgreSQL**-Instanz.

1. Verbindungszeichenfolge `DefaultConnection` in
   [`appsettings.json`](SimracingUtility/appsettings.json) (oder per User-Secrets)
   an deine Postgres-Instanz anpassen.
2. App starten:

   ```powershell
   dotnet run --project SimracingUtility
   ```

Beim Start werden **Migrationen automatisch angewendet** (`db.Database.Migrate()`)
und die Stammdaten aus `sim_data.json` geseedet — ein manuelles
`dotnet ef database update` ist nicht nötig.

Die App ist anschließend unter `https://localhost:7184` bzw. `http://localhost:5279`
erreichbar (siehe `Properties/launchSettings.json`).

### Migrationen

Das Projekt nutzt `dotnet-ef` als lokales Tool (siehe `dotnet-tools.json`):

```powershell
dotnet dotnet-ef migrations add <Name> --project SimracingUtility
```

### Tests ausführen

```powershell
dotnet test SimracingUtility.Tests/SimracingUtility.Tests/SimracingUtility.Tests.csproj
```

## Hinweise

- **ACC** besitzt real keine Hypercars/Prototypen — in `sim_data.json` ist dort
  daher bewusst nur die aktuelle GT3-Auswahl hinterlegt.
- Die Entität `RecentFuelCalculation` ist bewusst auf die bestehende Tabelle
  `FuelCalc` gemappt (siehe `ApplicationDbContext.OnModelCreating`).
- Beim Start wird `db.Database.Migrate()` ausgeführt — die Anwendung benötigt
  also eine erreichbare PostgreSQL-Instanz, sonst startet sie nicht.
