/* global ApiClient, Dashboard */
define([], function () {
    'use strict';

    var PLUGIN_ID = '097ed1ba-f0ae-496f-8f56-e54599fe54b1';

    return function (view) {

        function setStatus(text) {
            var el = view.querySelector('#SaveStatus');
            if (el) { el.textContent = text || ''; }
        }

        function load() {
            ApiClient.getPluginConfiguration(PLUGIN_ID).then(function (cfg) {
                // Absent (ancienne configuration) : le défaut du plugin est « activé ».
                view.querySelector('#EnableEpubCoverExtraction').checked = cfg.EnableEpubCoverExtraction !== false;
            }, function () {
                setStatus('Impossible de lire la configuration.');
            });
        }

        function save() {
            setStatus('');
            // On repart de la configuration actuelle du serveur (relue maintenant) et on ne modifie que la case :
            // les autres champs (dont la liste des bibliothèques déjà activées, gérée par le plugin) sont renvoyés tels quels.
            ApiClient.getPluginConfiguration(PLUGIN_ID).then(function (cfg) {
                cfg.EnableEpubCoverExtraction = view.querySelector('#EnableEpubCoverExtraction').checked;
                return ApiClient.updatePluginConfiguration(PLUGIN_ID, cfg).then(function () {
                    Dashboard.processPluginConfigurationUpdateResult();
                }, function () {
                    setStatus('Échec de l\'enregistrement.');
                });
            }, function () {
                setStatus('Impossible de lire la configuration.');
            });
        }

        view.querySelector('#BtnSave').addEventListener('click', save);
        view.addEventListener('viewshow', load);
    };
});
