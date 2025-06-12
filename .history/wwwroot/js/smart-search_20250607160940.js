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
        console.log('Smart Search: Initializing...');
        console.log('Smart Search: searchInput found:', !!this.searchInput);
        console.log('Smart Search: suggestionsContainer found:', !!this.suggestionsContainer);
        this.bindEvents();
        this.createLoadingIcon();
        this.addQuickActions();
        
        // Test immediate demo
        setTimeout(() => {
            console.log('Smart Search: Testing immediate demo');
            this.showDemoResults('test');
        }, 1000);
    }
    
    bindEvents() {
        // Input events
        this.searchInput.addEventListener('input', (e) => {
            this.handleInput(e.target.value);
        });
        
        this.searchInput.addEventListener('focus', () => {
            console.log('Smart Search: Input focused');
            this.container.classList.add('active');
            
            // Always show demo results on focus
            this.showDemoResults('focus test');
            
            if (this.currentResults.length > 0) {
                this.showSuggestions();
            }
        });
        
                // DISABLED FOR TESTING - these events hide the container
        // this.searchInput.addEventListener('blur', (e) => {
        //     // Delay hiding to allow clicking on suggestions
        //     setTimeout(() => {
        //         // Only hide if not clicking on suggestions
        //         if (!this.suggestionsContainer.matches(':hover')) {
        //             this.container.classList.remove('active');
        //             this.hideSuggestions();
        //         }
        //     }, 300);
        // });

        // Keyboard navigation
        this.searchInput.addEventListener('keydown', (e) => {
            this.handleKeyDown(e);
        });

        // DISABLED FOR TESTING - this event hides the container
        // document.addEventListener('click', (e) => {
        //     if (!this.container.contains(e.target)) {
        //         this.hideSuggestions();
        //         this.container.classList.remove('active');
        //     }
        // });
        
        // Prevent suggestions from hiding when clicking inside suggestions container
        this.suggestionsContainer.addEventListener('click', (e) => {
            e.stopPropagation();
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
        console.log('Smart Search: Input received:', query);
        
        if (query.trim().length < 1) {
            this.hideSuggestions();
            this.currentResults = [];
            return;
        }
        
        // Show instant demo results for quick testing
        if (query.trim().length >= 1) {
            this.showDemoResults(query);
        }
        
        // Also try real API after delay
        if (query.trim().length >= 2) {
            this.searchTimeout = setTimeout(() => {
                this.performSearch(query);
            }, 500);
        }
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
        console.log('Smart Search: Rendered HTML:', html.substring(0, 200) + '...');
        console.log('Smart Search: Container innerHTML length:', this.suggestionsContainer.innerHTML.length);
        
        // Force bright background for debugging
        this.suggestionsContainer.style.backgroundColor = 'lime';
        this.suggestionsContainer.style.color = 'black';
        
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
        console.log('Smart Search: Binding events for', suggestionItems.length, 'items');
        
        suggestionItems.forEach((item, index) => {
            // Remove any existing listeners
            item.replaceWith(item.cloneNode(true));
            const newItem = this.suggestionsContainer.querySelectorAll('.suggestion-item[data-url]')[index];
            
            newItem.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                const url = newItem.getAttribute('data-url');
                console.log('Smart Search: Clicked item with URL:', url);
                if (url) {
                    // Hide suggestions first
                    this.hideSuggestions();
                    // Navigate after a small delay
                    setTimeout(() => {
                        window.location.href = url;
                    }, 100);
                }
            });
            
            newItem.addEventListener('mouseenter', () => {
                this.setSelectedIndex(index);
            });
            
            // Prevent mousedown from causing blur
            newItem.addEventListener('mousedown', (e) => {
                e.preventDefault();
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
                // DISABLED FOR TESTING - this hides the container
                // this.hideSuggestions();
                // this.searchInput.blur();
                console.log('Escape pressed but hiding disabled for testing');
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
        console.log('Smart Search: Showing suggestions container');
        this.suggestionsContainer.classList.add('show');
        
        // EXTREME FORCE DEBUG - Fixed position at top of screen
        this.suggestionsContainer.style.cssText = `
            position: fixed !important;
            top: 100px !important;
            left: 50px !important;
            width: 600px !important;
            height: 400px !important;
            background: red !important;
            border: 10px solid blue !important;
            z-index: 999999 !important;
            display: block !important;
            visibility: visible !important;
            opacity: 1 !important;
            pointer-events: auto !important;
            overflow-y: auto !important;
            padding: 20px !important;
            border-radius: 15px !important;
            box-shadow: 0 20px 50px rgba(0,0,0,0.8) !important;
            font-size: 16px !important;
            font-weight: bold !important;
            color: white !important;
            text-shadow: 1px 1px 2px black !important;
        `;
        
        // Log container info
        const rect = this.suggestionsContainer.getBoundingClientRect();
        console.log('Suggestions container rect:', rect);
        console.log('Container computed style display:', getComputedStyle(this.suggestionsContainer).display);
        console.log('Container computed style visibility:', getComputedStyle(this.suggestionsContainer).visibility);
        console.log('Container computed style opacity:', getComputedStyle(this.suggestionsContainer).opacity);
    }
    
    hideSuggestions() {
        console.log('Smart Search: Hiding suggestions container');
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
        console.log('Smart Search: Showing demo results for:', query);
        
        // Smart demo results based on query
        let demoResults = [];
        
        if (query.toLowerCase().includes('nước') || query.toLowerCase().includes('suối')) {
            demoResults = [
                {
                    id: 1,
                    title: `💧 Nước suối Aquafina`,
                    subtitle: "Đồ uống - Nước giải khát",
                    type: "product",
                    icon: "fas fa-tint",
                    url: "/Admin/Product",
                    badge: "15,000₫"
                },
                {
                    id: 2,
                    title: `💧 Nước suối Lavie`,
                    subtitle: "Đồ uống - Nước tinh khiết",
                    type: "product",
                    icon: "fas fa-tint",
                    url: "/Admin/Product/Create",
                    badge: "12,000₫"
                }
            ];
        } else {
            demoResults = [
                {
                    id: 1,
                    title: `🔍 Tìm kiếm: "${query}"`,
                    subtitle: "Smart Search đang hoạt động! ✨",
                    type: "suggestion",
                    icon: "fas fa-magic",
                    url: "/Admin/Product",
                    badge: "DEMO"
                },
                {
                    id: 2,
                    title: "🍔 Burger Beef Deluxe",
                    subtitle: "Sản phẩm Hot - Fast Food",
                    type: "product",
                    icon: "fas fa-hamburger",
                    url: "/Admin/Product",
                    badge: "45,000₫"
                },
                {
                    id: 3,
                    title: "📦 Quản lý đơn hàng",
                    subtitle: "Xem tất cả đơn hàng",
                    type: "order",
                    icon: "fas fa-shopping-cart",
                    url: "/Admin/Order",
                    badge: "Manager"
                },
                {
                    id: 4,
                    title: "👥 Quản lý người dùng",
                    subtitle: "Danh sách khách hàng",
                    type: "user",
                    icon: "fas fa-users",
                    url: "/Admin/User",
                    badge: "Admin"
                },
                {
                    id: 5,
                    title: "🏪 Quản lý cửa hàng",
                    subtitle: "Hệ thống cửa hàng",
                    type: "store",
                    icon: "fas fa-store",
                    url: "/Admin/Store",
                    badge: "System"
                }
            ];
        }
        
        // Always add quick actions at the end
        demoResults.push({
            id: 99,
            title: "➕ Thêm sản phẩm mới",
            subtitle: "Tạo sản phẩm trong hệ thống",
            type: "suggestion",
            icon: "fas fa-plus-circle",
            url: "/Admin/Product/Create",
            badge: "Quick"
        });
        
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
    console.log('Smart Search: DOM loaded');
    const searchContainer = document.querySelector('.smart-search');
    if (searchContainer) {
        console.log('Smart Search: Container found, initializing...');
        new SmartSearch(searchContainer);
    } else {
        console.log('Smart Search: Container not found!');
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