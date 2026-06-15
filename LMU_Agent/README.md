# LMU Agent – Le Mans Ultimate Daten-Agent

Eine eigenständige **Windows-Tray-Anwendung**, die Renndaten aus dem Spiel
**Le Mans Ultimate** ausliest, lokal in SQLite speichert, die Auswertung an die
SimracingUtility-Website pusht und Telemetrie-Downloads bereitstellt. Der Agent
ist ein **von der Website getrenntes Projekt**; die Website bietet ihn zum
Download an (siehe [Integration mit der Website](#integration-mit-der-website)).

## 📋 Übersicht

- **Race Results** – liest die XML-Ergebnisdateien (rFactor-2-Format)
- **Statistik** – Sprint/Endurance, Top-Platzierungen, beste Runde je Strecke,
  häufigste Mitstreiter & gegnerische Teams (KI-Rennen ausgeschlossen)
- **Push** – sendet die Auswertung an die Website (REST)
- **Telemetrie** – lokaler Download der `.ld`/`.ldx`-Dateien je Strecke
- **Tray-App** – läuft im Infobereich, liest die Daten periodisch neu ein

## 🏗️ Architektur

Drei Teilprojekte, gemeinsam in [`LMU.Agent.slnx`](LMU.Agent.slnx):

### 1. LMU.Agent.Core (`net10.0`)
Kernbibliothek mit Datenmodellen, dem EF-Core-`DbContext` (SQLite) und den
Parser-Diensten (Schnittstellen + Implementierungen) inkl. Statistikberechnung.

### 2. LMU.Agent.UI (`net10.0`, ASP.NET Core Web API)
Stellt die REST-Endpunkte über **MVC-Controller** bereit
([`Controllers/`](src/LMU.Agent.UI/Controllers)) und registriert die Core-Dienste
per Dependency Injection.

### 3. LMU.Agent.Service (`net10.0-windows`, Tray-App)
Die ausführbare **WinForms-Tray-App** ([`Program.cs`](src/LMU.Agent.Service/Program.cs),
[`TrayAppContext`](src/LMU.Agent.Service/TrayAppContext.cs)). Im Hintergrund läuft
über den **Generic Host** ein [`Worker`](src/LMU.Agent.Service/Worker.cs)
(`BackgroundService`), der die LMU-Dateien im Intervall (Standard: 5 Min.) einliest,
die Auswertung pusht; zusätzlich liefert der
[`TelemetryServer`](src/LMU.Agent.Service/TelemetryServer.cs) Telemetrie-ZIPs aus.

## 📁 Projektstruktur

```
LMU_Agent/
├── LMU.Agent.slnx
├── publish.ps1                 # baut & paketiert die App für den Website-Download
├── README.md
├── src/
│   ├── LMU.Agent.Core/
│   │   ├── Models/             # RaceResult, UserDashboard, Statistics, …
│   │   ├── Data/               # LMUAgentContext (SQLite, gemeinsamer Pfad)
│   │   └── Services/           # Parser, DashboardBuilder, LmuPathResolver,
│   │   │                       #   TelemetryLocator, StandardTeams
│   ├── LMU.Agent.UI/           # Legacy: REST-API + /stats-Seite (optional)
│   └── LMU.Agent.Service/      # Tray-App (net10.0-windows)
│       ├── Program.cs          # Generic Host + WinForms-Tray
│       ├── TrayAppContext.cs   # Tray-Symbol & Menü
│       ├── Worker.cs           # BackgroundService (Erfassung + Push)
│       ├── StatsPushClient.cs  # POST an die Website
│       ├── TelemetryServer.cs  # lokaler ZIP-Download-Endpunkt
│       └── appsettings.json    # Lmu:ResultsPath, Website, Telemetry
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

Pro Renn-Session wird ein Datensatz je Fahrer erzeugt: Klassen-/Gesamtposition,
Klasse, Runden, beste Rundenzeit und `FinishStatus`. Das XML-Schema ist in
[`RaceResultParser`](src/LMU.Agent.Core/Services/RaceResultParser.cs) dokumentiert
und durch Unit-Tests abgedeckt.

> ✅ **Gegen echte LMU-Dateien verifiziert.** Der Parser wurde gegen ~1090 echte
> Ergebnisdateien (311 Rennen, ~9800 Datensätze) geprüft: Datum (`TimeString`),
> Strecke (`TrackCourse`), Klassen, Positionen und DNF-Erkennung
> (`FinishStatus`: `None` = beendet, `DNF` = Ausfall) stimmen. Da LMU multiclass
> ist, wird die **Klassenposition** ausgewertet und die Top-50 % je Klasse
> berechnet. Korrupte Einzeldateien (abgebrochene Schreibvorgänge) werden
> protokolliert und übersprungen.
>
> Die `Event`/`DriverProfile`-Parser bleiben unverifizierte Platzhalter und
> werden vom Dienst derzeit nicht aktiv genutzt.

## Konfiguration

Der Ergebnis-Ordner wird in dieser Reihenfolge bestimmt:

1. `Lmu:ResultsPath` in [`appsettings.json`](src/LMU.Agent.Service/appsettings.json)
2. Umgebungsvariable `LMU_RESULTS_PATH`
3. **Automatische Steam-Erkennung** (scannt `…\Steam`/`…\SteamLibrary` aller
   Laufwerke und `libraryfolders.vdf`) – meist ist keine Konfiguration nötig.
4. Standard-Steam-Pfad (Fallback)

Existiert der Ordner nicht, wird eine Warnung protokolliert und der Lauf
übersprungen (kein Absturz).

**Eigener Fahrername** (optional): `Lmu:DriverName` setzen, um den Besitzer
eindeutig festzulegen. Leer = automatisch der Fahrer mit den meisten Ergebnissen.

### Push an die Website

Damit der Dienst die Statistiken an die SimracingUtility-Website sendet, im
Abschnitt `Website` setzen (oder per Umgebungsvariablen `Website__BaseUrl` /
`Website__ApiKey`):

```json
"Website": {
  "BaseUrl": "http://localhost:5279",
  "ApiKey": "dev-local-key"
}
```

Ist `BaseUrl` leer, wird der Push übersprungen. Der `ApiKey` muss mit
`Lmu:IngestApiKey` der Website übereinstimmen.

## 🗄️ Datenbank

**SQLite** als lokale Datei-Datenbank unter
`%LOCALAPPDATA%\LMUAgent\lmu_agent.db` – ein **fester, gemeinsamer Pfad**, damit
Dienst (Schreiber) und Web-API (Leser) unabhängig vom Arbeitsverzeichnis dieselbe
Datenbank verwenden. Die DB ist ein **aus den XML-Dateien reproduzierbarer
Cache**: beim Start wird sie per `EnsureCreatedAsync()` angelegt. Statt
EF-Migrationen trägt der Kontext eine `SchemaVersion`; ändert sich das Schema
(neuer Agent), wird die Datei verworfen und neu aufgebaut – ein Update mit neuen
Spalten bricht also nicht.

### Berechnetes Nutzer-Dashboard

Ausgewertet wird **der Besitzer**: der via `Lmu:DriverName` konfigurierte Fahrer,
sonst der menschliche Fahrer mit den meisten Ergebnissen. Wichtige Regeln aus dem
Abgleich mit echten Dateien:

- **Nur Wettkampfrennen**: eine Session zählt nur mit **≥ 2 menschlichen Fahrern**.
  Reine KI-/Solo-Trainingsrennen fallen raus, echte Online-Rennen mit ein paar
  KI-Auffüllautos bleiben erhalten.
- **Sprint vs. Endurance** getrennt (Renndauer ≥ 90 min = Endurance; bei reinen
  Rundenrennen aus Rundenzahl × bester Runde geschätzt), je mit: Rennen, **P1**,
  **Podium**, **Top 5/10/50 %**, **DNF**, beste Position. Positionen sind
  **Klassenpositionen** (LMU ist multiclass); DNFs zählen separat.
- **Beste Runde je Strecke** statt eines globalen Werts.
- **„Am meisten gefahren mit"**: menschliche Fahrer, mit denen man am häufigsten
  im selben Rennen war. Echte *Teamkollegen* (Fahrerwechsel) sind nicht ableitbar –
  die Ergebnisse listen genau einen Fahrer pro Auto, und `TeamName` ist keine
  eindeutige Auto-Kennung.
- **Häufigste gegnerische custom Teams**: Standard-/Default-Liverys werden über
  mehrere sichere Signale erkannt und herausgefiltert – ein `TeamName`, der (a)
  von einem KI-Bot gefahren wird, (b) dem Saison-Muster „Jahr + #Startnummer"
  entspricht (z. B. „Akkodis ASP Team 2025 #87"), (c) im selben Rennen von
  mehreren Fahrern genutzt wird, (d) insgesamt von ≥ 8 Fahrern oder (e) in der
  kuratierten Liste offizieller Teamnamen steht
  ([`StandardTeams`](src/LMU.Agent.Core/Services/StandardTeams.cs), fängt
  jahrlose Stock-Namen wie „United Autosports #22" ab). Zusätzlich
  zählen nur **echte Teams** (von ≥ 2 verschiedenen Fahrern genutzt), und Namen,
  die bereits unter „Am meisten gefahren mit" stehen, werden nicht doppelt
  gezeigt.

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

## 🚀 Installation & Nutzung (Tray-App)

Der Agent ist eine **normale Windows-Anwendung mit Tray-Symbol** – kein
Dienst-Setup, keine Admin-Rechte nötig.

```powershell
# im Ordner LMU_Agent: self-contained .exe erzeugen (legt das ZIP zugleich für
# den Website-Download ab)
pwsh ./publish.ps1
```

1. ZIP herunterladen/entpacken und `LMU.Agent.Service.exe` **doppelklicken**.
2. Es erscheint ein Symbol im **Infobereich (Tray)** unten rechts. Der Agent liest
   die Renndaten ein und pusht sie an die Website.
3. **Rechtsklick aufs Tray-Symbol** → „Ergebnis-Ordner öffnen",
   „Telemetrie-Ordner öffnen" oder **„Beenden"** (stoppt den Agent).

Es läuft immer nur **eine Instanz** (erneutes Starten zeigt nur einen Hinweis).
Der Steam-/Ergebnis-Pfad wird automatisch erkannt; nur bei exotischen
Installationen muss `LMU_RESULTS_PATH` bzw. `Lmu:ResultsPath` gesetzt werden.
Für den Autostart die `.exe` in den Windows-Autostart-Ordner legen
(`shell:startup`).

## 📥 Telemetrie-Download

Der Agent stellt lokal einen kleinen HTTP-Endpunkt bereit
(`http://localhost:5601/telemetry?track=<Strecke>`, Port via `Telemetry:Port`),
der die MoTeC-Dateien einer Strecke als **ZIP** ausliefert (`.ld` + `.ldx`
zusammen) – die Dateien liegen in `<Spiel>\LOG`. Standardmäßig wird nur die
**jüngste Session** der Strecke geliefert (klein & relevant); mit `&all=1` alle
Sessions. Die Website-Seite **„Meine Stats"** verlinkt diesen Download je Strecke
(funktioniert auf dem Rechner, auf dem der Agent läuft).

## 🛠️ Entwicklung

Alle drei Projekte bauen:

```powershell
dotnet build LMU.Agent.slnx
```

Tray-App starten (zeigt das Tray-Symbol, Hintergrund-Loop + Telemetrie-Server):

```powershell
dotnet run --project src/LMU.Agent.Service
```

Web-API + Statistikseite (Legacy-UI) lokal starten:

```powershell
dotnet run --project src/LMU.Agent.UI
```

Tests ausführen:

```powershell
dotnet test tests/LMU.Agent.Tests/LMU.Agent.Tests.csproj
```

## Integration mit der Website

**Datenfluss (Push).** Nach jeder Erfassung berechnet der Agent das
Nutzer-Dashboard (Sprint-/Endurance-Stats, Strecken-Bestzeiten, häufigste
Mitstreiter und gegnerische custom Teams) und sendet es per
`POST {Website:BaseUrl}/api/lmu/stats` (Header `X-Api-Key`) an die Website.
Diese speichert es in PostgreSQL und zeigt es unter **`/LmuStats`** („Meine
Stats") an. Das entspricht dem Projektantrag (REST-API zwischen Agent und
Webplattform) und funktioniert auch bei gehosteter Website.

**Download.** Die Website bietet den Agent zusätzlich unter **`/Download`** an;
das Publish-Skript [`publish.ps1`](publish.ps1) legt das ZIP nach
`../SimracingUtility/wwwroot/downloads/LMU.Agent.Service.zip`.

> **Hinweis:** Seit der Push-Integration ist das Projekt **`LMU.Agent.UI`**
> (eigene REST-API + `/stats`-Seite) für das Produkt nicht mehr nötig – die
> Anzeige liegt jetzt in der MVC-Website. Es bleibt vorerst als optionale lokale
> API/Debug-Oberfläche erhalten und kann später entfernt werden.

## Offene Punkte

- **Event-/DriverProfile-Quellen** klären (Format/Pfad) oder die Platzhalter
  entfernen, falls nicht benötigt.
- Optional: korrupte Ergebnisdateien (abgebrochene Schreibvorgänge) toleranter
  behandeln, statt sie zu überspringen.
- **EF-Migrationen** statt `EnsureCreated`, sobald sich das Schema stabilisiert.
- Optional: Tray-Menü um „Jetzt aktualisieren" und Status-/Pfad-Anzeige erweitern;
  echtes Icon statt System-Standardicon.
- API-Key (`Website:ApiKey`) aus den Dev-Defaults in ein Secret ziehen.

## 📝 Lizenz

Teil des SimracingUtility-Projekts; gleiche Lizenz wie das Hauptprojekt.
