// Browser implementations of the platform services used by Algorist.App.Web (see Platform/*.cs).
window.algoristPlatform = {
    storage: {
        get: function (key) {
            try { return window.localStorage.getItem(key); } catch { return null; }
        },
        set: function (key, value) {
            window.localStorage.setItem(key, value);
        },
        remove: function (key) {
            window.localStorage.removeItem(key);
        }
    },
    getBrowserLanguage: function () {
        var languages = navigator.languages;
        if (languages && languages.length > 0 && languages[0]) {
            return languages[0];
        }
        return navigator.language || null;
    },
    setDocumentLanguage: function (language) {
        document.documentElement.lang = language;
    },
    // Google Identity Services: the only external script, loaded on demand by the Sign In screen.
    google: {
        scriptUrl: "https://accounts.google.com/gsi/client",
        loading: null,
        initializedFor: null,
        callback: null,
        load: function () {
            if (window.google && window.google.accounts && window.google.accounts.id) {
                return Promise.resolve();
            }
            var self = this;
            if (!self.loading) {
                self.loading = new Promise(function (resolve, reject) {
                    var script = document.createElement("script");
                    script.src = self.scriptUrl;
                    script.async = true;
                    script.onload = function () { resolve(); };
                    script.onerror = function () {
                        self.loading = null;
                        script.remove();
                        reject(new Error("Google sign-in script failed to load."));
                    };
                    document.head.appendChild(script);
                });
            }
            return self.loading;
        },
        // Renders Google's button in the container; returns false when Google sign-in cannot be used.
        renderButton: async function (container, clientId, locale, dotNetCallback) {
            var self = this;
            try {
                await self.load();
            } catch {
                return false;
            }
            self.callback = dotNetCallback;
            if (self.initializedFor !== clientId) {
                window.google.accounts.id.initialize({
                    client_id: clientId,
                    ux_mode: "popup",
                    auto_select: false,
                    cancel_on_tap_outside: true,
                    callback: function (response) {
                        if (response && response.credential && self.callback) {
                            self.callback.invokeMethodAsync("OnCredential", response.credential);
                        }
                    }
                });
                self.initializedFor = clientId;
            }
            var width = Math.max(200, Math.min(400, Math.floor(container.clientWidth || 320)));
            window.google.accounts.id.renderButton(container, {
                type: "standard",
                theme: "outline",
                size: "large",
                text: "signin_with",
                shape: "pill",
                logo_alignment: "left",
                width: width,
                locale: locale
            });
            return true;
        }
    }
};
