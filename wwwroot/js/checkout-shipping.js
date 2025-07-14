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
    
    // FORCE check shipping display based on selected delivery method
    forceCorrectShippingDisplay();
    
    // Initialize shipping calculation on page load
    calculateShippingOnLoad();
    
    console.log('Checkout shipping initialized');
}

// FORCE correct shipping display immediately
function forceCorrectShippingDisplay() {
    const selectedRadio = document.querySelector('.delivery-radio-new:checked');
    const shippingContainer = document.querySelector('#shipping-fee-container');
    
    if (selectedRadio && shippingContainer) {
        const deliveryId = parseInt(selectedRadio.value);
        console.log('🔧 FORCING correct display for delivery ID:', deliveryId);
        
        // Remove existing classes
        shippingContainer.classList.remove('force-hide', 'force-show');
        
        if (deliveryId === 2) {
            // Hẹn lấy tại cửa hàng - ẩn shipping fee hoàn toàn
            shippingContainer.classList.add('force-hide');
            shippingContainer.style.display = 'none';
            console.log('🚫 Forced HIDE shipping fee for pickup');
        } else if (deliveryId === 1) {
            // Giao hàng tận nơi - hiện shipping fee
            shippingContainer.classList.add('force-show');
            shippingContainer.style.display = 'block';
            console.log('✅ Forced SHOW shipping fee for delivery');
        }
    } else {
        console.log('❌ Selected radio or shipping container not found');
    }
}

// Calculate shipping on page load
function calculateShippingOnLoad() {
    const selectedDeliveryMethod = document.querySelector('.delivery-radio-new:checked');
    const shippingFeeContainer = document.querySelector('#shipping-fee-container');
    
    if (selectedDeliveryMethod) {
        const deliveryMethodId = parseInt(selectedDeliveryMethod.value);
        const methodName = selectedDeliveryMethod.nextElementSibling.textContent.toLowerCase();
        
        console.log('🔄 Initial shipping calculation for method:', methodName, 'ID:', deliveryMethodId);
        
        // CHÍNH XÁC: Logic đơn giản dựa trên ID
        if (shippingFeeContainer) {
            if (deliveryMethodId === 2) {
                // Pickup method - ẩn hoàn toàn
                shippingFeeContainer.classList.remove('force-show');
                shippingFeeContainer.classList.add('force-hide');
                shippingFeeContainer.style.display = 'none';
                console.log('📦 Hidden shipping fee for pickup method');
            } else if (deliveryMethodId === 1) {
                // Delivery method - hiển thị
                shippingFeeContainer.classList.remove('force-hide');
                shippingFeeContainer.classList.add('force-show');
                shippingFeeContainer.style.display = 'block';
                console.log('🚚 Shown shipping fee for delivery method');
            }
        }
        
        // Tính toán phí giao hàng
        updateShippingCalculation(deliveryMethodId);
        
        console.log('✅ Initial shipping setup complete');
    } else {
        console.log('❌ No delivery method selected on page load');
    }
}

// Handle delivery method radio button changes
function handleDeliveryMethodChange() {
    const deliveryRadios = document.querySelectorAll('.delivery-radio-new');
    const storePickupSection = document.querySelector('.store-pickup-section');
    const shippingFeeContainer = document.querySelector('#shipping-fee-container');
    
    deliveryRadios.forEach(radio => {
        radio.addEventListener('change', function() {
            if (this.checked) {
                const deliveryMethodId = parseInt(this.value);
                const methodName = this.nextElementSibling.textContent.toLowerCase();
                
                console.log('🔄 Delivery method changed to ID:', deliveryMethodId, 'Method:', methodName);
                
                // Remove existing classes first
                if (shippingFeeContainer) {
                    shippingFeeContainer.classList.remove('force-hide', 'force-show');
                }
                
                if (deliveryMethodId === 2) {
                    // Hẹn lấy tại cửa hàng (ID = 2)
                    console.log('📦 Pickup method selected - hiding shipping fee');
                    storePickupSection.style.display = 'block';
                    makeStoreFieldsRequired(true);
                    
                    // Ẩn phí giao hàng hoàn toàn
                    if (shippingFeeContainer) {
                        shippingFeeContainer.classList.add('force-hide');
                        shippingFeeContainer.style.display = 'none';
                    }
                } else if (deliveryMethodId === 1) {
                    // Giao hàng tận nơi (ID = 1) 
                    console.log('🚚 Delivery method selected - showing shipping fee');
                    storePickupSection.style.display = 'none';
                    makeStoreFieldsRequired(false);
                    
                    // Hiển thị phí giao hàng
                    if (shippingFeeContainer) {
                        shippingFeeContainer.classList.add('force-show');
                        shippingFeeContainer.style.display = 'block';
                    }
                }
                
                // Calculate shipping for new delivery method
                updateShippingCalculation(deliveryMethodId);
                
                console.log('✅ Shipping fee container classes:', shippingFeeContainer ? shippingFeeContainer.className : 'not found');
            }
        });
    });
}

// Update shipping calculation via API
async function updateShippingCalculation(deliveryMethodId) {
    try {
        // Get current subtotal from the page
        const subtotalElement = document.querySelector('.subtotal-amount');
        const subtotalText = subtotalElement ? subtotalElement.textContent : '0';
        const subtotal = parseFloat(subtotalText.replace(/[^\d]/g, '')) || 0;
        
        console.log('Calculating shipping for:', { deliveryMethodId, subtotal });
        
        // Call shipping API
        const response = await fetch('/Cart/CalculateShipping', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
                OrderAmount: subtotal,
                DeliveryMethodId: deliveryMethodId
            })
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const result = await response.json();
        console.log('Shipping calculation result:', result);
        
        if (result.success) {
            updateShippingUI(result, deliveryMethodId);
        } else {
            handleShippingError(result.message);
        }
        
    } catch (error) {
        console.error('Error calculating shipping:', error);
        handleShippingError('Không thể tính phí giao hàng. Vui lòng thử lại.');
    }
}

// Update shipping UI with calculation results
function updateShippingUI(result, deliveryMethodId) {
    console.log('🎨 Updating shipping UI for delivery method:', deliveryMethodId, 'Result:', result);
    
    // Update delivery method description with shipping fee
    const methodLabel = document.querySelector(`label[for="delivery_${deliveryMethodId}"] .delivery-description`);
    if (methodLabel) {
        if (result.shippingFee === 0) {
            methodLabel.textContent = result.isFreeship ? '(Miễn phí)' : '(0₫)';
            methodLabel.style.color = result.isFreeship ? '#28a745' : '#6c757d';
        } else {
            methodLabel.textContent = `(+${formatCurrency(result.shippingFee)})`;
            methodLabel.style.color = '#dc3545';
        }
    }
    
    // Cập nhật phí giao hàng trong order summary
    const shippingFeeContainer = document.querySelector('#shipping-fee-container');
    const dynamicShippingRow = document.querySelector('#dynamic-shipping-row');
    const dynamicShippingAmount = document.querySelector('#dynamic-shipping-amount');
    
    if (shippingFeeContainer) {
        // CHÍNH XÁC: Chỉ hiển thị phí giao hàng cho delivery method ID = 1 (Giao hàng tận nơi)
        if (deliveryMethodId === 1) {
            console.log('🚚 Showing shipping fee for delivery method');
            
            // Remove hide class and add show class
            shippingFeeContainer.classList.remove('force-hide');
            shippingFeeContainer.classList.add('force-show');
            shippingFeeContainer.style.display = 'block';
            
            if (dynamicShippingRow && dynamicShippingAmount) {
                if (result.shippingFee > 0) {
                    dynamicShippingAmount.textContent = formatCurrency(result.shippingFee);
                    dynamicShippingAmount.className = 'shipping-amount';
                    dynamicShippingRow.style.display = 'flex';
                } else if (result.isFreeship) {
                    dynamicShippingAmount.textContent = 'Miễn phí';
                    dynamicShippingAmount.className = 'shipping-amount free';
                    dynamicShippingRow.style.display = 'flex';
                } else {
                    dynamicShippingAmount.textContent = '0₫';
                    dynamicShippingAmount.className = 'shipping-amount';
                    dynamicShippingRow.style.display = 'flex';
                }
            }
        } else if (deliveryMethodId === 2) {
            console.log('📦 Hiding shipping fee for pickup method');
            
            // HOÀN TOÀN ẨN phí giao hàng cho pickup methods (ID = 2)
            shippingFeeContainer.classList.remove('force-show');
            shippingFeeContainer.classList.add('force-hide');
            shippingFeeContainer.style.display = 'none';
            
            if (dynamicShippingRow) {
                dynamicShippingRow.style.display = 'none';
            }
        }
    }
    
    // Update hidden form fields
    const shippingFeeInput = document.querySelector('input[name="ShippingFee"]');
    if (shippingFeeInput) {
        shippingFeeInput.value = result.shippingFee;
    }
    
    const totalAmountInput = document.querySelector('input[name="TotalAmount"]');
    if (totalAmountInput) {
        totalAmountInput.value = result.totalAmount;
    }
    
    // Update total amount display
    const totalAmountElement = document.querySelector('.total-amount');
    if (totalAmountElement) {
        totalAmountElement.textContent = formatCurrency(result.totalAmount);
    }
    
    // BỎ LUÔN shipping message để tránh confuse user
    // updateShippingMessage(result);
    
    // Handle minimum order validation
    if (!result.canCheckout && result.requiredAmount > 0) {
        showMinimumOrderError(result.requiredAmount);
    } else {
        hideMinimumOrderError();
    }
    
    console.log('Shipping UI updated:', {
        shippingFee: result.shippingFee,
        totalAmount: result.totalAmount,
        isFreeship: result.isFreeship,
        canCheckout: result.canCheckout
    });
}

// Update shipping message display
function updateShippingMessage(result) {
    let messageContainer = document.querySelector('.shipping-message');
    if (!messageContainer) {
        // Create message container if it doesn't exist
        messageContainer = document.createElement('div');
        messageContainer.className = 'shipping-message';
        
        // Insert after delivery methods section
        const deliverySection = document.querySelector('.delivery-methods-new');
        if (deliverySection) {
            deliverySection.parentNode.insertBefore(messageContainer, deliverySection.nextSibling);
        }
    }
    
    if (result.freeshipeMessage || result.message) {
        const messageText = result.freeshipeMessage || result.message;
        messageContainer.innerHTML = `
            <div class="alert ${result.isFreeship ? 'alert-success' : 'alert-info'} mb-3">
                <i class="fas ${result.isFreeship ? 'fa-gift' : 'fa-info-circle'}"></i>
                ${messageText}
            </div>
        `;
        messageContainer.style.display = 'block';
    } else {
        messageContainer.style.display = 'none';
    }
}

// Show minimum order error
function showMinimumOrderError(requiredAmount) {
    let errorContainer = document.querySelector('.minimum-order-error');
    if (!errorContainer) {
        errorContainer = document.createElement('div');
        errorContainer.className = 'minimum-order-error';
        
        // Insert at the top of the form
        const form = document.getElementById('checkoutForm');
        if (form) {
            form.insertBefore(errorContainer, form.firstChild);
        }
    }
    
    errorContainer.innerHTML = `
        <div class="alert alert-warning">
            <i class="fas fa-exclamation-triangle"></i>
            <strong>Đơn hàng tối thiểu 60,000₫</strong><br>
            Vui lòng thêm ${formatCurrency(requiredAmount)} nữa để có thể đặt hàng.
            <a href="/Menu/MonNgonPhaiThu" class="btn btn-sm btn-outline-primary ms-2">
                <i class="fas fa-plus"></i> Thêm món
            </a>
        </div>
    `;
    errorContainer.style.display = 'block';
    
    // Disable submit button
    const submitButton = document.querySelector('.continue-btn');
    if (submitButton) {
        submitButton.disabled = true;
        submitButton.textContent = 'ĐƠN HÀNG CHƯA ĐẠT TỐI THIỂU';
    }
}

// Hide minimum order error
function hideMinimumOrderError() {
    const errorContainer = document.querySelector('.minimum-order-error');
    if (errorContainer) {
        errorContainer.style.display = 'none';
    }
    
    // Enable submit button
    const submitButton = document.querySelector('.continue-btn');
    if (submitButton) {
        submitButton.disabled = false;
        submitButton.textContent = 'TIẾP THEO';
    }
}

// Handle shipping calculation errors
function handleShippingError(message) {
    console.error('Shipping error:', message);
    
    // Show error message
    let errorContainer = document.querySelector('.shipping-error');
    if (!errorContainer) {
        errorContainer = document.createElement('div');
        errorContainer.className = 'shipping-error';
        
        const deliverySection = document.querySelector('.delivery-methods-new');
        if (deliverySection) {
            deliverySection.parentNode.insertBefore(errorContainer, deliverySection.nextSibling);
        }
    }
    
    errorContainer.innerHTML = `
        <div class="alert alert-danger">
            <i class="fas fa-exclamation-triangle"></i>
            ${message}
        </div>
    `;
    errorContainer.style.display = 'block';
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
    const manualAddressTextarea = document.querySelector('textarea[name="DeliveryAddress"]');
    const customerNameInput = document.querySelector('input[name="CustomerFullName"]');
    const customerPhoneInput = document.querySelector('input[name="CustomerPhoneNumber"]');
    
    if (!addressSelect) return;
    
    addressSelect.addEventListener('change', function() {
        if (this.value) {
            // Get the selected option text and parse address info
            const selectedText = this.options[this.selectedIndex].text;
            console.log('Address selected:', selectedText);
            
            // Parse the format: "FullName - PhoneNumber - Address"
            const parts = selectedText.split(' - ');
            if (parts.length >= 3) {
                const fullName = parts[0].trim();
                const phoneNumber = parts[1].trim(); 
                const address = parts.slice(2).join(' - ').trim();
                
                // Auto-fill customer information
                if (customerNameInput) {
                    customerNameInput.value = fullName;
                    console.log('Auto-filled customer name:', fullName);
                }
                
                if (customerPhoneInput) {
                    customerPhoneInput.value = phoneNumber;
                    console.log('Auto-filled customer phone:', phoneNumber);
                }
                
                console.log('Address info parsed:', { fullName, phoneNumber, address });
            }
            
            // Clear manual address when selecting from dropdown
            if (manualAddressTextarea) {
                manualAddressTextarea.value = '';
                console.log('Cleared manual address field');
            }
        } else {
            // If no address selected, you might want to restore original user info
            console.log('Address selection cleared');
        }
    });
    
    // Also handle manual address input to clear dropdown selection
    if (manualAddressTextarea) {
        manualAddressTextarea.addEventListener('input', function() {
            if (this.value.trim() && addressSelect && addressSelect.value) {
                addressSelect.value = '';
                console.log('Cleared address selection when manual input detected');
            }
        });
    }
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
        
        // Create date string in YYYY-MM-DD format without timezone issues
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        const isoString = `${year}-${month}-${day}`;
        
        // Debug log to verify date generation
        console.log(`Date ${i} debug:`, {
            originalDate: date,
            year, month, day,
            finalString: isoString,
            displayText: dateString
        });
        
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
        
        console.log(`Generated date ${i}: ${dateString} -> ${isoString}`);
    }
}

// Handle form validation and submission
function handleFormValidation() {
    const form = document.getElementById('checkoutForm');
    if (!form) return;
    
    form.addEventListener('submit', function(e) {
        console.log('Form submission started');
        
        // Custom validation logic
        const deliveryMethodSelected = document.querySelector('.delivery-radio-new:checked');
        
        if (!deliveryMethodSelected) {
            e.preventDefault();
            console.log('Validation failed: No delivery method selected');
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
        
        // Check delivery address - simplified logic
        const addressSelect = document.getElementById('addressSelect');
        const manualAddress = document.querySelector('textarea[name="DeliveryAddress"]');
        
        console.log('Address validation check:');
        console.log('- Address select exists:', !!addressSelect);
        console.log('- Address select value:', addressSelect ? addressSelect.value : 'N/A');
        console.log('- Manual address exists:', !!manualAddress);
        console.log('- Manual address value:', manualAddress ? manualAddress.value : 'N/A');
        
        // Simple logic: If no address selected from dropdown, manual address is required
        const hasSelectedAddress = addressSelect && addressSelect.value;
        const hasManualAddress = manualAddress && manualAddress.value.trim();
        
        if (!hasSelectedAddress && !hasManualAddress) {
            e.preventDefault();
            console.log('Validation failed: No address provided (neither selected nor manual)');
            showError('Vui lòng chọn địa chỉ giao hàng hoặc nhập địa chỉ giao hàng chi tiết');
            return false;
        }
        
        console.log('Form validation passed - allowing submission');
        console.log('Form will be submitted to server for final validation');
        // Allow form to submit normally to server
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
    return new Intl.NumberFormat('vi-VN').format(amount) + '₫';
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

// Initialize form - no longer making fields readonly
document.addEventListener('DOMContentLoaded', function() {
    console.log('Form initialized - all fields are editable');
}); 