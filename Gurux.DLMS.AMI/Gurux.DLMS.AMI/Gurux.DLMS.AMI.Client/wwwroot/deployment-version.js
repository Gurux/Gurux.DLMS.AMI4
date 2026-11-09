// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

// Bootstrap documents and version.json must be served with Cache-Control: no-store.
(() => {
    const loaded = document.querySelector('meta[name="gx-wasm-version"]')?.content;
    if (!loaded) {
        return;
    } // Development and installations without static deployment.
    const originalFetch = window.fetch.bind(window);
    const versionUrl = new URL('version.json', document.baseURI);
    let changed = false;
    let pending;
    const showReload = () => {
        if (changed) {
            return;
        }
        changed = true;
        const panel = document.createElement('div');
        panel.setAttribute('role', 'alertdialog');
        panel.setAttribute('aria-modal', 'true');
        panel.style.cssText = 'position:fixed;inset:0;z-index:2147483647;background:#fff;color:#222;display:grid;place-content:center;padding:2rem;text-align:center';
        const message = document.createElement('p');
        message.textContent = document.documentElement.lang.startsWith('fi')
            ? 'AMI on päivitetty. Lataa sovellus uudelleen jatkaaksesi.'
            : 'AMI has been updated. Reload the application to continue.';
        const button = document.createElement('button');
        button.textContent = document.documentElement.lang.startsWith('fi') ? 'Lataa uudelleen' : 'Reload application';
        button.onclick = () => location.reload();
        panel.append(message, button);
        document.body.append(panel);
        button.focus();
    };
    async function check() {
        if (changed) {
            throw new Error('AMI updated; reload required.');
        }
        if (!pending) {
            pending = (async () => {
            try {
                const response = await originalFetch(versionUrl, { cache: 'no-store', credentials: 'same-origin' });
                if (response.ok && (await response.json()).version !== loaded) {
                    showReload();
                }
            } catch { /* Temporary backend/web-server outage: retry on next request. */ }
        })().finally(() => { pending = null; });
        }
        await pending;
        if (changed) {
            throw new Error('AMI updated; reload required.');
        }
    }
    window.fetch = async (input, init) => {
        const url = new URL(typeof input === 'string' || input instanceof URL ? input : input.url, document.baseURI);
        const protectedRequest = url.origin === location.origin && /\/(?:_framework|_content|api)\//i.test(url.pathname);
        if (protectedRequest) {
            await check();
        }
        const response = await originalFetch(input, init);
        // Catch a switch that happened during an in-flight lazy resource download.
        if (protectedRequest) {
            await check();
        }
        return response;
    };
    document.addEventListener('visibilitychange', () => { if (!document.hidden) {
        check().catch(() => {});
    } });
    window.addEventListener('focus', () => check().catch(() => {}));
    window.addEventListener('error', () => check().catch(() => {}), true);
    window.addEventListener('unhandledrejection', () => check().catch(() => {}));
    setInterval(() => check().catch(() => {}), 5000);
})();
