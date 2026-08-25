mergeInto(LibraryManager.library, {
    IsMobileDevice: function() {
        if (/Mobi|Android|iPhone|iPad|iPod/i.test(navigator.userAgent)) {
            return 1; // Mobile detected
        }
        return 0; // Desktop detected
    }
});