/*
 * @Author: snltty
 * @Date: 2021-08-19 23:04:50
 * @LastEditors: xr
 * @LastEditTime: 2022-01-08 17:30:28
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\request.js
 */
import { ElMessage } from 'element-plus'

let requestId = 0, ws = null, connected = false, wsUrl = '';
//请求缓存，等待回调
const requests = {};
const queues = [];

const sendQueueMsg = () => {
    if (queues.length > 0 && connected) {
        ws.send(queues.shift());
    }
    setTimeout(sendQueueMsg, 1000 / 60);
}
sendQueueMsg();

//发布订阅
export const pushListener = {
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
    connected = true;
    pushListener.push(websocketStateChangeKey, connected);
}
const onWebsocketClose = (e) => {
    connected = false;
    pushListener.push(websocketStateChangeKey, connected);
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
            ElMessage.error(json.Content);
        } else {
            pushListener.push(json.Path, json.Content);
        }
        delete requests[json.RequestId];
    } else {
        if (json.Path == 'merge') {
            let arr = json.Content;
            for (let i = 0, len = arr.length; i < len; i++) {
                pushListener.push(arr[i].Path, arr[i].Content);
            }
        } else {
            pushListener.push(json.Path, json.Content);
        }
    }
}
export const initWebsocket = (url = wsUrl) => {
    if (ws != null) {
        ws.close();
    }
    wsUrl = url;
    ws = new WebSocket(wsUrl);
    ws.onopen = onWebsocketOpen;
    ws.onclose = onWebsocketClose
    ws.onmessage = onWebsocketMsg
}

//发送消息
export const sendWebsocketMsg = (path, msg = {}) => {
    return new Promise((resolve, reject) => {
        let id = ++requestId;
        try {
            requests[id] = { resolve, reject };
            let str = JSON.stringify({
                Path: path,
                RequestId: id,
                Content: typeof msg == 'string' ? msg : JSON.stringify(msg)
            });
            if (connected) {
                ws.send(str);
            } else {
                queues.push(str);
            }
        } catch (e) {
            reject('网络错误~');
            ElMessage.error('网络错误~');
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