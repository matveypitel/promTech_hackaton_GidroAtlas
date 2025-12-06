// File download utility for Blazor
window.downloadFile = function (fileName, contentType, base64Content) {
    try {
        // Decode base64 to binary
        const byteCharacters = atob(base64Content);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);

        // Create blob
        const blob = new Blob([byteArray], { type: contentType });

        // Create download link
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;

        // Trigger download
        document.body.appendChild(link);
        link.click();

        // Cleanup
        document.body.removeChild(link);
        URL.revokeObjectURL(url);

        console.log('File downloaded:', fileName);
    } catch (error) {
        console.error('Error downloading file:', error);
        throw error;
    }
};

// Open file in new tab (alternative for PDF viewing)
window.openFileInNewTab = function (contentType, base64Content) {
    try {
        const byteCharacters = atob(base64Content);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: contentType });
        const url = URL.createObjectURL(blob);

        window.open(url, '_blank');

        console.log('File opened in new tab');
    } catch (error) {
        console.error('Error opening file:', error);
        throw error;
    }
};
