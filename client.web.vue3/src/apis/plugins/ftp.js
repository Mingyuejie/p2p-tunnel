/*
 * @Author: snltty
 * @Date: 2021-09-26 19:09:16
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-26 23:49:42
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\plugins\ftp.js
 */
import { sendWebsocketMsg } from "../request";

export const getLocalList = (path = '') => {
    return sendWebsocketMsg(`ftp/LocalList`, path);
}
export const getLocalSpecialList = () => {
    return sendWebsocketMsg(`ftp/LocalSpecialList`);
}
export const sendLocalCreate = (path = '') => {
    return sendWebsocketMsg(`ftp/LocalCreate`, path);
}
export const sendLocalDelete = (path = '') => {
    return sendWebsocketMsg(`ftp/LocalDelete`, path);
}
export const sendSetLocalPath = (path = '') => {
    return sendWebsocketMsg(`ftp/SetLocalPath`, path);
}


export const geRemoteList = (id, path = '') => {
    return sendWebsocketMsg(`ftp/RemoteList`, { Id: id, Path: path });
}
export const sendRemoteCreate = (id, path = '') => {
    return sendWebsocketMsg(`ftp/RemoteCreate`, { Id: id, Path: path });
}
export const sendRemoteDelete = (path = '') => {
    return sendWebsocketMsg(`ftp/RemoteDelete`, { Id: id, Path: path });
}