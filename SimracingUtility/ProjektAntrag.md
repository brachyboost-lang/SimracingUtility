Projektantrag

Simracing Utility Webservice

Entwicklung einer webbasierten Simracing-Plattform mit Setup-Hub, Fuel Calculator und automatisierter Rennstatistik-Integration
Projektbeschreibung
Im Simracing-Bereich werden zahlreiche Hilfsmittel wie Fahrzeug-Setups, Kraftstoffberechnungen und Rennstatistiken über verschiedene Plattformen verteilt angeboten. Dies führt zu einer unübersichtlichen Verwaltung und erschwert den Zugriff auf relevante Informationen für Fahrer und Teams.
Ziel des Projekts ist die Entwicklung einer zentralen webbasierten Plattform auf Basis von ASP.NET MVC, die verschiedene Simracing-Utilities bündelt und den Nutzern über eine einheitliche Oberfläche zur Verfügung stellt.
Die Plattform soll zunächst folgende Kernfunktionen bereitstellen:
-Fuel Calculator zur Berechnung des benötigten Kraftstoffs für Rennen und Stints
-Setup-Hub zum Hochladen, Verwalten und Herunterladen von Fahrzeug-Setups
-Benutzer- und Rollenverwaltung
-Datenbankgestützte Speicherung aller Setup-Daten mittels PostgreSQL
-Automatisierte Verwaltung von Fahrzeug-, Strecken- und Simulationsdaten
-Optional wird die Machbarkeit eines Client-Agenten untersucht, der auf dem Rechner des Nutzers ausgeführt wird und Informationen aus Le Mans Ultimate automatisiert ausliest. Ziel ist die Übertragung von Rennstatistiken, Ergebnissen und anstehenden Events in die Team-Webseite.

Projektziel
Entwicklung einer skalierbaren Webanwendung zur zentralen Verwaltung von Simracing-Ressourcen mit Fokus auf Benutzerfreundlichkeit, Datenkonsistenz und Erweiterbarkeit.
Durch die Plattform sollen Fahrer und Teams ihre Setups effizient verwalten, Kraftstoffstrategien planen und langfristig Rennstatistiken automatisiert auswerten können.

Projektumfeld
Die Anwendung wird als ASP.NET MVC-Webanwendung entwickelt.
Technologien:
ASP.NET MVC (.NET)
Entity Framework Core (Code First)
PostgreSQL
HTML5, CSS3, JavaScript
Bootstrap
Git zur Versionsverwaltung

Optional:
Lokaler Windows-Agent zur Datenerfassung aus Le Mans Ultimate
REST-API zur Kommunikation zwischen Agent und Webplattform
Projektphasen

1. Analyse und Planung (6 Stunden)
Anforderungsanalyse
Erstellung des Datenmodells
Architekturplanung
Recherche zur Integration von Le Mans Ultimate

2. Datenbankdesign (4 Stunden)
Erstellung des Entity-Relationship-Modells
Implementierung mittels Entity Framework Core (Code First)
Migration und Einrichtung der PostgreSQL-Datenbank

3. Entwicklung Backend (12 Stunden)
Benutzerverwaltung
Setup-Verwaltung
Datei-Upload und Download
Fuel Calculator Logik
API-Endpunkte

4. Entwicklung Frontend (8 Stunden)
Responsive Benutzeroberfläche
Setup-Hub
Fuel Calculator
Benutzeroberfläche für Statistiken

5. Test und Qualitätssicherung (3 Stunden)
Funktionstests
Fehlerbehebung
Performanceprüfung

6. Projektdokumentation (2 Stunden)
Projektdokumentation
Fazit und Ausblick

Zeitplanung
Projektphase	Stunden
Analyse und Planung	6
Datenbankdesign	4
Backend-Entwicklung	12
Frontend-Entwicklung	8
Testphase	3
Dokumentation	2
Gesamt	35

Wirtschaftlicher Nutzen
Die Plattform reduziert den organisatorischen Aufwand von Simracing-Teams durch die zentrale Bereitstellung wichtiger Werkzeuge. Fahrzeug-Setups können strukturiert verwaltet und wiederverwendet werden. Die automatisierte Bereitstellung von Renninformationen erhöht die Aktualität der Team-Webseite und reduziert manuelle Pflegeaufwände.
Geplante Ergebnisse
Funktionsfähige ASP.NET MVC-Webanwendung
PostgreSQL-Datenbank mit Entity Framework Code First
Fuel Calculator
Setup-Hub mit Upload- und Download-Funktion
Benutzerverwaltung
Dokumentierte Systemarchitektur
Bewertung der technischen Umsetzbarkeit einer automatisierten Le-Mans-Ultimate-Integration