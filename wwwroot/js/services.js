// Jollibee Services functionality - Match Real Design EXACTLY

class ServiceManager {
    constructor() {
        this.modal = null;
        this.modalContent = null;
        this.init();
    }

    init() {
        // Initialize modal elements
        const modalElement = document.getElementById('serviceModal');
        if (modalElement) {
            this.modal = new bootstrap.Modal(modalElement);
            this.modalContent = document.getElementById('serviceModalContent');
        }

        // Initialize event listeners
        this.bindEvents();
        
        // Initialize animations
        this.initAnimations();
    }

    bindEvents() {
        // Service image click events
        document.querySelectorAll('.service-image-container-real').forEach(wrapper => {
            wrapper.addEventListener('click', (e) => {
                const serviceRow = wrapper.closest('.service-row-real');
                const serviceId = this.getServiceIdFromRow(serviceRow);
                if (serviceId) {
                    this.viewServiceDetails(serviceId);
                }
            });
        });

        // Service content click events
        document.querySelectorAll('.service-content-real').forEach(content => {
            content.addEventListener('click', (e) => {
                // Only trigger if not clicking on button
                if (!e.target.closest('.btn-service-real')) {
                    const serviceRow = content.closest('.service-row-real');
                    const serviceId = this.getServiceIdFromRow(serviceRow);
                    if (serviceId) {
                        this.viewServiceDetails(serviceId);
                    }
                }
            });
        });

        // Keyboard accessibility for service rows
        document.querySelectorAll('.service-row-real').forEach(row => {
            // Make service rows focusable
            if (!row.hasAttribute('tabindex')) {
                row.setAttribute('tabindex', '0');
            }
            
            row.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    const serviceId = this.getServiceIdFromRow(row);
                    if (serviceId) {
                        this.viewServiceDetails(serviceId);
                    }
                }
            });
        });

        // Add hover effects
        document.querySelectorAll('.service-image-container-real').forEach(wrapper => {
            wrapper.style.cursor = 'pointer';
            
            wrapper.addEventListener('mouseenter', () => {
                const img = wrapper.querySelector('.service-image-real');
                if (img) {
                    img.style.transform = 'scale(1.05)';
                }
            });
            
            wrapper.addEventListener('mouseleave', () => {
                const img = wrapper.querySelector('.service-image-real');
                if (img) {
                    img.style.transform = 'scale(1)';
                }
            });
        });
    }

    getServiceIdFromRow(row) {
        // Try to get service ID from button's onclick attribute
        const button = row.querySelector('.btn-service-real');
        if (button && button.onclick) {
            const onclickStr = button.onclick.toString();
            const match = onclickStr.match(/viewServiceDetails\((\d+)\)/);
            return match ? match[1] : null;
        }
        return null;
    }

    initAnimations() {
        // Intersection Observer for scroll animations
        if ('IntersectionObserver' in window) {
            const observer = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        entry.target.style.opacity = '1';
                        entry.target.style.transform = 'translateY(0)';
                    }
                });
            }, {
                threshold: 0.1,
                rootMargin: '0px 0px -50px 0px'
            });

            document.querySelectorAll('.service-row-real').forEach((row, index) => {
                // Only animate if not already visible
                if (window.getComputedStyle(row).opacity === '0') {
                    observer.observe(row);
                }
            });
        }

        // Add smooth scroll for better UX
        this.addSmoothScrolling();
    }

    addSmoothScrolling() {
        // Smooth scroll when modal opens
        document.addEventListener('shown.bs.modal', (e) => {
            if (e.target.id === 'serviceModal') {
                e.target.scrollTop = 0;
            }
        });
    }

    viewServiceDetails(serviceId) {
        if (!this.modal || !this.modalContent) {
            console.error('Modal elements not found');
            return;
        }

        // Show loading state
        this.showLoading();
        
        // Show modal
        this.modal.show();
        
        // Fetch service details
        this.fetchServiceDetails(serviceId)
            .then(data => {
                if (data.success) {
                    this.renderServiceDetails(data.data);
                } else {
                    this.showError(data.message || 'Không thể tải thông tin dịch vụ.');
                }
            })
            .catch(error => {
                console.error('Error fetching service details:', error);
                this.showError('Có lỗi xảy ra khi tải thông tin dịch vụ.');
            });
    }

    async fetchServiceDetails(serviceId) {
        try {
            const response = await fetch(`/Service/GetServiceDetails?id=${serviceId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('Fetch error:', error);
            throw error;
        }
    }

    showLoading() {
        this.modalContent.innerHTML = `
            <div class="text-center py-5">
                <div class="spinner-border text-primary mb-3" role="status" style="width: 3rem; height: 3rem;">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
                <p class="text-muted">Đang tải thông tin dịch vụ...</p>
            </div>
        `;
    }

    renderServiceDetails(service) {
        const imageHtml = service.imageUrl 
            ? `<div class="text-center mb-4">
                 <img src="${service.imageUrl}" alt="${this.escapeHtml(service.serviceName)}" 
                      class="service-detail-image" style="max-width: 100%; height: auto;" />
               </div>`
            : '';

        const shortDescriptionHtml = service.shortDescription 
            ? `<p class="lead text-muted mb-4">${this.escapeHtml(service.shortDescription)}</p>`
            : '';

        const contentHtml = service.content 
            ? `<div class="service-detail-content">${service.content}</div>`
            : '<p class="text-muted">Thông tin chi tiết sẽ được cập nhật sớm.</p>';

        this.modalContent.innerHTML = `
            <div class="service-detail">
                ${imageHtml}
                <h4 class="text-primary mb-3 text-center">${this.escapeHtml(service.serviceName)}</h4>
                ${shortDescriptionHtml}
                ${contentHtml}
            </div>
        `;

        // Add fade-in animation
        const serviceDetail = this.modalContent.querySelector('.service-detail');
        if (serviceDetail) {
            serviceDetail.style.opacity = '0';
            serviceDetail.style.transform = 'translateY(20px)';
            serviceDetail.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
            
            setTimeout(() => {
                serviceDetail.style.opacity = '1';
                serviceDetail.style.transform = 'translateY(0)';
            }, 50);
        }
    }

    showError(message) {
        this.modalContent.innerHTML = `
            <div class="alert alert-warning d-flex align-items-center" role="alert">
                <i class="fas fa-exclamation-triangle me-3 text-warning" style="font-size: 1.5rem;"></i>
                <div>
                    <strong>Oops!</strong><br>
                    ${this.escapeHtml(message)}
                </div>
            </div>
            <div class="text-center mt-4">
                <button type="button" class="btn btn-primary" data-bs-dismiss="modal">
                    <i class="fas fa-arrow-left me-2"></i>
                    Quay lại
                </button>
            </div>
        `;
    }

    escapeHtml(text) {
        if (!text) return '';
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, function(m) { return map[m]; });
    }
}

// Global function for backward compatibility
function viewServiceDetails(serviceId) {
    if (window.serviceManager) {
        window.serviceManager.viewServiceDetails(serviceId);
    } else {
        console.error('ServiceManager not initialized');
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.serviceManager = new ServiceManager();
    
    // Add ARIA labels for better accessibility
    document.querySelectorAll('.service-row-real').forEach((row, index) => {
        row.setAttribute('role', 'button');
        row.setAttribute('aria-label', `Xem chi tiết dịch vụ ${index + 1}`);
    });

    // Add loading class for better UX
    document.body.classList.add('services-loaded');
});

// Utility functions for new design
const ServiceUtilsReal = {
    // Format text for display
    formatText: function(text, maxLength = 150) {
        if (!text) return '';
        if (text.length <= maxLength) return text;
        return text.substring(0, maxLength) + '...';
    },

    // Check if image exists
    imageExists: function(url) {
        return new Promise((resolve) => {
            const img = new Image();
            img.onload = () => resolve(true);
            img.onerror = () => resolve(false);
            img.src = url;
        });
    },

    // Lazy load images with new classes
    lazyLoadImages: function() {
        const images = document.querySelectorAll('.service-image-real[data-src]');
        
        if ('IntersectionObserver' in window) {
            const imageObserver = new IntersectionObserver((entries, observer) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const img = entry.target;
                        img.src = img.dataset.src;
                        img.classList.remove('lazy');
                        imageObserver.unobserve(img);
                    }
                });
            });

            images.forEach(img => imageObserver.observe(img));
        } else {
            // Fallback for older browsers
            images.forEach(img => {
                img.src = img.dataset.src;
                img.classList.remove('lazy');
            });
        }
    },

    // Add ripple effect to new buttons
    addRippleEffect: function() {
        document.querySelectorAll('.btn-service-real').forEach(button => {
            button.addEventListener('click', function(e) {
                const ripple = document.createElement('span');
                const rect = this.getBoundingClientRect();
                const size = Math.max(rect.width, rect.height);
                const x = e.clientX - rect.left - size / 2;
                const y = e.clientY - rect.top - size / 2;
                
                ripple.style.width = ripple.style.height = size + 'px';
                ripple.style.left = x + 'px';
                ripple.style.top = y + 'px';
                ripple.classList.add('ripple-effect');
                
                // Add ripple styles
                ripple.style.position = 'absolute';
                ripple.style.borderRadius = '50%';
                ripple.style.background = 'rgba(255, 255, 255, 0.3)';
                ripple.style.transform = 'scale(0)';
                ripple.style.animation = 'ripple-animation 0.6s linear';
                ripple.style.pointerEvents = 'none';
                
                this.appendChild(ripple);
                
                setTimeout(() => {
                    ripple.remove();
                }, 600);
            });
        });
    },

    // Enhanced hover effects for real design
    addEnhancedHoverEffects: function() {
        document.querySelectorAll('.service-row-real').forEach(row => {
            row.addEventListener('mouseenter', function() {
                const title = this.querySelector('.service-title-real');
                const description = this.querySelector('.service-description-real');
                
                if (title) {
                    title.style.transition = 'color 0.3s ease';
                    title.style.color = '#c41230';
                }
                
                if (description) {
                    description.style.transition = 'color 0.3s ease';
                    description.style.color = '#555';
                }
            });

            row.addEventListener('mouseleave', function() {
                const title = this.querySelector('.service-title-real');
                const description = this.querySelector('.service-description-real');
                
                if (title) {
                    title.style.color = '#e31937';
                }
                
                if (description) {
                    description.style.color = '#666';
                }
            });
        });
    }
};

// Initialize utilities for real design
document.addEventListener('DOMContentLoaded', function() {
    ServiceUtilsReal.lazyLoadImages();
    ServiceUtilsReal.addRippleEffect();
    ServiceUtilsReal.addEnhancedHoverEffects();
});

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ServiceManager, ServiceUtilsReal };
} 