// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
    const THEME_STORAGE_KEY = "sd-theme";

    const applyTheme = (theme) => {
        const resolvedTheme = theme === "dark" ? "dark" : "light";
        document.documentElement.setAttribute("data-theme", resolvedTheme);

        const toggleButton = document.getElementById("themeToggleBtn");
        if (!toggleButton) {
            return;
        }

        const isDark = resolvedTheme === "dark";
        const label = isDark ? "Switch to light mode" : "Switch to dark mode";
        toggleButton.setAttribute("aria-label", label);
        toggleButton.setAttribute("title", label);
        toggleButton.classList.toggle("is-dark", isDark);
    };

    const initThemeToggle = () => {
        const storedTheme = localStorage.getItem(THEME_STORAGE_KEY);
        const preferredTheme = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
        const initialTheme = storedTheme === "dark" || storedTheme === "light" ? storedTheme : preferredTheme;

        applyTheme(initialTheme);

        const toggleButton = document.getElementById("themeToggleBtn");
        toggleButton?.addEventListener("click", () => {
            const currentTheme = document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
            const nextTheme = currentTheme === "dark" ? "light" : "dark";

            localStorage.setItem(THEME_STORAGE_KEY, nextTheme);
            applyTheme(nextTheme);
        });
    };

    const setTimeSelectValue = (select, value) => {
        if (!select || !value) {
            return;
        }

        const optionExists = Array.from(select.options).some((o) => o.value === value);
        if (!optionExists) {
            const extra = document.createElement("option");
            extra.value = value;
            extra.textContent = value;
            select.appendChild(extra);
        }
        select.value = value;
    };

    const initCourseFormEnhancer = () => {
        const config = document.querySelector("[data-course-form-enhancer='true']");
        if (!config) {
            return;
        }

        const dayOrder = ["M", "T", "W", "R", "F"];
        const daysInput = document.getElementById("courseMeetingDaysInput");
        const timeInput = document.getElementById("courseMeetingTimeInput");
        const startInput = document.getElementById("meetingStartInput");
        const endInput = document.getElementById("meetingEndInput");
        const officeHoursTimeInput = document.getElementById("officeHoursTimeInput");
        const officeStartInput = document.getElementById("officeStartInput");
        const officeEndInput = document.getElementById("officeEndInput");
        const dayButtons = Array.from(document.querySelectorAll(".day-toggle"));

        const applyDaysToButtons = () => {
            const selected = new Set((daysInput?.value || "").split(""));
            dayButtons.forEach((btn) => {
                const isActive = selected.has(btn.dataset.day);
                btn.classList.toggle("btn-primary", isActive);
                btn.classList.toggle("btn-outline-primary", !isActive);
            });
        };

        const updateDaysHidden = () => {
            if (!daysInput) {
                return;
            }
            const selected = dayButtons
                .filter((btn) => btn.classList.contains("btn-primary"))
                .map((btn) => btn.dataset.day);
            daysInput.value = dayOrder.filter((d) => selected.includes(d)).join("");
        };

        const applyHiddenTimeToSelects = () => {
            const parts = (timeInput?.value || "").split("-");
            if (parts.length !== 2) {
                return;
            }
            setTimeSelectValue(startInput, parts[0].trim());
            setTimeSelectValue(endInput, parts[1].trim());
        };

        const applyHiddenOfficeHoursToSelects = () => {
            const parts = (officeHoursTimeInput?.value || "").split("-");
            if (parts.length !== 2) {
                return;
            }
            setTimeSelectValue(officeStartInput, parts[0].trim());
            setTimeSelectValue(officeEndInput, parts[1].trim());
        };

        const updateTimeHidden = () => {
            if (!timeInput || !startInput || !endInput) {
                return;
            }
            const start = startInput.value.trim();
            const end = endInput.value.trim();
            if (start && end) {
                timeInput.value = `${start}-${end}`;
            }
        };

        const updateOfficeHoursHidden = () => {
            if (!officeHoursTimeInput || !officeStartInput || !officeEndInput) {
                return;
            }
            const start = officeStartInput.value.trim();
            const end = officeEndInput.value.trim();
            if (start && end) {
                officeHoursTimeInput.value = `${start}-${end}`;
            }
        };

        dayButtons.forEach((btn) => {
            btn.addEventListener("click", () => {
                btn.classList.toggle("btn-primary");
                btn.classList.toggle("btn-outline-primary");
                updateDaysHidden();
            });
        });

        startInput?.addEventListener("change", updateTimeHidden);
        endInput?.addEventListener("change", updateTimeHidden);
        officeStartInput?.addEventListener("change", updateOfficeHoursHidden);
        officeEndInput?.addEventListener("change", updateOfficeHoursHidden);

        applyDaysToButtons();
        applyHiddenTimeToSelects();
        applyHiddenOfficeHoursToSelects();

        const lookupBtn = document.getElementById("lookupCrnBtn");
        const crnInput = document.getElementById("courseCrnInput");
        const courseNameInput = document.getElementById("courseNameInput");
        const sectionInput = document.getElementById("courseSectionInput");
        const leaderInput = document.getElementById("courseLeaderInput");
        const lookupBaseUrl = config.getAttribute("data-lookup-url") || "";
        const leaderLookupBaseUrl = config.getAttribute("data-leader-lookup-url") || "";
        const excludeId = config.getAttribute("data-exclude-id") || "";
        let leaderManuallyEdited = false;

        const fetchLeaderForCourseName = async () => {
            const courseName = (courseNameInput?.value || "").trim();
            if (!leaderLookupBaseUrl || !courseName || leaderManuallyEdited) {
                return;
            }

            try {
                const response = await fetch(`${leaderLookupBaseUrl}?courseName=${encodeURIComponent(courseName)}&excludeId=${encodeURIComponent(excludeId)}`);
                if (!response.ok) {
                    return;
                }

                const data = await response.json();
                if (leaderInput && data.courseLeader) {
                    leaderInput.value = data.courseLeader;
                }
            } catch {
                // Leave manual entry untouched if lookup fails.
            }
        };

        lookupBtn?.addEventListener("click", async () => {
            const crn = (crnInput?.value || "").trim();
            if (!crn || !lookupBaseUrl) {
                return;
            }

            try {
                const response = await fetch(`${lookupBaseUrl}?crn=${encodeURIComponent(crn)}&excludeId=${encodeURIComponent(excludeId)}`);
                if (!response.ok) {
                    return;
                }

                const data = await response.json();
                if (courseNameInput) {
                    courseNameInput.value = data.courseName || "";
                }
                if (sectionInput) {
                    sectionInput.value = data.courseSection || "";
                }

                await fetchLeaderForCourseName();
            } catch {
                // Leave manual entry untouched if lookup fails.
            }
        });

        courseNameInput?.addEventListener("blur", fetchLeaderForCourseName);
        courseNameInput?.addEventListener("change", fetchLeaderForCourseName);
        leaderInput?.addEventListener("input", () => {
            leaderManuallyEdited = true;
        });
    };

    const initSessionFormEnhancer = () => {
        const marker = document.querySelector("[data-session-form-enhancer='true']");
        if (!marker) {
            return;
        }

        const dayInput = document.getElementById("sessionDayInput");
        const timeInput = document.getElementById("sessionTimeInput");
        const startInput = document.getElementById("sessionStartInput");
        const endInput = document.getElementById("sessionEndInput");
        const dayButtons = Array.from(document.querySelectorAll(".session-day-toggle"));

        const applyDay = () => {
            dayButtons.forEach((btn) => {
                const active = btn.dataset.day === dayInput?.value;
                btn.classList.toggle("btn-primary", active);
                btn.classList.toggle("btn-outline-primary", !active);
            });
        };

        const applyTime = () => {
            const parts = (timeInput?.value || "").split("-");
            if (parts.length !== 2) {
                return;
            }
            setTimeSelectValue(startInput, parts[0].trim());
            setTimeSelectValue(endInput, parts[1].trim());
        };

        dayButtons.forEach((btn) => btn.addEventListener("click", () => {
            if (dayInput) {
                dayInput.value = btn.dataset.day || "";
            }
            applyDay();
        }));

        const updateTime = () => {
            if (!timeInput || !startInput || !endInput) {
                return;
            }
            const s = startInput.value.trim();
            const e = endInput.value.trim();
            if (s && e) {
                timeInput.value = `${s}-${e}`;
            }
        };

        startInput?.addEventListener("change", updateTime);
        endInput?.addEventListener("change", updateTime);

        applyDay();
        applyTime();
    };

    document.addEventListener("DOMContentLoaded", () => {
        initThemeToggle();
        initCourseFormEnhancer();
        initSessionFormEnhancer();
    });
})();
