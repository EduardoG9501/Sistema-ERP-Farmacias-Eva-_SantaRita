const fs = require('fs');
const filePath = 'c:/Users/cambi/Downloads/ERP SCM/ERP SCM/SistemaERP/SistemaERP/Views/Facturacion/POS.cshtml';
const content = fs.readFileSync(filePath, 'utf8');
const lines = content.split('\n');

const targetLines = [927, 985, 1001];
targetLines.forEach(lineNum => {
    const line = lines[lineNum - 1];
    console.log(`Line ${lineNum}: ${line.trim()}`);
    console.log('Char codes:');
    const codes = [];
    for (let i = 0; i < line.length; i++) {
        const char = line[i];
        const code = line.charCodeAt(i);
        if (code > 127) {
            codes.push(`${char} (${code.toString(16)})`);
        }
    }
    console.log(codes.join(', '));
});
