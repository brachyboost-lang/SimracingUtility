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
├── src/
│   ├── LMU.Agent.Core/
│   │   ├── Models/             # RaceResult, Statistics, Event, DriverProfile
│   │   ├── Data/               # LMUAgentContext (SQLite, gemeinsamer Pfad)
│   │   └── Services/           # I*Parser + Implementierungen, LmuPathResolver
│   ├── LMU.Agent.UI/
│   │   ├── Controllers/        # API: Results/Stats/Events/Profiles  +  StatsController
│   │   ├── Views/Stats/        # HTML-Statistikseite
│   │   └── Program.cs
│   └── LMU.Agent.Service/
│       ├── Program.cs          # Generic Host + AddWindowsService
│       ├── Worker.cs           # BackgroundService (periodische Erfassung)
│       └── appsettings.json    # Lmu:ResultsPath
└── tests/
    └── LMU.Agent.Tests/        # xUnit-Tests (Parser, Statistik, Pfad)
```

## 📊 Datenquellen

Le Mans Ultimate (rFactor-2-Engine) speichert die Rennergebnisse als einzelne
**XML-Dateien** im **Steam-Installationsordner**:

```
<Steam>\steamapps\common\Le Mans Ultimate\UserData\Log\Results\*.xml
```

> ⚠️ **Pfad ist pro User verschieden.** Steam kann auf jedem Laufwerk / in jeder
> Library liegen – ein fester Default ist nicht zuverlässig. Der Pfad ist daher
> **konfigurierbar** (siehe [Konfiguration](#konfiguration)); der eingebaute
> Default deckt nur die Standard-Steam-Installation ab.

Pro Renn-Session wird ein Datensatz je Fahrer erzeugt: Position, Klasse, Runden,
beste Rundenzeit und `FinishStatus` (für die DNF-Erkennung). Das angenommene
XML-Schema ist in [`RaceResultParser`](src/LMU.Agent.Core/Services/RaceResultParser.cs)
dokumentiert und durch Unit-Tests abgedeckt.

> ⚠️ **Noch gegen echte Dateien zu verifizieren:** Die genauen XML-Tag-Namen
> beruhen auf dem dokumentierten rFactor-2-Format und sollten an einer echten
> LMU-Ergebnisdatei gegengeprüft werden. Die `Event`/`DriverProfile`-Parser sind
> unverifizierte Platzhalter und werden vom Dienst derzeit nicht aktiv genutzt.

## Konfiguration

Der Ergebnis-Ordner wird in dieser Reihenfolge bestimmt:

1. `Lmu:ResultsPath` in [`appsettings.json`](src/LMU.Agent.Service/appsettings.json)
2. Umgebungsvariable `LMU_RESULTS_PATH`
3. Standard-Steam-Pfad (Fallback)

Existiert der Ordner nicht, protokolliert der Dienst eine Warnung mit Anleitung
und überspringt den Lauf (kein Absturz).

## 🗄️ Datenbank

**SQLite** als lokale Datei-Datenbank unter
`%LOCALAPPDATA%\LMUAgent\lmu_agent.db` – ein **fester, gemeinsamer Pfad**, damit
Dienst (Schreiber) und Web-API (Leser) unabhängig vom Arbeitsverzeichnis dieselbe
Datenbank verwenden. Das Schema wird beim Start per
`Database.EnsureCreatedAsync()` angelegt – es gibt (noch) **keine EF-Migrationen**.

### Berechnete Statistiken

Pro Fahrer werden aus den Rennergebnissen berechnet: Anzahl Rennen, **P1** (Siege),
**Podium** (Top 3), **Top 5**, **Top 10**, **Top 50 %** des Feldes, **DNF**, beste
Position, schnellste Runde und letztes Renndatum. Positionsbasierte Zähler werten
nur regulär beendete Rennen; DNFs werden separat gezählt.

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

Zusätzlich rendert das UI-Projekt unter **`/`** bzw. **`/stats`** eine
HTML-Statistikseite (Tabelle aller Fahrer) – als schnelle Sichtprüfung, ob der
Agent die Ergebnisse korrekt erfasst hat.

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

Ergebnis-Pfad setzen (falls Steam nicht im Standardpfad liegt):

```powershell
[System.Environment]::SetEnvironmentVariable("LMU_RESULTS_PATH", "D:\Steam\steamapps\common\Le Mans Ultimate\UserData\Log\Results", "Machine")
```

> Ein Hintergrund-Windows-Dienst kann keinen interaktiven First-Run-Dialog
> zeigen – der Pfad wird daher über Konfiguration/Umgebungsvariable gesetzt. Für
> eine spätere interaktive Installer-/Tray-Variante wäre hier ein Pfad-Picker der
> passende Ort.

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

Web-API + Statistikseite lokal starten:

```powershell
dotnet run --project src/LMU.Agent.UI
```

Tests ausführen:

```powershell
dotnet test tests/LMU.Agent.Tests/LMU.Agent.Tests.csproj
```

## Integration mit der Website

Die SimracingUtility-Website bietet den Agent unter **`/Download`** an. Das
Publish-Skript [`publish.ps1`](publish.ps1) legt das ZIP nach
`../SimracingUtility/wwwroot/downloads/LMU.Agent.Service.zip`; der
`DownloadController` der Website liefert es von dort aus. Das ZIP selbst wird
nicht eingecheckt – es entsteht beim Veröffentlichen.

## Offene Punkte

- **XML-Tag-Namen gegen eine echte LMU-Ergebnisdatei verifizieren** und den
  Parser bei Bedarf anpassen.
- **Event-/DriverProfile-Quellen** klären (Format/Pfad) oder die Platzhalter
  entfernen, falls nicht benötigt.
- **EF-Migrationen** statt `EnsureCreated`, sobald sich das Schema stabilisiert.
- Optional: interaktiver Pfad-Picker (Installer/Tray-App) statt reiner
  Konfiguration.

## 📝 Lizenz

Teil des SimracingUtility-Projekts; gleiche Lizenz wie das Hauptprojekt.
