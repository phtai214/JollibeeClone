// Checkout Page JavaScript
class CheckoutManager {
    constructor() {
        this.currentSubtotal = 0;
        this.currentShippingFee = 0;
        this.currentDiscountAmount = 0;
        this.appliedVoucherCode = null;
        this.appliedPromotionId = null;
        
        this.init();
    }

    init() {
        this.bindEvents();
        this.initializeAmounts();
        this.setupFormValidation();
    }

    bindEvents() {
        // Voucher application
        const applyVoucherBtn = document.getElementById('applyVoucherBtn');
        const voucherInput = document.getElementById('voucherCode');
        const toggleVouchersBtn = document.getElementById('toggleVouchersBtn');
        const vouchersList = document.getElementById('vouchersList');

        if (applyVoucherBtn) {
            applyVoucherBtn.addEventListener('click', () => this.applyVoucher());
        }

        if (voucherInput) {
            voucherInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    this.applyVoucher();
                }
            });
        }

        if (toggleVouchersBtn && vouchersList) {
            toggleVouchersBtn.addEventListener('click', () => {
                const isVisible = vouchersList.style.display !== 'none';
                vouchersList.style.display = isVisible ? 'none' : 'block';
                toggleVouchersBtn.classList.toggle('active', !isVisible);
            });
        }

        // Form submission
        const checkoutForm = document.getElementById('checkoutForm');
        if (checkoutForm) {
            checkoutForm.addEventListener('submit', (e) => this.handleFormSubmit(e));
        }

        // Payment method selection validation
        const paymentRadios = document.querySelectorAll('input[name="PaymentMethodID"]');
        paymentRadios.forEach(radio => {
            radio.addEventListener('change', () => this.validatePaymentMethod());
        });
    }

    initializeAmounts() {
        // Get initial amounts from hidden fields or page elements
        const subtotalElement = document.querySelector('input[name="SubtotalAmount"]');
        const shippingElement = document.querySelector('input[name="ShippingFee"]');
        const discountElement = document.querySelector('input[name="DiscountAmount"]');

        if (subtotalElement) this.currentSubtotal = parseFloat(subtotalElement.value) || 0;
        if (shippingElement) this.currentShippingFee = parseFloat(shippingElement.value) || 0;
        if (discountElement) this.currentDiscountAmount = parseFloat(discountElement.value) || 0;

        console.log('Initialized amounts:', {
            subtotal: this.currentSubtotal,
            shipping: this.currentShippingFee,
            discount: this.currentDiscountAmount
        });
    }

    async applyVoucher() {
        const voucherInput = document.getElementById('voucherCode');
        const messageDiv = document.getElementById('voucherMessage');
        const applyBtn = document.getElementById('applyVoucherBtn');

        if (!voucherInput || !messageDiv) return;

        const voucherCode = voucherInput.value.trim();
        if (!voucherCode) {
            this.showVoucherMessage('Vui lòng nhập mã voucher', 'error');
            return;
        }

        // Show loading state
        applyBtn.disabled = true;
        applyBtn.textContent = 'ĐANG XỬ LÝ...';

        try {
            const response = await fetch('/Cart/ApplyVoucher', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify({
                    VoucherCode: voucherCode,
                    OrderAmount: this.currentSubtotal,
                    UserID: this.getCurrentUserId()
                })
            });

            const result = await response.json();

            if (result.success) {
                // Apply voucher successfully
                this.appliedVoucherCode = voucherCode;
                this.appliedPromotionId = result.promotionID;
                this.currentDiscountAmount = result.discountAmount;

                this.updateOrderSummary();
                this.showVoucherMessage(`Voucher đã được áp dụng! Tiết kiệm ${this.formatCurrency(result.discountAmount)}`, 'success');
                
                // Update hidden fields
                this.updateHiddenFields();
                
                // Clear input
                voucherInput.value = '';
                
                // Hide available vouchers
                const vouchersList = document.getElementById('vouchersList');
                if (vouchersList) {
                    vouchersList.style.display = 'none';
                    const toggleBtn = document.getElementById('toggleVouchersBtn');
                    if (toggleBtn) toggleBtn.classList.remove('active');
                }

            } else {
                this.showVoucherMessage(result.message || 'Mã voucher không hợp lệ', 'error');
            }

        } catch (error) {
            console.error('Error applying voucher:', error);
            this.showVoucherMessage('Có lỗi xảy ra khi áp dụng voucher', 'error');
        } finally {
            // Reset button
            applyBtn.disabled = false;
            applyBtn.textContent = 'ÁP DỤNG';
        }
    }

    updateOrderSummary() {
        // Update discount row in order summary
        const subtotalInfo = document.querySelector('.subtotal-info');
        if (!subtotalInfo) return;

        // Remove existing discount row
        const existingDiscountRow = subtotalInfo.querySelector('.discount-row');
        if (existingDiscountRow) {
            existingDiscountRow.remove();
        }

        // Add new discount row if there's a discount
        if (this.currentDiscountAmount > 0 && this.appliedVoucherCode) {
            const vatNotice = subtotalInfo.querySelector('.vat-notice');
            
            const discountRow = document.createElement('div');
            discountRow.className = 'subtotal-row discount-row';
            discountRow.innerHTML = `
                <span class="subtotal-label">Voucher (${this.appliedVoucherCode})</span>
                <span class="subtotal-amount discount-amount">-${this.formatCurrency(this.currentDiscountAmount)}</span>
            `;
            
            if (vatNotice) {
                subtotalInfo.insertBefore(discountRow, vatNotice);
            } else {
                subtotalInfo.appendChild(discountRow);
            }
        }

        // Update total amount
        const finalTotal = this.currentSubtotal + this.currentShippingFee - this.currentDiscountAmount;
        const totalAmountElement = document.getElementById('finalTotalAmount');
        if (totalAmountElement) {
            totalAmountElement.textContent = this.formatCurrency(finalTotal);
        }
    }

    updateHiddenFields() {
        // Update hidden form fields
        const discountField = document.querySelector('input[name="DiscountAmount"]');
        const appliedPromotionField = document.querySelector('input[name="AppliedPromotionID"]');

        if (discountField) {
            discountField.value = this.currentDiscountAmount;
        }

        if (appliedPromotionField) {
            appliedPromotionField.value = this.appliedPromotionId || '';
        }
    }

    showVoucherMessage(message, type) {
        const messageDiv = document.getElementById('voucherMessage');
        if (!messageDiv) return;

        messageDiv.textContent = message;
        messageDiv.className = `voucher-message ${type}`;
        messageDiv.style.display = 'block';

        // Auto hide after 5 seconds
        setTimeout(() => {
            messageDiv.style.display = 'none';
        }, 5000);
    }

    setupFormValidation() {
        const form = document.getElementById('checkoutForm');
        if (!form) return;

        // Add custom validation for payment method
        form.addEventListener('submit', (e) => {
            if (!this.validatePaymentMethod()) {
                e.preventDefault();
                return false;
            }
        });
    }

    validatePaymentMethod() {
        const selectedPayment = document.querySelector('input[name="PaymentMethodID"]:checked');
        const errorDiv = document.querySelector('span[data-valmsg-for="PaymentMethodID"]');

        if (!selectedPayment) {
            if (errorDiv) {
                errorDiv.textContent = 'Vui lòng chọn phương thức thanh toán';
                errorDiv.style.display = 'block';
            }
            
            // Scroll to payment section
            const paymentSection = document.querySelector('.payment-methods');
            if (paymentSection) {
                paymentSection.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
            
            return false;
        } else {
            if (errorDiv) {
                errorDiv.style.display = 'none';
            }
            return true;
        }
    }

    handleFormSubmit(e) {
        // Show loading state on submit button
        const submitBtn = e.target.querySelector('.place-order-btn');
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.textContent = 'ĐANG XỬ LÝ...';
            
            // Re-enable if form validation fails
            setTimeout(() => {
                if (!this.validatePaymentMethod()) {
                    submitBtn.disabled = false;
                    submitBtn.textContent = 'ĐẶT HÀNG';
                }
            }, 100);
        }
    }

    formatCurrency(amount) {
        return new Intl.NumberFormat('vi-VN').format(amount) + ' đ';
    }

    getAntiForgeryToken() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : '';
    }

    getCurrentUserId() {
        const userIdField = document.querySelector('input[name="UserID"]');
        return userIdField ? parseInt(userIdField.value) || null : null;
    }
}

// Global function for voucher cards
window.applyVoucherFromCard = function(voucherCode) {
    const voucherInput = document.getElementById('voucherCode');
    if (voucherInput) {
        voucherInput.value = voucherCode;
        
        // Trigger apply voucher
        if (window.checkoutManager) {
            window.checkoutManager.applyVoucher();
        }
    }
};

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('🛒 Checkout page initialized');
    window.checkoutManager = new CheckoutManager();
});

// Additional utility functions
window.CheckoutUtils = {
    // Remove applied voucher
    removeVoucher: function() {
        if (window.checkoutManager) {
            window.checkoutManager.appliedVoucherCode = null;
            window.checkoutManager.appliedPromotionId = null;
            window.checkoutManager.currentDiscountAmount = 0;
            window.checkoutManager.updateOrderSummary();
            window.checkoutManager.updateHiddenFields();
            window.checkoutManager.showVoucherMessage('Đã hủy voucher', 'success');
        }
    },

    // Refresh order totals
    refreshTotals: function() {
        if (window.checkoutManager) {
            window.checkoutManager.updateOrderSummary();
        }
    },

    // Validate form before submit
    validateForm: function() {
        if (window.checkoutManager) {
            return window.checkoutManager.validatePaymentMethod();
        }
        return true;
    }
}; 