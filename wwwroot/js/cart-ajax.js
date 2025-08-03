// Cart AJAX functionality
class CartManager {
    constructor() {
        this.init();
    }

    init() {
        console.log('CartManager initialized');
        // Bind events for quantity controls
        this.bindQuantityControls();
        
        // Bind events for add to cart buttons
        this.bindAddToCartButtons();
        
        // Bind events for remove from cart buttons
        this.bindRemoveFromCartButtons();
    }

    bindQuantityControls() {
        // Use event delegation to handle dynamically loaded elements
        document.addEventListener('click', (e) => {
            // Handle plus button
            if (e.target.closest('.qty-right-plus')) {
                e.preventDefault();
                e.stopPropagation();
                const button = e.target.closest('.qty-right-plus');
                const input = button.parentElement.querySelector('.qty-input');
                if (input) {
                    let value = parseInt(input.value) || 1;
                    const max = parseInt(input.getAttribute('max')) || 999;
                    if (value < max) {
                        input.value = value + 1;
                        console.log('Plus clicked, new value:', input.value);
                    }
                }
            }
            
            // Handle minus button
            if (e.target.closest('.qty-left-minus')) {
                e.preventDefault();
                e.stopPropagation();
                const button = e.target.closest('.qty-left-minus');
                const input = button.parentElement.querySelector('.qty-input');
                if (input) {
                    let value = parseInt(input.value) || 1;
                    const min = parseInt(input.getAttribute('min')) || 1;
                    if (value > min) {
                        input.value = value - 1;
                        console.log('Minus clicked, new value:', input.value);
                    }
                }
            }
        });
    }

    bindAddToCartButtons() {
        // Use event delegation for add to cart buttons
        document.addEventListener('click', (e) => {
            // Only handle if it's an add to cart button (has data-product-id but not remove-from-cart class)
            const button = e.target.closest('[data-product-id]');
            if (button && !button.classList.contains('remove-from-cart') && button.hasAttribute('data-product-name')) {
                console.log('Add to cart button clicked:', button);
                e.preventDefault();
                this.addToCart(button);
            }
        });
    }

    bindRemoveFromCartButtons() {
        // Use event delegation for remove from cart buttons
        document.addEventListener('click', (e) => {
            // Only handle if it's a remove from cart button
            const button = e.target.closest('.remove-from-cart');
            if (button) {
                console.log('Remove from cart button clicked:', button);
                e.preventDefault();
                e.stopPropagation();
                const productId = button.getAttribute('data-product-id');
                if (productId) {
                    this.removeFromCart(productId);
                }
            }
        });
    }

    addToCart(button) {
        const productId = button.getAttribute('data-product-id');
        const productName = button.getAttribute('data-product-name') || 'sản phẩm';
        
        console.log('Adding to cart - ProductId:', productId, 'ProductName:', productName);
        
        // Get quantity from input - try multiple selectors
        let quantity = 1;
        const qtyInput = button.closest('form')?.querySelector('input[name="quantity"]') || 
                        button.closest('.product-buttons')?.querySelector('#quantity-input') ||
                        button.closest('.note-box')?.querySelector('.qty-input') ||
                        button.closest('.input-group')?.querySelector('.qty-input') ||
                        button.closest('.d-flex')?.querySelector('.qty-input') ||
                        button.parentElement?.querySelector('.qty-input');
        
        if (qtyInput) {
            quantity = parseInt(qtyInput.value) || 1;
            console.log('Found quantity input:', qtyInput.value);
        } else {
            console.log('No quantity input found, using default: 1');
        }
        
        if (quantity < 1) {
            this.showNotification('Số lượng phải lớn hơn 0!', 'error');
            return;
        }

        console.log('Adding to cart:', productId, 'quantity:', quantity);

        // Disable button to prevent double click
        button.disabled = true;
        const originalText = button.innerHTML;
        button.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i> Đang thêm...';

        // Call API
        fetch('/api/shopapi/add-to-cart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                productId: parseInt(productId),
                quantity: quantity
            })
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Update cart badge
                this.updateCartBadge(data.count);
                
                // Update cart popup
                this.updateCartPopup(data.cartItems, data.total);
                
                // Show success notification
                this.showNotification('Đã thêm ' + productName + ' vào giỏ hàng!', 'success');
                
                // Reset button
                button.disabled = false;
                button.innerHTML = originalText;
            } else {
                throw new Error(data.message || 'Có lỗi xảy ra!');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            this.showNotification('Có lỗi xảy ra khi thêm vào giỏ hàng!', 'error');
            
            // Reset button
            button.disabled = false;
            button.innerHTML = originalText;
        });
    }

    removeFromCart(productId) {
        console.log('Removing from cart:', productId);

        // Call API
        fetch(`/api/shopapi/remove-from-cart/${productId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
            }
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Update cart badge
                this.updateCartBadge(data.count);
                
                // Update cart popup
                this.updateCartPopup(data.cartItems, data.total);
                
                // Show success notification
                this.showNotification('Đã xóa sản phẩm khỏi giỏ hàng!', 'success');
                
                // If we're on the cart page, update the display directly
                if (window.location.pathname === '/shop/cart') {
                    this.updateCartPage(data.cartItems, data.total);
                }
            } else {
                throw new Error(data.message || 'Có lỗi xảy ra!');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            this.showNotification('Có lỗi xảy ra khi xóa khỏi giỏ hàng!', 'error');
        });
    }

    updateCartBadge(count) {
        const badge = document.querySelector('.header-wishlist .badge');
        if (badge) {
            badge.textContent = count;
            if (count > 0) {
                badge.style.display = 'block';
            } else {
                badge.style.display = 'none';
            }
        }
    }

    updateCartPopup(cartItems, total) {
        const cartList = document.querySelector('.cart-list');
        const totalElement = document.querySelector('.price-box h4');
        
        if (cartList && totalElement) {
            if (cartItems && cartItems.length > 0) {
                let cartHtml = '';
                cartItems.forEach(item => {
                    cartHtml += `
                        <li class="product-box-contain">
                            <div class="drop-cart d-flex">
                                <a href="/shop/detail/${item.productId}" class="drop-image">
                                    <img src="${item.image}" class="blur-up lazyloaded" alt="${item.name}" />
                                </a>
                                <div class="drop-contain w-100">
                                    <div class="d-flex justify-content-between align-items-start w-100">
                                        <div class="flex-grow-1 me-3">
                                            <a href="/shop/detail/${item.productId}" class="text-decoration-none text-dark">
                                                <h5 class="mb-1">${item.name}</h5>
                                            </a>
                                            <h6 class="mb-0"><span>${item.quantity} x</span> ₫${item.price.toLocaleString()}</h6>
                                        </div>
                                        <div style="flex-shrink: 0;">
                                            <a href="#" class="text-dark remove-from-cart" data-product-id="${item.productId}">
                                                <i class="fa-solid fa-xmark"></i>
                                            </a>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </li>
                    `;
                });
                cartList.innerHTML = cartHtml;
            } else {
                cartList.innerHTML = '<li class="text-center p-2">Giỏ hàng của bạn trống.</li>';
            }
            
            // Update total
            if (totalElement) {
                totalElement.textContent = '₫' + total.toLocaleString();
            }
        }
    }

    updateCartPage(cartItems, total) {
        const cartTable = document.querySelector('.cart-table tbody');
        const subtotalElement = document.getElementById('subtotalAmount');
        const finalAmountElement = document.getElementById('finalAmount');
        
        if (cartTable) {
            if (cartItems && cartItems.length > 0) {
                // If cart still has items, reload to recalculate discounts properly
                // This ensures discount codes are preserved and recalculated correctly
                setTimeout(() => {
                    window.location.reload();
                }, 500);
            } else {
                // Cart is empty, show empty message and clear discount
                cartTable.innerHTML = `
                    <tr>
                        <td colspan="5" class="text-center p-4">
                            <h4 class="text-muted">Giỏ hàng của bạn trống</h4>
                        </td>
                    </tr>
                `;
                
                // Update totals
                if (subtotalElement) subtotalElement.textContent = '0';
                if (finalAmountElement) finalAmountElement.textContent = '0';
                
                // Clear discount code if cart is empty
                this.clearDiscountCode();
            }
        }
    }

    clearDiscountCode() {
        // Remove discount code from session and update UI
        fetch('/api/shopapi/clear-discount', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            }
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Update discount display
                const discountElement = document.getElementById('discountAmount');
                if (discountElement) {
                    discountElement.textContent = '0';
                }
                
                // Update final amount
                const finalAmountElement = document.getElementById('finalAmount');
                if (finalAmountElement) {
                    finalAmountElement.textContent = '0';
                }
                
                // Clear coupon input if exists
                const couponInput = document.getElementById('couponCode');
                if (couponInput) {
                    couponInput.value = '';
                }
                
                console.log('Discount code cleared successfully');
            }
        })
        .catch(error => {
            console.error('Error clearing discount:', error);
        });
    }

    showNotification(message, type) {
        // Create notification
        const notification = document.createElement('div');
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 15px 20px;
            border-radius: 5px;
            color: white;
            font-weight: bold;
            z-index: 9999;
            animation: slideIn 0.3s ease;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        `;
        
        if (type === 'success') {
            notification.style.backgroundColor = '#4CAF50';
        } else {
            notification.style.backgroundColor = '#f44336';
        }
        
        notification.textContent = message;
        document.body.appendChild(notification);
        
        // Remove notification after 3 seconds
        setTimeout(() => {
            notification.style.animation = 'slideOut 0.3s ease';
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
            }, 300);
        }, 3000);
    }

    // Load cart info on page load
    loadCartInfo() {
        fetch('/api/shopapi/cart-info')
            .then(response => response.json())
            .then(data => {
                this.updateCartBadge(data.count);
                this.updateCartPopup(data.cartItems, data.total);
            })
            .catch(error => {
                console.error('Error loading cart info:', error);
            });
    }
}

// Initialize cart manager when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Add CSS for animations
    if (!document.querySelector('#cart-notification-styles')) {
        const style = document.createElement('style');
        style.id = 'cart-notification-styles';
        style.textContent = `
            @keyframes slideIn {
                from { transform: translateX(100%); opacity: 0; }
                to { transform: translateX(0); opacity: 1; }
            }
            @keyframes slideOut {
                from { transform: translateX(0); opacity: 1; }
                to { transform: translateX(100%); opacity: 0; }
            }
        `;
        document.head.appendChild(style);
    }

    // Initialize cart manager
    window.cartManager = new CartManager();
    
    // Load cart info on page load
    window.cartManager.loadCartInfo();
}); 