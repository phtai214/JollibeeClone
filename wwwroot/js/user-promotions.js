// User Promotions Page JavaScript

// Global variables
let currentTab = 'available';

// Initialize the promotions page
function initializeUserPromotions() {
    initializeTabs();
    initializeTooltips();
    addEventListeners();
    checkExpiredPromotions();
}

// Tab functionality
function initializeTabs() {
    const tabButtons = document.querySelectorAll('.tab-btn');
    const tabContents = document.querySelectorAll('.tab-content');

    tabButtons.forEach(button => {
        button.addEventListener('click', () => {
            const tabId = button.getAttribute('data-tab');
            switchTab(tabId);
        });
    });
}

function switchTab(tabId) {
    currentTab = tabId;

    // Update tab buttons
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    document.querySelector(`[data-tab="${tabId}"]`).classList.add('active');

    // Update tab content
    document.querySelectorAll('.tab-content').forEach(content => {
        content.classList.remove('active');
    });
    document.getElementById(tabId).classList.add('active');

    // Add animation to cards
    const cards = document.querySelectorAll(`#${tabId} .promotion-card, #${tabId} .history-item`);
    cards.forEach((card, index) => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        
        setTimeout(() => {
            card.style.transition = 'all 0.3s ease';
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        }, index * 100);
    });
}

// Copy coupon code functionality
function copyCouponCode(couponCode, element) {
    if (!couponCode) {
        showToast('Mã giảm giá không hợp lệ', 'error');
        return;
    }

    // Modern clipboard API
    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(couponCode).then(() => {
            showToast(`Đã sao chép mã: ${couponCode}`, 'success');
            animateCopySuccess(element);
        }).catch(err => {
            console.warn('Clipboard API failed, using fallback:', err);
            fallbackCopyTextToClipboard(couponCode, element);
        });
    } else {
        // Fallback for older browsers
        fallbackCopyTextToClipboard(couponCode, element);
    }
}

// Fallback copy method for older browsers
function fallbackCopyTextToClipboard(text, element) {
    const textArea = document.createElement("textarea");
    textArea.value = text;
    textArea.style.top = "0";
    textArea.style.left = "0";
    textArea.style.position = "fixed";
    textArea.style.opacity = "0";

    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();

    try {
        const successful = document.execCommand('copy');
        if (successful) {
            showToast(`Đã sao chép mã: ${text}`, 'success');
            animateCopySuccess(element);
        } else {
            // Vẫn thành công nếu text đã được select, user có thể copy thủ công
            showToast(`Mã đã được chọn: ${text}`, 'success');
            animateCopySuccess(element);
        }
    } catch (err) {
        // Ngay cả khi có lỗi, text vẫn được select cho user copy thủ công
        console.warn('Copy command failed, but text is selected:', err);
        showToast(`Mã đã được chọn: ${text} (Nhấn Ctrl+C để sao chép)`, 'success');
        animateCopySuccess(element);
    }

    // Clean up sau 2 giây để user có thời gian copy
    setTimeout(() => {
        if (document.body.contains(textArea)) {
            document.body.removeChild(textArea);
        }
    }, 2000);
}

// Animate copy success
function animateCopySuccess(element) {
    if (!element) return;
    
    const couponElement = element.closest('.coupon-code') || element;
    if (couponElement) {
        couponElement.style.transform = 'scale(1.05)';
        couponElement.style.backgroundColor = '#d4edda';
        couponElement.style.borderColor = '#28a745';
        
        setTimeout(() => {
            couponElement.style.transform = 'scale(1)';
            couponElement.style.backgroundColor = '';
            couponElement.style.borderColor = '';
        }, 200);
    }
}

// Use promotion functionality
function usePromotion(promotionId) {
    if (!promotionId) {
        showToast('ID khuyến mãi không hợp lệ', 'error');
        return;
    }

    // Here you would typically redirect to the menu/cart page
    // with the promotion pre-selected
    showToast('Đang chuyển hướng đến trang đặt hàng...', 'success');
    
    // Simulate redirect delay
    setTimeout(() => {
        // You can customize this URL based on your menu/cart implementation
        window.location.href = `/Menu/MonNgonPhaiThu?promotionId=${promotionId}`;
    }, 1000);
}

// Share promotion functionality
function sharePromotion(promotionName, couponCode) {
    const shareText = `🎉 Ưu đãi tuyệt vời từ Jollibee: ${promotionName}\n💫 Mã giảm giá: ${couponCode}\n🍗 Đặt hàng ngay tại Jollibee!`;
    const shareUrl = window.location.origin;

    // Modern Web Share API
    if (navigator.share) {
        navigator.share({
            title: `Ưu đãi Jollibee: ${promotionName}`,
            text: shareText,
            url: shareUrl
        }).then(() => {
            showToast('Đã chia sẻ thành công!', 'success');
        }).catch(err => {
            console.log('Error sharing:', err);
            fallbackShare(shareText);
        });
    } else {
        // Fallback for desktop browsers
        fallbackShare(shareText);
    }
}

// Fallback share method
function fallbackShare(text) {
    // Tạo một temporary element để copy text
    const textArea = document.createElement("textarea");
    textArea.value = text;
    textArea.style.position = "fixed";
    textArea.style.opacity = "0";

    document.body.appendChild(textArea);
    textArea.select();

    try {
        const successful = document.execCommand('copy');
        if (successful) {
            showToast('Đã sao chép thông tin chia sẻ!', 'success');
        } else {
            showToast('Thông tin đã được chọn để chia sẻ!', 'success');
        }
    } catch (err) {
        console.warn('Share copy failed:', err);
        showToast('Thông tin đã được chọn để chia sẻ!', 'success');
    }

    setTimeout(() => {
        if (document.body.contains(textArea)) {
            document.body.removeChild(textArea);
        }
    }, 1000);
}

// Toast notification system
function showToast(message, type = 'success') {
    const toast = document.getElementById('promotionToast');
    const toastIcon = toast.querySelector('.toast-icon');
    const toastMessage = toast.querySelector('.toast-message');

    // Update content
    toastMessage.textContent = message;
    
    // Update icon based on type
    if (type === 'success') {
        toastIcon.className = 'toast-icon fas fa-check-circle';
        toast.classList.remove('error');
    } else if (type === 'error') {
        toastIcon.className = 'toast-icon fas fa-exclamation-circle';
        toast.classList.add('error');
    }

    // Show toast
    toast.classList.add('show');

    // Hide after 3 seconds
    setTimeout(() => {
        toast.classList.remove('show');
    }, 3000);
}

// Initialize tooltips
function initializeTooltips() {
    // Add tooltips to elements with title attributes
    const tooltipElements = document.querySelectorAll('[title]');
    tooltipElements.forEach(element => {
        // You can add custom tooltip implementation here
        // For now, we'll rely on browser default tooltips
    });
}

// Add general event listeners
function addEventListeners() {
    // Handle click outside toast to dismiss
    document.addEventListener('click', (event) => {
        const toast = document.getElementById('promotionToast');
        if (toast && !toast.contains(event.target) && toast.classList.contains('show')) {
            toast.classList.remove('show');
        }
    });

    // Handle keyboard shortcuts
    document.addEventListener('keydown', (event) => {
        // Press 1 for available promotions tab
        if (event.key === '1' && !event.ctrlKey && !event.altKey) {
            switchTab('available');
            event.preventDefault();
        }
        
        // Press 2 for history tab
        if (event.key === '2' && !event.ctrlKey && !event.altKey) {
            switchTab('history');
            event.preventDefault();
        }

        // Press Escape to close toast
        if (event.key === 'Escape') {
            const toast = document.getElementById('promotionToast');
            if (toast.classList.contains('show')) {
                toast.classList.remove('show');
            }
        }
    });

    // Add hover effects to promotion cards
    const promotionCards = document.querySelectorAll('.promotion-card');
    promotionCards.forEach(card => {
        card.addEventListener('mouseenter', () => {
            card.style.transform = 'translateY(-4px)';
        });

        card.addEventListener('mouseleave', () => {
            card.style.transform = 'translateY(0)';
        });
    });
}

// Check for expired promotions and add visual indicators
function checkExpiredPromotions() {
    const promotionCards = document.querySelectorAll('.promotion-card[data-promotion-id]');
    
    promotionCards.forEach(card => {
        const validityInfo = card.querySelector('.validity-info span');
        if (validityInfo) {
            const validityText = validityInfo.textContent;
            const dateMatch = validityText.match(/(\d{2}\/\d{2}\/\d{4})/);
            
            if (dateMatch) {
                const expiryDate = new Date(dateMatch[1].split('/').reverse().join('-'));
                const now = new Date();
                const timeDiff = expiryDate.getTime() - now.getTime();
                const daysDiff = Math.ceil(timeDiff / (1000 * 3600 * 24));

                if (daysDiff <= 0) {
                    // Expired
                    card.classList.add('expired');
                    card.style.opacity = '0.6';
                    const statusElement = card.querySelector('.promotion-status');
                    if (statusElement) {
                        statusElement.innerHTML = '<i class="fas fa-times-circle"></i><span>Đã hết hạn</span>';
                        statusElement.className = 'promotion-status expired';
                        statusElement.style.backgroundColor = 'rgba(220, 53, 69, 0.1)';
                        statusElement.style.color = '#dc3545';
                    }
                } else if (daysDiff <= 3) {
                    // Expiring soon
                    const statusElement = card.querySelector('.promotion-status');
                    if (statusElement) {
                        statusElement.innerHTML = '<i class="fas fa-exclamation-triangle"></i><span>Sắp hết hạn</span>';
                        statusElement.className = 'promotion-status expiring';
                        statusElement.style.backgroundColor = 'rgba(255, 193, 7, 0.1)';
                        statusElement.style.color = '#ffc107';
                    }
                }
            }
        }
    });
}

// Search functionality (if needed in the future)
function filterPromotions(searchTerm) {
    const promotionCards = document.querySelectorAll('.promotion-card');
    const historyItems = document.querySelectorAll('.history-item');
    
    const searchLower = searchTerm.toLowerCase();
    
    // Filter available promotions
    promotionCards.forEach(card => {
        const title = card.querySelector('.promotion-title')?.textContent.toLowerCase() || '';
        const description = card.querySelector('.promotion-description')?.textContent.toLowerCase() || '';
        const couponCode = card.querySelector('.coupon-code .code')?.textContent.toLowerCase() || '';
        
        if (title.includes(searchLower) || description.includes(searchLower) || couponCode.includes(searchLower)) {
            card.style.display = 'block';
        } else {
            card.style.display = 'none';
        }
    });
    
    // Filter history items
    historyItems.forEach(item => {
        const title = item.querySelector('.history-title')?.textContent.toLowerCase() || '';
        const couponCode = item.querySelector('.code-used')?.textContent.toLowerCase() || '';
        
        if (title.includes(searchLower) || couponCode.includes(searchLower)) {
            item.style.display = 'block';
        } else {
            item.style.display = 'none';
        }
    });
}

// Utility function to format currency
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
        minimumFractionDigits: 0
    }).format(amount);
}

// Utility function to format date
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    });
}

// Load more functionality (if pagination is implemented)
function loadMorePromotions() {
    // Implementation for loading more promotions
    showToast('Đang tải thêm ưu đãi...', 'success');
    
    // Simulate loading
    setTimeout(() => {
        showToast('Đã tải xong!', 'success');
    }, 1000);
}

// Refresh promotions data
function refreshPromotions() {
    showToast('Đang cập nhật danh sách ưu đãi...', 'success');
    
    // Simulate refresh
    setTimeout(() => {
        window.location.reload();
    }, 1000);
}

// Export functions for global access
window.copyCouponCode = function(couponCode, element) {
    return copyCouponCode(couponCode, element || event?.target);
};
window.usePromotion = usePromotion;
window.sharePromotion = sharePromotion;
window.switchTab = switchTab;
window.filterPromotions = filterPromotions;
window.refreshPromotions = refreshPromotions;
window.initializeUserPromotions = initializeUserPromotions; 