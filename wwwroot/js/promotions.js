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
    
    // Use modern clipboard API if available
    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(couponCode).then(() => {
            showToast(`Đã sao chép mã: ${couponCode}`, 'success');
            
            // Add visual feedback
            const couponElements = document.querySelectorAll('.coupon-value');
            couponElements.forEach(element => {
                if (element.textContent.includes(couponCode)) {
                    element.style.background = 'rgba(39, 174, 96, 0.1)';
                    element.style.color = '#27ae60';
                    
                    setTimeout(() => {
                        element.style.background = '';
                        element.style.color = '';
                    }, 2000);
                }
            });
        }).catch(() => {
            fallbackCopyToClipboard(couponCode);
        });
    } else {
        fallbackCopyToClipboard(couponCode);
    }
}

// Fallback copy method for older browsers
function fallbackCopyToClipboard(text) {
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.position = 'fixed';
    textArea.style.left = '-999999px';
    textArea.style.top = '-999999px';
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();
    
    try {
        document.execCommand('copy');
        showToast(`Đã sao chép mã: ${text}`, 'success');
    } catch (err) {
        showToast('Không thể sao chép mã. Vui lòng sao chép thủ công.', 'error');
    }
    
    document.body.removeChild(textArea);
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