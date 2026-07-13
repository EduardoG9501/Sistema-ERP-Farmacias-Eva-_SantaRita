const fs = require('fs');
const filePath = 'c:/Users/cambi/Downloads/ERP SCM/ERP SCM/SistemaERP/SistemaERP/Views/Facturacion/POS.cshtml';
let content = fs.readFileSync(filePath, 'utf8');

// Strip UTF-8 BOM if present at the start of the string
if (content.charCodeAt(0) === 0xFEFF) {
    content = content.substring(1);
}

// Remove any literal "&#65279;" string that might have been written by the previous run
content = content.replace(/^&#65279;/, '');

const startTag = '<script>';
const endTag = '</script>';

let result = "";
let pos = 0;

while (pos < content.length) {
    const startIdx = content.indexOf(startTag, pos);
    if (startIdx === -1) {
        const remaining = content.substring(pos);
        result += escapeHtml(remaining);
        break;
    }
    
    result += escapeHtml(content.substring(pos, startIdx));
    result += startTag;
    
    const endIdx = content.indexOf(endTag, startIdx + startTag.length);
    if (endIdx === -1) {
        const remaining = content.substring(startIdx + startTag.length);
        result += escapeJs(remaining);
        break;
    }
    
    result += escapeJs(content.substring(startIdx + startTag.length, endIdx));
    result += endTag;
    
    pos = endIdx + endTag.length;
}

function escapeHtml(text) {
    let out = "";
    for (let i = 0; i < text.length; i++) {
        const code = text.charCodeAt(i);
        if (code > 127) {
            out += `&#${code};`;
        } else {
            out += text[i];
        }
    }
    return out;
}

function escapeJs(text) {
    let out = "";
    for (let i = 0; i < text.length; i++) {
        const code = text.charCodeAt(i);
        if (code > 127) {
            const hex = code.toString(16).toUpperCase().padStart(4, '0');
            out += `\\u${hex}`;
        } else {
            out += text[i];
        }
    }
    return out;
}

// Write the result back as UTF-8 with BOM
const BOM = Buffer.from([0xEF, 0xBB, 0xBF]);
const fileContent = Buffer.concat([BOM, Buffer.from(result, 'utf8')]);
fs.writeFileSync(filePath, fileContent);

console.log('Successfully escaped POS.cshtml and saved with UTF-8 BOM!');
