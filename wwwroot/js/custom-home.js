// Custom JavaScript for Home Page Improvements

document.addEventListener('DOMContentLoaded', function() {
    
    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Add loading animation to product boxes
    const productBoxes = document.querySelectorAll('.product-box');
    productBoxes.forEach(box => {
        box.classList.add('loading');
        
        // Remove loading class when image is loaded
        const img = box.querySelector('img');
        if (img) {
            img.addEventListener('load', function() {
                box.classList.remove('loading');
            });
            
            // Remove loading class if image fails to load
            img.addEventListener('error', function() {
                box.classList.remove('loading');
                // Set fallback image
                img.src = '/images/vegetable/product/4.png';
            });
        }
    });

    // Improve quantity buttons functionality - COMMENTED OUT TO AVOID CONFLICT
    /*
    const quantityButtons = document.querySelectorAll('.qty-left-minus, .qty-right-plus');
    quantityButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            
            const input = this.parentNode.querySelector('.qty-input');
            const currentValue = parseInt(input.value) || 0;
            
            if (this.classList.contains('qty-left-minus')) {
                if (currentValue > 0) {
                    input.value = currentValue - 1;
                }
            } else if (this.classList.contains('qty-right-plus')) {
                input.value = currentValue + 1;
            }
            
            // Trigger change event
            input.dispatchEvent(new Event('change'));
        });
    });
    */

    // Add to cart button functionality - COMMENTED OUT TO AVOID CONFLICT
    /*
    const addCartButtons = document.querySelectorAll('.addcart-button');
    addCartButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            
            const productBox = this.closest('.product-box');
            const productName = productBox.querySelector('.name')?.textContent || 'Product';
            const quantity = productBox.querySelector('.qty-input')?.value || 1;
            
            // Show success message
            showNotification(`Added ${quantity} ${productName} to cart!`, 'success');
            
            // Reset quantity to 0
            const qtyInput = productBox.querySelector('.qty-input');
            if (qtyInput) {
                qtyInput.value = 0;
            }
        });
    });
    */

    // Wishlist functionality
    const wishlistButtons = document.querySelectorAll('.notifi-wishlist');
    wishlistButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            
            const productBox = this.closest('.product-box');
            const productName = productBox.querySelector('.name')?.textContent || 'Product';
            
            // Toggle wishlist state
            this.classList.toggle('active');
            
            if (this.classList.contains('active')) {
                showNotification(`${productName} added to wishlist!`, 'success');
                this.style.color = '#ff6b6b';
            } else {
                showNotification(`${productName} removed from wishlist!`, 'info');
                this.style.color = '';
            }
        });
    });

    // Quick view modal improvements
    const quickViewButtons = document.querySelectorAll('[data-bs-target="#view"]');
    quickViewButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            
            const productBox = this.closest('.product-box');
            const productName = productBox.querySelector('.name')?.textContent || 'Product';
            const productPrice = productBox.querySelector('.price')?.textContent || '$0.00';
            const productImage = productBox.querySelector('img')?.src || '';
            
            // Update modal content
            updateQuickViewModal(productName, productPrice, productImage);
        });
    });

    // Smooth scroll for anchor links
    const anchorLinks = document.querySelectorAll('a[href^="#"]');
    anchorLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            const targetId = this.getAttribute('href');
            const targetElement = document.querySelector(targetId);
            
            if (targetElement) {
                targetElement.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Lazy loading improvements
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src || img.src;
                    img.classList.remove('lazyload');
                    observer.unobserve(img);
                }
            });
        });

        const lazyImages = document.querySelectorAll('img[data-src]');
        lazyImages.forEach(img => imageObserver.observe(img));
    }

    // Timer countdown improvements
    const timerElements = document.querySelectorAll('#clockdiv-1');
    timerElements.forEach(timer => {
        const hours = parseInt(timer.dataset.hours) || 1;
        const minutes = parseInt(timer.dataset.minutes) || 2;
        const seconds = parseInt(timer.dataset.seconds) || 3;
        
        let totalSeconds = hours * 3600 + minutes * 60 + seconds;
        
        const countdown = setInterval(() => {
            totalSeconds--;
            
            if (totalSeconds <= 0) {
                clearInterval(countdown);
                timer.innerHTML = '<div class="text-danger">Offer Expired!</div>';
                return;
            }
            
            const h = Math.floor(totalSeconds / 3600);
            const m = Math.floor((totalSeconds % 3600) / 60);
            const s = totalSeconds % 60;
            
            const timeElements = timer.querySelectorAll('h6');
            if (timeElements.length >= 3) {
                timeElements[1].textContent = h.toString().padStart(2, '0');
                timeElements[2].textContent = m.toString().padStart(2, '0');
                timeElements[3].textContent = s.toString().padStart(2, '0');
            }
        }, 1000);
    });

    // Responsive improvements
    function handleResize() {
        const productBoxes = document.querySelectorAll('.product-box');
        const isMobile = window.innerWidth <= 768;
        
        productBoxes.forEach(box => {
            if (isMobile) {
                box.style.marginBottom = '20px';
            } else {
                box.style.marginBottom = '';
            }
        });
    }

    window.addEventListener('resize', handleResize);
    handleResize(); // Initial call

    // Utility functions
    function showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
        notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        notification.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        document.body.appendChild(notification);
        
        // Auto remove after 3 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, 3000);
    }

    function updateQuickViewModal(name, price, image) {
        const modal = document.getElementById('view');
        if (modal) {
            const titleElement = modal.querySelector('.title-name');
            const priceElement = modal.querySelector('.price');
            const imageElement = modal.querySelector('.slider-image img');
            
            if (titleElement) titleElement.textContent = name;
            if (priceElement) priceElement.textContent = price;
            if (imageElement) imageElement.src = image;
        }
    }

    // Performance optimization: Debounce scroll events
    let scrollTimeout;
    window.addEventListener('scroll', function() {
        clearTimeout(scrollTimeout);
        scrollTimeout = setTimeout(() => {
            // Handle scroll-based animations here
            const scrolledElements = document.querySelectorAll('.product-box');
            scrolledElements.forEach(element => {
                const rect = element.getBoundingClientRect();
                if (rect.top < window.innerHeight && rect.bottom > 0) {
                    element.style.opacity = '1';
                }
            });
        }, 100);
    });

    console.log('Custom Home JavaScript loaded successfully!');
}); 