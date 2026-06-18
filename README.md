# SimracingUtility

Eine webbasierte Plattform zur zentralen Verwaltung von Simracing-Ressourcen für Teams und Fahrer. Die Anwendung bietet einen Setup-Hub zum Verwalten von Fahrzeugkonfigurationen, einen Fuel Calculator für Kraftstoffstrategien und eine automatisierte Integration von Rennstatistiken aus Le Mans Ultimate.

## Projektübersicht

SimracingUtility ist eine ASP.NET MVC-Anwendung, die verschiedene Simracing-Utilities bündelt und Nutzern über eine einheitliche Oberfläche zur Verfügung stellt. Die Plattform reduziert den organisatorischen Aufwand von Simracing-Teams durch die zentrale Bereitstellung wichtiger Werkzeuge.

### Kernfunktionen

- **Setup-Hub**: Hochladen, Verwalten und Herunterladen von Fahrzeug-Setups mit Metadaten wie Rundenzeit, Temperatur und Beschreibung
- **Fuel Calculator**: Berechnung des benötigten Kraftstoffs für Rennen und Stints inklusive Boxenstopps und Zeitverlust
- **Benutzerverwaltung**: ASP.NET Identity für Authentifizierung und Autorisierung
- **Datenbank**: PostgreSQL mit Entity Framework Core (Code First)
- **Statistik-Integration**: Automatisierte Verwaltung von Fahrzeug-, Strecken- und Simulationsdaten
- **SimGrid-Integration**: Profil-Link je Fahrer + best-effort-Stats aus dem öffentlichen SimGrid-Fahrerprofil
- **LMU-Events**: kuratierte Übersicht anstehender Special Events und Team-Meisterschaften
- **LMU-Agent-Download**: Tray-Programm zum lokalen Erfassen und Pushen der LMU-Daten

## Technologie-Stack

- **Backend**: ASP.NET MVC (.NET 10.0)
- **Datenbank**: PostgreSQL mit Entity Framework Core
- **Frontend**: HTML5, CSS3, JavaScript, Bootstrap
- **Authentifizierung**: ASP.NET Identity
- **Versionsverwaltung**: Git

## Installation

### Voraussetzungen

- .NET 10.0 SDK
- PostgreSQL Datenbank
- Git

### Einrichtung

1. Clone das Repository:

```bash
git clone https://github.com/brachyboost-lang/SimracingUtility.git
cd SimracingUtility
```

2. Konfigurieren Sie die Datenbankverbindung in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=SimracingUtility;Username=postgres;Password=yourpassword"
  }
}
```

3. Starten Sie die Anwendung:

```bash
dotnet run
```

Die Anwendung ist unter `https://localhost:5001` verfügbar.

## Projektstruktur

```
SimracingUtility/
├── Controllers/
│   ├── FuelCalcController.cs      # Fuel Calculator Endpunkte
│   ├── SetupController.cs         # Setup-Upload und Verwaltung
│   └── HomeController.cs          # Standard MVC Controller
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core DbContext
│   └── SimDataSeeder.cs           # Seed-Daten für Stammtabellen
├── Models/
│   ├── Setup.cs                   # Upload-Setup mit Datei
│   ├── SimCar.cs                  # Fahrzeugmodell
│   ├── SimTrack.cs                # Streckenmodell
│   ├── FuelCalcViewModel.cs       # Fuel Calculator Formular
│   └── SimGame.cs                 # Simulations-Enum (iRacing, rFactor2, etc.)
├── Services/
│   ├── RecentFuelCalcService.cs   # Fuel Berechnung und Persistenz
│   ├── SimGridClient.cs           # Holt + cached SimGrid-Profil-Stats (best effort)
│   ├── SimGridStatsParser.cs      # Parst Renn-Stats aus dem SimGrid-Profil-HTML
│   └── SimGridProfile.cs          # SimGrid-Profil-URL: Validierung + Slug-Extraktion
├── Views/
│   ├── FuelCalc/                  # Fuel Calculator Views
│   ├── Setup/                     # Setup-Hub Views
│   └── Home/                      # Standard Views
├── wwwroot/
│   ├── js/                        # JavaScript für AJAX und Filterung
│   └── data/                      # Seed-Daten (sim_data.json)
└── appsettings.json              # Konfiguration
```

## Setup-Hub

Der Setup-Hub ermöglicht es Nutzern, Fahrzeugkonfigurationen für verschiedene Simulationen zu verwalten.

### Features

- Upload von Setup-Dateien (bis 25 MB; native Endung je Sim oder `.zip`)
- Zuordnung zu Simulation, Auto und Strecke
- Metadaten: Name, Beschreibung, Rundenzeit, Temperatur
- Download von gespeicherten Setups
- Filterung nach Simulation, Auto und Strecke
- Übersicht mit den neuesten 200 Einträgen

### Datenmodell

```csharp
public class Setup
{
    public int Id { get; set; }
    public string OwnerId { get; set; }           // IdentityUser
    public SimGame Sim { get; set; }              // Simulation
    public SimCar? Car { get; set; }              // Fahrzeug
    public SimTrack? Track { get; set; }          // Strecke
    public string? Name { get; set; }             // Benutzerdefineter Name
    public string? Description { get; set; }      // Beschreibung
    public string? LapTime { get; set; }          // Rundenzeit als Text
    public double? TrackTempCelsius { get; set; }  // Temperatur in °C
    public string? CreatorName { get; set; }      // Name des Uploaders
    public string FileName { get; set; }          // Originaldateiname
    public string ContentType { get; set; }       // MIME-Typ
    public long FileSize { get; set; }            // Dateigröße in Bytes
    public byte[] FileData { get; set; }          // Binärdatei
    public DateTime CreatedAt { get; set; }       // Erstellungsdatum
}
```

## Fuel Calculator

Der Fuel Calculator hilft bei der Planung von Kraftstoffstrategien für Rennen.

### Berechnungsalgorithmus

Der Calculator simuliert das Rennen Runde für Runde und berücksichtigt:

- Renndauer in Minuten
- Boxenstopps mit Zeitverlust
- Tankkapazität und Reserve
- Verbrauch pro Runde
- Drive-Through-Penalties

Die Simulation ermittelt die benötigte Anzahl an Boxenstopps und den Gesamt-Kraftstoffbedarf, ohne dass der Tank über die Reserve hinaus entleert wird.

### ViewModel

```csharp
public class FuelCalcViewModel
{
    public int EventDurationMinutes { get; set; }
    public string TrackName { get; set; } = string.Empty;
    public int NumberOfPitStops { get; set; }
    public double PitBoxTime { get; set; }
    public double FuelPerLap { get; set; }
    public int DriveThroughTime { get; set; }
    public string CarName { get; set; } = string.Empty;
    public string CarClass { get; set; } = string.Empty;
    public double FuelTankCapacity { get; set; }
    public double TimePerLap { get; set; }
    public double TotalFuelNeeded { get; set; }
    public int Laps { get; set; }
    public double TotalTimeLost { get; set; }
    public double FuelReserveLiters { get; set; }
    public bool ReserveExceedsTank { get; set; }
    public List<FuelStint> Stints { get; set; } = new();
}
```

## Statistik-Integration (LMU-Agent)

Der LMU-Agent ist ein optionaler Client, der auf dem Rechner des Nutzers ausgeführt wird und Rennstatistiken aus Le Mans Ultimate automatisch erfasst.

### Architektur

- **Core**: Gemeinsame Modelle und Parser für RaceResults und Statistics
- **Service**: Tray-Programm (WinForms), das periodisch die Ergebnisdateien scannt,
  das Dashboard an die Website **pusht** und lokal Telemetrie bereitstellt
- **UI**: optionale lokale Web-API/Debug-Oberfläche
- **Datenbank**: SQLite zur lokalen Speicherung der Daten

### Datenquellen

Der Agent liest XML-Ergebnisdateien aus dem Steam-Installationsordner:

```
%LOCALAPPDATA%\steam\userdata\<user>\pfx\drive_c\program files (x86)\steam\steamapps\common\Le Mans Ultimate\UserData\Log\Results\*.xml
```

### Berechnete Statistiken

- Podiumsplatzierungen
- Top 5, Top 10, Top 50% Ergebnisse
- Beste Rundenzeit pro Strecke
- Am meisten gefahren mit (geteilte Sessions)
- Sprint vs. Endurance Trennung (nach Renndauer)

## Bekannte Einschränkungen

### Setup-Hub

- Die Übersicht zeigt maximal 200 Setups an. Bei mehr Einträgen fehlen ältere Einträge ohne Hinweis.
- Beim Wechsel der Simulation wird der Platzhaltertext in den Dropdowns auf "wählen" gesetzt statt "Alle".

### Fuel Calculator

- Keine Integration mit echten Fahrzeugdatenbanken. Die Werte müssen manuell eingegeben werden.

### LMU-Agent

- API-Keys und Sicherheitseinstellungen sind für Entwicklung konfiguriert. Für Produktion sind HTTPS und echte Secrets erforderlich.
- Telemetrie-Download funktioniert nur bei same-machine Verbindung.

## Geplante Erweiterungen

- **Auto-Erkennung beim Setup-Upload:** Ein Parser soll aus der hochgeladenen
  Setup-Datei (bzw. dem ZIP-Inhalt) selbst ermitteln, für welche **Simulation**,
  welches **Auto** und welche **Strecke** das Setup ist – statt manueller Auswahl.
- **Rundenzeit aus Telemetrie:** Liegt im ZIP eine **`.ld`/`.ldx`-Telemetriedatei**
  bei, soll daraus die damit gefahrene **Rundenzeit** ausgelesen und automatisch als
  Metadatum gesetzt werden.

## Fehlerkorrektur

Das Projekt enthält zwei Dokumentationen für Fehler:

- **BEKANNTE_FEHLER.md**: Liste offener Probleme und Einschränkungen
- **BEHOBENE_FEHLER.md**: Protokoll behobener Bugs und Verbesserungen

Wichtige behobene Punkte:

1. AJAX-Zeilen in der Fuel Calculator Tabelle zeigten vertauschte Spalten (Car Class/Name)
2. Der Calculate-Endpunkt übersprang die Validierung - jetzt geprüft
3. Oszillation bei Tank-Grenzfällen behoben durch Runde-für-Runde-Simulation
4. Setup-Übersicht lädt nur Metadaten ohne FileData für bessere Performance
5. Filter-Platzhalter "Alle" geht beim Sim-Wechsel verloren - UX verbessert

## Dokumentation

- [`readmeCode.md`](readmeCode.md) — **Code-Referenz**: jede Klasse + Methoden + Abhängigkeiten (C#, JavaScript, PowerShell).
- [`INTEGRATION.md`](INTEGRATION.md) — Einbau der LMU-Statistik in ein bestehendes System (`OwnerKey`-Naht).
- [`BEKANNTE_FEHLER.md`](BEKANNTE_FEHLER.md) / [`BEHOBENE_FEHLER.md`](BEHOBENE_FEHLER.md) — offene bzw. behobene Punkte.
- [`LMU_Agent/README.md`](LMU_Agent/README.md) — Details zum lokalen LMU-Agent.

## Lizenz

Dieses Projekt ist Teil der Projektwoche Teamzentrum an der LMU München.

## Kontakt

Für Fragen oder Beiträge: [GitHub Repository](https://github.com/brachyboost-lang/SimracingUtility)