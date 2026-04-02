window.iconpack = (function () {
    function setPackLink(href) {
        let link = document.getElementById("icon-pack");
        if (!link) {
            link = document.createElement("link");
            link.id = "icon-pack";
            link.rel = "stylesheet";
            document.head.appendChild(link);
        }
        link.href = href;
        return href;
    }

    async function fetchText(url) {
        const res = await fetch(url, { cache: "force-cache" });
        if (!res.ok) throw new Error("Failed to fetch CSS: " + res.status);
        return await res.text();
    }

    function parseBootstrapIconsCss(cssText) {
        const set = new Set();
        const re = /\.bi-([a-z0-9-]+)\s*::?before\s*\{/gi;
        let m;
        while ((m = re.exec(cssText)) !== null) {
            const name = m[1];
            if (name && name !== "icon") set.add(name);
        }
        return Array.from(set).sort();
    }

    function parseFontAwesomeCss(cssText) {
        const set = new Set();
        const re = /\.fa-([a-z0-9-]+)\s*:\s*before/gi;
        let m;
        while ((m = re.exec(cssText)) !== null) {
            const name = m[1];
            if (!name || ["solid", "regular", "brands", "lg", "xs", "sm", "xl", "2x", "3x", "4x", "5x", "6x", "7x", "8x", "9x", "10x", "fw", "spin", "pulse", "border", "pull-left", "pull-right", "flip-horizontal", "flip-vertical", "stack", "stack-1x", "stack-2x", "inverse"].includes(name)) continue;
            set.add(name);
        }
        return Array.from(set).sort();
    }

    async function load(packId, href) {
        setPackLink(href);
        const css = await fetchText(href);
        if (packId === "bootstrap-icons") return parseBootstrapIconsCss(css);
        if (packId === "fontawesome") return parseFontAwesomeCss(css);
        return [];
    }
    return { load };
})();
