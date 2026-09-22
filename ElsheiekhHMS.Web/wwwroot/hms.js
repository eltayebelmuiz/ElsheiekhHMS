(function () {
    "use strict";

    var themeKey = "hms-theme";

    function systemTheme() {
        return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches
            ? "dark"
            : "light";
    }

    function storedTheme() {
        try {
            var value = window.localStorage.getItem(themeKey);
            return value === "dark" || value === "light" ? value : null;
        } catch (_) {
            return null;
        }
    }

    function syncThemeControls(theme) {
        document.querySelectorAll("[data-hms-theme-toggle]").forEach(function (button) {
            var next = theme === "dark" ? "light" : "dark";
            button.setAttribute("aria-label", "Switch to " + next + " theme");
            button.setAttribute("aria-pressed", theme === "dark" ? "true" : "false");
            button.title = "Switch to " + next + " theme";
            var label = button.querySelector(".hms-theme-toggle__label");
            if (label && label.textContent !== (theme === "dark" ? "Light" : "Dark")) label.textContent = theme === "dark" ? "Light" : "Dark";
            var mark = button.querySelector(".hms-theme-toggle__mark");
            if (mark && mark.textContent !== (theme === "dark" ? "☼" : "◐")) mark.textContent = theme === "dark" ? "☼" : "◐";
        });
    }

    function applyTheme(theme, persist) {
        document.documentElement.dataset.theme = theme;
        document.documentElement.style.colorScheme = theme;
        if (persist) {
            try { window.localStorage.setItem(themeKey, theme); } catch (_) { /* preference storage is optional */ }
        }
        syncThemeControls(theme);
    }

    window.hmsTheme = {
        init: function () { applyTheme(storedTheme() || systemTheme(), false); },
        toggle: function () {
            var current = document.documentElement.dataset.theme || systemTheme();
            applyTheme(current === "dark" ? "light" : "dark", true);
        }
    };

    window.hmsUi = {
        togglePassword: function (inputId, button) {
            var input = document.getElementById(inputId);
            if (!input) return;
            var visible = input.type === "text";
            input.type = visible ? "password" : "text";
            button.setAttribute("aria-pressed", visible ? "false" : "true");
            button.textContent = visible ? "Show" : "Hide";
            button.setAttribute("aria-label", (visible ? "Show" : "Hide") + " password");
        }
    };

    window.hmsTheme.init();

    function sidebarToggle(button, open) {
        var checkbox = document.getElementById("hms-sidebar-toggle");
        if (!checkbox) return;
        checkbox.checked = open;
        button.setAttribute("aria-expanded", open ? "true" : "false");
        document.documentElement.classList.toggle("hms-nav-open", open);
        if (open) {
            var first = document.querySelector("#primary-navigation a, #primary-navigation button");
            if (first) first.focus();
        }
    }

    function closeSidebar() {
        var button = document.querySelector("[data-hms-menu-toggle]");
        if (button && document.getElementById("hms-sidebar-toggle")?.checked) {
            sidebarToggle(button, false);
            button.focus();
        }
    }

    document.addEventListener("click", function (event) {
        var menu = event.target.closest("[data-hms-menu-toggle]");
        if (menu) {
            var checkbox = document.getElementById("hms-sidebar-toggle");
            sidebarToggle(menu, !(checkbox && checkbox.checked));
            return;
        }
        if (event.target.closest(".hms-sidebar-scrim") || event.target.closest("#primary-navigation a")) {
            closeSidebar();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            var dialogClose = document.querySelector("[data-hms-dialog] [data-hms-dialog-close]");
            if (dialogClose) {
                dialogClose.click();
                return;
            }
            closeSidebar();
        }
    });

    var lastDialogFocus;
    var dialogObserver = new MutationObserver(function () {
        syncThemeControls(document.documentElement.dataset.theme || systemTheme());
        var dialog = document.querySelector("[data-hms-dialog]");
        if (dialog && !dialog.hasAttribute("data-hms-dialog-ready")) {
            lastDialogFocus = document.activeElement;
            dialog.setAttribute("data-hms-dialog-ready", "true");
            var first = dialog.querySelector("button, input, select, textarea, a");
            (first || dialog).focus();
        }
        if (!dialog && lastDialogFocus && typeof lastDialogFocus.focus === "function") {
            lastDialogFocus.focus();
            lastDialogFocus = null;
        }
    });
    function startObservers() {
        syncThemeControls(document.documentElement.dataset.theme || systemTheme());
        dialogObserver.observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", startObservers, { once: true });
    } else {
        startObservers();
    }

    if (window.matchMedia) {
        window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", function (event) {
            if (!storedTheme()) applyTheme(event.matches ? "dark" : "light", false);
        });
    }
}());
