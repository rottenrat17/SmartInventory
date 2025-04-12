/**
 * AJAX Handler
 * Provides common functionality for AJAX operations across the application
 */

const AjaxHandler = {
    /**
     * Shows the global AJAX loader
     * @param {string} message - Custom loading message (optional)
     */
    showLoader: function(message) {
        const loaderContainer = document.querySelector('.ajax-loading-container');
        if (loaderContainer) {
            if (message) {
                const messageElement = loaderContainer.querySelector('.loading-message');
                if (messageElement) {
                    messageElement.textContent = message;
                }
            }
            loaderContainer.classList.remove('d-none');
        }
    },

    /**
     * Hides the global AJAX loader
     */
    hideLoader: function() {
        const loaderContainer = document.querySelector('.ajax-loading-container');
        if (loaderContainer) {
            loaderContainer.classList.add('d-none');
        }
    },

    /**
     * Performs an AJAX request with the global loader
     * 
     * @param {Object} options - AJAX request options
     * @param {string} options.url - Request URL
     * @param {string} options.type - Request method (GET, POST, etc.)
     * @param {Object} options.data - Request data
     * @param {Function} options.success - Success callback
     * @param {Function} options.error - Error callback
     * @param {string} options.loadingMessage - Custom loading message
     */
    request: function(options) {
        this.showLoader(options.loadingMessage);
        
        $.ajax({
            url: options.url,
            type: options.type || 'GET',
            data: options.data || {},
            success: function(response) {
                if (options.success) {
                    options.success(response);
                }
            },
            error: function(xhr, status, error) {
                if (options.error) {
                    options.error(xhr, status, error);
                } else {
                    console.error('AJAX Error:', error);
                    alert('An error occurred while processing your request. Please try again.');
                }
            },
            complete: function() {
                AjaxHandler.hideLoader();
            }
        });
    },
    
    /**
     * Initializes AJAX-related event handlers
     */
    init: function() {
        // Add global AJAX start and stop events
        $(document).ajaxStart(function() {
            AjaxHandler.showLoader();
        });
        
        $(document).ajaxStop(function() {
            AjaxHandler.hideLoader();
        });
        
        // Cancel the global handlers for specific cases where we don't want the global loader
        $(document).on('ajax:beforeSend', '[data-no-loader]', function(event) {
            event.stopPropagation();
        });
    }
};

// Initialize AJAX handler when document is ready
$(document).ready(function() {
    AjaxHandler.init();
}); 