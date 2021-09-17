/*
 * @Author: snltty
 * @Date: 2021-09-05 20:05:45
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-17 21:13:03
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\states\fileserver.js
 */
import { provide, inject, reactive } from "vue";
import { subWebsocketState, subNotifyMsg } from '../apis/request'

const provideFileserverKey = Symbol();
export const provideFileserver = () => {
    const state = reactive({
        Root: '',
        IsStart: false
    });
    provide(provideFileserverKey, state);

    subNotifyMsg('fileserver/info', (json) => {
        state.IsStart = json.IsStart;
    });
    subWebsocketState((_state) => {
        if (!_state) {
            state.IsStart = false;
            state.Root = '';
        }
    })
}
export const injectFileserver = () => {
    return inject(provideFileserverKey);
}