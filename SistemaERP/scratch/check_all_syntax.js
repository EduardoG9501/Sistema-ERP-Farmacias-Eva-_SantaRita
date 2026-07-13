const fs = require('fs');
const vm = require('vm');

const filePath = 'c:/Users/cambi/Downloads/ERP SCM/ERP SCM/SistemaERP/SistemaERP/Views/Facturacion/POS.cshtml';
const content = fs.readFileSync(filePath, 'utf8');

const startTag = '<script>';
const endTag = '</script>';

const scriptBlocks = [];
let pos = 0;
while (true) {
    const startIdx = content.indexOf(startTag, pos);
    if (startIdx === -1) break;
    const endIdx = content.indexOf(endTag, startIdx);
    if (endIdx === -1) break;
    scriptBlocks.push({
        start: startIdx + startTag.length,
        end: endIdx,
        code: content.substring(startIdx + startTag.length, endIdx)
    });
    pos = endIdx + endTag.length;
}

console.log(`Checking ${scriptBlocks.length} script blocks...`);

scriptBlocks.forEach((block, index) => {
    let rawJs = block.code;
    
    // Stack-based parenthesis matcher to remove @(...) blocks
    let newJs = "const razor_expr = null; const razor_val = null; const System = { Text: { Json: { JsonSerializer: { Serialize: () => '[]' } } } };\n";
    let idx = 0;
    while (idx < rawJs.length) {
        if (rawJs.substring(idx, idx + 2) === '@(') {
            let depth = 1;
            let scan = idx + 2;
            while (scan < rawJs.length && depth > 0) {
                if (rawJs[scan] === '(') {
                    depth++;
                } else if (rawJs[scan] === ')') {
                    depth--;
                }
                scan++;
            }
            newJs += 'razor_expr';
            idx = scan;
        } else {
            newJs += rawJs[idx];
            idx++;
        }
    }
    
    // Remove @Html.Raw(...)
    rawJs = newJs;
    newJs = "";
    idx = 0;
    while (idx < rawJs.length) {
        if (rawJs.substring(idx, idx + 10) === '@Html.Raw(') {
            let depth = 1;
            let scan = idx + 10;
            while (scan < rawJs.length && depth > 0) {
                if (rawJs[scan] === '(') {
                    depth++;
                } else if (rawJs[scan] === ')') {
                    depth--;
                }
                scan++;
            }
            newJs += '[]';
            idx = scan;
        } else {
            newJs += rawJs[idx];
            idx++;
        }
    }
    
    // Replace simple @Variables
    rawJs = newJs.replace(/@[a-zA-Z0-9_.]+/g, 'razor_val');
    
    try {
        new vm.Script(rawJs);
        console.log(`Script block ${index + 1}: Syntax OK!`);
    } catch (err) {
        console.error(`Script block ${index + 1}: Syntax ERROR!`);
        console.error(err.message);
        console.error(err.stack);
        
        const lines = rawJs.split('\n');
        const matchLine = err.stack.match(/evalmachine\.<anonymous>:(\d+)/);
        if (matchLine) {
            const lineNum = parseInt(matchLine[1], 10);
            console.error(`Error around line ${lineNum}:`);
            const start = Math.max(0, lineNum - 10);
            const end = Math.min(lines.length, lineNum + 10);
            for (let i = start; i < end; i++) {
                console.log(`${i + 1}: ${lines[i]}`);
            }
        }
    }
});
