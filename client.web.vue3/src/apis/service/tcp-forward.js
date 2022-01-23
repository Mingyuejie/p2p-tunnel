/*
 * @Author: snltty
 * @Date: 2021-08-21 13:58:43
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-23 10:18:58
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\plugins\tcp-forward.js
 */
import { sendWebsocketMsg } from "../request";

export const getTcpForwards = () => {
    return sendWebsocketMsg(`tcpforward/list`);
}

export const sendTcpForwardStart = (id) => {
    return sendWebsocketMsg(`tcpforward/start`, {
        ID: id
    });
}

export const sendTcpForwardStop = (id) => {
    return sendWebsocketMsg(`tcpforward/stop`, {
        ID: id
    });
}

export const sendTcpForwardDel = (id) => {
    return sendWebsocketMsg(`tcpforward/del`, {
        ID: id
    });
}
export const sendTcpForwardAdd = (model) => {
    return sendWebsocketMsg(`tcpforward/add`, {
        ID: model.ID,
        Content: JSON.stringify(model)
    });
}