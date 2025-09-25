let keyboardNavigationSetup = false;
let homeComponentRef = null;

window.setupKeyboardNavigation = function(dotNetObjectReference) {
    homeComponentRef = dotNetObjectReference;
    if (!keyboardNavigationSetup) {
        document.addEventListener('keydown', handleKeyDown);
        keyboardNavigationSetup = true;
    }
};

window.cleanupKeyboardNavigation = function() {
    if (keyboardNavigationSetup) {
        document.removeEventListener('keydown', handleKeyDown);
        keyboardNavigationSetup = false;
        homeComponentRef = null;
    }
};

function handleKeyDown(event) {
    if (document.activeElement.tagName === 'INPUT' || 
        document.activeElement.tagName === 'TEXTAREA' || 
        document.activeElement.contentEditable === 'true') {
        return;
    }

    if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
        event.preventDefault();
        if (homeComponentRef) {
            homeComponentRef.invokeMethodAsync('HandleKeyPress', event.key);
        }
    }
}

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
