// wwwroot/js/app.js
window.svgHelper = {
    getSvgDimensions: function (elementId) {
        const svgElement = document.getElementById(elementId);
        if (svgElement) {
            const rect = svgElement.getBoundingClientRect();
            const dimensions = {
                width: rect.width,
                height: rect.height
            };
            return dimensions;
        } else {
            console.warn(`Element with ID "${elementId}" not found.`);
        }
        return null;
    }
};