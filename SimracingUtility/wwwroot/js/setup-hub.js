// Abhängige Dropdowns für den Setup-Hub:
// Bei Änderung der Simulation werden Auto- und Streckenliste per JSON nachgeladen.
(function () {
    "use strict";

    function fillSelect(select, items, placeholder, selectedValue) {
        select.innerHTML = "";
        var ph = document.createElement("option");
        ph.value = "";
        ph.textContent = placeholder;
        select.appendChild(ph);

        items.forEach(function (item) {
            var opt = document.createElement("option");
            opt.value = item.id;
            opt.textContent = item.name;
            if (selectedValue && String(item.id) === String(selectedValue)) {
                opt.selected = true;
            }
            select.appendChild(opt);
        });
    }

    function loadList(url, sim) {
        return fetch(url + "?sim=" + encodeURIComponent(sim), {
            headers: { "Accept": "application/json" }
        }).then(function (res) {
            if (!res.ok) throw new Error("Laden fehlgeschlagen");
            return res.json();
        });
    }

    function initCascade(root) {
        var simSelect = root.querySelector("[data-sim-select]");
        var carSelect = root.querySelector("[data-car-select]");
        var trackSelect = root.querySelector("[data-track-select]");
        if (!simSelect) return;

        var carsUrl = root.getAttribute("data-cars-url");
        var tracksUrl = root.getAttribute("data-tracks-url");

        function refresh(preselectCar, preselectTrack) {
            var sim = simSelect.value;
            if (!sim) {
                if (carSelect) fillSelect(carSelect, [], "-- zuerst Simulation wählen --", null);
                if (trackSelect) fillSelect(trackSelect, [], "-- zuerst Simulation wählen --", null);
                return;
            }
            if (carSelect && carsUrl) {
                loadList(carsUrl, sim)
                    .then(function (items) { fillSelect(carSelect, items, "-- Auto wählen --", preselectCar); })
                    .catch(function () { fillSelect(carSelect, [], "-- Fehler beim Laden --", null); });
            }
            if (trackSelect && tracksUrl) {
                loadList(tracksUrl, sim)
                    .then(function (items) { fillSelect(trackSelect, items, "-- Strecke wählen --", preselectTrack); })
                    .catch(function () { fillSelect(trackSelect, [], "-- Fehler beim Laden --", null); });
            }
        }

        simSelect.addEventListener("change", function () { refresh(null, null); });

        // Beim ersten Laden: falls bereits eine Sim gewählt ist (z. B. nach Filterung
        // oder Validierungsfehler), Listen mit vorausgewählten Werten füllen.
        if (simSelect.value) {
            refresh(
                carSelect ? carSelect.getAttribute("data-selected") : null,
                trackSelect ? trackSelect.getAttribute("data-selected") : null
            );
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll("[data-setup-cascade]").forEach(initCascade);
    });
})();
