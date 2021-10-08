/*
 * @Author: snltty
 * @Date: 2021-08-21 14:57:33
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-08 10:15:30
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