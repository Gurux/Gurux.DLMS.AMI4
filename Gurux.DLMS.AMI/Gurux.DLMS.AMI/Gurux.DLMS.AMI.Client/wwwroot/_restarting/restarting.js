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

(() => {
    'use strict';
    if (window.guruxRestarting) {
        return;
    }
    const nativeFetch = window.fetch.bind(window);
    const assets = new URL('.', document.currentScript.src);
    const api = new URL('api/', document.baseURI);
    const version = new URL('version.json', document.baseURI);
    const health = new URL('Configuration/Version', api);
    const standalone = document.body?.hasAttribute('data-ami-restarting');
    let active = false;
    let recovering = false;
    let pending;
    let timer;
    let messages;

    async function readMessages(name) {
        const response = await nativeFetch(new URL(name, assets), { credentials: 'same-origin', cache: 'no-cache' });
        if (!response.ok) {
            throw new Error('Restart resource lookup failed.');
        }
        const xml = new DOMParser().parseFromString(await response.text(), 'application/xml');
        return Object.fromEntries(Array.from(xml.querySelectorAll('data[name]')).map(item =>
            [item.getAttribute('name'), item.querySelector('value')?.textContent ?? '']));
    }
    const resources = (async () => {
        const values = await readMessages('messages.resx');
        document.documentElement.lang = 'en';
        messages = values;
        if (active) {
            render();
        }
    })().catch(error => console.debug(error));

    function render() {
        if (!document.body) {
            document.addEventListener('DOMContentLoaded', render, { once: true });
            return;
        }
        let panel = document.getElementById('ami-restarting');
        if (!panel) {
            panel = document.createElement('section');
            panel.id = 'ami-restarting';
            panel.setAttribute('role', 'status');
            panel.setAttribute('aria-live', 'polite');
            panel.setAttribute('aria-labelledby', 'ami-restarting-title');
            panel.setAttribute('tabindex', '-1');
            panel.innerHTML = '<div class="ami-restarting-card"><div class="ami-restarting-spinner" aria-hidden="true"></div><h1 id="ami-restarting-title"></h1><p class="ami-restarting-message"></p><p class="ami-restarting-checking"></p></div>';
            document.body.append(panel);
            panel.focus();
        }
        if (messages) {
            panel.querySelector('h1').textContent = messages.RestartingTitle;
            panel.querySelector('.ami-restarting-message').textContent = messages.RestartingMessage;
            panel.querySelector('.ami-restarting-checking').textContent = messages.RestartingChecking;
            document.title = messages.RestartingTitle;
        }
    }
    function schedule(milliseconds) {
        clearTimeout(timer);
        timer = setTimeout(check, milliseconds);
    }
    function show() {
        if (active || recovering) {
            return;
        }
        active = true;
        render();
        schedule(2000);
    }
    function isBackend(url) {
        return url.origin === location.origin && (url.pathname.startsWith(api.pathname) || url.pathname === version.pathname);
    }
    async function check() {
        if (pending || recovering) {
            return pending;
        }
        pending = (async () => {
            const controller = new AbortController();
            const timeout = setTimeout(() => controller.abort(), 5000);
            try {
                const response = await nativeFetch(health, {
                    credentials: 'same-origin', cache: 'no-store', redirect: 'error', signal: controller.signal,
                    headers: { Accept: 'text/plain' }
                });
                const value = response.ok ? (await response.text()).trim() : '';
                const ready = /^\d+\.\d+\.\d+(?:\.\d+)?(?:[-+][A-Za-z0-9.+-]+)?$/.test(value);
                if (ready) {
                    if (active) {
                        recovering = true;
                        if (location.pathname.startsWith(assets.pathname)) {
                            location.replace(new URL('../', api));
                        } else {
                            location.reload();
                        }
                    }
                } else {
                    show();
                }
            } catch {
                show();
            } finally {
                clearTimeout(timeout);
                pending = null;
                if (!recovering) {
                    schedule(active ? 2000 : 5000);
                }
            }
        })();
        return pending;
    }
    window.fetch = async (input, init) => {
        const url = new URL(typeof input === 'string' || input instanceof URL ? input : input.url, document.baseURI);
        const signal = init?.signal !== undefined ? init.signal : input instanceof Request ? input.signal : undefined;
        try {
            const response = await nativeFetch(input, init);
            if (isBackend(url) && [502, 503, 504].includes(response.status)) {
                void check();
            }
            return response;
        } catch (error) {
            if (isBackend(url) && !signal?.aborted && error?.name !== 'AbortError') {
                void check();
            }
            throw error;
        }
    };
    window.guruxRestarting = Object.freeze({ show });
    if (standalone) {
        show();
    } else {
        schedule(5000);
    }
})();
