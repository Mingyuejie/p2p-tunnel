/*
 * @Author: snltty
 * @Date: 2021-08-21 14:58:34
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-05 22:27:30
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\file-server.js
 */
import { sendWebsocketMsg } from "./request";

export const getServerConfig = (path = '') => {
    return sendWebsocketMsg(`fileserver/info`);
}

export const getLocalFiles = (path = '') => {
    return sendWebsocketMsg(`fileserver/locallist`, { ToId: 0, Path: path });
}

export const getRemoteFiles = (path = '', toid = 0) => {
    return sendWebsocketMsg(`fileserver/list`, { ToId: toid, Path: path });
}


export const sendUpdateServerConfig = (path = '', isstart = true) => {
    return sendWebsocketMsg(`fileserver/update`, { Root: path, IsStart: isstart });
}

export const sendStartServer = () => {
    return sendWebsocketMsg(`fileserver/Start`);
}
export const sendStopServer = () => {
    return sendWebsocketMsg(`fileserver/stop`);
}

export const sendUpload = (path, toid) => {
    return sendWebsocketMsg(`fileserver/upload`, { ToId: toid, Path: path });
}
export const sendDownload = (path, toid) => {
    return sendWebsocketMsg(`fileserver/download`, { ToId: toid, Path: path });
}