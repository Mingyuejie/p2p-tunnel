/*
 * @Author: snltty
 * @Date: 2021-09-30 15:15:20
 * @LastEditors: xr
 * @LastEditTime: 2022-01-09 15:38:37
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\plugins\cmd.js
 */
import { sendWebsocketMsg } from "../request";

export const sendCmd = (id, cmd = '') => {
    return sendWebsocketMsg(`cmds/execute`, { Id: id, cmd: cmd });
}
