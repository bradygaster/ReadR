let keyboardNavigationSetup = false;
let homeComponentRef = null;
let hasUsedKeyboard = false;

// Grid navigation state
let selectedCardIndex = 0;
let totalCards = 0;
let gridColumns = 1; // Will be updated dynamically based on window size

window.setupKeyboardNavigation = function(dotNetObjectReference) {
    homeComponentRef = dotNetObjectReference;
    if (!keyboardNavigationSetup) {
        document.addEventListener('keydown', handleKeyDown);
        window.addEventListener('resize', updateGridLayout);
        keyboardNavigationSetup = true;
    }
    // Initialize grid layout
    updateGridLayout();
    updateCardSelection();
    
    // Ensure toast container exists
    ensureToastContainer();
};

window.cleanupKeyboardNavigation = function() {
    if (keyboardNavigationSetup) {
        document.removeEventListener('keydown', handleKeyDown);
        window.removeEventListener('resize', updateGridLayout);
        keyboardNavigationSetup = false;
        homeComponentRef = null;
        hasUsedKeyboard = false;
    }
};

// Check if click was on overlay (outside of modal dialog)
window.isOverlayClick = function(clientX, clientY) {
    const modalDialog = document.querySelector('.modal-dialog');
    if (!modalDialog) {
        return true; // If no modal dialog found, treat as overlay click
    }
    
    const rect = modalDialog.getBoundingClientRect();
    return clientX < rect.left || clientX > rect.right || clientY < rect.top || clientY > rect.bottom;
};

// Toast notification functionality
function ensureToastContainer() {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }
    return container;
}

window.showToast = function(type, title, message) {
    const container = ensureToastContainer();
    
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    
    const icon = getToastIcon(type);
    
    toast.innerHTML = `
        <div class="toast-content">
            <div class="toast-icon">${icon}</div>
            <div class="toast-text">
                <div class="toast-title">${title}</div>
                ${message ? `<div class="toast-message">${message}</div>` : ''}
            </div>
            <button class="toast-close" onclick="closeToast(this.parentElement)">×</button>
        </div>
    `;
    
    container.appendChild(toast);
    
    // Trigger animation
    setTimeout(() => toast.classList.add('show'), 10);
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => closeToast(toast), 5000);
};

function getToastIcon(type) {
    switch (type) {
        case 'success': return '✅';
        case 'error': return '❌';
        case 'warning': return '⚠️';
        case 'info': return 'ℹ️';
        default: return 'ℹ️';
    }
}

window.closeToast = function(toast) {
    toast.classList.add('hiding');
    setTimeout(() => {
        if (toast.parentNode) {
            toast.parentNode.removeChild(toast);
        }
    }, 300);
};

function updateGridLayout() {
    const feedsGrid = document.querySelector('.feeds-grid');
    if (!feedsGrid) return;
    
    const cards = feedsGrid.querySelectorAll('.feed-card');
    totalCards = cards.length;
    
    if (totalCards === 0) return;
    
    // Calculate grid columns based on viewport width
    const viewportWidth = window.innerWidth;
    if (viewportWidth >= 1200) {
        gridColumns = Math.min(3, totalCards);
    } else if (viewportWidth >= 768) {
        gridColumns = Math.min(2, totalCards);
    } else {
        gridColumns = 1;
    }
    
    // Ensure selected index is within bounds
    if (selectedCardIndex >= totalCards) {
        selectedCardIndex = totalCards - 1;
    } else if (selectedCardIndex < 0) {
        selectedCardIndex = 0;
    }
}

function updateCardSelection() {
    const feedsGrid = document.querySelector('.feeds-grid');
    if (!feedsGrid) return;
    
    const cards = feedsGrid.querySelectorAll('.feed-card');
    
    // Remove previous selection
    cards.forEach(card => card.classList.remove('selected'));
    
    // Always show the first card as selected to indicate keyboard navigation is available
    if (cards[selectedCardIndex]) {
        cards[selectedCardIndex].classList.add('selected');
        
        // Only scroll into view if keyboard has been used
        if (hasUsedKeyboard) {
            cards[selectedCardIndex].scrollIntoView({ 
                behavior: 'smooth', 
                block: 'nearest' 
            });
        }
    }
}

function handleKeyDown(event) {
    // Don't intercept if user is typing in an input
    if (document.activeElement.tagName === 'INPUT' || 
        document.activeElement.tagName === 'TEXTAREA' || 
        document.activeElement.contentEditable === 'true') {
        return;
    }

    // Handle ASDF navigation and arrow keys
    let handled = false;
    let isNavigationKey = false;
    
    switch (event.key.toLowerCase()) {
        case 'a': // Move left
        case 'arrowleft':
            event.preventDefault();
            handled = true;
            isNavigationKey = true;
            if (selectedCardIndex % gridColumns === 0) {
                // At leftmost column, try to go to previous page
                if (homeComponentRef) {
                    homeComponentRef.invokeMethodAsync('HandleKeyPress', 'NavigatePrevious');
                }
            } else {
                // Move to the card on the left
                selectedCardIndex = Math.max(0, selectedCardIndex - 1);
                updateCardSelection();
            }
            break;
            
        case 'd': // Move right
        case 'arrowright':
            event.preventDefault();
            handled = true;
            isNavigationKey = true;
            if ((selectedCardIndex + 1) % gridColumns === 0 || selectedCardIndex === totalCards - 1) {
                // At rightmost column, try to go to next page
                if (homeComponentRef) {
                    homeComponentRef.invokeMethodAsync('HandleKeyPress', 'NavigateNext');
                }
            } else {
                // Move to the card on the right
                selectedCardIndex = Math.min(totalCards - 1, selectedCardIndex + 1);
                updateCardSelection();
            }
            break;
            
        case 's': // Move down
        case 'arrowdown':
            event.preventDefault();
            handled = true;
            isNavigationKey = true;
            const newDownIndex = selectedCardIndex + gridColumns;
            if (newDownIndex < totalCards) {
                selectedCardIndex = newDownIndex;
                updateCardSelection();
            }
            break;
            
        case 'w': // Move up
        case 'arrowup':
            event.preventDefault();
            handled = true;
            isNavigationKey = true;
            const newUpIndex = selectedCardIndex - gridColumns;
            if (newUpIndex >= 0) {
                selectedCardIndex = newUpIndex;
                updateCardSelection();
            }
            break;
            
        case 'enter': // Open selected card
        case ' ': // Space to open selected card
            event.preventDefault();
            handled = true;
            isNavigationKey = true;
            const feedsGrid = document.querySelector('.feeds-grid');
            if (feedsGrid) {
                const cards = feedsGrid.querySelectorAll('.feed-card');
                const selectedCard = cards[selectedCardIndex];
                if (selectedCard) {
                    const link = selectedCard.querySelector('.card-title a');
                    if (link) {
                        link.click();
                    }
                }
            }
            break;
            
        case '?': // Show keyboard help
            event.preventDefault();
            handled = true;
            if (homeComponentRef) {
                homeComponentRef.invokeMethodAsync('HandleKeyPress', 'ShowHelp');
            }
            break;
            
        case 'escape':
            if (homeComponentRef) {
                homeComponentRef.invokeMethodAsync('HandleKeyPress', 'Escape');
            }
            break;
    }
    
    // Track if keyboard navigation has been used for enhanced features
    if (isNavigationKey && !hasUsedKeyboard) {
        hasUsedKeyboard = true;
        
        // Add a subtle animation to indicate keyboard mode is active
        const entryCounter = document.querySelector('.entry-counter');
        if (entryCounter) {
            entryCounter.classList.add('keyboard-active');
        }
    }
    
    if (handled && homeComponentRef) {
        // Update the component with current selection info
        homeComponentRef.invokeMethodAsync('UpdateSelection', selectedCardIndex, totalCards);
    }
}

// Update selection when cards are refreshed (new page loaded)
window.refreshCardSelection = function() {
    selectedCardIndex = 0; // Reset to first card on new page
    updateGridLayout();
    updateCardSelection();
};

// Set specific card as selected (useful for maintaining state)
window.setSelectedCard = function(index) {
    selectedCardIndex = Math.max(0, Math.min(index, totalCards - 1));
    updateCardSelection();
};

window.handleFaviconError = function(img) {
    const domain = img.dataset.domain;
    const fallbackIcon = img.dataset.fallbackIcon || '🌐';
    const triedUrls = img.dataset.tried ? img.dataset.tried.split(',') : [];
    const currentSrc = img.src;

    if (!triedUrls.includes(currentSrc)) {
        triedUrls.push(currentSrc);
        img.dataset.tried = triedUrls.join(',');
    }

    if (!triedUrls.includes(`https://${domain}/favicon.ico`)) {
        img.src = `https://${domain}/favicon.ico`;
    } else {
        img.style.display = 'none';
        const emojiSpan = img.nextElementSibling;
        if (emojiSpan && emojiSpan.classList.contains('site-icon')) {
            emojiSpan.style.display = 'inline';
        }
    }
};
