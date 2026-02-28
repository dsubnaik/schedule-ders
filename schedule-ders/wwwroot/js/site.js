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

    const setTimeFieldValue = (field, value) => {
        if (!field || !value) {
            return;
        }

        if (field.tagName === "SELECT") {
            const optionExists = Array.from(field.options).some((o) => o.value === value);
            if (!optionExists) {
                const extra = document.createElement("option");
                extra.value = value;
                extra.textContent = value;
                field.appendChild(extra);
            }
        }
        field.value = value;
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
        const officeHoursDayInput = document.getElementById("officeHoursDayInput");
        const officeStartInput = document.getElementById("officeStartInput");
        const officeEndInput = document.getElementById("officeEndInput");
        const dayButtons = Array.from(document.querySelectorAll(".day-toggle"));
        const officeDayButtons = Array.from(document.querySelectorAll(".office-day-toggle"));

        const officeDayMap = {
            monday: "M",
            tuesday: "T",
            wednesday: "W",
            thursday: "R",
            friday: "F"
        };

        const normalizeOfficeDay = (value) => {
            const trimmed = (value || "").trim();
            if (!trimmed) {
                return "";
            }

            const upper = trimmed.toUpperCase();
            if (["M", "T", "W", "R", "F"].includes(upper)) {
                return upper;
            }

            const mapped = officeDayMap[trimmed.toLowerCase()];
            return mapped || trimmed;
        };

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
            setTimeFieldValue(startInput, parts[0].trim());
            setTimeFieldValue(endInput, parts[1].trim());
        };

        const applyHiddenOfficeHoursToSelects = () => {
            const parts = (officeHoursTimeInput?.value || "").split("-");
            if (parts.length !== 2) {
                return;
            }
            setTimeFieldValue(officeStartInput, parts[0].trim());
            setTimeFieldValue(officeEndInput, parts[1].trim());
        };

        const applyOfficeDayToButtons = () => {
            const selectedDay = normalizeOfficeDay(officeHoursDayInput?.value || "");
            officeDayButtons.forEach((btn) => {
                const isActive = btn.dataset.day === selectedDay;
                btn.classList.toggle("btn-primary", isActive);
                btn.classList.toggle("btn-outline-primary", !isActive);
            });
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

        officeDayButtons.forEach((btn) => {
            btn.addEventListener("click", () => {
                if (officeHoursDayInput) {
                    officeHoursDayInput.value = btn.dataset.day || "";
                }
                applyOfficeDayToButtons();
            });
        });

        startInput?.addEventListener("input", updateTimeHidden);
        endInput?.addEventListener("input", updateTimeHidden);
        startInput?.addEventListener("change", updateTimeHidden);
        endInput?.addEventListener("change", updateTimeHidden);
        officeStartInput?.addEventListener("input", updateOfficeHoursHidden);
        officeEndInput?.addEventListener("input", updateOfficeHoursHidden);
        officeStartInput?.addEventListener("change", updateOfficeHoursHidden);
        officeEndInput?.addEventListener("change", updateOfficeHoursHidden);

        applyDaysToButtons();
        applyOfficeDayToButtons();
        applyHiddenTimeToSelects();
        applyHiddenOfficeHoursToSelects();

        const lookupBtn = document.getElementById("lookupCrnBtn");
        const crnInput = document.getElementById("courseCrnInput");
        const courseNameInput = document.getElementById("courseNameInput");
        const courseTitleInput = document.getElementById("courseTitleInput");
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
                const response = await fetch(`${lookupBaseUrl}?crn=${encodeURIComponent(crn)}`);
                if (!response.ok) {
                    return;
                }

                const data = await response.json();
                if (courseNameInput) {
                    courseNameInput.value = data.courseName || "";
                }
                if (courseTitleInput) {
                    courseTitleInput.value = data.courseTitle || "";
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

    const initTimePickerDropdowns = () => {
        const inputs = Array.from(document.querySelectorAll(".time-picker-input[data-time-options]"));
        if (inputs.length === 0) {
            return;
        }

        const closeAll = () => {
            document.querySelectorAll("[data-time-picker-menu='true']").forEach((menu) => menu.classList.remove("show"));
        };

        const renderOptions = (input, filterText = "") => {
            const sourceId = input.getAttribute("data-time-options");
            const menu = input.parentElement?.querySelector("[data-time-picker-menu='true']");
            if (!sourceId || !menu) {
                return;
            }

            const datalist = document.getElementById(sourceId);
            if (!datalist) {
                return;
            }

            const options = Array.from(datalist.querySelectorAll("option"))
                .map((o) => (o.getAttribute("value") || "").trim())
                .filter((v) => v.length > 0);

            const normalizedFilter = filterText.trim().toLowerCase();
            const filtered = normalizedFilter
                ? options.filter((o) => o.toLowerCase().includes(normalizedFilter))
                : options;

            menu.innerHTML = "";
            if (filtered.length === 0) {
                menu.classList.remove("show");
                return;
            }

            const fragment = document.createDocumentFragment();
            filtered.slice(0, 60).forEach((value) => {
                const button = document.createElement("button");
                button.type = "button";
                button.className = "time-picker-option";
                button.textContent = value;
                button.addEventListener("click", () => {
                    input.value = value;
                    input.dispatchEvent(new Event("input", { bubbles: true }));
                    menu.classList.remove("show");
                });
                fragment.appendChild(button);
            });

            menu.appendChild(fragment);
            menu.classList.add("show");
        };

        inputs.forEach((input) => {
            input.addEventListener("focus", () => renderOptions(input, input.value || ""));
            input.addEventListener("input", () => renderOptions(input, input.value || ""));
            input.addEventListener("keydown", (event) => {
                if (event.key === "Escape") {
                    const menu = input.parentElement?.querySelector("[data-time-picker-menu='true']");
                    menu?.classList.remove("show");
                }
            });
        });

        document.addEventListener("click", (event) => {
            const target = event.target;
            if (!(target instanceof Element) || !target.closest(".time-picker")) {
                closeAll();
            }
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
        const courseSelect = document.getElementById("CourseID");
        const sectionTargetsContainer = document.querySelector("[data-section-targets-container='true']");
        const sectionTargetsList = sectionTargetsContainer?.querySelector("[data-section-targets-list='true']");

        const wireSectionTargetButtons = () => {
            sectionTargetsList?.querySelectorAll("label.btn input[type='checkbox']").forEach((input) => {
                const checkbox = input;
                const label = checkbox.closest("label.btn");
                if (!label) {
                    return;
                }

                const applyState = () => {
                    label.classList.toggle("btn-primary", checkbox.checked);
                    label.classList.toggle("btn-outline-primary", !checkbox.checked);
                };

                checkbox.addEventListener("change", applyState);
                applyState();
            });
        };

        const renderSectionTargets = (items, selectedCourseId) => {
            if (!sectionTargetsList) {
                return;
            }

            if (!Array.isArray(items) || items.length === 0) {
                sectionTargetsList.innerHTML = "<span class='text-muted small'>No sections found for this course.</span>";
                return;
            }

            sectionTargetsList.innerHTML = items.map((item) => {
                const id = Number(item.courseId || 0);
                const section = htmlEncode(String(item.section || ""));
                const checked = id === selectedCourseId ? "checked" : "";
                const checkedClass = id === selectedCourseId ? "btn-primary" : "btn-outline-primary";
                return `<label class="btn ${checkedClass} mb-0">
                    <input type="checkbox" class="d-none" name="sectionCourseIds" value="${id}" ${checked} />
                    ${section}
                </label>`;
            }).join("");

            wireSectionTargetButtons();
        };

        const refreshSectionTargets = async () => {
            if (!sectionTargetsContainer || !courseSelect) {
                return;
            }

            const courseIdValue = Number(courseSelect.value || 0);
            if (!courseIdValue) {
                if (sectionTargetsList) {
                    sectionTargetsList.innerHTML = "<span class='text-muted small'>Select a course to choose sections.</span>";
                }
                return;
            }

            const endpoint = sectionTargetsContainer.getAttribute("data-section-targets-url");
            if (!endpoint) {
                return;
            }

            try {
                const response = await fetch(`${endpoint}?courseId=${encodeURIComponent(courseIdValue)}`);
                if (!response.ok) {
                    return;
                }

                const items = await response.json();
                renderSectionTargets(items, courseIdValue);
            } catch {
                // Keep current selection if lookup fails.
            }
        };

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
            setTimeFieldValue(startInput, parts[0].trim());
            setTimeFieldValue(endInput, parts[1].trim());
        };

        const parseTimeToMinutes = (value) => {
            const match = String(value || "").trim().toLowerCase().match(/^(\d{1,2}):(\d{2})(am|pm)$/);
            if (!match) {
                return null;
            }

            let hour = Number(match[1]);
            const minute = Number(match[2]);
            const suffix = match[3];
            if (Number.isNaN(hour) || Number.isNaN(minute) || minute < 0 || minute > 59 || hour < 1 || hour > 12) {
                return null;
            }

            if (hour === 12) {
                hour = 0;
            }
            if (suffix === "pm") {
                hour += 12;
            }

            return (hour * 60) + minute;
        };

        const findOneHourAfterOption = (startValue) => {
            if (!endInput) {
                return null;
            }

            const startMinutes = parseTimeToMinutes(startValue);
            if (startMinutes == null) {
                return null;
            }

            const target = startMinutes + 60;
            const optionValues = Array.from(endInput.options)
                .map((o) => (o.value || "").trim())
                .filter((v) => v.length > 0);

            let bestValue = null;
            let bestMinutes = Number.POSITIVE_INFINITY;

            optionValues.forEach((value) => {
                const optionMinutes = parseTimeToMinutes(value);
                if (optionMinutes == null || optionMinutes < target) {
                    return;
                }
                if (optionMinutes < bestMinutes) {
                    bestMinutes = optionMinutes;
                    bestValue = value;
                }
            });

            if (bestValue) {
                return bestValue;
            }

            // If +1 hour is beyond available range, fall back to latest selectable end time.
            for (let i = optionValues.length - 1; i >= 0; i--) {
                if (parseTimeToMinutes(optionValues[i]) != null) {
                    return optionValues[i];
                }
            }

            return null;
        };

        let endWasManuallyChanged = false;
        let lastAutoEndValue = "";
        const autoFillEndFromStart = (force = false) => {
            if (!startInput || !endInput) {
                return;
            }

            const start = startInput.value.trim();
            if (!start) {
                return;
            }

            const canAutoFill = force
                || !endWasManuallyChanged
                || !endInput.value
                || endInput.value === lastAutoEndValue;

            if (!canAutoFill) {
                return;
            }

            const suggestedEnd = findOneHourAfterOption(start);
            if (!suggestedEnd) {
                return;
            }

            setTimeFieldValue(endInput, suggestedEnd);
            lastAutoEndValue = suggestedEnd;
            updateTime();
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

        startInput?.addEventListener("change", () => {
            autoFillEndFromStart();
            updateTime();
        });
        endInput?.addEventListener("change", () => {
            const current = (endInput?.value || "").trim();
            endWasManuallyChanged = current.length > 0 && current !== lastAutoEndValue;
            updateTime();
        });
        courseSelect?.addEventListener("change", refreshSectionTargets);

        applyDay();
        applyTime();
        autoFillEndFromStart(true);
        wireSectionTargetButtons();
    };

    const htmlEncode = (value) => {
        const div = document.createElement("div");
        div.textContent = value ?? "";
        return div.innerHTML;
    };

    const formatLocalDate = (value) => {
        if (!value) {
            return "No updates yet";
        }

        const parsed = new Date(value);
        if (Number.isNaN(parsed.getTime())) {
            return "No updates yet";
        }

        return parsed.toLocaleString(undefined, {
            year: "numeric",
            month: "short",
            day: "numeric",
            hour: "numeric",
            minute: "2-digit"
        });
    };

    const formatRelativeTime = (value) => {
        if (!value) {
            return "No updates yet";
        }

        const parsed = new Date(value);
        if (Number.isNaN(parsed.getTime())) {
            return "No updates yet";
        }

        const now = new Date();
        const deltaMs = now.getTime() - parsed.getTime();
        const minute = 60 * 1000;
        const hour = 60 * minute;
        const day = 24 * hour;

        if (deltaMs < minute) return "just now";
        if (deltaMs < hour) {
            const minutes = Math.max(1, Math.floor(deltaMs / minute));
            return `${minutes} minute${minutes === 1 ? "" : "s"} ago`;
        }
        if (deltaMs < day) {
            const hours = Math.max(1, Math.floor(deltaMs / hour));
            return `${hours} hour${hours === 1 ? "" : "s"} ago`;
        }
        if (deltaMs < 7 * day) {
            const days = Math.max(1, Math.floor(deltaMs / day));
            return `${days} day${days === 1 ? "" : "s"} ago`;
        }
        if (deltaMs < 31 * day) {
            const weeks = Math.max(1, Math.floor(deltaMs / (7 * day)));
            return `${weeks} week${weeks === 1 ? "" : "s"} ago`;
        }
        if (deltaMs < 365 * day) {
            const months = Math.max(1, Math.floor(deltaMs / (30 * day)));
            return `${months} month${months === 1 ? "" : "s"} ago`;
        }
        const years = Math.max(1, Math.floor(deltaMs / (365 * day)));
        return `${years} year${years === 1 ? "" : "s"} ago`;
    };

    const statusClass = (status) => {
        const normalized = (status || "").toLowerCase();
        if (normalized === "pending") {
            return "status-pill-pending";
        }
        if (normalized === "underreview" || normalized === "under review") {
            return "status-pill-review";
        }
        if (normalized === "approved") {
            return "status-pill-approved";
        }
        return "status-pill-denied";
    };

    const requestReviewActionClass = (status) => {
        const normalized = (status || "").toLowerCase().replace(/\s+/g, "");
        return normalized === "pending" || normalized === "underreview"
            ? "request-action-link-priority"
            : "request-action-link-subtle";
    };

    const statusToApiValue = (rawStatus) => {
        const numeric = Number(rawStatus);
        if (!Number.isNaN(numeric)) {
            return numeric;
        }

        const map = {
            pending: 0,
            underreview: 1,
            approved: 2,
            denied: 3
        };

        const normalized = String(rawStatus || "").replace(/\s+/g, "").toLowerCase();
        return Object.prototype.hasOwnProperty.call(map, normalized) ? map[normalized] : rawStatus;
    };

    const showApiError = (form, message) => {
        if (!form) {
            return;
        }

        let errorBox = form.querySelector("[data-api-error='true']");
        if (!errorBox) {
            errorBox = document.createElement("div");
            errorBox.setAttribute("data-api-error", "true");
            errorBox.className = "alert alert-danger";
            form.prepend(errorBox);
        }

        errorBox.textContent = message;
    };

    const initProfessorCreateApiSubmit = () => {
        const form = document.querySelector("[data-professor-request-create-form='true']");
        if (!form) {
            return;
        }

        form.addEventListener("submit", async (event) => {
            event.preventDefault();

            const apiUrl = form.getAttribute("data-api-url");
            const trackUrlTemplate = form.getAttribute("data-track-url-template");
            if (!apiUrl || !trackUrlTemplate) {
                showApiError(form, "Request API is not configured.");
                return;
            }

            const payload = {
                courseId: form.querySelector("[name='CourseID']")?.value || null,
                requestedCourseName: form.querySelector("[name='RequestedCourseName']")?.value || "",
                requestedCourseTitle: form.querySelector("[name='RequestedCourseTitle']")?.value || "",
                requestedCourseSection: form.querySelector("[name='RequestedCourseSection']")?.value || "",
                requestedCourseProfessor: form.querySelector("[name='RequestedCourseProfessor']")?.value || "",
                requestNotes: form.querySelector("[name='RequestNotes']")?.value || ""
            };

            if (payload.courseId === "") {
                payload.courseId = null;
            } else if (payload.courseId !== null) {
                payload.courseId = Number(payload.courseId);
            }

            try {
                const response = await fetch(apiUrl, {
                    method: "POST",
                    credentials: "same-origin",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(payload)
                });

                if (!response.ok) {
                    let apiMessage = "Unable to submit request right now. Please try again.";
                    try {
                        const errorBody = await response.json();
                        if (errorBody?.message) {
                            apiMessage = String(errorBody.message);
                        } else if (errorBody?.errors) {
                            const firstError = Object.values(errorBody.errors)[0];
                            if (Array.isArray(firstError) && firstError.length > 0) {
                                apiMessage = String(firstError[0]);
                            }
                        }
                    } catch {
                        // Keep default message.
                    }
                    showApiError(form, apiMessage);
                    return;
                }

                const created = await response.json();
                if (!created?.requestId) {
                    showApiError(form, "Request was created, but the response was invalid.");
                    return;
                }

                const target = trackUrlTemplate.replace("__id__", String(created.requestId));
                window.location.assign(target);
            } catch {
                showApiError(form, "Network error while submitting request.");
            }
        });
    };

    const initProfessorRequestFormEnhancer = () => {
        const form = document.querySelector("[data-professor-request-form='true']");
        if (!form) {
            return;
        }

        const sectionInput = form.querySelector("[name='RequestedCourseSection']");
        const courseIdInput = form.querySelector("[name='CourseID']");
        const courseNameInput = form.querySelector("[name='RequestedCourseName']");
        const courseTitleInput = form.querySelector("[name='RequestedCourseTitle']");
        const sectionButtons = Array.from(form.querySelectorAll("[data-section-value]"));

        const setActiveSectionButton = (value) => {
            sectionButtons.forEach((button) => {
                const isActive = button.getAttribute("data-section-value") === value;
                button.classList.toggle("btn-primary", isActive);
                button.classList.toggle("btn-outline-primary", !isActive);
            });
        };

        sectionButtons.forEach((button) => {
            button.addEventListener("click", () => {
                const selectedValue = button.getAttribute("data-section-value") || "";
                if (sectionInput) {
                    sectionInput.value = selectedValue;
                }
                if (courseIdInput) {
                    courseIdInput.value = "";
                }
                setActiveSectionButton(selectedValue);
            });
        });

        courseNameInput?.addEventListener("input", () => {
            if (courseIdInput) {
                courseIdInput.value = "";
            }
        });
        courseTitleInput?.addEventListener("input", () => {
            if (courseIdInput) {
                courseIdInput.value = "";
            }
        });

        setActiveSectionButton(sectionInput?.value || "");
    };

    const initProfessorTrackApiRefresh = () => {
        const container = document.querySelector("[data-professor-request-track='true']");
        if (!container) {
            return;
        }

        const apiUrl = container.getAttribute("data-status-api-url");
        if (!apiUrl) {
            return;
        }

        const progressBar = container.querySelector("[data-track-progress-bar='true']");
        const statusText = container.querySelector("[data-track-current-status='true']");
        const statusPill = container.querySelector("[data-track-current-status-pill='true']");
        const lastUpdatedText = container.querySelector("[data-track-last-updated='true']");
        const lastUpdatedRelative = container.querySelector("[data-track-last-updated-relative='true']");
        const submittedRelative = container.querySelector("[data-track-submitted-relative='true']");
        const adminNotesText = container.querySelector("[data-track-admin-notes='true']");
        const submittedStage = container.querySelector("[data-track-stage-submitted='true']");
        const reviewStage = container.querySelector("[data-track-stage-review='true']");
        const finalStage = container.querySelector("[data-track-stage-final='true']");

        const progressClassMap = ["progress-w-25", "progress-w-60", "progress-w-100"];

        fetch(apiUrl)
            .then((response) => (response.ok ? response.json() : null))
            .then((data) => {
                if (!data) {
                    return;
                }

                const progress = Number(data.progressPercent || 0);
                const status = data.status || "Pending";
                const isDenied = String(status).toLowerCase() === "denied";

                if (progressBar) {
                    progressClassMap.forEach((cssClass) => progressBar.classList.remove(cssClass));
                    const widthClass = progress >= 100 ? "progress-w-100" : progress >= 60 ? "progress-w-60" : "progress-w-25";
                    progressBar.classList.add(widthClass);
                    progressBar.classList.toggle("bg-danger", isDenied);
                    progressBar.classList.toggle("request-progress-warning", !isDenied);
                }

                if (statusText) {
                    statusText.textContent = status;
                }
                if (statusPill) {
                    statusPill.classList.remove("status-pill-pending", "status-pill-review", "status-pill-approved", "status-pill-denied");
                    statusPill.classList.add(statusClass(status));
                }

                if (lastUpdatedText) {
                    lastUpdatedText.textContent = formatLocalDate(data.lastUpdatedAtUtc);
                }
                if (lastUpdatedRelative) {
                    lastUpdatedRelative.textContent = formatRelativeTime(data.lastUpdatedAtUtc);
                }
                if (submittedRelative) {
                    submittedRelative.textContent = formatRelativeTime(data.submittedAtUtc);
                }

                if (adminNotesText) {
                    adminNotesText.textContent = (data.adminNotes || "").trim() || "No updates yet.";
                }

                if (submittedStage) {
                    submittedStage.textContent = "Complete";
                    submittedStage.classList.add("track-stage-complete");
                }
                if (reviewStage) {
                    const isUnderReview = String(status).toLowerCase().replace(/\s+/g, "") === "underreview";
                    reviewStage.textContent = progress >= 100 ? "Complete" : (isUnderReview ? "In Progress" : "Pending");
                    reviewStage.classList.remove("track-stage-current", "track-stage-complete");
                    if (isUnderReview) {
                        reviewStage.classList.add("track-stage-current");
                    } else if (progress >= 100) {
                        reviewStage.classList.add("track-stage-complete");
                    }
                }
                if (finalStage) {
                    const normalized = String(status).toLowerCase().replace(/\s+/g, "");
                    finalStage.textContent = normalized === "approved" ? "Approved" : (normalized === "denied" ? "Denied" : "Pending");
                    finalStage.classList.toggle("track-stage-complete", normalized === "approved" || normalized === "denied");
                }
            })
            .catch(() => {
                // Keep server-rendered values on API failure.
            });
    };

    const initDeleteModalBinding = (options) => {
        const {
            triggerSelector,
            idInputId,
            idsInputId,
            displayTargetId,
            idAttribute,
            idsAttribute,
            displayAttribute,
            defaultDisplayText
        } = options;

        const idInput = document.getElementById(idInputId);
        const idsInput = idsInputId ? document.getElementById(idsInputId) : null;
        const displayTarget = document.getElementById(displayTargetId);
        if (!idInput || !displayTarget) {
            return;
        }

        document.querySelectorAll(triggerSelector).forEach((trigger) => {
            trigger.addEventListener("click", () => {
                idInput.value = trigger.getAttribute(idAttribute) || "";
                if (idsInput && idsAttribute) {
                    idsInput.value = trigger.getAttribute(idsAttribute) || "";
                }
                displayTarget.textContent = trigger.getAttribute(displayAttribute) || defaultDisplayText;
            });
        });
    };

    const initAdminRequestsApiList = () => {
        const form = document.querySelector("[data-admin-requests-filter='true']");
        const tbody = document.querySelector("[data-admin-requests-tbody='true']");
        if (!form || !tbody) {
            return;
        }

        const apiUrl = form.getAttribute("data-api-url");
        const reviewTemplate = form.getAttribute("data-review-url-template");
        const removeTemplate = form.getAttribute("data-remove-api-url-template");
        const statusSelect = form.querySelector("[name='status']");
        const statusChips = Array.from(form.querySelectorAll("[data-admin-status-chips='true'] [data-status-value]"));
        if (!apiUrl || !reviewTemplate || !removeTemplate) {
            return;
        }

        const normalizeStatusValue = (value) => String(value || "").replace(/\s+/g, "").toLowerCase();

        const refreshStatusChipState = () => {
            const current = normalizeStatusValue(statusSelect?.value || "");
            statusChips.forEach((chip) => {
                const chipValue = normalizeStatusValue(chip.getAttribute("data-status-value") || "");
                const isActive = current === chipValue;
                chip.classList.toggle("is-active", isActive);
            });
        };

        const renderRows = (items) => {
            if (!Array.isArray(items) || items.length === 0) {
                tbody.innerHTML = "<tr><td colspan=\"5\" class=\"text-muted\">No SI requests found.</td></tr>";
                return;
            }

            tbody.innerHTML = items.map((item) => {
                const reviewUrl = reviewTemplate.replace("__id__", String(item.requestId));
                const removeUrl = removeTemplate.replace("__id__", String(item.requestId));
                const normalizedStatus = String(item.status || "").toLowerCase().replace(/\s+/g, "");
                const canRemove = normalizedStatus === "approved" || normalizedStatus === "denied";
                return `<tr>
                    <td>${htmlEncode(item.courseDisplay || "Manual Course Entry")}</td>
                    <td>${htmlEncode(item.professorName || "")}</td>
                    <td>
                        <div>${htmlEncode(formatLocalDate(item.submittedAtUtc))}</div>
                        <span class="relative-time-pill">${htmlEncode(formatRelativeTime(item.submittedAtUtc))}</span>
                    </td>
                    <td><span class="status-pill ${statusClass(item.status)}">${htmlEncode(item.status || "")}</span></td>
                    <td class="text-nowrap text-center">
                        <a class="request-action-link ${requestReviewActionClass(item.status)}" href="${reviewUrl}">Review</a>
                        ${canRemove ? `<button type="button" class="request-action-link request-action-delete request-action-link-inline" data-admin-request-remove-btn="true" data-remove-url="${removeUrl}" data-request-id="${item.requestId}">Remove</button>` : ""}
                    </td>
                </tr>`;
            }).join("");
        };

        const load = async () => {
            const status = form.querySelector("[name='status']")?.value || "";
            const query = new URLSearchParams();
            if (status) {
                query.set("status", status);
            }
            query.set("page", "1");
            query.set("pageSize", "200");

            const response = await fetch(`${apiUrl}?${query.toString()}`);
            if (!response.ok) {
                return;
            }

            const data = await response.json();
            renderRows(data.items || []);
        };

        form.addEventListener("submit", async (event) => {
            event.preventDefault();
            try {
                await load();
            } catch {
                showApiError(form, "Unable to load requests from API.");
            }
        });

        statusChips.forEach((chip) => {
            chip.addEventListener("click", () => {
                if (!statusSelect) {
                    return;
                }
                statusSelect.value = chip.getAttribute("data-status-value") || "";
                refreshStatusChipState();
                form.requestSubmit();
            });
        });

        tbody.addEventListener("click", async (event) => {
            const target = event.target;
            if (!(target instanceof HTMLElement)) {
                return;
            }

            const removeButton = target.closest("[data-admin-request-remove-btn='true']");
            if (!(removeButton instanceof HTMLElement)) {
                return;
            }

            const requestId = removeButton.getAttribute("data-request-id") || "";
            const removeUrl = removeButton.getAttribute("data-remove-url")
                || removeTemplate.replace("__id__", requestId);
            if (!removeUrl) {
                return;
            }

            const confirmed = window.confirm("Remove this request from the list?");
            if (!confirmed) {
                return;
            }

            try {
                const response = await fetch(removeUrl, {
                    method: "DELETE",
                    credentials: "same-origin"
                });

                if (!response.ok) {
                    showApiError(form, "Unable to remove request.");
                    return;
                }

                await load();
            } catch {
                showApiError(form, "Network error while removing request.");
            }
        });

        statusSelect?.addEventListener("change", refreshStatusChipState);
        refreshStatusChipState();

        load().catch(() => {
            // Keep server-rendered list on API failure.
        });
    };

    const initAdminRequestApiUpdate = () => {
        const form = document.querySelector("[data-admin-request-update-form='true']");
        if (!form) {
            return;
        }

        form.addEventListener("submit", async (event) => {
            event.preventDefault();

            const apiUrl = form.getAttribute("data-api-url");
            const successUrl = form.getAttribute("data-success-url");
            if (!apiUrl || !successUrl) {
                showApiError(form, "Update API is not configured.");
                return;
            }

            const rawStatus = form.querySelector("[name='Status']")?.value;
            const adminNotes = form.querySelector("[name='AdminNotes']")?.value || "";
            const payload = { status: statusToApiValue(rawStatus), adminNotes };

            try {
                const response = await fetch(apiUrl, {
                    method: "PATCH",
                    credentials: "same-origin",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(payload)
                });

                if (!response.ok) {
                    let apiMessage = "Unable to save status right now. Please try again.";
                    try {
                        const errorBody = await response.json();
                        if (errorBody?.message) {
                            apiMessage = String(errorBody.message);
                        }
                    } catch {
                        // Keep default message.
                    }
                    showApiError(form, apiMessage);
                    return;
                }

                window.location.assign(successUrl);
            } catch {
                showApiError(form, "Network error while saving status.");
            }
        });
    };

    const initCourseDetailsApiSessions = () => {
        const container = document.querySelector("[data-course-sessions-api='true']");
        const cards = container?.querySelector("[data-course-sessions-cards='true']");
        if (!container || !cards) {
            return;
        }

        const apiUrl = container.getAttribute("data-api-url");
        const editTemplate = container.getAttribute("data-edit-url-template");
        if (!apiUrl || !editTemplate) {
            return;
        }

        const wireDynamicDeleteButtons = () => {
            cards.querySelectorAll("[data-session-delete-trigger='true']").forEach((trigger) => {
                const button = trigger;
                button.addEventListener("click", () => {
                    const idInput = document.getElementById("deleteSessionIdInput");
                    const idsInput = document.getElementById("deleteSessionIdsInput");
                    const displayTarget = document.getElementById("deleteSessionName");
                    if (!idInput || !displayTarget) {
                        return;
                    }

                    idInput.value = button.getAttribute("data-session-id") || "";
                    if (idsInput) {
                        idsInput.value = button.getAttribute("data-session-ids") || "";
                    }
                    displayTarget.textContent = button.getAttribute("data-session-display") || "Session";
                });
            });
        };

        fetch(apiUrl)
            .then((response) => (response.ok ? response.json() : null))
            .then((data) => {
                const sessions = data?.sessions || [];
                if (!Array.isArray(sessions) || sessions.length === 0) {
                    cards.innerHTML = "<div class=\"col-12\"><div class=\"empty-state\"><p class=\"empty-state-title\">No sessions added yet.</p></div></div>";
                    return;
                }

                cards.innerHTML = sessions.map((session) => {
                    const editUrl = editTemplate.replace("__id__", String(session.sessionId));
                    const time = session.endTime ? `${session.startTime}-${session.endTime}` : session.startTime;
                    const courseId = container.getAttribute("data-course-id") || "";
                    const courseName = container.getAttribute("data-course-name") || "Course";
                    const courseTitle = container.getAttribute("data-course-title") || "";
                    const courseSection = container.getAttribute("data-course-section") || "";
                    const courseLeader = container.getAttribute("data-course-leader") || "";
                    const courseUrl = `/Courses/Details/${courseId}`;
                    const sessionLabel = `${courseName} (${courseSection}) - ${courseTitle} ${session.day || ""} ${time || ""}`.trim();
                    return `<div class="col-12 col-md-6 col-lg-4">
                        <article class="card h-100 shadow-sm student-course-card">
                            <div class="card-body">
                                <div class="session-card-top mb-2">
                                    <h3 class="h4 mb-0">
                                        <span class="fw-bold">${htmlEncode(courseName)}</span>
                                        ${courseTitle ? `<span class="text-muted"> - ${htmlEncode(courseTitle)}</span>` : ""}
                                    </h3>
                                </div>
                                <div class="d-flex align-items-center gap-2 mb-2 flex-wrap">
                                    <span class="badge session-meta-badge session-meta-badge-sections">Sections ${htmlEncode(courseSection)}</span>
                                    <span class="badge session-meta-badge session-meta-badge-leader">SI Leader: ${htmlEncode(courseLeader)}</span>
                                </div>
                                <div class="student-session-item session-card-slot mb-2">
                                    <span class="student-session-day">${htmlEncode(session.day || "")}</span>
                                    <span class="student-session-time">${htmlEncode(time || "")}</span>
                                    <span class="student-session-location">${htmlEncode(session.location || "")}</span>
                                </div>
                                <div class="session-card-actions-row">
                                    <div class="professor-request-actions">
                                        <a class="request-action-link request-action-link-compact" href="${editUrl}">Edit</a>
                                        <a class="request-action-link request-action-link-compact" href="${courseUrl}">Course</a>
                                        <button type="button"
                                                class="request-action-link request-action-delete request-action-link-compact"
                                                data-session-delete-trigger="true"
                                                data-session-id="${session.sessionId}"
                                                data-session-ids="${session.sessionId}"
                                                data-session-display="${htmlEncode(sessionLabel)}"
                                                data-bs-toggle="modal"
                                                data-bs-target="#deleteSessionModal">Delete</button>
                                    </div>
                                </div>
                            </div>
                        </article>
                    </div>`;
                }).join("");
                wireDynamicDeleteButtons();
            })
            .catch(() => {
                // Keep server-rendered list on API failure.
            });
    };


    const initStudentScheduleApiSummary = () => {
        const form = document.querySelector("[data-student-schedule-form='true']");
        if (!form) {
            return;
        }

        const apiUrl = form.getAttribute("data-api-url");
        const courseCountEl = document.querySelector("[data-student-course-count='true']");
        const sessionCountEl = document.querySelector("[data-student-session-count='true']");
        if (!apiUrl || !courseCountEl || !sessionCountEl) {
            return;
        }

        const search = form.querySelector("[name='search']")?.value || "";
        const time = form.querySelector("[name='time']")?.value || "";
        const day = form.querySelector("[name='day']")?.value || "";
        const professor = form.querySelector("[name='professor']")?.value || "";

        const query = new URLSearchParams();
        if (search) query.set("search", search);
        if (time) query.set("time", time);
        if (day) query.set("day", day);
        if (professor) query.set("professor", professor);
        query.set("page", "1");
        query.set("pageSize", "500");

        fetch(`${apiUrl}?${query.toString()}`)
            .then((response) => (response.ok ? response.json() : null))
            .then((data) => {
                const items = data?.items || [];
                if (!Array.isArray(items)) {
                    return;
                }

                const uniqueCourses = new Set(items.map((item) => `${item.courseName}|${item.courseTitle}|${item.professorName}|${item.siLeaderName}`));
                const uniqueSessions = new Set(items.map((item) => `${item.courseName}|${item.day}|${item.startTime}|${item.endTime}|${item.location}`));
                courseCountEl.textContent = String(uniqueCourses.size);
                sessionCountEl.textContent = String(uniqueSessions.size);
            })
            .catch(() => {
                // Keep server-rendered counts on API failure.
            });
    };

    document.addEventListener("DOMContentLoaded", () => {
        initThemeToggle();
        initTimePickerDropdowns();
        initCourseFormEnhancer();
        initSessionFormEnhancer();
        initProfessorRequestFormEnhancer();
        initProfessorCreateApiSubmit();
        initDeleteModalBinding({
            triggerSelector: "[data-request-delete-trigger='true']",
            idInputId: "deleteRequestIdInput",
            displayTargetId: "deleteRequestCourseName",
            idAttribute: "data-request-id",
            displayAttribute: "data-course-display",
            defaultDisplayText: "Manual Course Entry"
        });
        initDeleteModalBinding({
            triggerSelector: "[data-course-delete-trigger='true']",
            idInputId: "deleteCourseIdInput",
            displayTargetId: "deleteCourseName",
            idAttribute: "data-course-id",
            displayAttribute: "data-course-display",
            defaultDisplayText: "Unknown Course"
        });
        initDeleteModalBinding({
            triggerSelector: "[data-session-delete-trigger='true']",
            idInputId: "deleteSessionIdInput",
            idsInputId: "deleteSessionIdsInput",
            displayTargetId: "deleteSessionName",
            idAttribute: "data-session-id",
            idsAttribute: "data-session-ids",
            displayAttribute: "data-session-display",
            defaultDisplayText: "Session"
        });
        initProfessorTrackApiRefresh();
        initAdminRequestsApiList();
        initAdminRequestApiUpdate();
        initCourseDetailsApiSessions();
        initStudentScheduleApiSummary();
    });
})();

