const fs = require('fs');
const vm = require('vm');

try {
    const html = fs.readFileSync('Views/Facturacion/POS.cshtml', 'utf8');
    
    // Find all <script> blocks
    const scriptRegex = /<script>([\s\S]*?)<\/script>/gi;
    let match;
    let index = 1;
    
    while ((match = scriptRegex.exec(html)) !== null) {
        let code = match[1];
        
        // Replace C# serializations by replacing the entire block from variable assignment to the closing double-parenthesis-semicolon
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
        
        console.log(`Testing script block #${index}...`);
        
        try {
            new vm.Script(code);
            console.log(`Script block #${index} is syntactically VALID!`);
        } catch (e) {
            console.error(`Syntax error in script block #${index}:`);
            console.error(e.message);
            const lines = code.split('\n');
            const lineMatch = e.stack.match(/evalmachine\.<anonymous>:(\d+)/);
            if (lineMatch) {
                const lineNum = parseInt(lineMatch[1]);
                console.error(`Error around line ${lineNum}:`);
                for (let i = Math.max(0, lineNum - 5); i < Math.min(lines.length, lineNum + 5); i++) {
                    console.error(`${i + 1}: ${lines[i]}`);
                }
            } else {
                console.error(e.stack);
            }
        }
        index++;
    }
} catch (e) {
    console.error('Error running check script:', e);
}
