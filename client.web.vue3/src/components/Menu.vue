<!--
 * @Author: snltty
 * @Date: 2021-08-19 22:05:47
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-03 14:42:17
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\components\Menu.vue
-->
<template>
    <div class="menu-wrap flex">
        <div class="logo">
            <img src="@/assets/logo.svg" alt="">
        </div>
        <div class="navs flex-1">
            <router-link :to="{name:'Home'}">首页</router-link>
            <router-link :to="{name:'Register'}">注册服务 <i class="el-icon-circle-check" :class="{active:TcpConnected}"></i></router-link>
            <router-link :to="{name:'TcpForward'}">TCP转发 <i class="el-icon-circle-check" :class="{active:tcpForwardConnected}"></i></router-link>
            <router-link :to="{name:'UPNP'}">UPNP映射</router-link>
            <router-link :to="{name:'WakeUp'}">幻数据包</router-link>
            <router-link :to="{name:'About'}">关于</router-link>
        </div>
        <div class="meta">
            <a href="javascript:;">{{connectStr}}<i class="el-icon-refresh"></i></a>
            <Theme></Theme>
        </div>
    </div>
</template>
<script>
import { computed, toRefs } from '@vue/reactivity';
import { injectRegister } from '../states/register'
import { injectWebsocket } from '../states/websocket'
import { injectTcpForward } from '../states/tcpForward'
import Theme from './Theme.vue'
export default {
    components: { Theme },
    setup () {
        const registerState = injectRegister();
        const websocketState = injectWebsocket();
        const connectStr = computed(() => ['未连接', '已连接'][Number(websocketState.connected)]);

        const tcpForwardState = injectTcpForward();
        const tcpForwardConnected = computed(() => tcpForwardState.connected);

        return {
            ...toRefs(registerState), connectStr, tcpForwardConnected
        }
    }
}
</script>
<style lang="stylus" scoped>
.menu-wrap
    line-height: 8rem;
    height: 8rem;

.logo
    margin-left: -1rem;

    img
        height: 8rem;

.navs
    padding-left: 2rem;

    a
        margin-left: 0.4rem;
        padding: 0.6rem 1rem;
        border-radius: 0.4rem;
        transition: 0.3s;
        transition: 0.3s;
        color: #fff;
        text-shadow: 0 1px 1px #28866e;
        font-size: 1.4rem;

        &.router-link-active, &:hover
            color: #fff;
            background-color: rgba(0, 0, 0, 0.5);

        i
            opacity: 0.5;

            &.active
                color: #10da10;
                opacity: 1;

.meta
    a
        color: #fff;
</style>