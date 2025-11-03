angular.module("umbraco.services").factory("rteStyleService",
    function ($http) {
        
        var apiBase = "/umbraco/backoffice/RteStyleManager/RteStyleApi/";
        
        return {
            // Get current style configuration
            getConfig: function() {
                return $http.get(apiBase + "GetStyleConfig");
            },
            
            // Save style configuration
            saveConfig: function(config) {
                return $http.post(apiBase + "SaveStyleConfig", config);
            },
            
            // Get generated CSS
            getCss: function() {
                return $http.get(apiBase + "GetCss");
            }
        };
    }
);
