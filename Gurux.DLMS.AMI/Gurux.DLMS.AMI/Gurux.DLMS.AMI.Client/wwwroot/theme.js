export function getStyleValue(name) {
    const style = getComputedStyle(document.documentElement);
    return style.getPropertyValue(name).trim();
}