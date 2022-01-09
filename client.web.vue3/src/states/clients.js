/*
 * @Author: snltty
 * @Date: 2021-08-21 14:57:33
 * @LastEditors: snltty
 * @LastEditTime: 2022-01-04 17:17:33
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\states\clients.js
 */
import { provide, inject, reactive } from "vue";
import { subWebsocketState, subNotifyMsg } from '../apis/request'

const provideClientsKey = Symbol();
export const provideClients = () => {
    const state = reactive({
        clients: []
    });
    provide(provideClientsKey, state);

    subNotifyMsg('clients/list', (msg) => {
        msg.forEach(c => {
            let ip = c.Ip.split('.').slice(0, 2).join('.');
            c.islocal = (ip == '192.168' || ip == '127.0');
        });
        state.clients = msg;
    });
    subWebsocketState((_state) => {
        if (!_state) {
            state.clients = [];
        }
    })
}
export const injectClients = () => {
    return inject(provideClientsKey);
}