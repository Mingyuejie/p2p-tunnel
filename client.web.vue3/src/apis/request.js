/*
 * @Author: snltty
 * @Date: 2021-08-19 23:04:50
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-05 20:02:46
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\request.js
 */
let requestId = 0;
let ws = null;
//请求缓存，等待回调
const requests = {};

//发布订阅
const pushListener = {
    subs: {
    },
    add: function (type, callback) {
        if (typeof callback == 'function') {
            if (!this.subs[type]) {
                this.subs[type] = [];
            }
            this.subs[type].push(callback);
        }
    },
    remove (type, callback) {
        let funcs = this.subs[type] || [];
        for (let i = funcs.length - 1; i >= 0; i--) {
            if (funcs[i] == callback) {
                funcs.splice(i, 1);
            }
        }
    },
    push (type, data) {
        let funcs = this.subs[type] || [];
        for (let i = funcs.length - 1; i >= 0; i--) {
            funcs[i](data);
        }
    }
}

const websocketStateChangeKey = Symbol();
export const subWebsocketState = (callback) => {
    pushListener.add(websocketStateChangeKey, callback);
}
//消息处理
const onWebsocketOpen = () => {
    pushListener.push(websocketStateChangeKey, true);
}
const onWebsocketClose = () => {
    pushListener.push(websocketStateChangeKey, false);
    initWebsocket();
}

const onWebsocketMsg = (msg) => {
    let json = JSON.parse(msg.data);
    let callback = requests[json.RequestId];
    if (callback) {
        if (json.Code == 0) {
            callback.resolve(json.Content);
        } else if (json.Code == -1) {
            callback.reject(json.Content);
        } else {
            pushListener.push(json.Path, json.Content);
        }
        delete requests[json.RequestId];
    } else {
        pushListener.push(json.Path, json.Content);
    }
}
const initWebsocket = () => {
    ws = new WebSocket('ws://127.0.0.1:59410');
    ws.onopen = onWebsocketOpen;
    ws.onclose = onWebsocketClose
    ws.onmessage = onWebsocketMsg
}
initWebsocket();

//发送消息
export const sendWebsocketMsg = (path, msg = {}) => {
    return new Promise((resolve, reject) => {
        let id = ++requestId;
        try {
            requests[id] = { resolve, reject };
            ws.send(JSON.stringify({
                Path: path,
                RequestId: id,
                Content: JSON.stringify(msg)
            }));
        } catch (e) {
            reject('网络错误~');
            delete requests[id];
        }
    });
}


export const subNotifyMsg = (path, callback) => {
    pushListener.add(path, callback);
}
export const unsubNotifyMsg = (path, callback) => {
    pushListener.remove(path, callback);
}