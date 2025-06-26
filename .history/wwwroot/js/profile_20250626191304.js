// Profile Page JavaScript
document.addEventListener('DOMContentLoaded', function() {
    // Handle change password form
    const changePasswordForm = document.getElementById('changePasswordForm');
    const newPasswordInput = document.getElementById('newPassword');
    const confirmPasswordInput = document.getElementById('confirmPassword');

    if (changePasswordForm) {
        changePasswordForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            // Clear previous validation
            clearValidation();
            
            // Validate form
            if (validatePasswordForm()) {
                submitChangePassword();
            }
        });
    }

    // Real-time password confirmation validation
    if (confirmPasswordInput) {
        confirmPasswordInput.addEventListener('input', function() {
            validatePasswordMatch();
        });
    }

    // Password strength indicator
    if (newPasswordInput) {
        newPasswordInput.addEventListener('input', function() {
            validatePasswordStrength();
        });
    }

    // Auto-dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(function(alert) {
        setTimeout(function() {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });

    // Smooth scroll to error fields
    const invalidFields = document.querySelectorAll('.is-invalid');
    if (invalidFields.length > 0) {
        invalidFields[0].scrollIntoView({ 
            behavior: 'smooth', 
            block: 'center' 
        });
        invalidFields[0].focus();
    }
});

function clearValidation() {
    const formControls = document.querySelectorAll('#changePasswordForm .form-control');
    const feedbacks = document.querySelectorAll('#changePasswordForm .invalid-feedback');
    
    formControls.forEach(function(control) {
        control.classList.remove('is-invalid', 'is-valid');
    });
    
    feedbacks.forEach(function(feedback) {
        feedback.textContent = '';
    });
}

function validatePasswordForm() {
    let isValid = true;
    
    const currentPassword = document.getElementById('currentPassword');
    const newPassword = document.getElementById('newPassword');
    const confirmPassword = document.getElementById('confirmPassword');
    
    // Validate current password
    if (!currentPassword.value.trim()) {
        showFieldError(currentPassword, 'Vui lòng nhập mật khẩu hiện tại');
        isValid = false;
    }
    
    // Validate new password
    if (!newPassword.value.trim()) {
        showFieldError(newPassword, 'Vui lòng nhập mật khẩu mới');
        isValid = false;
    } else if (newPassword.value.length < 6) {
        showFieldError(newPassword, 'Mật khẩu mới phải có ít nhất 6 ký tự');
        isValid = false;
    } else if (newPassword.value === currentPassword.value) {
        showFieldError(newPassword, 'Mật khẩu mới phải khác mật khẩu hiện tại');
        isValid = false;
    }
    
    // Validate confirm password
    if (!confirmPassword.value.trim()) {
        showFieldError(confirmPassword, 'Vui lòng xác nhận mật khẩu mới');
        isValid = false;
    } else if (confirmPassword.value !== newPassword.value) {
        showFieldError(confirmPassword, 'Mật khẩu xác nhận không khớp');
        isValid = false;
    }
    
    return isValid;
}

function validatePasswordMatch() {
    const newPassword = document.getElementById('newPassword');
    const confirmPassword = document.getElementById('confirmPassword');
    
    if (confirmPassword.value && newPassword.value) {
        if (confirmPassword.value === newPassword.value) {
            showFieldSuccess(confirmPassword);
        } else {
            showFieldError(confirmPassword, 'Mật khẩu xác nhận không khớp');
        }
    }
}

function validatePasswordStrength() {
    const newPassword = document.getElementById('newPassword');
    const password = newPassword.value;
    
    if (password.length >= 6) {
        let strength = 0;
        let strengthText = '';
        let strengthClass = '';
        
        // Check password strength
        if (password.length >= 8) strength++;
        if (/[a-z]/.test(password)) strength++;
        if (/[A-Z]/.test(password)) strength++;
        if (/[0-9]/.test(password)) strength++;
        if (/[^a-zA-Z0-9]/.test(password)) strength++;
        
        switch (strength) {
            case 0:
            case 1:
                strengthText = 'Mật khẩu yếu';
                strengthClass = 'text-danger';
                break;
            case 2:
            case 3:
                strengthText = 'Mật khẩu trung bình';
                strengthClass = 'text-warning';
                break;
            case 4:
            case 5:
                strengthText = 'Mật khẩu mạnh';
                strengthClass = 'text-success';
                break;
        }
        
        // Show strength indicator
        const feedback = newPassword.parentElement.querySelector('.invalid-feedback');
        if (strength >= 2) {
            newPassword.classList.remove('is-invalid');
            newPassword.classList.add('is-valid');
            feedback.className = `valid-feedback ${strengthClass}`;
            feedback.textContent = strengthText;
        } else {
            feedback.className = 'invalid-feedback';
            feedback.textContent = strengthText;
        }
    }
}

function showFieldError(field, message) {
    field.classList.add('is-invalid');
    field.classList.remove('is-valid');
    const feedback = field.parentElement.querySelector('.invalid-feedback');
    if (feedback) {
        feedback.textContent = message;
    }
}

function showFieldSuccess(field) {
    field.classList.add('is-valid');
    field.classList.remove('is-invalid');
    const feedback = field.parentElement.querySelector('.invalid-feedback');
    if (feedback) {
        feedback.textContent = '';
    }
}

function submitChangePassword() {
    const form = document.getElementById('changePasswordForm');
    const submitBtn = form.querySelector('button[type="submit"]');
    const originalText = submitBtn.innerHTML;
    
    // Show loading state
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Đang xử lý...';
    
    // Prepare form data
    const formData = new FormData(form);
    
    // Submit via AJAX
    fetch('/Account/ChangePassword', {
        method: 'POST',
        body: formData,
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Show success message
            showSuccessMessage(data.message);
            
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('changePasswordModal'));
            modal.hide();
            
            // Reset form
            form.reset();
            clearValidation();
        } else {
            // Show error message
            showErrorMessage(data.message);
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showErrorMessage('Có lỗi xảy ra khi đổi mật khẩu. Vui lòng thử lại.');
    })
    .finally(() => {
        // Reset button state
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;
    });
}

function showSuccessMessage(message) {
    const alertHtml = `
        <div class="alert alert-success alert-dismissible fade show" role="alert">
            <i class="fas fa-check-circle me-2"></i>${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    const contentHeader = document.querySelector('.content-header');
    contentHeader.insertAdjacentHTML('afterend', alertHtml);
    
    // Scroll to top to show message
    document.querySelector('.account-content').scrollIntoView({ 
        behavior: 'smooth', 
        block: 'start' 
    });
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        const alert = document.querySelector('.alert-success');
        if (alert) {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }
    }, 5000);
}

function showErrorMessage(message) {
    const alertHtml = `
        <div class="alert alert-danger alert-dismissible fade show" role="alert">
            <i class="fas fa-exclamation-circle me-2"></i>${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    const modalBody = document.querySelector('#changePasswordModal .modal-body');
    modalBody.insertAdjacentHTML('afterbegin', alertHtml);
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        const alert = document.querySelector('#changePasswordModal .alert-danger');
        if (alert) {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }
    }, 5000);
}

// Form validation enhancements
function enhanceFormValidation() {
    const profileForm = document.querySelector('.profile-form');
    
    if (profileForm) {
        const requiredFields = profileForm.querySelectorAll('input[required], select[required]');
        
        requiredFields.forEach(field => {
            field.addEventListener('blur', function() {
                validateField(this);
            });
            
            field.addEventListener('input', function() {
                if (this.classList.contains('is-invalid')) {
                    validateField(this);
                }
            });
        });
    }
}

function validateField(field) {
    const value = field.value.trim();
    const fieldName = field.getAttribute('name');
    
    // Clear previous validation
    field.classList.remove('is-invalid', 'is-valid');
    
    if (field.hasAttribute('required') && !value) {
        field.classList.add('is-invalid');
        return false;
    }
    
    // Email validation
    if (field.type === 'email' && value) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(value)) {
            field.classList.add('is-invalid');
            return false;
        }
    }
    
    // Phone validation
    if (fieldName === 'PhoneNumber' && value) {
        const phoneRegex = /^[0-9+\-\s\(\)]{10,}$/;
        if (!phoneRegex.test(value)) {
            field.classList.add('is-invalid');
            return false;
        }
    }
    
    field.classList.add('is-valid');
    return true;
}

// Initialize form validation enhancements
document.addEventListener('DOMContentLoaded', function() {
    enhanceFormValidation();
});

// Handle sidebar menu active state
document.addEventListener('DOMContentLoaded', function() {
    const sidebarItems = document.querySelectorAll('.sidebar-item');
    const currentPath = window.location.pathname.toLowerCase();
    
    sidebarItems.forEach(item => {
        const href = item.getAttribute('href');
        if (href && href.toLowerCase() === currentPath) {
            item.classList.add('active');
        } else {
            item.classList.remove('active');
        }
    });
}); 