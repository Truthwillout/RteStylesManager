angular.module("umbraco").controller("RteStyleManager.DashboardController",
    function ($scope, rteStyleService, notificationsService) {
        
        var vm = this;
        
        // State
        vm.config = null;
        vm.loading = false;
        vm.saving = false;
        vm.errorMessage = null;
        vm.successMessage = null;
        vm.showCssModal = false;
        vm.generatedCss = '';

        // Initialize
        vm.init = function() {
            vm.loadConfig();
        };

        // Load configuration
        vm.loadConfig = function() {
            vm.loading = true;
            vm.errorMessage = null;
            vm.successMessage = null;

            rteStyleService.getConfig()
                .then(function(response) {
                    console.log('API Response:', response.data); // Debug log
                    
                    vm.config = response.data;
                    
                    // Ensure color property exists for all items
                    if (vm.config && vm.config.length > 0) {
                        angular.forEach(vm.config, function(category) {
                            if (category.Items) {
                                angular.forEach(category.Items, function(item) {
                                    if (!item.Color) {
                                        item.Color = vm.extractColorFromTitle(item.Title);
                                    }
                                });
                            }
                        });
                    }
                    
                    vm.loading = false;
                    console.log('Loaded config:', vm.config); // Debug log
                })
                .catch(function(error) {
                    vm.loading = false;
                    vm.errorMessage = "Failed to load configuration: " + (error.data?.message || error.statusText);
                    notificationsService.error("Error", vm.errorMessage);
                    console.error('Load error:', error); // Debug log
                });
        };

        // Save configuration
        vm.saveConfig = function() {
            vm.saving = true;
            vm.errorMessage = null;
            vm.successMessage = null;

            // Update titles to include color info and prepare data
            var configToSave = angular.copy(vm.config);
            angular.forEach(configToSave, function(category) {
                angular.forEach(category.items, function(item) {
                    // Update CSS class based on color if needed
                    if (item.color && !item.classes) {
                        item.classes = vm.generateClassName(item.title, item.color);
                    }
                });
            });

            rteStyleService.saveConfig(configToSave)
                .then(function(response) {
                    vm.saving = false;
                    vm.successMessage = "Configuration saved successfully! CSS files have been updated. Please restart your Umbraco application for changes to appear in the RTE dropdown.";
                    notificationsService.success("Success", "Configuration saved! Restart Umbraco to see changes in RTE.");
                    
                    // Clear success message after 10 seconds
                    setTimeout(function() {
                        $scope.$apply(function() {
                            vm.successMessage = null;
                        });
                    }, 10000);
                })
                .catch(function(error) {
                    vm.saving = false;
                    vm.errorMessage = "Failed to save configuration: " + (error.data?.message || error.statusText);
                    notificationsService.error("Error", vm.errorMessage);
                });
        };

        // Add new category
        vm.addCategory = function() {
            var newCategory = {
                Title: "New Category",
                Items: []
            };
            vm.config.push(newCategory);
        };

        // Remove category
        vm.removeCategory = function(index) {
            if (confirm("Are you sure you want to remove this category?")) {
                vm.config.splice(index, 1);
            }
        };

        // Add new item to category
        vm.addItem = function(category) {
            var newItem = {
                Title: "New Style",
                Block: "p",
                Classes: "",
                Color: "#000000"
            };
            category.Items.push(newItem);
        };

        // Remove item from category
        vm.removeItem = function(category, index) {
            if (confirm("Are you sure you want to remove this style?")) {
                category.Items.splice(index, 1);
            }
        };

        // Move item up in the list
        vm.moveItemUp = function(category, index) {
            if (index > 0) {
                var temp = category.Items[index];
                category.Items[index] = category.Items[index - 1];
                category.Items[index - 1] = temp;
            }
        };

        // Move item down in the list
        vm.moveItemDown = function(category, index) {
            if (index < category.Items.length - 1) {
                var temp = category.Items[index];
                category.Items[index] = category.Items[index + 1];
                category.Items[index + 1] = temp;
            }
        };

        // View generated CSS
        vm.viewGeneratedCss = function() {
            rteStyleService.getCss()
                .then(function(response) {
                    vm.generatedCss = response.data.css;
                    vm.showCssModal = true;
                })
                .catch(function(error) {
                    notificationsService.error("Error", "Failed to load CSS: " + (error.data?.message || error.statusText));
                });
        };

        // Close CSS modal
        vm.closeCssModal = function() {
            vm.showCssModal = false;
        };

        // Helper: Extract color from title
        vm.extractColorFromTitle = function(title) {
            if (!title) return "#000000";
            
            var lowerTitle = title.toLowerCase();
            
            if (lowerTitle.includes("red")) return "#ff0000";
            if (lowerTitle.includes("blue")) return "#0000ff";
            if (lowerTitle.includes("green")) return "#008000";
            if (lowerTitle.includes("yellow")) return "#ffff00";
            if (lowerTitle.includes("orange")) return "#ffa500";
            if (lowerTitle.includes("purple")) return "#800080";
            if (lowerTitle.includes("black")) return "#000000";
            if (lowerTitle.includes("white")) return "#ffffff";
            if (lowerTitle.includes("gray") || lowerTitle.includes("grey")) return "#808080";
            
            return "#000000";
        };

        // Helper: Generate CSS class name
        vm.generateClassName = function(title, color) {
            var name = title.toLowerCase()
                .replace(/[^a-z0-9\s-]/g, '')
                .replace(/\s+/g, '-')
                .replace(/-+/g, '-')
                .trim();
            
            return name || 'custom-style';
        };

        // Initialize on load
        vm.init();
    }
);
