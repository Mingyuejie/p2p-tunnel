/*
 * @Author: snltty
 * @Date: 2021-08-21 14:57:33
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-28 16:41:33
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ftp\list-share-data.js
 */
import { provide, inject, reactive } from "vue";

const provideFilesDataKey = Symbol();
export const provideFilesData = () => {
    const state = reactive({
        locals: [],
        clientId: null,
        remotes: []
    });
    provide(provideFilesDataKey, state);
}
export const injectFilesData = () => {
    return inject(provideFilesDataKey);
}