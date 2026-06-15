# Arbeitsanweisung für Agenten (vor Beginn lesen)

Diese Datei beschreibt **Stil und Vorgehen** für jede Arbeit in diesem Repo.
Bitte vollständig lesen, bevor du ein Feature baust oder etwas änderst.

## Projektkontext
- **SimracingUtility** – ASP.NET Core MVC, **.NET 10**, EF Core, **PostgreSQL**.
  Bereiche: Fuel Calculator, Setup-Hub, LMU-Statistik-Anzeige.
- **LMU_Agent** – getrenntes Projekt: lokale **Windows-Tray-App** (`net10.0-windows`,
  WinForms) + Core-Library (SQLite-Cache) + Legacy-Web-UI. Liest
  Le-Mans-Ultimate-XML-Ergebnisse, berechnet ein Dashboard und **pusht es per REST**
  an die Website; stellt lokal Telemetrie-Downloads bereit.
- **Login/Views der Website sind Platzhalter** für ein späteres Fremdsystem – die
  LMU-Statistik ist über `OwnerKey` host-agnostisch anbindbar (siehe
  [INTEGRATION.md](INTEGRATION.md)).
- Weitere Doku: `README.md` (je Projekt), `BEKANNTE_FEHLER.md`,
  `BEHOBENE_FEHLER.md`, `INTEGRATION.md`.

## Grundprinzipien (so wird hier gearbeitet)
1. **Daten vor Annahmen.** Nie raten, wie ein Dateiformat/Verhalten aussieht – erst an
   echten Dateien/echtem Verhalten verifizieren (Dateien öffnen, Werte zählen,
   Verteilungen prüfen), dann implementieren. Falsche Annahmen offen benennen und
   korrigieren. (LMU-Ergebnisse liegen z. B. als XML im Steam-Ordner
   `…\Le Mans Ultimate\UserData\Log\Results`, Telemetrie als `.ld/.ldx` in `…\LOG`.)
2. **Kleine, logische, committete Schritte.** Pro abgeschlossenem Teilschritt ein
   Commit – kein Riesen-Commit. Nach jeder Änderung bauen + Tests laufen lassen.
3. **Logik testbar und DB-frei halten.** Kernlogik in statische, seiteneffektfreie
   Methoden. Für jede neue Logik xUnit-Tests; grün vor Commit.
4. **End-to-End an echten Daten verifizieren**, nicht nur Unit-Tests: App starten,
   Endpunkte mit curl prüfen, Browser öffnen, Zahlen auf Plausibilität prüfen.
   Selbst gestartete Hintergrund-Tasks (Server/Agent) am Ende stoppen.
5. **Robust statt naiv.** Idempotenz (Upsert statt blind Insert), Edge Cases
   (leere/korrupte/encoding-kaputte Eingaben überspringen + loggen, nicht crashen),
   konfigurierbar statt hardcoded (Pfade, Keys, Namen), mehrere Erkennungssignale
   statt eines fragilen.
6. **Bestehenden Stil spiegeln.** Namens-/Kommentar-/Idiom-Dichte des umgebenden Codes
   übernehmen. Keine unnötigen Abhängigkeiten/Frameworks.

## Konventionen
- **Sprache:** Doku, Code-Kommentare, Commit-Messages, UI-Texte auf **Deutsch**.
  Code-Identifier (Klassen/Methoden/Variablen) auf **Englisch**.
- **Commits:** prägnanter deutscher Betreff + Bulletpoint-Body (Was/Warum). Jede
  Message endet mit:
  `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`
  Nur committen, nicht pushen, außer es wird ausdrücklich verlangt.
- **Tests:** xUnit v3, gleiche Paketversionen wie bestehende Testprojekte; sprechende
  Namen, deutsche Kommentare wo hilfreich.
- **EF/Migrationen:** generierte Migrationen prüfen und unbezogene Operationen (z. B.
  fremde Identity-Spalten) entfernen, sodass nur die gewollten Änderungen drinstehen.
  Die Agent-SQLite-DB ist ein reproduzierbarer Cache mit `SchemaVersion` (kein
  Migrations-Setup) – bei Schemaänderung wird sie verworfen/neu aufgebaut.
- **Realdaten-Check:** für Verifikation ein temporäres Konsolen-Projekt gegen die
  Core-DLL bauen, prüfen, danach wieder löschen (nicht einchecken).

## Vorgehen pro Feature
1. Relevante Dateien/Code lesen, betroffene Stellen identifizieren.
2. Bei Format-/Verhaltensfragen: echte Daten untersuchen (Verteilungen, Beispiele).
3. Bei echter Architektur-Weggabelung (mehrere sinnvolle Wege): kurz **nachfragen** mit
   Empfehlung – nicht raten, was viel Arbeit bestimmt. Bei klarer Default-Lösung:
   umsetzen und kurz erwähnen.
4. Kernlogik + Unit-Tests → bauen → testen → **committen**.
5. Anbindung (Controller/UI/Push) → bauen → end-to-end verifizieren → **committen**.
6. **Doku angleichen** (READMEs etc.) und auf veraltete Aussagen prüfen (grep nach
   alten Begriffen). Doku-Commit.
7. Abschluss: alles bauen, alle Tests grün, `git status` sauber, Tasks gestoppt.

## Qualität / Abschluss
- Nach Abschluss aktiv **auf Fehlerquellen/Lücken prüfen** und als Tabelle
  (Wahrscheinlichkeit | Auswirkung | Fix-Aufwand) priorisiert berichten – inklusive
  bewusst offen gelassener Punkte mit Begründung.
- Ergebnisse **ehrlich** berichten: fehlschlagende Tests/Schritte nennen, nichts
  beschönigen. „Fertig" nur, wenn gebaut + verifiziert.
- Bei behobenen Bugs: in `BEHOBENE_FEHLER.md` (Problem + Lösung) dokumentieren und in
  `BEKANNTE_FEHLER.md` als behoben markieren.

## Build- & Testbefehle
```powershell
dotnet build LMU_Agent/LMU.Agent.slnx
dotnet test  LMU_Agent/tests/LMU.Agent.Tests/LMU.Agent.Tests.csproj
dotnet build SimracingUtility/SimracingUtility.csproj
dotnet test  SimracingUtility.Tests/SimracingUtility.Tests/SimracingUtility.Tests.csproj
```
Voraussetzung Website: erreichbare PostgreSQL-Instanz (Migrationen laufen beim Start).

**CI:** [`.github/workflows/ci.yml`](.github/workflows/ci.yml) baut + testet beide
Projekte bei jedem Push auf `master` und jedem Pull Request (Windows-Runner,
.NET 10). Lokal grün halten – ein roter CI-Lauf blockiert die Übergabe.
