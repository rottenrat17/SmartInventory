// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Smart Inventory Management - Common JavaScript

// Show loading spinner
function showSpinner(targetElement) {
    const spinner = `
        <div class="text-center my-3">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="mt-2">Loading...</p>
        </div>
    `;
    
    if (typeof targetElement === 'string') {
        $(targetElement).html(spinner);
    } else {
        targetElement.html(spinner);
    }
}

// Handle AJAX errors
function handleAjaxError(error, targetElement) {
    console.error('AJAX Error:', error);
    
    const errorMessage = `
        <div class="alert alert-danger">
            <i class="bi bi-exclamation-triangle-fill"></i> 
            An error occurred while connecting to the server. Please try again.
        </div>
    `;
    
    if (typeof targetElement === 'string') {
        $(targetElement).html(errorMessage);
    } else if (targetElement) {
        targetElement.html(errorMessage);
    }
}

// Format currency
function formatCurrency(amount) {
    return new Intl.NumberFormat('en-US', { 
        style: 'currency', 
        currency: 'USD' 
    }).format(amount);
}

// Debounce function to limit how often a function can be called
function debounce(func, wait) {
    let timeout;
    return function(...args) {
        const context = this;
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(context, args), wait);
    };
}

// Add CSRF token to AJAX requests
$(document).ready(function() {
    // Get the CSRF token
    const csrfToken = $('input[name="__RequestVerificationToken"]').val();
    
    // Add the CSRF token to all AJAX requests
    $.ajaxSetup({
        headers: {
            'RequestVerificationToken': csrfToken
        }
    });
});
