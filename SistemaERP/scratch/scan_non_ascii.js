const fs = require('fs');
const filePath = 'c:/Users/cambi/Downloads/ERP SCM/ERP SCM/SistemaERP/SistemaERP/Views/Facturacion/POS.cshtml';
const content = fs.readFileSync(filePath, 'utf8');
const lines = content.split('\n');

let count = 0;
for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    let hasNonAscii = false;
    for (let j = 0; j < line.length; j++) {
        if (line.charCodeAt(j) > 127) {
            hasNonAscii = true;
            break;
        }
    }
    if (hasNonAscii) {
        count++;
        if (count < 20) {
            console.log(`Line ${i + 1}: ${line.trim()}`);
        }
    }
}
console.log(`Total lines with non-ASCII: ${count}`);
