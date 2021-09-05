/*
 * @Author: snltty
 * @Date: 2021-08-21 19:46:50
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-05 20:04:24
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\states\tcpForward.js
 */

import { provide, inject, reactive } from "vue";
import { subWebsocketState, subNotifyMsg } from '../apis/request'

const provideTcpForwardKey = Symbol();
export const provideTcpForward = () => {
    const state = reactive({
        connected: false
    });
    provide(provideTcpForwardKey, state);

    subNotifyMsg('tcpforward', (msg) => {
        let list = JSON.parse(msg);
        state.connected = list.filter(c => c.Listening == true).length > 0;
    });
    subWebsocketState(() => {
        state.connected = false;
    })
}
export const injectTcpForward = () => {
    return inject(provideTcpForwardKey);
}