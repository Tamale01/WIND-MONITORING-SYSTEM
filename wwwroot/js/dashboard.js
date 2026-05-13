// File: wwwroot/js/dashboard.js
// Live dashboard updater — polls /api/wind/latest every 5 seconds
// and updates the wind speed display and "last updated" counter.

(function () {
    "use strict";

    const speedEl      = document.getElementById("wind-speed-value");
    const updatedEl    = document.getElementById("last-updated");
    const warningEl    = document.getElementById("stale-warning");
    const sensorEl     = document.getElementById("sensor-id");
    const simulatedEl  = document.getElementById("is-simulated");

    let lastUpdatedAt  = null;  // UTC Date of last successful fetch
    let secondsCounter = 0;     // Counts seconds since last update

    // ── Fetch latest reading from API ────────────────────────────────────────
    async function fetchLatest() {
        try {
            const res  = await fetch("/api/wind/latest", { credentials: "include" });
            if (!res.ok) return;

            const data = await res.json();

            // Animate speed value change
            const newSpeed = parseFloat(data.windSpeed).toFixed(2);
            if (speedEl.textContent !== newSpeed) {
                speedEl.classList.add("speed-flash");
                speedEl.textContent = newSpeed;
                setTimeout(() => speedEl.classList.remove("speed-flash"), 600);
            }

            // Update gauge needle if present
            updateGauge(data.windSpeed);

            // Update metadata
            if (sensorEl)    sensorEl.textContent    = data.sensorId    || "—";
            if (simulatedEl) simulatedEl.textContent = data.isSimulated ? "Simulated" : "Real";

            // Track update time
            lastUpdatedAt  = new Date(data.timestamp + "Z");
            secondsCounter = 0;

            // Hide stale warning
            if (warningEl) warningEl.style.display = "none";

        } catch (err) {
            console.error("Failed to fetch latest wind reading:", err);
        }
    }

    // ── Update "last updated X seconds ago" counter ──────────────────────────
    function tickCounter() {
        secondsCounter++;
        if (updatedEl) {
            updatedEl.textContent = `Last updated: ${secondsCounter}s ago`;
        }
        // Show stale warning if no new data in 30+ seconds
        if (warningEl && secondsCounter >= 30) {
            warningEl.style.display = "block";
        }
    }

    // ── SVG Gauge Needle Update ───────────────────────────────────────────────
    // Assumes an SVG gauge with a needle element id="gauge-needle".
    // Wind range 0–30 m/s maps to needle rotation -90° to +90°.
    function updateGauge(speed) {
        const needle = document.getElementById("gauge-needle");
        if (!needle) return;
        const clampedSpeed = Math.min(Math.max(speed, 0), 30);
        const degrees      = -90 + (clampedSpeed / 30) * 180;
        needle.style.transform = `rotate(${degrees}deg)`;
    }

    // ── Bootstrap on DOMContentLoaded ────────────────────────────────────────
    fetchLatest();                              // Immediate first fetch
    setInterval(fetchLatest, 5000);            // Poll every 5 seconds
    setInterval(tickCounter, 1000);            // Tick counter every second

})();
