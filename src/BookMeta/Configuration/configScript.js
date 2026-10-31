/* global ApiClient, Dashboard */
define([], function () {
    'use strict';

    var PLUGIN_ID = '097ed1ba-f0ae-496f-8f56-e54599fe54b1';

    return function (view) {

        function setStatus(text) {
            var el = view.querySelector('#SaveStatus');
            if (el) { el.textContent = text || ''; }
        }

        var savedTimer = null;
        // Enregistrer n'est possible qu'une fois la configuration lue : sinon la case (décochée par défaut dans le HTML) écraserait la valeur du serveur.
        var loaded = false;

        function setSaveEnabled(enabled) {
            var btn = view.querySelector('#BtnSave');
            if (btn) { btn.disabled = !enabled; }
        }

        // Retour visible en plus du toast Emby standard (qui peut passer inaperçu).
        function showSaved() {
            setStatus('Configuration enregistrée.');
            if (savedTimer) { clearTimeout(savedTimer); }
            savedTimer = setTimeout(function () { setStatus(''); }, 5000);
        }

        function load() {
            ApiClient.getPluginConfiguration(PLUGIN_ID).then(function (cfg) {
                // Absent (ancienne configuration) : le défaut du plugin est « activé ».
                view.querySelector('#EnableEpubCoverExtraction').checked = cfg.EnableEpubCoverExtraction !== false;
                loaded = true;
                setSaveEnabled(true);
                setStatus('');
            }, function () {
                loaded = false;
                setSaveEnabled(false);
                setStatus('Impossible de lire la configuration : rechargez la page.');
            });
        }

        function save() {
            if (!loaded) { return; }
            setStatus('');
            // On repart de la configuration actuelle du serveur (relue maintenant) et on ne modifie que la case :
            // les autres champs (dont la liste des bibliothèques déjà activées, gérée par le plugin) sont renvoyés tels quels.
            ApiClient.getPluginConfiguration(PLUGIN_ID).then(function (cfg) {
                cfg.EnableEpubCoverExtraction = view.querySelector('#EnableEpubCoverExtraction').checked;
                return ApiClient.updatePluginConfiguration(PLUGIN_ID, cfg).then(function () {
                    Dashboard.processPluginConfigurationUpdateResult();
                    showSaved();
                }, function () {
                    setStatus('Échec de l\'enregistrement.');
                });
            }, function () {
                setStatus('Impossible de lire la configuration.');
            });
        }

        setSaveEnabled(false);
        view.querySelector('#BtnSave').addEventListener('click', save);
        view.addEventListener('viewshow', function () {
            loaded = false;
            setSaveEnabled(false);
            load();
        });
    };
});
