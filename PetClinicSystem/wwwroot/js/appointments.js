// appointments.js

(function () {
    "use strict";

    console.log("%cAppointments JS (v2) Loaded", "color:green;font-weight:700;");

    // -------------------------
    // Utilities
    // -------------------------
    function formatLocalISODate(d) {
        // returns yyyy-mm-dd based on client's local time
        const yyyy = d.getFullYear();
        const mm = String(d.getMonth() + 1).padStart(2, "0");
        const dd = String(d.getDate()).padStart(2, "0");
        return `${yyyy}-${mm}-${dd}`;
    }

    function parseHHMMToDate(dateStr, hhmm) {
        // dateStr: "yyyy-mm-dd", hhmm: "HH:mm" (24h)
        const [y, m, d] = dateStr.split("-").map(Number);
        const parts = hhmm.split(":").map(Number);
        const hour = parts[0] || 0;
        const min = parts[1] || 0;
        return new Date(y, m - 1, d, hour, min, 0);
    }

    function safeLog() {
        if (console && console.log) {
            console.log.apply(console, arguments);
        }
    }

    function showToast(msg, type) {
        // prefer your global toast helper if present
        try {
            if (typeof showPublicToast === "function") {
                showPublicToast(msg, type === "error" ? "error" : (type === "warning" ? "warning" : "success"));
                return;
            }
        } catch (e) {
            // fallback
        }
        alert(msg);
    }

    function debounce(fn, wait) {
        let t;
        return function () {
            const args = arguments;
            clearTimeout(t);
            t = setTimeout(function () { fn.apply(null, args); }, wait);
        };
    }

    // -------------------------
    // CSS Injection for slots
    // -------------------------
    (function injectStyles() {
        const css = `
.time-grid { border: 1px solid #ddd; border-radius: 8px; min-height: 60px; padding: 6px; display:flex; flex-wrap:wrap; gap:6px; }
.time-slot { padding:8px 12px; border-radius:6px; border:1px solid #aaa; cursor:pointer; user-select:none; min-width:70px; text-align:center; transition:0.15s; font-weight:500; background:#fff; }
.time-slot:hover { background:#f3f3f3; }
.time-slot.selected { background:#0d6efd; border-color:#0d6efd; color:#fff; }
.time-slot.disabled, .time-slot.past { opacity:0.45; cursor:not-allowed; }
.time-slot.legacy-selected { outline: 2px dashed #ffc107; } /* existing booked slot in the past */
`;
        const style = document.createElement("style");
        style.type = "text/css";
        style.appendChild(document.createTextNode(css));
        document.head.appendChild(style);
    })();

    // -------------------------
    // Core: loadSlots & render
    // -------------------------
    function loadSlots(cfg) {
        try {
            const $date = $(cfg.date);
            const $service = $(cfg.service);
            const $grid = $(cfg.grid);
            const $slotField = $(cfg.slotField);

            if (!$date.length || !$service.length || !$grid.length) {
                safeLog("Slot engine aborted: missing elements", cfg);
                return;
            }

            const dateVal = $date.val(); // yyyy-mm-dd
            const serviceId = $service.val();

            if (!dateVal || !serviceId) {
                $grid.html('<div class="text-muted small">Select date & service</div>');
                return;
            }

            $grid.html('<div class="text-muted small">Loading…</div>');

            $.get("/Appointments/GetValidStartTimes", { date: dateVal, serviceId: serviceId })
                .done(function (slots) {
                    safeLog("Server slots:", slots);
                    $grid.empty();

                    if (!slots || slots.length === 0) {
                        $grid.html('<div class="text-danger small">No available times</div>');
                        return;
                    }

                    // determine whether dateVal is local "today"
                    const now = new Date();
                    const todayIso = formatLocalISODate(now);
                    const isToday = (dateVal === todayIso);

                    // existing (current) slot in edit mode
                    const existingSlot = parseInt($slotField.val() || 0, 10);

                    slots.forEach(function (s) {
                        const $btn = $("<div>").addClass("time-slot").text(s.start).data("slotId", s.slotId);

                        // compute whether this slot is past (client local)
                        let isPast = false;
                        try {
                            if (isToday && s.start) {
                                const slotDT = parseHHMMToDate(dateVal, s.start);
                                if (slotDT.getTime() <= now.getTime()) {
                                    isPast = true;
                                }
                            }
                        } catch (err) {
                            // If parsing fails, treat as not past
                            safeLog("parse slot time error", err);
                        }

                        // If this is the existing slot (edit), we will mark it selected.
                        // If it's a past slot but is the existing slot, mark as legacy-selected (visible), but do NOT allow selecting other past slots.
                        if (s.slotId === existingSlot) {
                            $btn.addClass("selected");
                            $slotField.val(existingSlot);

                            if (isPast) {
                                $btn.addClass("legacy-selected"); // visual cue
                                // we still allow the legacy selection to remain, but other past slots disabled
                            }
                        }

                        if (isPast) {
                            // If this past slot is the existing one, let it remain selected (but stylized),
                            // otherwise disable it.
                            if (s.slotId === existingSlot) {
                                // already handled above
                            } else {
                                $btn.addClass("disabled past").attr("title", "This time has already passed");
                                // prevent clicking later by not binding click
                            }
                        } else {
                            // safe to bind click
                            $btn.on("click", function () {
                                // Only allow selection of non-disabled buttons
                                if ($(this).hasClass("disabled")) return;
                                $grid.find(".time-slot").removeClass("selected");
                                $(this).addClass("selected");
                                $slotField.val($(this).data("slotId"));
                                safeLog("Slot selected:", $slotField.val());
                            });
                        }

                        $grid.append($btn);
                    });
                })
                .fail(function (xhr, status, err) {
                    safeLog("GetValidStartTimes failed", status, err);
                    $grid.html('<div class="text-danger small">Failed to load times</div>');
                });
        } catch (ex) {
            console.error("loadSlots error:", ex);
        }
    }

    // -------------------------
    // Slot engine init (cfg)
    // cfg = { date: selector, service: selector, grid: selector, slotField: selector, mode: "create"|"edit" }
    // -------------------------
    function initSlotEngine(cfg) {
        if (!cfg || !cfg.date || !cfg.service || !cfg.grid || !cfg.slotField) {
            safeLog("initSlotEngine missing cfg", cfg);
            return;
        }

        // make idempotent: store a sentinel on the grid element
        const gridEl = document.querySelector(cfg.grid);
        if (!gridEl) {
            safeLog("initSlotEngine aborted, grid element not found:", cfg.grid);
            return;
        }
        if (gridEl.__slotEngineInited) {
            safeLog("Slot engine already initialized for grid", cfg.grid);
            return;
        }
        gridEl.__slotEngineInited = true;

        const $date = $(cfg.date);
        const $service = $(cfg.service);
        const $grid = $(cfg.grid);
        const $slotField = $(cfg.slotField);

        // debounced loader
        const debouncedLoad = debounce(function () { loadSlots(cfg); }, 120);

        $date.on("change", debouncedLoad);
        $service.on("change", debouncedLoad);

        // If edit mode, auto-load immediately
        if (cfg.mode === "edit") {
            // Slight delay can help when modal is being shown and DOM still animating
            setTimeout(function () {
                loadSlots(cfg);
            }, 80);
        }

        // Validate on form submit: ensure slot selected for non-cancelled appointments
        // find nearest form (modal) containing the grid
        const $form = $grid.closest("form");
        if ($form && $form.length) {
            $form.off("submit.appointments").on("submit.appointments", function (e) {
                // if the slotField exists and is empty -> prevent submit
                const val = $slotField.val();
                if (!val || Number(val) === 0) {
                    e.preventDefault();
                    showToast("Please select a time slot before saving.", "error");
                    return false;
                }
                // otherwise allow submit
                return true;
            });
        }

        safeLog("Slot engine initialized:", cfg);
    }

    // -------------------------
    // Auto-initialize when modals are shown
    // -------------------------
    $(document).on("shown.bs.modal", function (e) {
        const modal = e.target;

        // create
        if ($(modal).find("#appointmentDate").length > 0 && $(modal).find("#serviceDropdown").length > 0) {
            initSlotEngine({
                date: "#appointmentDate",
                service: "#serviceDropdown",
                grid: "#timeGrid",
                slotField: "#selectedSlotId",
                mode: "create"
            });
        }

        // edit
        if ($(modal).find("#appointmentDateEdit").length > 0 && $(modal).find("#serviceDropdownEdit").length > 0) {
            initSlotEngine({
                date: "#appointmentDateEdit",
                service: "#serviceDropdownEdit",
                grid: "#timeGridEdit",
                slotField: "#selectedSlotIdEdit",
                mode: "edit"
            });
        }
    });

    // -------------------------
    // Global API (exposed for manual usage)
    // -------------------------
    window.initSlotEngineForModal = initSlotEngine;
    window.loadSlotsForCfg = loadSlots;

    // -------------------------
    // Debug helper (optional)
    // -------------------------
    window._appointmentsDebug = {
        formatLocalISODate: formatLocalISODate,
        parseHHMMToDate: parseHHMMToDate,
        loadSlots: loadSlots,
        initSlotEngine: initSlotEngine
    };
})();
