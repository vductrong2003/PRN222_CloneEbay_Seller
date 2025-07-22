// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Order Actions JavaScript
document.addEventListener('DOMContentLoaded', function() {
    // Initialize order actions
    initializeOrderActions();
});

function initializeOrderActions() {
    // Handle quick action buttons
    const quickActionBtns = document.querySelectorAll('.action-btn');
    quickActionBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            const action = this.dataset.action;
            handleQuickAction(action);
        });
    });

    // Handle advance step button
    const advanceStepBtn = document.querySelector('.advance-step-btn');
    if (advanceStepBtn) {
        advanceStepBtn.addEventListener('click', function() {
            const nextStatus = this.dataset.nextStatus;
            if (nextStatus) {
                updateOrderStatus(nextStatus);
            }
        });
    }

    // Handle manual status update form
    const statusForm = document.querySelector('.status-update-form');
    if (statusForm) {
        const selectElement = statusForm.querySelector('select');
        const submitBtn = statusForm.querySelector('.btn');
        
        if (selectElement && submitBtn) {
            selectElement.addEventListener('change', function() {
                submitBtn.disabled = this.value === '';
            });
        }
    }
}

function handleQuickAction(action) {
    const orderId = getOrderIdFromUrl();
    
    let confirmMessage = '';
    let statusUpdate = '';
    
    switch(action) {
        case 'ship':
            confirmMessage = 'Mark this order as shipped?';
            statusUpdate = 'Shipped';
            break;
        case 'cancel':
            confirmMessage = 'Cancel this order? This action cannot be undone.';
            statusUpdate = 'Cancelled';
            break;
        case 'refund':
            confirmMessage = 'Process refund for this order?';
            statusUpdate = 'Refunded';
            break;
        case 'mark-paid':
            confirmMessage = 'Mark this order as paid?';
            statusUpdate = 'Paid';
            break;
        case 'processing':
            confirmMessage = 'Mark this order as processing?';
            statusUpdate = 'Processing';
            break;
        case 'delivered':
            confirmMessage = 'Mark this order as delivered?';
            statusUpdate = 'Delivered';
            break;
        default:
            console.error('Unknown action:', action);
            return;
    }
    
    if (confirm(confirmMessage)) {
        updateOrderStatus(statusUpdate);
    }
}

function updateOrderStatus(newStatus) {
    const orderId = getOrderIdFromUrl();
    
    if (!orderId) {
        alert('Order ID not found');
        return;
    }

    // Show loading state
    showLoadingState();
    
    // Make AJAX request to update status
    fetch(`/Orders/UpdateStatus/${orderId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify({ status: newStatus })
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        }
        throw new Error('Failed to update status');
    })
    .then(data => {
        if (data.success) {
            // Reload page to show updated status
            window.location.reload();
        } else {
            alert(data.message || 'Failed to update order status');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        alert('An error occurred while updating the order status');
    })
    .finally(() => {
        hideLoadingState();
    });
}

function getOrderIdFromUrl() {
    const pathParts = window.location.pathname.split('/');
    const detailsIndex = pathParts.indexOf('Details');
    if (detailsIndex !== -1 && detailsIndex + 1 < pathParts.length) {
        return pathParts[detailsIndex + 1];
    }
    return null;
}

function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}

function showLoadingState() {
    // Disable all action buttons
    const buttons = document.querySelectorAll('.action-btn, .advance-step-btn, .status-update-form .btn');
    buttons.forEach(btn => {
        btn.disabled = true;
        btn.style.opacity = '0.6';
    });
    
    // Show loading indicator
    const loadingIndicator = document.createElement('div');
    loadingIndicator.id = 'loading-indicator';
    loadingIndicator.innerHTML = `
        <div style="position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); 
                    background: rgba(0,0,0,0.8); color: white; padding: 20px; border-radius: 8px; z-index: 9999;">
            <div style="text-align: center;">
                <div style="margin-bottom: 10px;">Updating order status...</div>
                <div style="width: 20px; height: 20px; border: 3px solid #ffffff; border-top: 3px solid transparent; 
                           border-radius: 50%; animation: spin 1s linear infinite; margin: 0 auto;"></div>
            </div>
        </div>
        <style>
            @keyframes spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }
        </style>
    `;
    document.body.appendChild(loadingIndicator);
}

function hideLoadingState() {
    // Re-enable all action buttons
    const buttons = document.querySelectorAll('.action-btn, .advance-step-btn, .status-update-form .btn');
    buttons.forEach(btn => {
        btn.disabled = false;
        btn.style.opacity = '1';
    });
    
    // Remove loading indicator
    const loadingIndicator = document.getElementById('loading-indicator');
    if (loadingIndicator) {
        loadingIndicator.remove();
    }
}
