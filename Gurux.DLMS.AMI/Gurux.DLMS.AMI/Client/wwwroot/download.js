export function saveFile(url) {
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.click();
    anchorElement.remove();
}