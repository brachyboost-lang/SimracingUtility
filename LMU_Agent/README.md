# LMU Agent – Le Mans Ultimate Daten-Agent

Ein eigenständiger Windows-Dienst, der Renndaten aus dem Spiel **Le Mans Ultimate**
ausliest, lokal speichert und über eine REST-API bereitstellt. Der Agent ist ein
**von der SimracingUtility-Website getrenntes Projekt**; die Website bietet ihn
lediglich zum Download an (siehe [Integration mit der Website](#integration-mit-der-website)).

## 📋 Übersicht

- **Events** – liest anstehende und vergangene Renn-Events
- **Race Results** – speichert Ergebnisse der letzten Rennen
- **Driver Profiles** – Fahrer-Profilinformationen
- **Statistics** – berechnet pro Fahrer Siege, Podien, beste Runde u. a.
- **REST API** – stellt die Daten über HTTP bereit
- **Windows-Dienst** – läuft als Hintergrunddienst und liest die Daten periodisch neu ein

## 🏗️ Architektur

Drei Teilprojekte, gemeinsam in [`LMU.Agent.slnx`](LMU.Agent.slnx):

### 1. LMU.Agent.Core (`net10.0`)
Kernbibliothek mit Datenmodellen, dem EF-Core-`DbContext` (SQLite) und den
Parser-Diensten (Schnittstellen + Implementierungen) inkl. Statistikberechnung.

### 2. LMU.Agent.UI (`net10.0`, ASP.NET Core Web API)
Stellt die REST-Endpunkte über **MVC-Controller** bereit
([`Controllers/`](src/LMU.Agent.UI/Controllers)) und registriert die Core-Dienste
per Dependency Injection.

### 3. LMU.Agent.Service (`net10.0`, Worker)
Der eigentliche Hintergrunddienst. Nutzt den **Generic Host** mit
`AddWindowsService()` und einem [`Worker`](src/LMU.Agent.Service/Worker.cs)
(`BackgroundService`), der die LMU-Dateien in einem Intervall (Standard: 5 Min.)
einliest und die Statistiken aktualisiert.

## 📁 Projektstruktur

```
LMU_Agent/
├── LMU.Agent.slnx
├── publish.ps1                 # baut & paketiert den Dienst für die Website
├── README.md
└── src/
    ├── LMU.Agent.Core/
    │   ├── Models/             # Event, RaceResult (+ LapTime), DriverProfile, Statistics
    │   ├── Data/               # LMUAgentContext (SQLite)
    │   └── Services/           # I*Parser + Implementierungen
    ├── LMU.Agent.UI/
    │   ├── Controllers/        # Events / Results / Profiles / Statistics
    │   └── Program.cs
    └── LMU.Agent.Service/
        ├── Program.cs          # Generic Host + AddWindowsService
        └── Worker.cs           # BackgroundService (periodische Erfassung)
```

## 📊 Datenquellen

Der Agent liest die lokalen JSON-Dateien von Le Mans Ultimate. Standardpfad
(überschreibbar per Umgebungsvariable `LMU_DATA_PATH`):

```
%USERPROFILE%\AppData\LocalLow\SlightlyMad\LeMansUltimate\
```

| Datei | Beschreibung |
|-------|--------------|
| `Events.json` | Anstehende und vergangene Events |
| `RaceResults.json` | Ergebnisse der letzten Rennen |
| `DriverProfiles.json` | Fahrer-Profilinformationen |

> ⚠️ **Hinweis:** Die Datenmodelle bilden ein angenommenes JSON-Schema ab und sind
> noch nicht gegen echte LMU-Dateien verifiziert. Das ist der offene
> Machbarkeits-/Risikopunkt des Projekts (siehe [Offene Punkte](#offene-punkte)).

## 🗄️ Datenbank

**SQLite** als lokale Datei-Datenbank (`lmu_agent.db` im Arbeitsverzeichnis des
Dienstes). Das Schema wird beim Start per `Database.EnsureCreatedAsync()` angelegt
– es gibt (noch) **keine EF-Migrationen**.

### Idempotentes Schreiben

Die Parser schreiben **idempotent** (Upsert statt blindem Insert): Ein erneuter
Lauf über dieselbe Datei legt keine Duplikate an, sondern aktualisiert bestehende
Datensätze. Das ist nötig, weil der Worker periodisch dieselben Dateien einliest –
ohne Upsert würde die Datenbank bei jedem Lauf weiter anwachsen und Statistiken
mehrfach zählen. Natürliche Schlüssel:

| Entität | Schlüssel | Verhalten |
|---------|-----------|-----------|
| Event | Name + Date | Felder aktualisieren |
| DriverProfile | Name | Profilwerte aktualisieren |
| RaceResult | Fahrer + Renndatum + Position | nur einfügen, wenn neu |
| Statistics | DriverName | Werte aktualisieren |

## 🔌 REST API Endpoints

| Endpoint | Methode | Beschreibung |
|----------|---------|--------------|
| `/api/events` | GET | Anstehende Events |
| `/api/events/{id}` | GET | Event mit ID |
| `/api/results` | GET | Letzte Rennergebnisse |
| `/api/results/{id}` | GET | Ergebnis mit ID |
| `/api/profiles` | GET | Alle Fahrer-Profile |
| `/api/profiles/{id}` | GET | Profil mit ID |
| `/api/statistics` | GET | Statistiken aller Fahrer |
| `/api/statistics/driver/{name}` | GET | Statistiken eines Fahrers |
| `/api/statistics/recalculate` | POST | Statistiken neu berechnen & speichern |

## 🚀 Installation als Windows-Dienst

Am einfachsten über das Publish-Skript (legt das Artefakt zugleich für den
Website-Download ab):

```powershell
# im Ordner LMU_Agent
pwsh ./publish.ps1
```

Das Skript erzeugt unter `publish/` eine self-contained `LMU.Agent.Service.exe`.
Diese als Dienst registrieren (**PowerShell als Administrator**):

```powershell
sc create "LMU Agent" binPath= "C:\Pfad\zu\publish\LMU.Agent.Service.exe"
sc start "LMU Agent"
```

Optionalen Datenpfad setzen:

```powershell
[System.Environment]::SetEnvironmentVariable("LMU_DATA_PATH", "C:\Pfad\zu\LMU\Daten", "Machine")
```

## 🛠️ Entwicklung

Alle drei Projekte bauen:

```powershell
dotnet build LMU.Agent.slnx
```

Dienst zum Debuggen direkt als Konsole starten (läuft dank Generic Host ohne
Service-Installation):

```powershell
dotnet run --project src/LMU.Agent.Service
```

Web-API lokal starten:

```powershell
dotnet run --project src/LMU.Agent.UI
```

## Integration mit der Website

Die SimracingUtility-Website bietet den Agent unter **`/Download`** an. Das
Publish-Skript [`publish.ps1`](publish.ps1) legt das ZIP nach
`../SimracingUtility/wwwroot/downloads/LMU.Agent.Service.zip`; der
`DownloadController` der Website liefert es von dort aus. Das ZIP selbst wird
nicht eingecheckt – es entsteht beim Veröffentlichen.

## Offene Punkte

- **Echtes LMU-JSON-Format verifizieren** und die Modelle anpassen.
- **EF-Migrationen** statt `EnsureCreated`, sobald sich das Schema stabilisiert.
- **Unit-Tests** für die Parser und die Statistikberechnung ergänzen.

## 📝 Lizenz

Teil des SimracingUtility-Projekts; gleiche Lizenz wie das Hauptprojekt.
