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

const databaseName = "gurux-ami-resources";
const storeName = "resources";
let database;

// These entries are disposable server resources, never user preferences or dialog layouts.
try {
    for (const key of Object.keys(localStorage)) {
        if (key.startsWith("ami-resource:") || key.startsWith("ami-initial:")) {
            localStorage.removeItem(key);
        }
    }
} catch {
    // Storage may be disabled by browser policy.
}

function openDatabase() {
    if (!database) {
        database = new Promise((resolve, reject) => {
            const request = indexedDB.open(databaseName, 1);
            request.onupgradeneeded = () => request.result.createObjectStore(storeName);
            request.onerror = () => reject(request.error);
            request.onblocked = () => reject(new Error("Resource database upgrade is blocked."));
            request.onsuccess = () => {
                const db = request.result;
                db.onversionchange = () => {
                    db.close();
                    database = undefined;
                };
                resolve(db);
            };
        }).catch(error => {
            database = undefined;
            throw error;
        });
    }
    return database;
}

function keyRange(group) {
    return group ? IDBKeyRange.bound(group + ":", group + ":\uffff") : undefined;
}

async function transaction(mode, action) {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, mode);
        let result;
        tx.oncomplete = () => resolve(result);
        tx.onabort = () => reject(tx.error ?? new Error("Resource transaction aborted."));
        tx.onerror = () => reject(tx.error);
        action(tx.objectStore(storeName), value => { result = value; });
    });
}

export function get(key) {
    return transaction("readonly", (store, done) => {
        const request = store.get(key);
        request.onsuccess = () => done(request.result ?? null);
    });
}

export function set(key, value) {
    return transaction("readwrite", store => {
        if (value === null) {
            store.delete(key);
        } else {
            store.put(value, key);
        }
    });
}

export function clear(group) {
    return transaction("readwrite", store => {
        if (group) {
            store.delete(keyRange(group));
        } else {
            store.clear();
        }
    });
}

export function keys(group) {
    return transaction("readonly", (store, done) => {
        const request = store.getAllKeys(keyRange(group));
        request.onsuccess = () => done(request.result);
    });
}
