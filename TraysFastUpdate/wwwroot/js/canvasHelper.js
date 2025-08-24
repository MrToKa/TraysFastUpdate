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
    
    // Comprehensive canvas export with multiple quality levels (synchronous)
    exportCanvasComprehensive: function(canvasId, format = 'image/jpeg', rotate = false) {
        console.log(`Starting comprehensive canvas export for ${canvasId}${rotate ? ' with 90° CCW rotation' : ''}`);
        
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            throw new Error(`Canvas with ID ${canvasId} not found`);
        }
        
        if (canvas.tagName !== 'CANVAS') {
            throw new Error(`Element ${canvasId} is not a canvas`);
        }
        
        if (canvas.width === 0 || canvas.height === 0) {
            throw new Error(`Canvas has invalid dimensions: ${canvas.width}x${canvas.height}`);
        }
        
        // If no rotation is needed, use direct export
        if (!rotate) {
            return this.exportCanvasDirect(canvas, format);
        }
        
        // Create rotated canvas for export
        return this.exportCanvasRotated(canvas, format);
    },
    
    // Direct canvas export without rotation
    exportCanvasDirect: function(canvas, format = 'image/jpeg') {
        // Try different quality levels in order of preference
        const strategies = [
            { name: 'High Quality', quality: 0.95 },
            { name: 'Good Quality', quality: 0.9 },
            { name: 'Medium Quality', quality: 0.8 },
            { name: 'Standard Quality', quality: 0.7 },
            { name: 'Lower Quality', quality: 0.6 },
            { name: 'Backup Quality', quality: 0.5 }
        ];
        
        for (let strategy of strategies) {
            try {
                console.log(`Trying ${strategy.name} (${strategy.quality})`);
                const dataUrl = canvas.toDataURL(format, strategy.quality);
                
                if (dataUrl && dataUrl.includes('data:') && dataUrl.length > 1000) {
                    console.log(`Success with ${strategy.name}! Data length: ${dataUrl.length}`);
                    return dataUrl;
                }
            } catch (error) {
                console.log(`${strategy.name} failed: ${error.message}`);
                continue;
            }
        }
        
        throw new Error('All export strategies failed');
    },
    
    // Export canvas rotated 90 degrees counter-clockwise
    exportCanvasRotated: function(originalCanvas, format = 'image/jpeg') {
        try {
            console.log(`Creating rotated canvas for export...`);
            
            // Create a temporary canvas for rotation
            const rotatedCanvas = document.createElement('canvas');
            const rotatedCtx = rotatedCanvas.getContext('2d');
            
            // For 90° counter-clockwise rotation, new dimensions are swapped
            rotatedCanvas.width = originalCanvas.height;
            rotatedCanvas.height = originalCanvas.width;
            
            console.log(`Original canvas: ${originalCanvas.width}x${originalCanvas.height}`);
            console.log(`Rotated canvas: ${rotatedCanvas.width}x${rotatedCanvas.height}`);
            
            // Set high quality rendering
            rotatedCtx.imageSmoothingEnabled = true;
            rotatedCtx.imageSmoothingQuality = 'high';
            
            // Apply transformation for 90° counter-clockwise rotation
            rotatedCtx.translate(0, originalCanvas.width);
            rotatedCtx.rotate(-Math.PI / 2);
            
            // Draw the original canvas onto the rotated canvas
            rotatedCtx.drawImage(originalCanvas, 0, 0);
            
            // Export the rotated canvas with quality strategies
            const strategies = [
                { name: 'High Quality Rotated', quality: 0.95 },
                { name: 'Good Quality Rotated', quality: 0.9 },
                { name: 'Medium Quality Rotated', quality: 0.8 },
                { name: 'Standard Quality Rotated', quality: 0.7 },
                { name: 'Lower Quality Rotated', quality: 0.6 },
                { name: 'Backup Quality Rotated', quality: 0.5 }
            ];
            
            for (let strategy of strategies) {
                try {
                    console.log(`Trying ${strategy.name} (${strategy.quality})`);
                    const dataUrl = rotatedCanvas.toDataURL(format, strategy.quality);
                    
                    if (dataUrl && dataUrl.includes('data:') && dataUrl.length > 1000) {
                        console.log(`Success with ${strategy.name}! Data length: ${dataUrl.length}`);
                        return dataUrl;
                    }
                } catch (error) {
                    console.log(`${strategy.name} failed: ${error.message}`);
                    continue;
                }
            }
            
            throw new Error('All rotated export strategies failed');
            
        } catch (error) {
            console.error(`Rotated canvas export error: ${error.message}`);
            throw error;
        }
    },
    
    // Export canvas to file and save directly to server (primary method)
    exportCanvasToServer: function(canvasId, trayName, format = 'image/jpeg', quality = 0.9, rotate = false) {
        return new Promise((resolve, reject) => {
            try {
                console.log(`Exporting canvas ${canvasId} to server for tray ${trayName}${rotate ? ' with rotation' : ''}`);
                
                const canvas = document.getElementById(canvasId);
                if (!canvas) {
                    reject(new Error(`Canvas with ID ${canvasId} not found`));
                    return;
                }
                
                // Try synchronous export first
                let dataUrl = null;
                try {
                    dataUrl = this.exportCanvasComprehensive(canvasId, format, rotate);
                } catch (syncError) {
                    console.log(`Synchronous export failed: ${syncError.message}`);
                    reject(syncError);
                    return;
                }
                
                if (dataUrl) {
                    // Send to server via fetch
                    this.sendToServer(dataUrl, trayName, rotate)
                        .then(() => resolve({ success: true, method: 'synchronous', rotated: rotate }))
                        .catch(reject);
                } else {
                    reject(new Error('Failed to generate canvas data'));
                }
                
            } catch (error) {
                console.error(`Export to server error: ${error.message}`);
                reject(error);
            }
        });
    },
    
    // Send canvas data to server endpoint
    sendToServer: function(dataUrl, trayName, rotated = false) {
        return new Promise((resolve, reject) => {
            try {
                console.log(`Sending canvas data to server for tray ${trayName}${rotated ? ' (rotated)' : ''}`);
                
                // Extract base64 data
                const base64Data = dataUrl.split(',')[1];
                
                const payload = {
                    imageData: base64Data,
                    trayName: trayName,
                    format: 'jpeg',
                    rotated: rotated
                };
                
                fetch('/api/canvas/save-image', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(payload)
                })
                .then(response => {
                    if (!response.ok) {
                        throw new Error(`Server responded with status: ${response.status}`);
                    }
                    return response.json();
                })
                .then(result => {
                    console.log(`Server save successful: ${JSON.stringify(result)}`);
                    resolve(result);
                })
                .catch(error => {
                    console.error(`Server save failed: ${error.message}`);
                    reject(error);
                });
                
            } catch (error) {
                console.error(`Send to server error: ${error.message}`);
                reject(error);
            }
        });
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
                style: canvas.style.cssText,
                memoryUsage: canvas.width * canvas.height * 4,
                pixelRatio: window.devicePixelRatio || 1,
                isVisible: canvas.offsetParent !== null
            };
        } catch (error) {
            return { exists: false, error: error.message };
        }
    },
    
    // Force redraw canvas (sometimes helps with rendering issues)
    forceCanvasRedraw: function(canvasId) {
        try {
            const canvas = document.getElementById(canvasId);
            if (!canvas) {
                return false;
            }
            
            // Force repaint by temporarily changing a style
            const originalDisplay = canvas.style.display;
            canvas.style.display = 'none';
            canvas.offsetHeight; // Trigger reflow
            canvas.style.display = originalDisplay;
            
            console.log(`Forced redraw for canvas ${canvasId}`);
            return true;
        } catch (error) {
            console.error(`Force redraw error: ${error.message}`);
            return false;
        }
    }
};