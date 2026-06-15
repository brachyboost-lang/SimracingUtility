# Integrationsleitfaden: LMU-Statistik in ein bestehendes System einbinden

Dieses Dokument beschreibt, wie die **LMU-Statistik-Funktion** in eine bestehende
Website mit eigenem Login-System übernommen wird. Die aktuelle SimracingUtility-
Website dient nur der **Präsentation während der Entwicklung**.

## Architektur (Datenfluss)

```
LMU-Agent (lokal beim Nutzer)
   ── liest XML-Ergebnisse, berechnet Dashboard
   ── POST /api/lmu/stats   (Header: X-Api-Key, X-User-Key)
        │
        ▼
Host-Website (dein System)
   ── speichert je Nutzer (OwnerKey) in der DB
   ── GET /api/lmu/stats?owner=<key>   → JSON fürs Frontend
```

## Was übernommen wird vs. was Platzhalter ist

| Übernehmen (stabil) | Platzhalter / wegwerfbar |
|---|---|
| Ingest-Endpunkt `POST /api/lmu/stats` | Razor-Views (`Views/LmuStats`, `Views/Download`, Home) |
| Lese-API `GET /api/lmu/stats` | Dev-Login (ASP.NET Identity in diesem Projekt) |
| Datenmodell `LmuDriver` + Kind-Tabellen | Dev-API-Key `dev-local-key` |
| DTOs (`LmuStatsPushDto`, `LmuStatsResponseDto`) | die konkrete `_Layout`/Navigation |
| EF-Migration `AddLmuStats` … `LmuOwnerKey` | |

## Die Nahtstelle: `OwnerKey`

Statt Stats an „den zuletzt gepushten Fahrer" zu binden, trägt jeder Datensatz
einen **`OwnerKey`** – einen vom Host-/Login-System vergebenen Nutzer-Identifier.

- Der Agent sendet ihn als Header **`X-User-Key`** (Konfig `Lmu:UserKey`).
- Der Ingest-Endpunkt ordnet per `OwnerKey` zu (Upsert); fehlt er, wird – wie im
  Einzelnutzer-/Dev-Modus – über den Fahrernamen zugeordnet.
- Die Lese-API filtert per `?owner=<key>`.

So mappt das Host-System seine eigenen Nutzer, ohne dass der Agent oder die
Stats-Logik das Login-System kennen müssen.

## Endpunkte

| Methode | Route | Header | Zweck |
|---|---|---|---|
| POST | `/api/lmu/stats` | `X-Api-Key`, `X-User-Key` | Agent pusht das Dashboard |
| GET  | `/api/lmu/stats?owner=<key>` | – | Dashboard eines Nutzers als JSON |
| GET  | `/api/lmu/stats?driver=<name>` | – | alternativ per Fahrername |
| GET  | `/api/lmu/stats` | – | zuletzt aktualisierter Fahrer (Dev) |

Das JSON von GET entspricht `LmuStatsResponseDto` (Sprint/Endurance, Strecken-
Bestzeiten, Mitstreiter, gegnerische Teams) – damit kann ein beliebiges Frontend
rendern, ganz ohne die Razor-Views.

## Schritte fürs Host-System

1. **Entities + DTOs + den Controller** (`Controllers/LmuStatsApiController.cs`,
   `Models/LmuDriver.cs`, `Models/LmuStatsPushDto.cs`) sowie die LMU-Migrationen
   ins Zielprojekt übernehmen (oder als eigenes Modul/Area kapseln).
2. **Pro Nutzer einen `OwnerKey`** aus dem Login-System ableiten (z. B. die
   bestehende User-Id) und dem Nutzer für seinen Agent bereitstellen.
3. **Agent konfigurieren** (pro Nutzer): `Website:BaseUrl`, `Website:ApiKey`,
   `Lmu:UserKey = <OwnerKey>`.
4. **Absichern** (ersetzt die Dev-Platzhalter):
   - `Lmu:IngestApiKey` als echtes Secret, Website per **HTTPS**.
   - `X-User-Key` serverseitig gegen den eigenen User-Store validieren (idealerweise
     ein pro Nutzer ausgestelltes Token statt einer rohen Id).
   - Lese-API ggf. hinter das eigene Login stellen.
5. **Anzeige**: das eigene Frontend gegen die GET-API rendern; die mitgelieferte
   Razor-Seite `/LmuStats` ist nur Referenz.

## Hinweis Telemetrie

Der Telemetrie-Download wird vom **lokal laufenden Agent** bereitgestellt
(`http://localhost:5601/telemetry?track=…`). Der Link funktioniert nur auf dem
Rechner, auf dem der Agent läuft. Für ein zentral gehostetes Portal müsste der
Agent die ZIPs aktiv hochladen statt sie lokal zu servieren.
