// ==================================================
// JOLLIBEE CART MANAGEMENT SYSTEM
// ==================================================

class JollibeeCart {
    constructor() {
        this.isInitialized = false;
        this.cartData = null;
        this.isLoading = false;
        
        this.init();
    }

    async init() {
        if (this.isInitialized) return;
        
        try {
            // Create cart elements
            this.createCartElements();
            
            // Bind events
            this.bindEvents();
            
            // Load cart data
            await this.loadCart();
            
            this.isInitialized = true;
            console.log('🛒 Jollibee Cart initialized successfully');
        } catch (error) {
            console.error('❌ Error initializing cart:', error);
        }
    }

    createCartElements() {
        // Create cart overlay
        const overlay = document.createElement('div');
        overlay.className = 'cart-overlay';
        overlay.id = 'cartOverlay';
        document.body.appendChild(overlay);

        // Create cart float button
        const floatBtn = document.createElement('div');
        floatBtn.className = 'cart-float-btn';
        floatBtn.id = 'cartFloatBtn';
        floatBtn.innerHTML = `
            <i class="fas fa-shopping-cart cart-icon"></i>
            <span class="cart-badge" id="cartBadge">0</span>
        `;
        document.body.appendChild(floatBtn);

        // Create cart panel
        const panel = document.createElement('div');
        panel.className = 'cart-panel';
        panel.id = 'cartPanel';
        panel.innerHTML = this.getCartPanelHTML();
        document.body.appendChild(panel);

        // Create success animation
        const successAnimation = document.createElement('div');
        successAnimation.className = 'cart-success-animation';
        successAnimation.id = 'cartSuccessAnimation';
        successAnimation.innerHTML = `
            <i class="fas fa-check-circle"></i>
            <span>Đã thêm vào giỏ hàng!</span>
        `;
        document.body.appendChild(successAnimation);
    }

    getCartPanelHTML() {
        return `
            <div class="cart-header">
                <h3><i class="fas fa-shopping-cart"></i> Giỏ Hàng</h3>
                <button class="cart-close" id="cartClose">
                    <i class="fas fa-times"></i>
                </button>
            </div>
            <div class="cart-content" id="cartContent">
                <div class="cart-loading" id="cartLoading">
                    <div class="cart-loading-spinner"></div>
                </div>
            </div>
            <div class="cart-summary" id="cartSummary" style="display: none;">
                <div class="cart-summary-row">
                    <span class="cart-summary-label">Tạm tính:</span>
                    <span class="cart-summary-value" id="cartSubTotal">0đ</span>
                </div>
                <div class="cart-summary-row cart-total-row">
                    <span class="cart-total-label">Tổng cộng:</span>
                    <span class="cart-total-value" id="cartTotal">0đ</span>
                </div>
                <button class="cart-checkout-btn" id="cartCheckoutBtn">
                    <i class=""></i> Thanh Toán
                </button>
                <button class="cart-continue-btn" id="cartContinueBtn">
                    <i class="fas fa-arrow-left"></i> Tiếp Tục Mua Sắm
                </button>
            </div>
        `;
    }

    bindEvents() {
        // Float button click
        document.getElementById('cartFloatBtn').addEventListener('click', () => {
            this.openCart();
        });

        // Close button click
        document.getElementById('cartClose').addEventListener('click', () => {
            this.closeCart();
        });

        // Overlay click
        document.getElementById('cartOverlay').addEventListener('click', () => {
            this.closeCart();
        });

        // Continue shopping
        document.getElementById('cartContinueBtn').addEventListener('click', () => {
            this.closeCart();
            // Điều hướng về trang menu nếu không ở trang menu
            if (!window.location.pathname.includes('/Menu/')) {
                window.location.href = '/Menu/MonNgonPhaiThu';
            }
        });

        // Checkout button
        document.getElementById('cartCheckoutBtn').addEventListener('click', () => {
            this.proceedToCheckout();
        });

        // ESC key to close cart
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeCart();
            }
        });
    }

    async loadCart() {
        try {
            this.showLoading();
            
            console.log('📱 Client: Loading cart...');
            const response = await fetch('/Cart/GetCart');
            const result = await response.json();
            
            console.log('📱 Client: GetCart response:', result);
            
            if (result.success) {
                this.cartData = result.data;
                console.log('📱 Client: Cart data received:', this.cartData);
                console.log('📱 Client: Cart items count:', this.cartData?.CartItems?.length || 0);
                this.updateCartDisplay();
                this.updateCartBadge();
            } else {
                console.error('Error loading cart:', result.message);
                this.showError('Không thể tải giỏ hàng');
            }
        } catch (error) {
            console.error('Error loading cart:', error);
            this.showError('Lỗi kết nối');
        } finally {
            this.hideLoading();
        }
    }

    async addToCart(productId, quantity = 1, selectedOptions = []) {
        if (this.isLoading) return;
        
        try {
            this.isLoading = true;
            
            const requestData = {
                ProductID: productId,
                Quantity: quantity,
                SelectedOptions: selectedOptions
            };

            console.log('📱 Client: Adding to cart with data:', requestData);

            const response = await fetch('/Cart/AddToCart', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(requestData)
            });

            const result = await response.json();
            console.log('📱 Client: AddToCart response:', result);
            
            if (result.success) {
                this.cartData = result.data;
                console.log('📱 Client: Updated cart data:', this.cartData);
                console.log('📱 Client: Updated cart items count:', this.cartData?.CartItems?.length || 0);
                this.updateCartDisplay();
                this.updateCartBadge();
                this.showSuccessAnimation();
                
                // Show notification
                this.showNotification('success', result.message || 'Đã thêm vào giỏ hàng!');
                
                return true;
            } else {
                this.showNotification('error', result.message || 'Không thể thêm vào giỏ hàng');
                return false;
            }
        } catch (error) {
            console.error('Error adding to cart:', error);
            this.showNotification('error', 'Lỗi kết nối');
            return false;
        } finally {
            this.isLoading = false;
        }
    }

    async updateQuantity(cartItemId, quantity) {
        if (this.isLoading) return;
        
        try {
            this.isLoading = true;
            
            const requestData = {
                CartItemID: cartItemId,
                Quantity: quantity
            };

            console.log('📱 Client: Updating quantity with data:', requestData);

            const response = await fetch('/Cart/UpdateQuantity', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(requestData)
            });

            const result = await response.json();
            console.log('📱 Client: UpdateQuantity response:', result);
            
            if (result.success) {
                this.cartData = result.data;
                console.log('📱 Client: Updated cart data after quantity change:', this.cartData);
                console.log('📱 Client: Updated cart items count after quantity change:', this.cartData?.CartItems?.length || 0);
                this.updateCartDisplay();
                this.updateCartBadge();
                return true;
            } else {
                this.showNotification('error', result.message || 'Không thể cập nhật số lượng');
                return false;
            }
        } catch (error) {
            console.error('Error updating quantity:', error);
            this.showNotification('error', 'Lỗi kết nối');
            return false;
        } finally {
            this.isLoading = false;
        }
    }

    async removeItem(cartItemId) {
        if (this.isLoading) return;
        
        try {
            this.isLoading = true;
            
            const requestData = {
                CartItemID: cartItemId
            };

            console.log('📱 Client: Removing item with data:', requestData);

            const response = await fetch('/Cart/RemoveItem', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(requestData)
            });

            const result = await response.json();
            console.log('📱 Client: RemoveItem response:', result);
            
            if (result.success) {
                this.cartData = result.data;
                console.log('📱 Client: Updated cart data after item removal:', this.cartData);
                console.log('📱 Client: Updated cart items count after item removal:', this.cartData?.CartItems?.length || 0);
                this.updateCartDisplay();
                this.updateCartBadge();
                this.showNotification('success', result.message || 'Đã xóa sản phẩm');
                return true;
            } else {
                this.showNotification('error', result.message || 'Không thể xóa sản phẩm');
                return false;
            }
        } catch (error) {
            console.error('Error removing item:', error);
            this.showNotification('error', 'Lỗi kết nối');
            return false;
        } finally {
            this.isLoading = false;
        }
    }

    async clearCart() {
        if (this.isLoading) return;
        
        if (!confirm('Bạn có chắc muốn xóa tất cả sản phẩm trong giỏ hàng?')) {
            return;
        }
        
        try {
            this.isLoading = true;
            
            console.log('📱 Client: Clearing cart...');

            const response = await fetch('/Cart/ClearCart', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            const result = await response.json();
            console.log('📱 Client: ClearCart response:', result);
            
            if (result.success) {
                this.cartData = result.data;
                console.log('📱 Client: Updated cart data after clear:', this.cartData);
                console.log('📱 Client: Updated cart items count after clear:', this.cartData?.CartItems?.length || 0);
                this.updateCartDisplay();
                this.updateCartBadge();
                this.showNotification('success', result.message || 'Đã xóa tất cả sản phẩm');
                return true;
            } else {
                this.showNotification('error', result.message || 'Không thể xóa giỏ hàng');
                return false;
            }
        } catch (error) {
            console.error('Error clearing cart:', error);
            this.showNotification('error', 'Lỗi kết nối');
            return false;
        } finally {
            this.isLoading = false;
        }
    }

    async editItem(cartItemId) {
        try {
            console.log('✏️ Client: editItem called for:', cartItemId);
            
            // Find the cart item
            const cartItem = this.cartData?.CartItems?.find(item => item.CartItemID === cartItemId);
            if (!cartItem) {
                console.error('❌ Cart item not found:', cartItemId);
                this.showNotification('error', 'Không tìm thấy sản phẩm trong giỏ hàng');
                return;
            }

            if (!cartItem.IsConfigurable) {
                console.warn('⚠️ Item is not configurable:', cartItemId);
                this.showNotification('error', 'Sản phẩm này không thể chỉnh sửa');
                return;
            }

            // Store editing context
            this.editingCartItemId = cartItemId;
            this.editingCartItem = cartItem;
            
            // Get combo options for this product
            const response = await fetch(`/Menu/GetComboOptions?productId=${cartItem.ProductID}`);
            const comboData = await response.json();
            
            if (!comboData || !comboData.groups) {
                console.error('❌ Failed to get combo options');
                this.showNotification('error', 'Không thể tải tùy chọn combo');
                return;
            }

            // Add basePrice from current cart item
            comboData.basePrice = cartItem.UnitPrice;
            
            // Open modal and populate with current configuration
            this.openEditModal(comboData, cartItem);
            
        } catch (error) {
            console.error('❌ Client: editItem error:', error);
            this.showNotification('error', 'Có lỗi xảy ra khi chỉnh sửa sản phẩm');
        }
    }

    openEditModal(comboData, cartItem) {
        console.log('🎛️ Opening edit modal for:', cartItem.ProductName);
        
        // Find existing order options modal from menu page
        let modal = document.getElementById('orderOptionsModal');
        
        if (!modal) {
            console.error('❌ Order options modal not found');
            this.showNotification('error', 'Không thể mở cửa sổ chỉnh sửa');
            return;
        }

        // Update modal title
        const modalTitle = document.getElementById('orderOptionsModalLabel');
        if (modalTitle) {
            modalTitle.textContent = `Chỉnh sửa - ${cartItem.ProductName}`;
        }

        // Store combo data globally for price calculation
        window.currentCombo = comboData;
        
        // Render combo options in the form
        this.renderEditComboOptions(comboData, cartItem);
        
        // Update the add to cart button to save changes instead
        const addToCartBtn = document.getElementById('addToCartBtn');
        if (addToCartBtn) {
            addToCartBtn.innerHTML = '<i class="fas fa-save"></i> Lưu thay đổi';
            addToCartBtn.onclick = () => this.saveItemChanges();
        }

        // Show modal
        const bootstrapModal = new bootstrap.Modal(modal);
        bootstrapModal.show();
    }

    renderEditComboOptions(comboData, cartItem) {
        const form = document.getElementById('orderOptionsForm');
        if (!form) {
            console.error('❌ Order options form not found');
            return;
        }
        
        if (!comboData || !comboData.groups || comboData.groups.length === 0) {
            form.innerHTML = '<div class="text-danger">Không có tùy chọn cho combo này.</div>';
            return;
        }
        
        let html = '';
        comboData.groups.forEach((group, groupIdx) => {
            html += `<div class="mb-3">
                <label class="form-label fw-bold">${group.groupName}</label>
                <div>`;
            group.options.forEach((option, optIdx) => {
                const inputId = `group${group.configGroupID}-option${option.configOptionID}`;
                
                // Check if this option is currently selected in cart item
                const isSelected = this.isOptionSelected(cartItem, group.configGroupID, option.configOptionID);
                const checked = isSelected ? 'checked' : '';
                
                html += `
                    <input type="radio" class="btn-check option-radio" name="group${group.configGroupID}" id="${inputId}" 
                           data-price="${option.priceAdjustment || 0}" autocomplete="off" ${checked}
                           onchange="updateComboPrice()">
                    <label class="btn btn-outline-danger mb-2" for="${inputId}">
                        ${option.productImage ? `<img src="${option.productImage}" alt="${option.productName}" width="50">` : ''}
                        ${option.productName}
                        ${option.variantName ? `(${option.variantName})` : ''}
                        ${option.priceAdjustment && option.priceAdjustment > 0 ? `(+${option.priceAdjustment.toLocaleString()}đ)` : ''}
                    </label>`;
            });
            html += '</div></div>';
        });
        
        // Quantity
        html += `<div class="mb-3">
            <label class="form-label fw-bold">Số lượng</label>
            <div class="input-group" style="max-width: 150px;">
              <button class="btn btn-outline-secondary" type="button" id="qtyMinus" onclick="changeQuantity(-1)">-</button>
              <input type="number" class="form-control text-center" id="orderQty" value="${cartItem.Quantity}" min="1" onchange="updateComboPrice()">
              <button class="btn btn-outline-secondary" type="button" id="qtyPlus" onclick="changeQuantity(1)">+</button>
            </div>
          </div>`;
        
        form.innerHTML = html;
        
        // Calculate initial price
        if (typeof updateComboPrice === 'function') {
            updateComboPrice();
        }
    }

    isOptionSelected(cartItem, configGroupID, configOptionID) {
        // Check if the option is currently selected in cart item configurations
        for (const config of cartItem.Configurations) {
            for (const option of config.Options) {
                if (option.ConfigOptionID === configOptionID) {
                    return true;
                }
            }
        }
        return false;
    }

    async saveItemChanges() {
        try {
            if (!this.editingCartItemId || !this.editingCartItem) {
                console.error('❌ No item being edited');
                this.showNotification('error', 'Không có sản phẩm đang được chỉnh sửa');
                return;
            }

            // Get selected options from form
            const selectedOptions = [];
            const form = document.getElementById('orderOptionsForm');
            const radioGroups = form.querySelectorAll('input[type="radio"]:checked');
            
            radioGroups.forEach(radio => {
                const optionData = radio.id.match(/group(\d+)-option(\d+)/);
                if (optionData) {
                    const configGroupID = parseInt(optionData[1]);
                    const configOptionID = parseInt(optionData[2]);
                    selectedOptions.push({
                        ConfigGroupID: configGroupID,
                        ConfigOptionID: configOptionID,
                        OptionProductID: 0, // Will be populated by backend
                        VariantID: null // Will be populated by backend
                    });
                }
            });

            // Get quantity
            const quantity = parseInt(document.getElementById('orderQty').value) || 1;

            // Call API to update cart item
            const response = await fetch('/Cart/UpdateItemConfiguration', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify({
                    CartItemID: this.editingCartItemId,
                    Quantity: quantity,
                    SelectedOptions: selectedOptions
                })
            });

            const result = await response.json();
            
            if (result.success) {
                // Update cart data
                this.cartData = result.data;
                this.updateCartDisplay();
                this.updateCartBadge();
                
                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('orderOptionsModal'));
                if (modal) {
                    modal.hide();
                }
                
                // Reset editing state
                this.editingCartItemId = null;
                this.editingCartItem = null;
                
                this.showNotification('success', 'Đã cập nhật sản phẩm thành công!');
            } else {
                console.error('❌ Failed to update cart item:', result.message);
                this.showNotification('error', result.message || 'Không thể cập nhật sản phẩm');
            }
        } catch (error) {
            console.error('❌ Error saving item changes:', error);
            this.showNotification('error', 'Có lỗi xảy ra khi lưu thay đổi');
        }
    }

    updateCartDisplay() {
        console.log('📱 Client: updateCartDisplay called');
        const cartContent = document.getElementById('cartContent');
        const cartSummary = document.getElementById('cartSummary');
        
        console.log('📱 Client: Cart data for display:', this.cartData);
        console.log('📱 Client: Cart items for display:', this.cartData?.CartItems);
        
        if (!this.cartData || !this.cartData.CartItems || this.cartData.CartItems.length === 0) {
            console.log('📱 Client: Showing empty cart');
            cartContent.innerHTML = this.getEmptyCartHTML();
            cartSummary.style.display = 'none';
            return;
        }

        console.log('📱 Client: Showing cart with items:', this.cartData.CartItems.length);
        cartContent.innerHTML = this.getCartItemsHTML();
        cartSummary.style.display = 'block';
        
        // Update summary
        document.getElementById('cartSubTotal').textContent = this.formatCurrency(this.cartData.TotalAmount);
        document.getElementById('cartTotal').textContent = this.formatCurrency(this.cartData.FinalAmount);
        
        // Bind item events
        this.bindCartItemEvents();
    }

    getEmptyCartHTML() {
        return `
            <div class="cart-empty">
                <div class="cart-empty-icon">
                    <i class="fas fa-shopping-cart"></i>
                </div>
                <h4>Giỏ hàng trống</h4>
                <p>Hãy thêm sản phẩm vào giỏ hàng để bắt đầu mua sắm!</p>
                <button class="cart-continue-shopping-btn" onclick="window.jollibeeCart.closeCart(); window.location.href='/Menu/MonNgonPhaiThu';">
                    <i class="fas fa-arrow-left"></i> Tiếp tục mua sắm
                </button>
            </div>
        `;
    }

    getCartItemsHTML() {
        return this.cartData.CartItems.map(item => this.getCartItemHTML(item)).join('');
    }

    getCartItemHTML(item) {
        const configurationsHTML = item.Configurations.map(config => {
            const optionsHTML = config.Options.map(option => `
                <div class="cart-option-item">
                    ${option.OptionProductImage ? `<img src="${option.OptionProductImage}" alt="${option.OptionProductName}" class="cart-option-image">` : ''}
                    <span class="cart-option-quantity">${option.Quantity || 1} x</span>
                    <span class="cart-option-name">${option.OptionProductName} ${option.VariantName ? `(${option.VariantName})` : ''}</span>
                    ${option.PriceAdjustment > 0 ? `<span class="cart-option-price">+${this.formatCurrency(option.PriceAdjustment)}</span>` : ''}
                </div>
            `).join('');
            
            return `
                <div class="cart-option-group">
                    <div class="cart-option-label">${config.GroupName}:</div>
                    ${optionsHTML}
                </div>
            `;
        }).join('');

        return `
            <div class="cart-item" data-item-id="${item.CartItemID}">
                <div class="cart-item-actions">
                    ${item.IsConfigurable ? `
                        <button class="cart-item-edit" onclick="window.jollibeeCart.editItem(${item.CartItemID})" title="Chỉnh sửa combo">
                            <i class="fas fa-edit"></i>
                        </button>
                    ` : ''}
                    <button class="cart-item-remove" onclick="window.jollibeeCart.removeItem(${item.CartItemID})">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
                
                <div class="cart-item-header">
                    <img src="${item.ProductImage || '/assets/images/default-product.png'}" 
                         alt="${item.ProductName}" class="cart-item-image">
                    <div class="cart-item-info">
                        <h4 class="cart-item-name">${item.ProductName}</h4>
                        <div class="cart-item-price">${this.formatCurrency(item.TotalPrice)}</div>
                    </div>
                </div>
                
                ${configurationsHTML ? `<div class="cart-item-options">${configurationsHTML}</div>` : ''}
                
                <div class="cart-quantity-controls">
                    <button class="cart-qty-btn" onclick="window.jollibeeCart.decreaseQuantity(${item.CartItemID}, ${item.Quantity})" 
                            ${item.Quantity <= 1 ? 'disabled' : ''}>-</button>
                    <input type="number" class="cart-qty-input" value="${item.Quantity}" 
                           onchange="window.jollibeeCart.updateQuantity(${item.CartItemID}, this.value)"
                           min="1" max="99">
                    <button class="cart-qty-btn" onclick="window.jollibeeCart.increaseQuantity(${item.CartItemID}, ${item.Quantity})">+</button>
                </div>
            </div>
        `;
    }

    bindCartItemEvents() {
        // Quantity input events are handled inline
        // Additional events can be bound here if needed
    }

    async increaseQuantity(cartItemId, currentQuantity) {
        await this.updateQuantity(cartItemId, currentQuantity + 1);
    }

    async decreaseQuantity(cartItemId, currentQuantity) {
        if (currentQuantity > 1) {
            await this.updateQuantity(cartItemId, currentQuantity - 1);
        }
    }

    updateCartBadge() {
        const badge = document.getElementById('cartBadge');
        const totalItems = this.cartData ? this.cartData.TotalItems : 0;
        
        badge.textContent = totalItems;
        badge.style.display = totalItems > 0 ? 'flex' : 'none';
    }

    openCart() {
        document.getElementById('cartPanel').classList.add('open');
        document.getElementById('cartOverlay').classList.add('show');
        document.body.style.overflow = 'hidden';
    }

    closeCart() {
        document.getElementById('cartPanel').classList.remove('open');
        document.getElementById('cartOverlay').classList.remove('show');
        document.body.style.overflow = '';
    }

    showLoading() {
        document.getElementById('cartLoading').style.display = 'flex';
    }

    hideLoading() {
        document.getElementById('cartLoading').style.display = 'none';
    }

    showSuccessAnimation() {
        const animation = document.getElementById('cartSuccessAnimation');
        animation.classList.add('show');
        
        setTimeout(() => {
            animation.classList.remove('show');
        }, 2000);
    }

    showNotification(type, message) {
        // Show simple alert for now - you can implement toast later
        if (type === 'error') {
            console.error(message);
            alert('❌ ' + message);
        } else {
            console.log(message);
            // Success messages are shown via animation
        }
    }

    showError(message) {
        const cartContent = document.getElementById('cartContent');
        cartContent.innerHTML = `
            <div class="cart-empty">
                <div class="cart-empty-icon">
                    <i class="fas fa-exclamation-triangle" style="color: #ff4757;"></i>
                </div>
                <h4 style="color: #ff4757;">Có lỗi xảy ra</h4>
                <p>${message}</p>
                <div class="cart-error-actions">
                    <button class="btn btn-primary" onclick="window.jollibeeCart.loadCart()">
                        <i class="fas fa-redo"></i> Thử lại
                    </button>
                    <button class="cart-continue-shopping-btn" onclick="window.jollibeeCart.closeCart(); window.location.href='/Menu/MonNgonPhaiThu';">
                        <i class="fas fa-arrow-left"></i> Tiếp tục mua sắm
                    </button>
                </div>
            </div>
        `;
    }

    async proceedToCheckout() {
        if (!this.cartData || !this.cartData.CartItems || this.cartData.CartItems.length === 0) {
            this.showNotification('error', 'Giỏ hàng trống');
            return;
        }
        
        // Check if user is logged in
        const isLoggedIn = await this.checkUserAuthentication();
        
        if (isLoggedIn) {
            // User is logged in, proceed to shipping
            window.location.href = '/Cart/Shipping';
        } else {
            // User is not logged in, show login modal
            this.showLoginModal();
        }
    }

    async checkUserAuthentication() {
        try {
            const response = await fetch('/Account/CheckAuthenticationStatus', {
                method: 'GET',
                credentials: 'same-origin'
            });
            
            if (response.ok) {
                const data = await response.json();
                return data.isAuthenticated === true;
            }
            
            return false;
        } catch (error) {
            console.error('❌ Error checking authentication:', error);
            return false;
        }
    }

    showLoginModal() {
        // Close cart panel first
        this.closeCart();
        
        // Show login requirement modal
        const existingModal = document.getElementById('loginRequiredModal');
        if (existingModal) {
            existingModal.remove();
        }
        
        // Create login modal
        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'loginRequiredModal';
        modal.setAttribute('tabindex', '-1');
        modal.setAttribute('aria-labelledby', 'loginRequiredModalLabel');
        modal.setAttribute('aria-hidden', 'true');
        modal.innerHTML = `
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header bg-danger text-white">
                        <h5 class="modal-title" id="loginRequiredModalLabel">
                            <i class=""></i> Yêu cầu đăng nhập
                        </h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body text-center py-4">
                        <div class="mb-3">
                            <i class="fas fa-user-lock text-danger" style="font-size: 3rem;"></i>
                        </div>
                        <h6 class="mb-3">Bạn cần đăng nhập để tiếp tục thanh toán</h6>
                        <p class="text-muted mb-4">
                            Để đảm bảo bảo mật và có được trải nghiệm tốt nhất, vui lòng đăng nhập vào tài khoản của bạn trước khi thanh toán.
                        </p>
                        <div class="d-grid gap-2">
                            <a href="/Account/Login?returnUrl=%2FCart%2FShipping" class="btn btn-danger btn-lg">
                                <i class=""></i> Đăng nhập
                            </a>
                            <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal" onclick="window.location.href='/Menu/MonNgonPhaiThu';">
                                <i class="fas fa-arrow-left me-2"></i>Tiếp tục mua sắm
                            </button>
                        </div>
                        <hr class="my-3">
                        <p class="mb-0">
                            <small class="text-muted">
                                Chưa có tài khoản? 
                                <a href="/Account/Register" class="text-decoration-none">
                                    <strong>Đăng ký tại đây</strong>
                                </a>
                            </small>
                        </p>
                    </div>
                </div>
            </div>
        `;
        
        document.body.appendChild(modal);
        
        // Show modal using Bootstrap
        const bootstrapModal = new bootstrap.Modal(modal);
        bootstrapModal.show();
        
        // Clean up modal when hidden
        modal.addEventListener('hidden.bs.modal', () => {
            modal.remove();
        });
    }

    formatCurrency(amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount).replace('₫', 'đ');
    }

    getSessionId() {
        // Use ASP.NET Core session ID from cookie
        const sessionCookie = document.cookie
            .split('; ')
            .find(row => row.startsWith('.AspNetCore.Session='));
        
        if (sessionCookie) {
            return sessionCookie.split('=')[1];
        }
        
        // Fallback: Generate or get session ID
        let sessionId = sessionStorage.getItem('jollibee_session_id');
        if (!sessionId) {
            sessionId = 'sess_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
            sessionStorage.setItem('jollibee_session_id', sessionId);
        }
        return sessionId;
    }

    getAntiForgeryToken() {
        // Get anti-forgery token from meta tag or hidden input
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
                     document.querySelector('meta[name="__RequestVerificationToken"]')?.content;
        return token || '';
    }

    // Public API methods
    getCartData() {
        return this.cartData;
    }

    getCartItemCount() {
        return this.cartData ? this.cartData.TotalItems : 0;
    }

    getCartTotal() {
        return this.cartData ? this.cartData.FinalAmount : 0;
    }
}

// ==================================================
// GLOBAL CART INITIALIZATION
// ==================================================

// Initialize cart when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Initialize global cart instance
    window.jollibeeCart = new JollibeeCart();
});

// Export for use in other scripts
window.JollibeeCart = JollibeeCart; 