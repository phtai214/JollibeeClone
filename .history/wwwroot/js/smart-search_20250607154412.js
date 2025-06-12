// Smart Search Functionality
class SmartSearch {
    constructor(container) {
        this.container = container;
        this.searchInput = container.querySelector('.search-input');
        this.suggestionsContainer = container.querySelector('.search-suggestions');
        this.searchIcon = container.querySelector('.search-ai-icon');
        
        this.currentResults = [];
        this.selectedIndex = -1;
        this.searchTimeout = null;
        this.isLoading = false;
        
        this.init();
    }
    
    init() {
        this.bindEvents();
        this.createLoadingIcon();
        this.addQuickActions();
    }
    
    bindEvents() {
        // Input events
        this.searchInput.addEventListener('input', (e) => {
            this.handleInput(e.target.value);
        });
        
        this.searchInput.addEventListener('focus', () => {
            this.container.classList.add('active');
            if (this.currentResults.length > 0) {
                this.showSuggestions();
            }
        });
        
        this.searchInput.addEventListener('blur', (e) => {
            // Delay hiding to allow clicking on suggestions
            setTimeout(() => {
                this.container.classList.remove('active');
                this.hideSuggestions();
            }, 200);
        });
        
        // Keyboard navigation
        this.searchInput.addEventListener('keydown', (e) => {
            this.handleKeyDown(e);
        });
        
        // Click outside to close
        document.addEventListener('click', (e) => {
            if (!this.container.contains(e.target)) {
                this.hideSuggestions();
                this.container.classList.remove('active');
            }
        });
    }
    
    createLoadingIcon() {
        this.loadingIcon = document.createElement('i');
        this.loadingIcon.className = 'fas fa-spinner search-loading';
        this.loadingIcon.style.display = 'none';
        this.container.appendChild(this.loadingIcon);
    }
    
    handleInput(query) {
        clearTimeout(this.searchTimeout);
        
        if (query.trim().length < 2) {
            this.hideSuggestions();
            this.currentResults = [];
            return;
        }
        
        // Debounce search
        this.searchTimeout = setTimeout(() => {
            this.performSearch(query);
        }, 300);
    }
    
    async performSearch(query) {
        try {
            this.showLoading();
            
            const response = await fetch(`/Admin/Api/SmartSearch?query=${encodeURIComponent(query)}`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            });
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (data.success) {
                this.currentResults = data.results;
                this.renderSuggestions(data.results);
                this.showSuggestions();
            } else {
                console.error('Search error:', data.error);
                this.showErrorMessage();
            }
        } catch (error) {
            console.error('Search request failed:', error);
            // Show some demo results for testing
            this.showDemoResults(query);
        } finally {
            this.hideLoading();
        }
    }
    
    renderSuggestions(results) {
        this.selectedIndex = -1;
        
        if (results.length === 0) {
            this.renderNoResults();
            return;
        }
        
        // Group results by type
        const grouped = this.groupResultsByType(results);
        
        let html = '';
        
        // Render each group
        Object.keys(grouped).forEach(type => {
            const items = grouped[type];
            const categoryName = this.getCategoryName(type);
            
            html += `
                <div class="suggestion-category">
                    <h6>${categoryName}</h6>
                    ${items.map(item => this.renderSuggestionItem(item)).join('')}
                </div>
            `;
        });
        
        // Add quick actions
        html += this.renderQuickActions();
        
        this.suggestionsContainer.innerHTML = html;
        this.bindSuggestionEvents();
    }
    
    groupResultsByType(results) {
        const grouped = {};
        results.forEach(item => {
            if (!grouped[item.type]) {
                grouped[item.type] = [];
            }
            grouped[item.type].push(item);
        });
        return grouped;
    }
    
    getCategoryName(type) {
        const names = {
            'product': 'Sản phẩm',
            'order': 'Đơn hàng',
            'category': 'Danh mục',
            'user': 'Người dùng',
            'store': 'Cửa hàng',
            'suggestion': 'Gợi ý'
        };
        return names[type] || 'Khác';
    }
    
    renderSuggestionItem(item) {
        const badgeClass = this.getBadgeClass(item.badge);
        
        return `
            <div class="suggestion-item" data-type="${item.type}" data-url="${item.url}" data-id="${item.id}">
                <i class="${item.icon}"></i>
                <div class="suggestion-content">
                    <div class="suggestion-title">${this.escapeHtml(item.title)}</div>
                    <div class="suggestion-subtitle">${this.escapeHtml(item.subtitle)}</div>
                </div>
                ${item.badge ? `<span class="suggestion-badge ${badgeClass}">${this.escapeHtml(item.badge)}</span>` : ''}
            </div>
        `;
    }
    
    getBadgeClass(badge) {
        if (!badge) return '';
        
        const lowerBadge = badge.toLowerCase();
        if (lowerBadge.includes('hoạt động') || lowerBadge.includes('mở cửa')) {
            return 'status-active';
        }
        if (lowerBadge.includes('khóa') || lowerBadge.includes('đóng cửa')) {
            return 'status-inactive';
        }
        return '';
    }
    
    renderNoResults() {
        this.suggestionsContainer.innerHTML = `
            <div class="search-no-results">
                <i class="fas fa-search"></i>
                <h6>Không tìm thấy kết quả</h6>
                <p>Thử tìm kiếm với từ khóa khác</p>
            </div>
            ${this.renderQuickActions()}
        `;
        this.bindSuggestionEvents();
    }
    
    renderQuickActions() {
        return `
            <div class="search-quick-actions">
                <a href="/Admin/Product/Create" class="quick-action">
                    <i class="fas fa-plus"></i>
                    Thêm sản phẩm
                </a>
                <a href="/Admin/Order" class="quick-action">
                    <i class="fas fa-list"></i>
                    Xem đơn hàng
                </a>
                <a href="/Admin/Category" class="quick-action">
                    <i class="fas fa-folder"></i>
                    Quản lý danh mục
                </a>
                <a href="/Admin/User" class="quick-action">
                    <i class="fas fa-users"></i>
                    Quản lý người dùng
                </a>
            </div>
        `;
    }
    
    bindSuggestionEvents() {
        const suggestionItems = this.suggestionsContainer.querySelectorAll('.suggestion-item[data-url]');
        
        suggestionItems.forEach((item, index) => {
            item.addEventListener('click', (e) => {
                e.preventDefault();
                const url = item.getAttribute('data-url');
                if (url) {
                    window.location.href = url;
                }
            });
            
            item.addEventListener('mouseenter', () => {
                this.setSelectedIndex(index);
            });
        });
    }
    
    handleKeyDown(e) {
        const suggestionItems = this.suggestionsContainer.querySelectorAll('.suggestion-item[data-url]');
        
        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                this.selectedIndex = Math.min(this.selectedIndex + 1, suggestionItems.length - 1);
                this.updateSelection();
                break;
                
            case 'ArrowUp':
                e.preventDefault();
                this.selectedIndex = Math.max(this.selectedIndex - 1, -1);
                this.updateSelection();
                break;
                
            case 'Enter':
                e.preventDefault();
                if (this.selectedIndex >= 0 && suggestionItems[this.selectedIndex]) {
                    const url = suggestionItems[this.selectedIndex].getAttribute('data-url');
                    if (url) {
                        window.location.href = url;
                    }
                }
                break;
                
            case 'Escape':
                this.hideSuggestions();
                this.searchInput.blur();
                break;
        }
    }
    
    setSelectedIndex(index) {
        this.selectedIndex = index;
        this.updateSelection();
    }
    
    updateSelection() {
        const suggestionItems = this.suggestionsContainer.querySelectorAll('.suggestion-item[data-url]');
        
        suggestionItems.forEach((item, index) => {
            item.classList.remove('keyboard-active');
            if (index === this.selectedIndex) {
                item.classList.add('keyboard-active');
                item.scrollIntoView({ block: 'nearest' });
            }
        });
    }
    
    showSuggestions() {
        this.suggestionsContainer.classList.add('show');
    }
    
    hideSuggestions() {
        this.suggestionsContainer.classList.remove('show');
    }
    
    showLoading() {
        this.isLoading = true;
        this.loadingIcon.style.display = 'block';
        this.searchIcon.style.display = 'none';
    }
    
    hideLoading() {
        this.isLoading = false;
        this.loadingIcon.style.display = 'none';
        this.searchIcon.style.display = 'block';
    }
    
    showErrorMessage() {
        this.suggestionsContainer.innerHTML = `
            <div class="search-no-results">
                <i class="fas fa-exclamation-triangle"></i>
                <h6>Lỗi tìm kiếm</h6>
                <p>Vui lòng thử lại sau</p>
            </div>
            ${this.renderQuickActions()}
        `;
        this.bindSuggestionEvents();
        this.showSuggestions();
    }
    
    showDemoResults(query) {
        // Demo results for testing when API is not available
        const demoResults = [
            {
                id: 1,
                title: `Tìm kiếm: ${query}`,
                subtitle: "Kết quả demo - API đang được kiểm tra",
                type: "suggestion",
                icon: "fas fa-search",
                url: "/Admin/Product",
                badge: "Demo"
            },
            {
                id: 2,
                title: "Burger Beef",
                subtitle: "Sản phẩm - Fast Food",
                type: "product",
                icon: "fas fa-hamburger",
                url: "/Admin/Product/Details/1",
                badge: "45,000₫"
            },
            {
                id: 3,
                title: "Đơn hàng #ORD001",
                subtitle: "Nguyễn Văn A - Đã hoàn thành",
                type: "order",
                icon: "fas fa-shopping-cart",
                url: "/Admin/Order/Details/1",
                badge: "120,000₫"
            }
        ];
        
        this.currentResults = demoResults;
        this.renderSuggestions(demoResults);
        this.showSuggestions();
    }
    
    addQuickActions() {
        // Add default suggestions when input is focused but empty
        this.defaultSuggestions = [
            {
                id: 0,
                title: "Xem tất cả sản phẩm",
                subtitle: "Quản lý sản phẩm trong hệ thống",
                type: "suggestion",
                icon: "fas fa-hamburger",
                url: "/Admin/Product",
                badge: ""
            },
            {
                id: 0,
                title: "Xem đơn hàng mới",
                subtitle: "Đơn hàng cần xử lý",
                type: "suggestion",
                icon: "fas fa-shopping-cart",
                url: "/Admin/Order",
                badge: ""
            },
            {
                id: 0,
                title: "Quản lý người dùng",
                subtitle: "Danh sách khách hàng",
                type: "suggestion",
                icon: "fas fa-users",
                url: "/Admin/User",
                badge: ""
            }
        ];
    }
    
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize Smart Search when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    const searchContainer = document.querySelector('.smart-search');
    if (searchContainer) {
        new SmartSearch(searchContainer);
    }
});

// Add search shortcuts (Ctrl+K or Cmd+K)
document.addEventListener('keydown', function(e) {
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        const searchInput = document.querySelector('.search-input');
        if (searchInput) {
            searchInput.focus();
        }
    }
}); 