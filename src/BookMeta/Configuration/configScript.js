/* global ApiClient, Dashboard */
define([], function () {
    'use strict';

    var PLUGIN_ID = '097ed1ba-f0ae-496f-8f56-e54599fe54b1';

    return function (view) {
        // Coquille : chargement/enregistrement du réglage ajoutés en LOT-3 (#8).
        view.addEventListener('viewshow', function () {
            ApiClient.getPluginConfiguration(PLUGIN_ID);
        });
    };
});
