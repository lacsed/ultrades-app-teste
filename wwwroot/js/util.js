function saveFile(filename, content) {
    const a = document.createElement("a");
    const file = new Blob([content], { type: "text/plain" });
    a.href = URL.createObjectURL(file);
    a.download = filename.replace(/[^a-zA-Z0-9\.]/g, '_');
    a.click();
}

async function readFile(name) {
   
    const input = document.getElementById(name);
    const files = input.files;
    var file = files[0];
    var reader = new FileReader();

    return await new Promise((resolve, reject) => {
        reader.onerror = () => {
            reader.abort();
            reject(new DOMException("Problem parsing input file."));
        };

        reader.onload = () => resolve(reader.result);

        reader.readAsText(file);
    });
}

function downloadSVGInterno(fileName, svgInterno) {
    // Cria um blob do conteúdo SVG com o tipo MIME correto
    const blob = new Blob([svgInterno], { type: 'image/svg+xml' });
  
    // Cria uma URL temporária para o blob
    const url = URL.createObjectURL(blob);
  
    // Cria um elemento <a> para simular o clique de download
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName.endsWith('.svg') ? fileName : fileName + '.svg'; // garante a extensão .svg
    document.body.appendChild(a); // necessário em alguns navegadores
  
    a.click(); // aciona o download
  
    document.body.removeChild(a); // remove o elemento
    URL.revokeObjectURL(url); // libera a memória
  }
  

function downloadLatex(fileName, textContent) {
    const blob = new Blob([textContent], { type: 'text/plain' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = fileName;
    link.click();
}

function downloadSVG(fileName, dot) {
    const content = Viz(dot, { format: 'svg', engine: 'dot', scale: undefined, totalMemory: 1024 * 1024 * 1024, files: undefined, images: undefined });
    const a = document.createElement('a');
    const file = new Blob([content], { type: 'text/plain' });
    a.href = URL.createObjectURL(file);
    a.download = fileName;
    a.click();
}

function downloadPNG(fileName, dot) {
    const content = Viz(dot, { format: 'svg', engine: 'dot', scale: undefined, totalMemory: 1024 * 1024 * 1024, files: undefined, images: undefined });
    Viz.svgXmlToPngImageElement(content, undefined, (err, img) => {
        const source = img.src;
        const a = document.createElement('a');
        document.body.appendChild(a);

        a.href = source;
        a.target = '_self';
        a.download = fileName;
        a.click();
    });
}

window.saveFile = saveFile;
window.readFile = readFile;
window.GraphViz = dot => Viz(dot);
window.downloadPNG = downloadPNG;
window.downloadSVG = downloadSVG;
