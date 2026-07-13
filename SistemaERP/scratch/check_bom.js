const fs = require('fs');
const filePath = 'c:/Users/cambi/Downloads/ERP SCM/ERP SCM/SistemaERP/SistemaERP/Views/Facturacion/POS.cshtml';
const buffer = Buffer.alloc(10);
const fd = fs.openSync(filePath, 'r');
fs.readSync(fd, buffer, 0, 10, 0);
fs.closeSync(fd);
console.log('First 10 bytes:', buffer.toString('hex').match(/../g).join(' '));
if (buffer[0] === 0xEF && buffer[1] === 0xBB && buffer[2] === 0xBF) {
    console.log('UTF-8 BOM detected!');
} else {
    console.log('No UTF-8 BOM detected.');
}
