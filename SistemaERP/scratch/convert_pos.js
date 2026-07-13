const fs = require('fs');
const vm = require('vm');

try {
    // Read the file as UTF-8 string
    const originalContent = fs.readFileSync('Views/Facturacion/POS.cshtml', 'utf8');
    
    // Convert to binary buffer and decode again as UTF-8 to resolve double UTF-8 encoding
    const binaryBuffer = Buffer.from(originalContent, 'binary');
    const convertedContent = binaryBuffer.toString('utf8');
    
    // Save to temp file
    fs.writeFileSync('Views/Facturacion/POS.cshtml.tmp', convertedContent, 'utf8');
    
    // Validate the temporary file
    const html = convertedContent;
    const scriptRegex = /<script>([\s\S]*?)<\/script>/gi;
    let match;
    let index = 1;
    let hasErrors = false;
    
    while ((match = scriptRegex.exec(html)) !== null) {
        let code = match[1];
        
        // Replace C# serializations
        code = code.replace(/var _posProductos = [\s\S]*?\)\);/g, 'var _posProductos = [];');
        code = code.replace(/var _bodegasList = [\s\S]*?\)\);/g, 'var _bodegasList = [];');
        code = code.replace(/let productosList = [\s\S]*?\)\);/g, 'let productosList = [];');
        code = code.replace(/let bodegasList = [\s\S]*?\)\);/g, 'let bodegasList = [];');
        code = code.replace(/const presentacionesMap = [\s\S]*?\)\);/g, 'const presentacionesMap = {};');
        code = code.replace(/const proveedoresMap = [\s\S]*?\)\);/g, 'const proveedoresMap = {};');
        code = code.replace(/let clientesList = [\s\S]*?\)\);/g, 'let clientesList = [];');
        code = code.replace(/const BANCOS_LIST = [\s\S]*?\)\);/g, 'const BANCOS_LIST = [];');
        code = code.replace(/const LINEAS_DESCUENTO = [\s\S]*?\)\);/g, 'const LINEAS_DESCUENTO = [];');
        code = code.replace(/const LINEAS_DETALLES = [\s\S]*?\)\);/g, 'const LINEAS_DETALLES = [];');
        code = code.replace(/const COMBOS = [\s\S]*?\)\);/g, 'const COMBOS = [];');
        code = code.replace(/const COMBOS_DETALLES = [\s\S]*?\)\);/g, 'const COMBOS_DETALLES = [];');
        code = code.replace(/const DESCUENTOS_PROVEEDOR = [\s\S]*?\)\);/g, 'const DESCUENTOS_PROVEEDOR = [];');
        
        // Replace CIERRE_ID specifically
        code = code.replace(/const CIERRE_ID     = [\s\S]*?;/g, 'const CIERRE_ID     = null;');
        
        // Replace other simpler Razor tags
        code = code.replace(/@Html\.Raw\([\s\S]*?\)/g, '{}');
        code = code.replace(/@\(.*?\)/g, 'true');
        code = code.replace(/@ViewBag\..*?;/g, 'true;');
        code = code.replace(/@Url\.Action\([\s\S]*?\)/g, '"/url"');
        code = code.replace(/@montoMinCF\.ToString\([\s\S]*?\)/g, '50.00');
        code = code.replace(/'@TempData\[".*?"\]'/g, '""');
        code = code.replace(/'@User\.Identity\?.Name'/g, '"admin"');
        
        try {
            new vm.Script(code);
        } catch (e) {
            console.error(`Syntax error in script block #${index} of converted file:`, e.message);
            hasErrors = true;
        }
        index++;
    }
    
    if (!hasErrors) {
        console.log("SUCCESS: Converted content is syntactically VALID! Overwriting POS.cshtml...");
        
        // Overwrite POS.cshtml with UTF-8 BOM
        // In Node.js, we write the BOM bytes prefixing the string: \ufeff
        fs.writeFileSync('Views/Facturacion/POS.cshtml', '\ufeff' + convertedContent, 'utf8');
        console.log("POS.cshtml successfully converted and saved as UTF-8 with BOM!");
    } else {
        console.error("FAILED: Converted content has syntax errors. POS.cshtml was NOT modified.");
    }
    
    // Clean up temp file
    if (fs.existsSync('Views/Facturacion/POS.cshtml.tmp')) {
        fs.unlinkSync('Views/Facturacion/POS.cshtml.tmp');
    }
} catch (e) {
    console.error(e);
}
