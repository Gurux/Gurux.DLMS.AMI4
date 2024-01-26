export function initializeFileDropZone(component, inputFile) {
    function onDragHover(e) {
        e.preventDefault();
        component.classList.add("hover");
    }
    function onDragLeave(e) {
        e.preventDefault();
        component.classList.remove("hover");
    }
    function onDrop(e) {
        e.preventDefault();
        component.classList.remove("hover");

        // Set the files property of the input element and raise the change event
        inputFile.files = e.dataTransfer.files;
        const event = new Event('change', { bubbles: true });
        inputFile.dispatchEvent(event);
    }
  
    function onPaste(e) {
        inputFile.files = e.clipboardData.files;
        const event = new Event('change', { bubbles: true });
        inputFile.dispatchEvent(event);
    }

    component.addEventListener("dragenter", onDragHover);
    component.addEventListener("dragover", onDragHover);
    component.addEventListener("dragleave", onDragLeave);
    component.addEventListener("drop", onDrop);
    component.addEventListener('paste', onPaste);

    // The returned object allows to unregister the events when the Blazor component is destroyed
    return {
        dispose: () => {
            component.removeEventListener('dragenter', onDragHover);
            component.removeEventListener('dragover', onDragHover);
            component.removeEventListener('dragleave', onDragLeave);
            component.removeEventListener("drop", onDrop);
            component.removeEventListener('paste', onPaste);
        }
    }
}