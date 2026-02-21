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
                const response = await fetch(`${lookupBaseUrl}?crn=${encodeURIComponent(crn)}`);
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
                requestedCourseSection: form.querySelector("[name='RequestedCourseSection']")?.value || "",
                requestedCourseProfessor: form.querySelector("[name='RequestedCourseProfessor']")?.value || "",
                professorName: form.querySelector("[name='ProfessorName']")?.value || "",
                professorEmail: form.querySelector("[name='ProfessorEmail']")?.value || "",
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
                    showApiError(form, "Unable to submit request right now. Please try again.");
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
        const lastUpdatedText = container.querySelector("[data-track-last-updated='true']");
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

                if (lastUpdatedText) {
                    lastUpdatedText.textContent = formatLocalDate(data.lastUpdatedAtUtc);
                }

                if (adminNotesText) {
                    adminNotesText.textContent = (data.adminNotes || "").trim() || "No updates yet.";
                }

                if (submittedStage) {
                    submittedStage.textContent = "Complete";
                }
                if (reviewStage) {
                    reviewStage.textContent = progress >= 60 ? "Complete" : "Pending";
                }
                if (finalStage) {
                    finalStage.textContent = progress >= 100 ? "Complete" : "Pending";
                }
            })
            .catch(() => {
                // Keep server-rendered values on API failure.
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
        if (!apiUrl || !reviewTemplate) {
            return;
        }

        const renderRows = (items) => {
            if (!Array.isArray(items) || items.length === 0) {
                tbody.innerHTML = "<tr><td colspan=\"5\" class=\"text-muted\">No SI requests found.</td></tr>";
                return;
            }

            tbody.innerHTML = items.map((item) => {
                const reviewUrl = reviewTemplate.replace("__id__", String(item.requestId));
                return `<tr>
                    <td>${htmlEncode(item.courseDisplay || "Manual Course Entry")}</td>
                    <td>${htmlEncode(item.professorName || "")}</td>
                    <td>${htmlEncode(formatLocalDate(item.submittedAtUtc))}</td>
                    <td><span class="status-pill ${statusClass(item.status)}">${htmlEncode(item.status || "")}</span></td>
                    <td><a href="${reviewUrl}">Review</a></td>
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
        const tbody = container?.querySelector("[data-course-sessions-tbody='true']");
        if (!container || !tbody) {
            return;
        }

        const apiUrl = container.getAttribute("data-api-url");
        const editTemplate = container.getAttribute("data-edit-url-template");
        const deleteTemplate = container.getAttribute("data-delete-url-template");
        if (!apiUrl || !editTemplate || !deleteTemplate) {
            return;
        }

        fetch(apiUrl)
            .then((response) => (response.ok ? response.json() : null))
            .then((data) => {
                const sessions = data?.sessions || [];
                if (!Array.isArray(sessions) || sessions.length === 0) {
                    tbody.innerHTML = "<tr><td colspan=\"4\" class=\"text-muted\">No sessions added yet.</td></tr>";
                    return;
                }

                tbody.innerHTML = sessions.map((session) => {
                    const editUrl = editTemplate.replace("__id__", String(session.sessionId));
                    const deleteUrl = deleteTemplate.replace("__id__", String(session.sessionId));
                    const time = session.endTime ? `${session.startTime}-${session.endTime}` : session.startTime;
                    return `<tr>
                        <td>${htmlEncode(session.day || "")}</td>
                        <td>${htmlEncode(time || "")}</td>
                        <td>${htmlEncode(session.location || "")}</td>
                        <td class="text-nowrap"><a href="${editUrl}">Edit</a> | <a href="${deleteUrl}">Delete</a></td>
                    </tr>`;
                }).join("");
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
        const day = form.querySelector("[name='day']")?.value || "";
        const professor = form.querySelector("[name='professor']")?.value || "";

        const query = new URLSearchParams();
        if (search) query.set("search", search);
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

                const uniqueCourses = new Set(items.map((item) => `${item.courseId}|${item.courseSection}`));
                courseCountEl.textContent = String(uniqueCourses.size);
                sessionCountEl.textContent = String(items.length);
            })
            .catch(() => {
                // Keep server-rendered counts on API failure.
            });
    };

    document.addEventListener("DOMContentLoaded", () => {
        initThemeToggle();
        initCourseFormEnhancer();
        initSessionFormEnhancer();
        initProfessorCreateApiSubmit();
        initProfessorTrackApiRefresh();
        initAdminRequestsApiList();
        initAdminRequestApiUpdate();
        initCourseDetailsApiSessions();
        initStudentScheduleApiSummary();
    });
})();

