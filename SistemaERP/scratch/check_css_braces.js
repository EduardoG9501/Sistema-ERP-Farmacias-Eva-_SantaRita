const fs = require('fs');
const filePath = 'c:/Users/cambi/Downloads/ERP SCM/ERP SCM/SistemaERP/SistemaERP/wwwroot/css/site.css';
const css = fs.readFileSync(filePath, 'utf8');

let braceCount = 0;
let lineNum = 1;
let colNum = 1;
let inString = false;
let stringChar = '';

for (let i = 0; i < css.length; i++) {
    const char = css[i];
    if (char === '\n') {
        lineNum++;
        colNum = 1;
    } else {
        colNum++;
    }
    
    if (char === '{' && !inString) {
        braceCount++;
    } else if (char === '}' && !inString) {
        braceCount--;
        if (braceCount < 0) {
            console.error(`Mismatched close brace at line ${lineNum}, col ${colNum}`);
            break;
        }
    }
}

console.log(`Final brace count: ${braceCount}`);
if (braceCount === 0) {
    console.log('All braces are balanced!');
} else {
    console.error('Braces are UNBALANCED!');
}
