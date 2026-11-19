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
    }
};
