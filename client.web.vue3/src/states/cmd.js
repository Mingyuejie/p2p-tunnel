/*
 * @Author: snltty
 * @Date: 2021-09-30 15:57:11
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-30 15:57:55
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\states\cmd.js
 */
import { provide, inject, reactive } from "vue";

const provideCmdKey = Symbol();
export const provideCmd = (state) => {
    provide(provideCmdKey, state);
}
export const injectCmd = () => {
    return inject(provideCmdKey);
}