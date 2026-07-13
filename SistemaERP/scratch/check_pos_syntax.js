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

if (scriptBlocks.length >= 2) {
    let mainJs = scriptBlocks[scriptBlocks.length - 1].code;
    
    // Stack-based parenthesis matcher to remove @(...) blocks
    let newJs = "const razor_expr = null; const razor_val = null;\n";
    let idx = 0;
    while (idx < mainJs.length) {
        if (mainJs.substring(idx, idx + 2) === '@(') {
            let depth = 1;
            let scan = idx + 2;
            while (scan < mainJs.length && depth > 0) {
                if (mainJs[scan] === '(') {
                    depth++;
                } else if (mainJs[scan] === ')') {
                    depth--;
                }
                scan++;
            }
            newJs += 'razor_expr';
            idx = scan;
        } else {
            newJs += mainJs[idx];
            idx++;
        }
    }
    
    mainJs = newJs;
    newJs = "";
    idx = 0;
    while (idx < mainJs.length) {
        if (mainJs.substring(idx, idx + 10) === '@Html.Raw(') {
            let depth = 1;
            let scan = idx + 10;
            while (scan < mainJs.length && depth > 0) {
                if (mainJs[scan] === '(') {
                    depth++;
                } else if (mainJs[scan] === ')') {
                    depth--;
                }
                scan++;
            }
            newJs += '[]';
            idx = scan;
        } else {
            newJs += mainJs[idx];
            idx++;
        }
    }
    
    mainJs = newJs.replace(/@[a-zA-Z0-9_.]+/g, 'razor_val');
    
    try {
        new vm.Script(mainJs);
        console.log('Main POS Script: Syntax OK!');
    } catch (err) {
        console.error('Main POS Script: Syntax ERROR!');
        console.error(err.message);
        console.error(err.stack);
        
        const lines = mainJs.split('\n');
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
}
