# LMU Agent - Le Mans Ultimate Daten-Agent

Ein Windows-Dienst, der Daten aus dem Spiel **Le Mans Ultimate** extrahiert und bereitstellt.

## 📋 Übersicht

Der LMU Agent ist ein eigenständiges Projekt, das folgende Funktionen bietet:

- **Events**: Extrahiert anstehende Renn-Events
- **Race Results**: Speichert Ergebnisse der letzten Rennen
- **Driver Profiles**: Speichert Fahrer-Profilinformationen und -statistiken
- **REST API**: Bietet einen RESTful API-Endpunkt für den Datenzugriff
- **Windows Service**: Läuft als Hintergrunddienst auf Windows

## 🏗️ Architektur

Das Projekt ist in drei Hauptprojekte unterteilt:

### 1. LMU.Agent.Core
Die Kernbibliothek mit:
- Datenmodellen (Entities)
- DbContext für die Datenbank
- Parser-Schnittstellen und Implementierungen
- Statistiken-Berechnung

### 2. LMU.Agent.UI
Ein ASP.NET Core Web API Projekt, das:
- REST API Endpoints bereitstellt
- Die Parser-Dienste orchestriert
- Daten über HTTP zugänglich macht

### 3. LMU.Agent.Service
Ein Windows Service Projekt, das:
- Als Hintergrunddienst läuft
- Daten automatisch parsen und speichern kann
- Konfigurierbar über Umgebungsvariablen ist

## 📁 Projektstruktur

```
LMU_Agent/
├── LMU_Agent.sln
├── README.md
└── src/
    ├── LMU.Agent.Core/
    │   ├── Models/
    │   │   ├── Event.cs
    │   │   ├── RaceResult.cs
    │   │   ├── DriverProfile.cs
    │   │   └── Statistics.cs
    │   ├── Data/
    │   │   └── LMUAgentContext.cs
    │   └── Services/
    │       ├── IEventParser.cs
    │       ├── EventParser.cs
    │       ├── IRaceResultParser.cs
    │       ├── RaceResultParser.cs
    │       ├── IDriverProfileParser.cs
    │       ├── DriverProfileParser.cs
    │       ├── IStatisticsParser.cs
    │       └── StatisticsParser.cs
    ├── LMU.Agent.UI/
    │   ├── Controllers/
    │   │   ├── EventsController.cs
    │   │   ├── ResultsController.cs
    │   │   ├── ProfilesController.cs
    │   │   └── StatisticsController.cs
    │   └── Program.cs
    └── LMU.Agent.Service/
        ├── Services/
        │   └── LMUAgentService.cs
        └── Program.cs
```

## 📊 Datenquellen

Der Agent liest Daten aus den lokalen Dateien von Le Mans Ultimate:

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| Events.json | `%APPDATA%\SlightlyMad\LeMansUltimate\Events.json` | Anstehende und vergangene Events |
| RaceResults.json | `%APPDATA%\SlightlyMad\LeMansUltimate\RaceResults.json` | Ergebnisse der letzten Rennen |
| DriverProfiles.json | `%APPDATA%\SlightlyMad\LeMansUltimate\DriverProfiles.json` | Fahrer-Profilinformationen |

## 🗄️ Datenbank

Der Agent verwendet **SQLite** als lokale Datenbank:
- Dateipfad: `lmu_agent.db` (im Verzeichnis des Services)
- Enthält alle extrahierten Daten und berechnete Statistiken

## 🔌 REST API Endpoints

| Endpoint | Methode | Beschreibung |
|----------|---------|--------------|
| `/api/events` | GET | Alle anstehenden Events |
| `/api/events/{id}` | GET | Event mit spezifischer ID |
| `/api/results` | GET | Ergebnisse der letzten Rennen |
| `/api/results/{id}` | GET | Ergebnis mit spezifischer ID |
| `/api/profiles` | GET | Alle Fahrer-Profile |
| `/api/profiles/{id}` | GET | Profil mit spezifischer ID |
| `/api/statistics` | GET | Alle Statistiken |
| `/api/statistics/driver/{name}` | GET | Statistiken für einen Fahrer |

## 🚀 Installation und Einrichtung

### 1. Projekt auflösen

```powershell
dotnet restore
```

### 2. Windows Service installieren

```powershell
# Als Administrator in einem PowerShell-Fenster
cd LMU_Agent
dotnet publish -c Release -r win-x64 --self-contained true -o publish
sc create LMUAgent binPath="\"C:\Pfad\zu\publish\LMU.Agent.Service.dll\""
sc start LMUAgent
```

### 3. Umgebungsvariable setzen (optional)

```powershell
[System.Environment]::SetEnvironmentVariable("LMU_DATA_PATH", "C:\Pfad\zu\LMU\Daten")
```

## 🛠️ Entwicklung

### Debugging

Für die Entwicklung können Sie das Projekt als normale .NET-Anwendung ausführen:

```powershell
dotnet run --project src/LMU.Agent.Service
```

### Datenbank-Migrationen

Falls neue Datenmodelle hinzugefügt werden, müssen Migrationen erstellt werden:

```powershell
dotnet ef migrations add MigrationName
dotnet ef database update
```

## 📝 Lizenz

Dieses Projekt ist Teil des SimracingUtility-Projekts und wird unter der gleichen Lizenz wie das Hauptprojekt veröffentlicht.

## 🤝 Beitrag

Bei Fragen oder Verbesserungsvorschlägen: Bitte erstellen Sie einen Issue auf GitHub.