// File: wwwroot/js/charts.js
// Chart.js wind history chart with date range picker.
// Fetches data from GET /api/wind/history?hours=N and renders a line chart.

(function () {
    "use strict";

    let windChart = null; // Chart.js instance

    // ── Fetch history data and (re)draw chart ────────────────────────────────
    async function loadChart(hours) {
        try {
            const res = await fetch(`/api/wind/history?hours=${hours}`, { credentials: "include" });
            if (!res.ok) {
                console.error("API error:", res.status);
                return;
            }

            const json     = await res.json();
            const readings = json.readings || [];
            const stats    = json.stats;

            // ── Update stats cards ───────────────────────────────────────────
            const setEl = (id, val) => {
                const el = document.getElementById(id);
                if (el) el.textContent = val ?? "—";
            };
            setEl("stat-count",   stats?.count   ?? 0);
            setEl("stat-avg",     stats?.average != null ? stats.average.toFixed(2) + " m/s" : "—");
            setEl("stat-min",     stats?.min     != null ? parseFloat(stats.min).toFixed(2) + " m/s" : "—");
            setEl("stat-max",     stats?.max     != null ? parseFloat(stats.max).toFixed(2) + " m/s" : "—");

            // ── Prepare labels and data ──────────────────────────────────────
            const labels = readings.map(r => {
                const d = new Date(r.timestamp + "Z");
                return d.toLocaleString([], { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" });
            });
            const speeds = readings.map(r => parseFloat(r.windSpeed));

            // ── Build gradient fill ──────────────────────────────────────────
            const canvas = document.getElementById("windChart");
            if (!canvas) return;
            const ctx    = canvas.getContext("2d");
            const grad   = ctx.createLinearGradient(0, 0, 0, canvas.height);
            grad.addColorStop(0,   "rgba(99, 179, 237, 0.5)");
            grad.addColorStop(1,   "rgba(99, 179, 237, 0.02)");

            const dataset = {
                label: "Wind Speed (m/s)",
                data: speeds,
                borderColor: "#63b3ed",
                backgroundColor: grad,
                borderWidth: 2,
                pointRadius: readings.length < 100 ? 3 : 0,
                pointHoverRadius: 5,
                tension: 0.4,
                fill: true
            };

            // ── Create or update chart ───────────────────────────────────────
            if (windChart) {
                windChart.data.labels   = labels;
                windChart.data.datasets = [dataset];
                windChart.update("active");
            } else {
                windChart = new Chart(ctx, {
                    type: "line",
                    data: { labels, datasets: [dataset] },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        animation: { duration: 600 },
                        plugins: {
                            legend: {
                                labels: { color: "#e2e8f0", font: { family: "Inter" } }
                            },
                            tooltip: {
                                callbacks: {
                                    label: ctx => ` ${ctx.parsed.y.toFixed(2)} m/s`
                                }
                            }
                        },
                        scales: {
                            x: {
                                ticks: { color: "#a0aec0", maxTicksLimit: 12, font: { size: 11 } },
                                grid:  { color: "rgba(255,255,255,0.05)" }
                            },
                            y: {
                                min: 0,
                                suggestedMax: 35,
                                ticks: { color: "#a0aec0", callback: v => v + " m/s" },
                                grid:  { color: "rgba(255,255,255,0.08)" }
                            }
                        }
                    }
                });
            }

        } catch (err) {
            console.error("Failed to load chart data:", err);
        }
    }

    // ── Date range button wiring ─────────────────────────────────────────────
    document.addEventListener("DOMContentLoaded", () => {
        const buttons = document.querySelectorAll("[data-hours]");

        buttons.forEach(btn => {
            btn.addEventListener("click", () => {
                buttons.forEach(b => b.classList.remove("active"));
                btn.classList.add("active");
                loadChart(parseInt(btn.dataset.hours, 10));
            });
        });

        // Default: load last 24 hours
        loadChart(24);
    });

})();
