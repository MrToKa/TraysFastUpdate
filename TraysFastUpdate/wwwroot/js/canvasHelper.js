// Canvas Helper Functions for Blazor Canvas Export

window.canvasHelper = {
    // Check if canvas element exists and is properly rendered
    validateCanvas: function(canvasId) {
        try {
            const canvas = document.getElementById(canvasId);
            if (!canvas) {
                console.log(`Canvas with ID ${canvasId} not found`);
                return false;
            }
            
            if (canvas.tagName !== 'CANVAS') {
                console.log(`Element ${canvasId} is not a canvas element`);
                return false;
            }
            
            if (canvas.width === 0 || canvas.height === 0) {
                console.log(`Canvas ${canvasId} has zero dimensions: ${canvas.width}x${canvas.height}`);
                return false;
            }
            
            console.log(`Canvas ${canvasId} validation successful: ${canvas.width}x${canvas.height}`);
            return true;
        } catch (error) {
            console.error(`Canvas validation error: ${error.message}`);
            return false;
        }
    },
    
    // Export canvas to data URL with better error handling
    exportCanvas: function(canvasId, format = 'image/jpeg', quality = 0.9) {
        try {
            const canvas = document.getElementById(canvasId);
            if (!canvas) {
                throw new Error(`Canvas with ID ${canvasId} not found`);
            }
            
            if (canvas.tagName !== 'CANVAS') {
                throw new Error(`Element ${canvasId} is not a canvas`);
            }
            
            // Check canvas dimensions
            if (canvas.width === 0 || canvas.height === 0) {
                throw new Error(`Canvas has invalid dimensions: ${canvas.width}x${canvas.height}`);
            }
            
            console.log(`Exporting canvas ${canvasId} (${canvas.width}x${canvas.height}) as ${format}`);
            
            const dataUrl = canvas.toDataURL(format, quality);
            
            if (!dataUrl || !dataUrl.includes('data:')) {
                throw new Error('Failed to generate data URL from canvas');
            }
            
            console.log(`Canvas export successful, data URL length: ${dataUrl.length}`);
            return dataUrl;
            
        } catch (error) {
            console.error(`Canvas export error: ${error.message}`);
            throw error;
        }
    },
    
    // Get canvas information for debugging
    getCanvasInfo: function(canvasId) {
        try {
            const canvas = document.getElementById(canvasId);
            if (!canvas) {
                return { exists: false, error: 'Canvas not found' };
            }
            
            return {
                exists: true,
                isCanvas: canvas.tagName === 'CANVAS',
                width: canvas.width,
                height: canvas.height,
                clientWidth: canvas.clientWidth,
                clientHeight: canvas.clientHeight,
                style: canvas.style.cssText
            };
        } catch (error) {
            return { exists: false, error: error.message };
        }
    }
};