/*
 * @Author: snltty
 * @Date: 2021-08-19 22:39:45
 * @LastEditors: xr
 * @LastEditTime: 2022-01-21 16:49:52
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\states\register.js
 */
// import { provide, inject, reactive } from 'vue'

import { provide, inject, reactive } from "vue";
import { subWebsocketState, subNotifyMsg } from '../apis/request'

const provideRegisterKey = Symbol();
export const provideRegister = () => {
    const state = reactive({
        ClientConfig: {
            GroupId: '',
            Name: '',
            AutoReg: false,
            UseMac: false,
        },
        ServerConfig: {
            Ip: '',
            UdpPort: 0,
            TcpPort: 0,
        },
        LocalInfo: {
            RouteLevel: 0,
            Mac: '',
            Port: 0,
            TcpPort: 0,
            IsConnecting: false,
            UdpConnected: false,
            TcpConnected: false,
            LocalIp: ''
        },
        RemoteInfo: {
            Ip: '',
            TcpPort: 0,
            ConnectId: 0,
        }
    });
    provide(provideRegisterKey, state);

    subNotifyMsg('register/info', (json) => {
        state.LocalInfo.UdpConnected = json.LocalInfo.UdpConnected;
        state.LocalInfo.TcpConnected = json.LocalInfo.TcpConnected;
        state.LocalInfo.UdpPort = json.LocalInfo.UdpPort;
        state.LocalInfo.TcpPort = json.LocalInfo.TcpPort;
        state.LocalInfo.Mac = json.LocalInfo.Mac;
        state.LocalInfo.LocalIp = json.LocalInfo.LocalIp;

        state.RemoteInfo.TcpPort = json.RemoteInfo.TcpPort;
        state.RemoteInfo.Ip = json.RemoteInfo.Ip;
        state.RemoteInfo.ConnectId = json.RemoteInfo.ConnectId;

        state.LocalInfo.IsConnecting = json.LocalInfo.IsConnecting;
        state.LocalInfo.RouteLevel = json.LocalInfo.RouteLevel;
        if (!state.ClientConfig.GroupId) {

            state.ClientConfig.GroupId = json.ClientConfig.GroupId;
        }
    });
    subWebsocketState(() => {
        state.UdpConnected = false;
        state.TcpConnected = false;
    })
}
export const injectRegister = () => {
    return inject(provideRegisterKey);
}