/*
 * @Author: snltty
 * @Date: 2021-08-20 16:06:04
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-24 20:04:39
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\plugins\ddns.js
 */
import { sendWebsocketMsg } from "../request";

export const getDoamins = () => {
    return sendWebsocketMsg(`ddns/domains`);
}
export const addDoamin = (json = { Platform: '', Group: '', Domain: '', AutoUpdate: false }) => {
    return sendWebsocketMsg(`ddns/AddDomain`, json);
}
export const switchDomain = (json = { Platform: '', Group: '', Domain: '', AutoUpdate: false, }) => {
    return sendWebsocketMsg(`ddns/SwitchDomain`, json);
}
export const deleteDomain = (json = { Platform: '', Group: '', Domain: '' }) => {
    return sendWebsocketMsg(`ddns/DeleteDomain`, json);
}
export const getRecords = (json = { Platform: '', Domain: '' }) => {
    return sendWebsocketMsg(`ddns/GetRecords`, json);
}



export const switchGroup = (json = { Platform: '', Group: '', AutoUpdate: false, }) => {
    return sendWebsocketMsg(`ddns/SwitchGroup`, json);
}

