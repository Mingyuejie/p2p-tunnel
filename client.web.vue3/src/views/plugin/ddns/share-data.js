/*
 * @Author: snltty
 * @Date: 2021-08-21 14:57:33
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-23 23:41:46
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ddns\share-data.js
 */
import { provide, inject } from "vue";

const shareDataKey = Symbol();
export const provideShareData = (state) => {
    provide(shareDataKey, state);
}
export const injectShareData = () => {
    return inject(shareDataKey);
}