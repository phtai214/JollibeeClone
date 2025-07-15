// Promotions Page JavaScript

// Global variables
let searchTimeout;
let countdownIntervals = [];

// Initialize the promotions page
function initializePromotions() {
    console.log('Initializing promotions page...');
    
    // Initialize search functionality
    initializeSearch();
    
    // Initialize countdown timers
    initializeCountdowns();
    
    // Initialize copy functionality
    initializeCopyFeatures();
    
    // Initialize promotion actions
    initializePromotionActions();
    
    // Initialize animations
    initializeAnimations();
    
    console.log('Promotions page initialized successfully');
}

// Search functionality
function initializeSearch() {
    const searchInput = document.getElementById('searchPromotion');
    const searchBtn = document.querySelector('.search-btn');
    
    if (searchInput) {
        // Real-time search as user types
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                performSearch(this.value.trim());
            }, 300);
        });
        
        // Search on Enter key
        searchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                performSearch(this.value.trim());
            }
        });
    }
    
    if (searchBtn) {
        searchBtn.addEventListener('click', function() {
            const searchTerm = searchInput ? searchInput.value.trim() : '';
            performSearch(searchTerm);
        });
    }
}

// Perform search
function performSearch(searchTerm) {
    const promotionCards = document.querySelectorAll('.promotion-card');
    let visibleCount = 0;
    
    promotionCards.forEach(card => {
        const promotionName = card.getAttribute('data-promotion-name') || '';
        const couponCode = card.getAttribute('data-coupon') || '';
        const description = card.querySelector('.promotion-description')?.textContent || '';
        
        const searchContent = (promotionName + ' ' + couponCode + ' ' + description).toLowerCase();
        const isVisible = searchTerm === '' || searchContent.includes(searchTerm.toLowerCase());
        
        if (isVisible) {
            card.style.display = 'block';
            card.style.animation = 'fadeInUp 0.4s ease-out';
            visibleCount++;
        } else {
            card.style.display = 'none';
        }
    });
    
    // Show/hide empty state
    updateEmptyState(visibleCount, searchTerm);
}

// Update empty state
function updateEmptyState(visibleCount, searchTerm) {
    let emptyState = document.querySelector('.search-empty-state');
    
    if (visibleCount === 0 && searchTerm !== '') {
        if (!emptyState) {
            emptyState = document.createElement('div');
            emptyState.className = 'search-empty-state empty-state';
            emptyState.innerHTML = `
                <div class="empty-icon">
                    <i class="fas fa-search"></i>
                </div>
                <h3>Không tìm thấy ưu đãi nào</h3>
                <p>Thử tìm kiếm với từ khóa khác hoặc xem tất cả ưu đãi hiện có.</p>
                <button class="btn btn-primary" onclick="clearSearch()">Xem tất cả ưu đãi</button>
            `;
            
            const promotionsContainer = document.getElementById('promotionsContainer');
            if (promotionsContainer) {
                promotionsContainer.parentNode.appendChild(emptyState);
            }
        }
        emptyState.style.display = 'block';
    } else if (emptyState) {
        emptyState.style.display = 'none';
    }
}

// Clear search
function clearSearch() {
    const searchInput = document.getElementById('searchPromotion');
    if (searchInput) {
        searchInput.value = '';
        performSearch('');
    }
}

// Initialize countdown timers
function initializeCountdowns() {
    const timers = document.querySelectorAll('.promotion-timer');
    
    // Clear existing intervals
    countdownIntervals.forEach(interval => clearInterval(interval));
    countdownIntervals = [];
    
    timers.forEach(timer => {
        const endDateStr = timer.getAttribute('data-end-date');
        if (endDateStr) {
            const endDate = new Date(endDateStr);
            const countdownElement = timer.querySelector('.countdown');
            
            if (countdownElement) {
                const interval = setInterval(() => {
                    updateCountdown(endDate, countdownElement, timer);
                }, 1000);
                
                countdownIntervals.push(interval);
                
                // Initial update
                updateCountdown(endDate, countdownElement, timer);
            }
        }
    });
}

// Update individual countdown
function updateCountdown(endDate, element, timerContainer) {
    const now = new Date().getTime();
    const distance = endDate.getTime() - now;
    
    if (distance > 0) {
        const days = Math.floor(distance / (1000 * 60 * 60 * 24));
        const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((distance % (1000 * 60)) / 1000);
        
        let timeText = '';
        if (days > 0) {
            timeText = `${days} ngày ${hours} giờ`;
        } else if (hours > 0) {
            timeText = `${hours} giờ ${minutes} phút`;
        } else if (minutes > 0) {
            timeText = `${minutes} phút ${seconds} giây`;
        } else {
            timeText = `${seconds} giây`;
        }
        
        element.textContent = timeText;
        
        // Add urgency styling for last 24 hours
        if (distance < 24 * 60 * 60 * 1000) {
            timerContainer.classList.add('urgent');
            timerContainer.style.background = 'rgba(231, 76, 60, 0.15)';
            timerContainer.style.color = '#e74c3c';
        }
    } else {
        element.textContent = 'Đã hết hạn';
        timerContainer.style.background = 'rgba(149, 165, 166, 0.15)';
        timerContainer.style.color = '#95a5a6';
        
        // Optionally hide expired promotions
        const promotionCard = timerContainer.closest('.promotion-card');
        if (promotionCard) {
            promotionCard.classList.add('expired');
            promotionCard.style.opacity = '0.6';
        }
    }
}

// Initialize copy features
function initializeCopyFeatures() {
    // This function is called from HTML onclick events
    // No additional initialization needed
    console.log('Copy features initialized');
}

// Copy coupon code to clipboard
function copyCouponCode(couponCode) {
    if (!couponCode) {
        showToast('Không có mã giảm giá để sao chép', 'error');
        return;
    }
    
    console.log('Attempting to copy coupon code:', couponCode);
    
    // Try modern clipboard API first
    if (navigator.clipboard && window.isSecureContext) {
        console.log('Using modern clipboard API');
        navigator.clipboard.writeText(couponCode).then(() => {
            console.log('Modern clipboard API success');
            showToast(`Đã sao chép mã: ${couponCode}`, 'success');
            addVisualFeedback(couponCode);
        }).catch((error) => {
            console.log('Modern clipboard API failed:', error);
            fallbackCopyToClipboard(couponCode);
        });
    } else {
        console.log('Modern clipboard API not available, using fallback');
        fallbackCopyToClipboard(couponCode);
    }
}

// Add visual feedback when copying
function addVisualFeedback(couponCode) {
    const couponElements = document.querySelectorAll('.coupon-value, .coupon-code, [data-coupon]');
    couponElements.forEach(element => {
        const elementText = element.textContent || element.getAttribute('data-coupon') || '';
        if (elementText.includes(couponCode)) {
            element.style.background = 'rgba(39, 174, 96, 0.1)';
            element.style.color = '#27ae60';
            element.style.transition = 'all 0.3s ease';
            
            setTimeout(() => {
                element.style.background = '';
                element.style.color = '';
            }, 2000);
        }
    });
}

// Fallback copy method for older browsers
function fallbackCopyToClipboard(text) {
    console.log('Using fallback copy method for:', text);
    
    // Method 1: Try execCommand first
    const textArea = document.createElement('textarea');
    textArea.value = text;
    
    // Make the textarea invisible but not display:none
    textArea.style.position = 'fixed';
    textArea.style.left = '-9999px';
    textArea.style.top = '-9999px';
    textArea.style.width = '1px';
    textArea.style.height = '1px';
    textArea.style.padding = '0';
    textArea.style.border = 'none';
    textArea.style.outline = 'none';
    textArea.style.boxShadow = 'none';
    textArea.style.background = 'transparent';
    textArea.setAttribute('readonly', '');
    textArea.tabIndex = -1;
    
    document.body.appendChild(textArea);
    
    try {
        // Focus and select
        textArea.focus();
        textArea.setSelectionRange(0, text.length);
        textArea.select();
        
        // Try to copy
        const successful = document.execCommand('copy');
        console.log('execCommand copy result:', successful);
        
        if (successful) {
            showToast(`Đã sao chép mã: ${text}`, 'success');
            addVisualFeedback(text);
        } else {
            throw new Error('execCommand failed');
        }
    } catch (err) {
        console.error('Fallback copy failed:', err);
        
        // Method 2: Manual copy instruction
        showManualCopyDialog(text);
    } finally {
        document.body.removeChild(textArea);
    }
}

// Show manual copy dialog when all else fails
function showManualCopyDialog(text) {
    // Create modal for manual copy
    const modal = document.createElement('div');
    modal.className = 'copy-modal';
    modal.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(0,0,0,0.5);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 10000;
        font-family: 'Roboto', sans-serif;
    `;
    
    modal.innerHTML = `
        <div style="
            background: white;
            padding: 30px;
            border-radius: 12px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.3);
            max-width: 400px;
            width: 90%;
            text-align: center;
        ">
            <h3 style="color: #e31937; margin-bottom: 15px;">
                <i class="fas fa-copy"></i> Sao chép mã giảm giá
            </h3>
            <p style="color: #666; margin-bottom: 20px;">
                Vui lòng sao chép mã bên dưới:
            </p>
            <div style="
                background: #f8f9fa;
                padding: 15px;
                border-radius: 8px;
                margin-bottom: 20px;
                border: 2px dashed #e31937;
            ">
                <strong style="
                    font-size: 18px;
                    color: #e31937;
                    letter-spacing: 2px;
                    user-select: all;
                ">${text}</strong>
            </div>
            <div>
                <button onclick="this.closest('.copy-modal').remove()" style="
                    background: #e31937;
                    color: white;
                    border: none;
                    padding: 10px 20px;
                    border-radius: 6px;
                    cursor: pointer;
                    font-size: 14px;
                    margin: 0 5px;
                ">Đóng</button>
                <button onclick="selectTextAndClose('${text}', this)" style="
                    background: #ffc627;
                    color: #e31937;
                    border: none;
                    padding: 10px 20px;
                    border-radius: 6px;
                    cursor: pointer;
                    font-size: 14px;
                    margin: 0 5px;
                    font-weight: bold;
                ">Chọn để sao chép</button>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    
    // Close on backdrop click
    modal.addEventListener('click', function(e) {
        if (e.target === modal) {
            modal.remove();
        }
    });
}

// Helper function for manual copy dialog
function selectTextAndClose(text, button) {
    const textElement = button.closest('.copy-modal').querySelector('strong');
    if (textElement) {
        // Select the text
        const range = document.createRange();
        range.selectNodeContents(textElement);
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);
        
        // Try one more time with execCommand
        try {
            document.execCommand('copy');
            showToast(`Đã sao chép mã: ${text}`, 'success');
        } catch (e) {
            showToast('Văn bản đã được chọn. Nhấn Ctrl+C để sao chép', 'info');
        }
    }
    
    // Close modal after a delay
    setTimeout(() => {
        button.closest('.copy-modal').remove();
    }, 1000);
}

// Initialize promotion actions
function initializePromotionActions() {
    // Use promotion buttons
    const usePromotionBtns = document.querySelectorAll('.btn-use-promotion');
    usePromotionBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            const promotionId = this.getAttribute('data-promotion-id');
            usePromotion(promotionId);
        });
    });
    
    // Share buttons are handled by inline onclick events
    console.log('Promotion actions initialized');
}

// Use promotion action
function usePromotion(promotionId) {
    if (!promotionId) {
        showToast('Không thể sử dụng ưu đãi này', 'error');
        return;
    }
    
    // Add loading state
    const button = document.querySelector(`[data-promotion-id="${promotionId}"]`);
    if (button) {
        const originalText = button.innerHTML;
        button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang xử lý...';
        button.disabled = true;
        
        // Simulate API call delay
        setTimeout(() => {
            button.innerHTML = originalText;
            button.disabled = false;
            
            // For now, just show success message
            // In a real app, this would redirect to cart/checkout with the promotion applied
            showToast('Ưu đãi đã được áp dụng! Chuyển đến giỏ hàng để tiếp tục.', 'success');
            
            // Optionally redirect to menu or cart
            // window.location.href = '/Menu';
        }, 1500);
    }
}

// Share promotion
function sharePromotion(promotionName, couponCode) {
    const shareText = couponCode 
        ? `🎉 ${promotionName}\n📱 Mã giảm giá: ${couponCode}\n🍔 Thưởng thức ngay tại Jollibee!`
        : `🎉 ${promotionName}\n🍔 Ưu đãi đặc biệt tại Jollibee!`;
    
    const shareUrl = window.location.href;
    
    // Check if Web Share API is supported
    if (navigator.share) {
        navigator.share({
            title: promotionName,
            text: shareText,
            url: shareUrl
        }).then(() => {
            showToast('Đã chia sẻ ưu đãi thành công!', 'success');
        }).catch((error) => {
            console.log('Error sharing:', error);
            fallbackShare(shareText, shareUrl);
        });
    } else {
        fallbackShare(shareText, shareUrl);
    }
}

// Fallback share method
function fallbackShare(text, url) {
    // Copy share text to clipboard
    const shareContent = `${text}\n${url}`;
    
    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(shareContent).then(() => {
            showToast('Đã sao chép link chia sẻ!', 'success');
        });
    } else {
        fallbackCopyToClipboard(shareContent);
    }
}

// Initialize animations
function initializeAnimations() {
    // Intersection Observer for scroll animations
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };
    
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.animationDelay = '0s';
                entry.target.style.animationFillMode = 'both';
                entry.target.classList.add('animate-in');
            }
        });
    }, observerOptions);
    
    // Observe promotion cards
    const promotionCards = document.querySelectorAll('.promotion-card');
    promotionCards.forEach((card, index) => {
        card.style.animationDelay = `${index * 0.1}s`;
        observer.observe(card);
    });
    
    // Add CSS for animate-in class
    if (!document.querySelector('#promotion-animations')) {
        const style = document.createElement('style');
        style.id = 'promotion-animations';
        style.textContent = `
            .promotion-card.animate-in {
                animation: fadeInUp 0.6s ease-out forwards;
            }
        `;
        document.head.appendChild(style);
    }
}

// Show toast notification
function showToast(message, type = 'info', duration = 3000) {
    const toast = document.getElementById('promotionToast');
    const toastMessage = toast.querySelector('.toast-message');
    
    if (!toast || !toastMessage) {
        console.error('Toast elements not found');
        return;
    }
    
    // Set message and type
    toastMessage.textContent = message;
    toast.className = `toast ${type}`;
    
    // Show toast
    toast.classList.add('show');
    
    // Hide after duration
    setTimeout(() => {
        toast.classList.remove('show');
    }, duration);
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
function formatDate(date) {
    return new Intl.DateTimeFormat('vi-VN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
    }).format(new Date(date));
}

// Handle window resize for responsive features
function handleResize() {
    // Update any responsive features if needed
    console.log('Window resized, updating responsive features...');
}

// Debounce function for performance
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Initialize resize handler
window.addEventListener('resize', debounce(handleResize, 250));

// Cleanup function
function cleanupPromotions() {
    // Clear intervals
    countdownIntervals.forEach(interval => clearInterval(interval));
    countdownIntervals = [];
    
    // Clear timeouts
    if (searchTimeout) {
        clearTimeout(searchTimeout);
    }
    
    console.log('Promotions cleanup completed');
}

// Call cleanup when page unloads
window.addEventListener('beforeunload', cleanupPromotions);

// Export functions for global access
window.copyCouponCode = copyCouponCode;
window.sharePromotion = sharePromotion;
window.initializePromotions = initializePromotions;
window.clearSearch = clearSearch;
window.selectTextAndClose = selectTextAndClose; 