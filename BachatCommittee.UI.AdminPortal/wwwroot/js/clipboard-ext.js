function copyToClipboard(text) {
    // Preferred: Native Clipboard API
    if (navigator.clipboard && window.isSecureContext) {
        return navigator.clipboard.writeText(text);
    }

    // Fallback: clipboard.js (already downloaded)
    return new Promise((resolve, reject) => {
        const tempInput = document.createElement("input");
        tempInput.value = text;
        document.body.appendChild(tempInput);
        tempInput.select();

        try {
            document.execCommand("copy");
            resolve();
        } catch (err) {
            reject(err);
        } finally {
            document.body.removeChild(tempInput);
        }
    });
}
