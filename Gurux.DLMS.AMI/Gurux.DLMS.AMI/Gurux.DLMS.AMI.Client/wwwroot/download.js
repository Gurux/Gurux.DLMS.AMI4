export function saveFile(url) {
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.click();
    anchorElement.remove();
}

export function loadFile(fileName, arrayBuffer) {
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}