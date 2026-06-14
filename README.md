# SimracingUtility

Eine kleine ASP.NET-Core-Webanwendung für Sim-Racing. Kernfunktion ist ein
**Spritrechner (Fuel Calculator)**, der für ein zeitbasiertes Rennen die nötige
Tankmenge, die Anzahl der Boxenstopps und die gefahrenen Runden berechnet und
jede Berechnung dauerhaft speichert.

## Funktionsumfang

- **Spritrechner** – Eingabe von Strecke, Renndauer, Verbrauch/Runde, Rundenzeit,
  Tankgröße sowie Boxen-/Durchfahrtszeiten; daraus werden Boxenstopps, Runden und
  Gesamt-Spritbedarf berechnet.
- **Fahrzeugauswahl** – Fahrzeugklasse und -name werden aus
  [`cars.json`](SimracingUtility/wwwroot/data/cars.json) geladen; die Fahrzeugliste
  filtert sich per JavaScript nach gewählter Klasse.
- **Verlauf** – Jede Berechnung wird in der Datenbank abgelegt und in einer Tabelle
  ("Recent Calculations") angezeigt; eingereichte Berechnungen erscheinen ohne
  Seiten-Neuladen via AJAX.
- **Benutzerverwaltung** – ASP.NET Core Identity (Registrierung/Login) ist
  eingebunden.

## Technologie-Stack

- **.NET 10** / ASP.NET Core MVC + Razor Pages
- **Entity Framework Core 10** mit **SQL Server** (LocalDB im Standard);
  PostgreSQL-Provider (Npgsql) ist als Paket ebenfalls referenziert
- **ASP.NET Core Identity** zur Authentifizierung
- **Bootstrap 5** + jQuery Validation (Frontend)
- **Docker** (Dockerfile für Linux-Container vorhanden)
- **xUnit** für Unit-Tests

## Projektstruktur

| Pfad | Inhalt |
|------|--------|
| `SimracingUtility/Program.cs` | Anwendungs-Setup, DI, Middleware, Routing |
| `SimracingUtility/Controllers/FuelCalcController.cs` | Spritrechner: Anzeige, Speichern, AJAX-Endpunkt `Calculate` |
| `SimracingUtility/Controllers/HomeController.cs` | Startseite & Fehlerseite |
| `SimracingUtility/Controllers/SetupController.cs` | Gerüst (Platzhalter-CRUD, derzeit ohne Logik) |
| `SimracingUtility/Models/FuelCalcViewModel.cs` | Eingabe-/Ergebnismodell **inkl. Berechnungslogik** (`CalculateFuel`) |
| `SimracingUtility/Models/RecentFuelCalculation.cs` | EF-Entität, gemappt auf Tabelle `FuelCalc` |
| `SimracingUtility/Services/RecentFuelCalcService.cs` | Datenzugriff (CRUD + "letzte N" laden) |
| `SimracingUtility/Data/ApplicationDbContext.cs` | EF-Kontext (Identity + `RecentFuelCalculations`) |
| `SimracingUtility/Data/Migrations/` | EF-Core-Migrationen |
| `SimracingUtility/Views/FuelCalc/Index.cshtml` | Formular, Verlaufstabelle und Client-Skripte |
| `SimracingUtility/Pages/RecentCalculations/` | Razor Pages (Scaffolding für CRUD der Berechnungen) |
| `SimracingUtility/wwwroot/data/cars.json` | Fahrzeugklassen und -modelle |
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

## Lokal starten

Voraussetzung: .NET 10 SDK und eine SQL-Server-(LocalDB-)Instanz.

```powershell
# Datenbank anlegen/aktualisieren
dotnet ef database update --project SimracingUtility

# Anwendung starten
dotnet run --project SimracingUtility
```

Die App ist anschließend unter `https://localhost:7184` bzw. `http://localhost:5279`
erreichbar (siehe `Properties/launchSettings.json`). Die Verbindungszeichenfolge
`DefaultConnection` wird in `appsettings.json` konfiguriert.

### Tests ausführen

```powershell
dotnet test SimracingUtility.Tests
```

## Hinweise

- `SetupController` ist aktuell nur ein leeres Controller-Gerüst.
- Die Entität `RecentFuelCalculation` ist bewusst auf die bestehende Tabelle
  `FuelCalc` gemappt (siehe `ApplicationDbContext.OnModelCreating`).
