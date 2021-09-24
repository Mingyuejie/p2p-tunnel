/*
 * @Author: snltty
 * @Date: 2021-08-20 16:06:04
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-24 10:41:01
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\plugins\album.js
 */
import { sendWebsocketMsg } from "../request";

export const loadSetting = () => {
    return sendWebsocketMsg(`albumsetting/load`);
}