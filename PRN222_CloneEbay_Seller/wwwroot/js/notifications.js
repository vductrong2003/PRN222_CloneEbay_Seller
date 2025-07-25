// SignalR connection for real-time notifications
let connection = null;

// Initialize SignalR connection
function initializeSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .build();

    connection.start().then(function () {
        console.log("SignalR Connected");
        
        // Join appropriate group based on user role
        const userId = getUserId();
        const userRole = getUserRole();
        
        if (userId && userRole) {
            if (userRole === "User") {
                connection.invoke("JoinUserGroup", userId);
            } else if (userRole === "Seller") {
                connection.invoke("JoinSellerGroup", userId);
            }
        }
    }).catch(function (err) {
        console.error("SignalR Connection Error: " + err.toString());
    });

    // Handle incoming notifications
    connection.on("ReceiveNotification", function (notification) {
        showNotification(notification.message, notification.type);
        
        // Reload page if specified (for sellers on multiple devices)
        if (notification.shouldReload && getUserRole() === "Seller") {
            setTimeout(() => {
                location.reload();
            }, 2000); // Give time to show notification first
        }
    });
}

// Get user ID from session or hidden field
function getUserId() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.getAttribute('data-user-id') ||
           sessionStorage.getItem('userId') ||
           localStorage.getItem('userId');
}

// Get user role from session or hidden field
function getUserRole() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.getAttribute('data-user-role') ||
           sessionStorage.getItem('userRole') ||
           localStorage.getItem('userRole');
}

// Show notification to user
function showNotification(message, type = 'info') {
    // Create notification container if it doesn't exist
    let container = document.getElementById('notification-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'notification-container';
        container.className = 'notification-container';
        document.body.appendChild(container);
    }

    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <div class="notification-content">
            <span class="notification-message">${message}</span>
            <button class="notification-close" onclick="closeNotification(this)">&times;</button>
        </div>
    `;

    container.appendChild(notification);

    // Auto remove after 5 seconds
    setTimeout(() => {
        closeNotification(notification.querySelector('.notification-close'));
    }, 5000);

    // Animate in
    setTimeout(() => {
        notification.classList.add('show');
    }, 100);
}

// Close notification
function closeNotification(closeBtn) {
    const notification = closeBtn.closest('.notification');
    notification.classList.remove('show');
    setTimeout(() => {
        notification.remove();
    }, 300);
}

// AJAX helper functions
function makeAjaxRequest(url, method = 'GET', data = null, successCallback = null, errorCallback = null) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    const options = {
        method: method,
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        }
    };

    if (data && (method === 'POST' || method === 'PUT')) {
        options.body = JSON.stringify(data);
    }

    fetch(url, options)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                showNotification(data.message || 'Operation completed successfully', 'success');
                if (successCallback) successCallback(data);
            } else {
                showNotification(data.message || 'Operation failed', 'error');
                if (errorCallback) errorCallback(data);
            }
        })
        .catch(error => {
            console.error('Ajax Error:', error);
            showNotification('An error occurred while processing your request', 'error');
            if (errorCallback) errorCallback(error);
        });
}

// Update order status via AJAX
function updateOrderStatus(orderId, status) {
    const url = `/Orders/UpdateStatus`;
    const data = { orderId: orderId, status: status };
    
    makeAjaxRequest(url, 'POST', data, (response) => {
        // Reload page after successful status update
        setTimeout(() => {
            window.location.reload();
        }, 1500);
    });
}

// Delete listing via AJAX
function deleteListing(listingId) {
    if (!confirm('Are you sure you want to delete this listing?')) {
        return;
    }
    
    const url = `/Listing/Delete/${listingId}`;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    const formData = new FormData();
    formData.append('__RequestVerificationToken', token);
    
    fetch(url, {
        method: 'POST',
        body: formData
    })
    .then(response => {
        if (response.redirected) {
            window.location.href = response.url;
        } else {
            return response.text();
        }
    })
    .then(html => {
        if (html) {
            document.body.innerHTML = html;
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('An error occurred while deleting the listing', 'error');
    });
}

// Initialize everything when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Initialize SignalR if user is logged in
    if (getUserId()) {
        initializeSignalR();
    }
    
    // Add event listeners for order status updates
    document.addEventListener('click', function(e) {
        if (e.target.classList.contains('update-status-btn')) {
            e.preventDefault();
            const orderId = e.target.getAttribute('data-order-id');
            const status = e.target.getAttribute('data-status');
            updateOrderStatus(orderId, status);
        }
        
        if (e.target.classList.contains('delete-listing-btn')) {
            e.preventDefault();
            const listingId = e.target.getAttribute('data-listing-id');
            deleteListing(listingId);
        }
    });
});

// Handle page unload
window.addEventListener('beforeunload', function() {
    if (connection) {
        connection.stop();
    }
});
