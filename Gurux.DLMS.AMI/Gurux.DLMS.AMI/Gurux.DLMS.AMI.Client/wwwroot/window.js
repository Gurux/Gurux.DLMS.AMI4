export function resize(component) {
    window.addEventListener("resize", function () {
        setTimeout(raiseEvent, 1, component, "OnWindowResized", window.innerWidth, window.innerHeight);
    });

    function raiseEvent(comp, eventname, x, y) {
        console.log("resize");
        comp.invokeMethodAsync(eventname, x, y);
    }
}

export function top() {
    //Get location.
    var rect = document.body.getBoundingClientRect();
    return rect.top;
}
export function left(component) {
    var wnd = document.getElementById(component);
    //Get location.
    return wnd.getBoundingClientRect().left;
    /*
    var rect = document.body.getBoundingClientRect();
    return rect.left;
    */
}

export function width() {
    return window.innerWidth;
}

export function height() {
    return window.innerHeight;
}

export function initializeSplitter(component) {
    var slider;
    //Is slider vertical or horizontal.
    var vertical = false;
    //Is user moving the slider.
    var pressed = false;
    var leftDiv;
    var rightDiv;
    var topDiv = document.getElementById('topDiv')
    if (topDiv != null) {
        console.log("vertical");
        vertical = true;
        var bottomDiv = document.getElementById('bottomDiv')
        if (bottomDiv == null) {
            alert('Invalid bottomDiv.');
        }
        slider = document.getElementById('horizontalDivider')
        if (slider == null) {
            alert('Invalid horizontal divider.');
        }
    }
    else {
        console.log("horizontal");
        leftDiv = document.getElementById('leftDiv')
        if (leftDiv == null) {
            alert('Invalid leftDiv.');
        }
        rightDiv = document.getElementById('rightDiv')
        if (rightDiv == null) {
            alert('Invalid rightDiv.');
        }
        slider = document.getElementById('verticalDivider')
        if (slider == null) {
            alert('Invalid vertical divider.');
        }
    }

    function mousedown(ev) {
        pressed = true;
        document.body.addEventListener('mousemove', mousemove);
        document.body.addEventListener('mouseup', mouseup);
        console.log("mousedown");
    }
    function mouseup(ev) {
        pressed = false;
        console.log("mouseup");
    }
    if (slider != null) {
        slider.addEventListener('mousedown', mousedown);
    }

    function mousemove(ev) {
        console.log("mousemove2" + ev.clientX);
        if (pressed) {
            var rect = leftDiv.getBoundingClientRect();
            var x = ev.clientX - rect.left
            leftDiv.style.flexBasis = x + "px"
        } else {
            document.body.removeEventListener('mouseup', mouseup)
            document.body.removeEventListener('mousemove', mousemove)
        }
    };
}
