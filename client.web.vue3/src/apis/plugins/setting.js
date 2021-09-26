/*
 * @Author: snltty
 * @Date: 2021-08-21 14:58:34
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-26 09:37:22
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\plugins\setting.js
 */
import { sendWebsocketMsg } from "../request";

export const getPlugins = () => {
    return sendWebsocketMsg(`setting/list`);
}

export const loadSetting = (className) => {
    return sendWebsocketMsg(`setting/load`, { ClassName: className });
}

export const loadPlugins = () => {
    return sendWebsocketMsg(`setting/plugins`);
}

export const saveSetting = (className, content) => {
    return sendWebsocketMsg(`setting/save`, { ClassName: className, Content: content });
}