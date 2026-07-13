const fs = require('fs');

try {
    // Read the file as raw bytes
    const buffer = fs.readFileSync('Views/Facturacion/POS.cshtml');
    
    // 1. Try decoding the entire buffer as UTF-8 (this is how the compiler sees it now)
    const utf8String = buffer.toString('utf8');
    
    // 2. Try converting from double UTF-8 to single UTF-8
    // To do this, we convert the UTF-8 string back to ISO-8859-1 (binary) bytes, and then parse those bytes as UTF-8.
    // However, if the file contains a mix of single and double UTF-8, this might corrupt the single UTF-8 parts.
    // Let's see if we can do a smart conversion, or if the entire file was originally double-encoded.
    
    // Let's check some specific lines to see if they look clean.
    const lines = utf8String.split('\n');
    
    console.log("Line 927 original:", lines[926]); // 0-indexed is 926
    
    // Let's perform a translation of the double-encoded characters using a mapping, which is safer if there's a mix.
    // Or we can try the binary conversion and see if it works.
    const binaryBuffer = Buffer.from(utf8String, 'binary');
    let converted = '';
    try {
        converted = binaryBuffer.toString('utf8');
        const convertedLines = converted.split('\n');
        console.log("Line 927 converted:", convertedLines[926]);
    } catch (e) {
        console.error("Conversion failed:", e);
    }
} catch (e) {
    console.error(e);
}
