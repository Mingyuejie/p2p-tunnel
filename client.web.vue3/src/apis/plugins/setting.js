/*
 * @Author: snltty
 * @Date: 2021-08-21 14:58:34
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-23 10:18:42
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\plugins\setting.js
 */
import { sendWebsocketMsg } from "../request";

export const getPlugins = () => {
    return sendWebsocketMsg(`setting/list`);
}

export const loadSetting = (code) => {
    return sendWebsocketMsg(`setting/load`, { Code: code });
}

export const saveSetting = (code, content) => {
    return sendWebsocketMsg(`setting/save`, { Code: code, Content: content });
}