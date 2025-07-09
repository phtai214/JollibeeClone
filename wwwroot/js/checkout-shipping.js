// Checkout Shipping JavaScript
document.addEventListener('DOMContentLoaded', function() {
    initializeCheckoutShipping();
});

function initializeCheckoutShipping() {
    handleDeliveryMethodChange();
    handleAddressSelection();
    handleStoreSelection();
    generatePickupDates();
    handleFormValidation();
    setupScrollIndicator();
    console.log('Checkout shipping initialized');
}

// Handle delivery method radio button changes
function handleDeliveryMethodChange() {
    const deliveryRadios = document.querySelectorAll('.delivery-radio-new');
    const storePickupSection = document.querySelector('.store-pickup-section');
    
    deliveryRadios.forEach(radio => {
        radio.addEventListener('change', function() {
            if (this.checked) {
                const methodName = this.nextElementSibling.textContent.toLowerCase();
                
                // Show store pickup section if pickup method is selected
                if (methodName.includes('lấy tại') || methodName.includes('pickup') || methodName.includes('cửa hàng')) {
                    storePickupSection.style.display = 'block';
                    makeStoreFieldsRequired(true);
                } else {
                    storePickupSection.style.display = 'none';
                    makeStoreFieldsRequired(false);
                }
                
                console.log('Delivery method changed:', methodName);
            }
        });
    });
}

// Make store-related fields required or optional
function makeStoreFieldsRequired(required) {
    const storeSelect = document.getElementById('storeSelect');
    const pickupDateSelect = document.getElementById('pickupDateSelect');
    const pickupTimeSelect = document.querySelector('select[name="PickupTimeSlot"]');
    
    if (storeSelect) {
        storeSelect.required = required;
    }
    if (pickupDateSelect) {
        pickupDateSelect.required = required;
    }
    if (pickupTimeSelect) {
        pickupTimeSelect.required = required;
    }
}

// Handle address selection for logged-in users
function handleAddressSelection() {
    const addressSelect = document.getElementById('addressSelect');
    if (!addressSelect) return;
    
    addressSelect.addEventListener('change', function() {
        if (this.value) {
            // Get the selected option text and parse address info
            const selectedText = this.options[this.selectedIndex].text;
            console.log('Address selected:', selectedText);
            
            // You can parse and populate customer info fields here if needed
            // For now, the address ID will be submitted with the form
        }
    });
}

// Handle store selection and address updates
function handleStoreSelection() {
    const storeSelect = document.getElementById('storeSelect');
    if (!storeSelect) return;
    
    storeSelect.addEventListener('change', function() {
        if (this.value) {
            const selectedOption = this.options[this.selectedIndex];
            const storeAddress = selectedOption.getAttribute('data-address');
            
            console.log('Store selected:', selectedOption.text);
            console.log('Store address:', storeAddress);
            
            // You can display the store address somewhere if needed
        }
    });
}

// Generate pickup dates (today + next 7 days)
function generatePickupDates() {
    const pickupDateSelect = document.getElementById('pickupDateSelect');
    if (!pickupDateSelect) return;
    
    const today = new Date();
    const daysOfWeek = ['Chủ Nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy'];
    
    // Clear existing options except the first one
    while (pickupDateSelect.children.length > 1) {
        pickupDateSelect.removeChild(pickupDateSelect.lastChild);
    }
    
    // Add today and next 7 days
    for (let i = 0; i < 8; i++) {
        const date = new Date(today);
        date.setDate(today.getDate() + i);
        
        const dayName = daysOfWeek[date.getDay()];
        const dateString = date.toLocaleDateString('vi-VN');
        const isoString = date.toISOString().split('T')[0];
        
        const option = document.createElement('option');
        option.value = isoString;
        
        if (i === 0) {
            option.text = `Hôm nay, ${dateString}`;
        } else if (i === 1) {
            option.text = `Ngày mai, ${dateString}`;
        } else {
            option.text = `${dayName}, ${dateString}`;
        }
        
        pickupDateSelect.appendChild(option);
    }
}

// Handle form validation and submission
function handleFormValidation() {
    const form = document.getElementById('checkoutForm');
    if (!form) return;
    
    form.addEventListener('submit', function(e) {
        // Custom validation logic
        const deliveryMethodSelected = document.querySelector('.delivery-radio-new:checked');
        
        if (!deliveryMethodSelected) {
            e.preventDefault();
            showError('Vui lòng chọn phương thức vận chuyển');
            return false;
        }
        
        // Check if store pickup is selected and required fields are filled
        const storePickupSection = document.querySelector('.store-pickup-section');
        if (storePickupSection && storePickupSection.style.display !== 'none') {
            const storeSelect = document.getElementById('storeSelect');
            const pickupDateSelect = document.getElementById('pickupDateSelect');
            const pickupTimeSelect = document.querySelector('select[name="PickupTimeSlot"]');
            
            if (!storeSelect.value) {
                e.preventDefault();
                showError('Vui lòng chọn cửa hàng');
                return false;
            }
            
            if (!pickupDateSelect.value) {
                e.preventDefault();
                showError('Vui lòng chọn ngày nhận hàng');
                return false;
            }
            
            if (!pickupTimeSelect.value) {
                e.preventDefault();
                showError('Vui lòng chọn thời gian nhận hàng');
                return false;
            }
        }
        
        // Check delivery address for home delivery
        const isHomeDelivery = deliveryMethodSelected && 
            deliveryMethodSelected.nextElementSibling.textContent.toLowerCase().includes('giao hàng');
        
        if (isHomeDelivery) {
            const addressSelect = document.getElementById('addressSelect');
            const manualAddress = document.querySelector('textarea[name="DeliveryAddress"]');
            
            if (addressSelect && !addressSelect.value && (!manualAddress || !manualAddress.value.trim())) {
                e.preventDefault();
                showError('Vui lòng chọn địa chỉ giao hàng hoặc nhập địa chỉ thủ công');
                return false;
            }
        }
        
        console.log('Form validation passed');
        return true;
    });
}

// Show error message
function showError(message) {
    // Create or update error alert
    let errorAlert = document.querySelector('.checkout-error-alert');
    
    if (!errorAlert) {
        errorAlert = document.createElement('div');
        errorAlert.className = 'alert alert-danger checkout-error-alert';
        errorAlert.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            max-width: 400px;
            padding: 15px;
            background: #f8d7da;
            border: 1px solid #f5c6cb;
            border-radius: 8px;
            color: #721c24;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        `;
        document.body.appendChild(errorAlert);
    }
    
    errorAlert.innerHTML = `
        <strong><i class="fas fa-exclamation-triangle"></i> Lỗi:</strong> ${message}
        <button type="button" class="btn-close float-end" aria-label="Close" 
                onclick="this.parentElement.remove()" style="background: none; border: none; font-size: 20px; cursor: pointer;">&times;</button>
    `;
    
    // Auto remove after 5 seconds
    setTimeout(() => {
        if (errorAlert && errorAlert.parentNode) {
            errorAlert.remove();
        }
    }, 5000);
    
    // Scroll to top to make sure user sees the error
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

// Setup scroll indicator for order items list
function setupScrollIndicator() {
    const orderItemsList = document.querySelector('.order-items-list');
    if (!orderItemsList) return;
    
    function checkScrollability() {
        if (orderItemsList.scrollHeight > orderItemsList.clientHeight) {
            orderItemsList.classList.add('has-scroll');
        } else {
            orderItemsList.classList.remove('has-scroll');
        }
    }
    
    // Check on load
    checkScrollability();
    
    // Check on resize
    window.addEventListener('resize', checkScrollability);
    
    // Optional: Hide fade effect when scrolled to bottom
    orderItemsList.addEventListener('scroll', function() {
        const isScrolledToBottom = Math.abs(this.scrollHeight - this.clientHeight - this.scrollTop) < 5;
        if (isScrolledToBottom) {
            this.classList.remove('has-scroll');
        } else if (this.scrollHeight > this.clientHeight) {
            this.classList.add('has-scroll');
        }
    });
}

// Utility function to format currency
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN').format(amount) + ' đ';
}

// Handle edit address button click
document.addEventListener('click', function(e) {
    if (e.target.closest('.edit-address-btn')) {
        const customerInputs = document.querySelectorAll('.customer-input');
        const manualAddress = document.querySelector('textarea[name="DeliveryAddress"]');
        
        // Enable editing of customer info
        customerInputs.forEach(input => {
            input.disabled = false;
            input.style.backgroundColor = 'white';
        });
        
        if (manualAddress) {
            manualAddress.disabled = false;
            manualAddress.style.backgroundColor = 'white';
        }
        
        // Change button text
        e.target.closest('.edit-address-btn').innerHTML = '<i class="fas fa-save"></i> Lưu';
        e.target.closest('.edit-address-btn').classList.add('save-address-btn');
        e.target.closest('.edit-address-btn').classList.remove('edit-address-btn');
        
        console.log('Address editing enabled');
    }
    
    if (e.target.closest('.save-address-btn')) {
        const customerInputs = document.querySelectorAll('.customer-input');
        const manualAddress = document.querySelector('textarea[name="DeliveryAddress"]');
        
        // Disable editing
        customerInputs.forEach(input => {
            input.disabled = true;
            input.style.backgroundColor = '#f8f9fa';
        });
        
        if (manualAddress) {
            manualAddress.disabled = true;
            manualAddress.style.backgroundColor = '#f8f9fa';
        }
        
        // Change button text back
        e.target.closest('.save-address-btn').innerHTML = '<i class="fas fa-edit"></i> Chỉnh sửa';
        e.target.closest('.save-address-btn').classList.add('edit-address-btn');
        e.target.closest('.save-address-btn').classList.remove('save-address-btn');
        
        console.log('Address editing saved');
    }
    
    // Handle edit order button click
    if (e.target.closest('.edit-order-btn')) {
        // Redirect back to menu page for adding more items
        if (confirm('Bạn có muốn quay lại thực đơn để thêm món?')) {
            window.location.href = '/Menu/MonNgonPhaiThu';
        }
    }
    
    // Handle edit delivery button click
    if (e.target.closest('.edit-delivery-btn')) {
        // Redirect to delivery addresses page
        if (confirm('Bạn có muốn chỉnh sửa thông tin giao hàng?')) {
            window.location.href = '/Account/DeliveryAddresses';
        }
    }
});

// Initialize address display as readonly if user is logged in and has addresses
document.addEventListener('DOMContentLoaded', function() {
    const addressSelect = document.getElementById('addressSelect');
    if (addressSelect && addressSelect.options.length > 1) {
        const customerInputs = document.querySelectorAll('.customer-input');
        customerInputs.forEach(input => {
            input.disabled = true;
            input.style.backgroundColor = '#f8f9fa';
        });
    }
}); 