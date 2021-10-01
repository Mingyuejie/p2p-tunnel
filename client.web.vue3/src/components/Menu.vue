<!--
 * @Author: snltty
 * @Date: 2021-08-19 22:05:47
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-01 20:00:25
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
            <router-link :to="{name:'Register'}">注册服务 <i class="el-icon-circle-check" :class="{active:LocalInfo.TcpConnected}"></i></router-link>
            <el-dropdown>
                <span class="el-dropdown-link">应用插件 <i class="el-icon-arrow-down el-icon--right"></i></span>
                <template #dropdown>
                    <el-dropdown-menu>
                        <template v-if="websocketState.connected">
                            <auth-item name="TcpForwardPlugin">
                                <el-dropdown-item>
                                    <router-link :to="{name:'PluginTcpForward'}">TCP转发 <i class="el-icon-circle-check" :class="{active:tcpForwardConnected}"></i></router-link>
                                </el-dropdown-item>
                            </auth-item>
                            <auth-item name="AlbumSettingPlugin">
                                <el-dropdown-item>
                                    <router-link :to="{name:'PluginAlbum'}">图片相册</router-link>
                                </el-dropdown-item>
                            </auth-item>
                            <auth-item name="FtpPlugin">
                                <el-dropdown-item>
                                    <router-link :to="{name:'PluginFtp'}">文件服务</router-link>
                                </el-dropdown-item>
                            </auth-item>
                            <auth-item name="CmdsPlugin">
                                <el-dropdown-item>
                                    <router-link :to="{name:'PluginCmd'}">远程命令</router-link>
                                </el-dropdown-item>
                            </auth-item>
                            <auth-item name="UpnpPlugin">
                                <el-dropdown-item>
                                    <router-link :to="{name:'PluginUPNP'}">UPNP映射</router-link>
                                </el-dropdown-item>
                            </auth-item>
                            <auth-item name="WakeUpPlugin">
                                <el-dropdown-item>
                                    <router-link :to="{name:'PluginWakeUp'}">幻数据包</router-link>
                                </el-dropdown-item>
                            </auth-item>
                        </template>
                        <template v-else>
                            <el-dropdown-item>
                                <router-link :to="{name:'PluginTcpForward'}" class="disabled">TCP转发</router-link>
                            </el-dropdown-item>
                            <el-dropdown-item>
                                <router-link :to="{name:'PluginAlbum'}" class="disabled">图片相册</router-link>
                            </el-dropdown-item>
                            <el-dropdown-item>
                                <router-link :to="{name:'PluginFtp'}" class="disabled">文件服务</router-link>
                            </el-dropdown-item>
                            <el-dropdown-item>
                                <router-link :to="{name:'PluginCmd'}" class="disabled">远程命令</router-link>
                            </el-dropdown-item>
                            <el-dropdown-item>
                                <router-link :to="{name:'PluginUPNP'}" class="disabled">UPNP映射</router-link>
                            </el-dropdown-item>
                            <el-dropdown-item>
                                <router-link :to="{name:'PluginWakeUp'}" class="disabled">幻数据包</router-link>
                            </el-dropdown-item>
                        </template>
                    </el-dropdown-menu>
                </template>
            </el-dropdown>
            <router-link :to="{name:'About'}">关于</router-link>
        </div>
        <div class="meta">
            <a href="javascript:;" :class="{active:websocketState.connected}">{{connectStr}}<i class="el-icon-refresh"></i></a>
            <Theme></Theme>
        </div>
    </div>
</template>
<script>
import { computed, toRefs } from '@vue/reactivity';
import { injectRegister } from '../states/register'
import { injectWebsocket } from '../states/websocket'
import { injectTcpForward } from '../states/tcpForward'
import { injectFileserver } from '../states/fileserver'
import { subNotifyMsg } from '../apis/request'
import { ElNotification } from 'element-plus'
import Theme from './Theme.vue'
import AuthItem from './auth/AuthItem.vue';
export default {
    components: { Theme, AuthItem },
    setup () {
        const registerState = injectRegister();
        const websocketState = injectWebsocket();
        const connectStr = computed(() => ['未连接', '已连接'][Number(websocketState.connected)]);

        const tcpForwardState = injectTcpForward();
        const tcpForwardConnected = computed(() => tcpForwardState.connected);

        const fileserverState = injectFileserver();
        const fileServerStarted = computed(() => fileserverState.IsStart);

        subNotifyMsg('system/version', (json) => {
            let localVersion = json.Local.split('\r\n')[0];
            let remoteVersion = json.Remote.split('\n')[0];
            if (localVersion != remoteVersion && remoteVersion.length > 0) {
                ElNotification({
                    title: '新信息',
                    dangerouslyUseHTMLString: true,
                    message: `<ul><li>${json.Remote.split('\r\n').slice(1).join('</li><li>')}</li></ul>`,
                    type: 'warning',
                    duration: 0
                });
            }
        });

        return {
            ...toRefs(registerState), websocketState, connectStr, tcpForwardConnected, fileServerStarted
        }
    }
}
</script>
<style lang="stylus" scoped>
.menu-wrap
    line-height: 8rem;
    height: 8rem;

.el-dropdown-menu__item
    padding: 0;
    line-height: normal;

    &:hover
        background-color: rgba(0, 0, 0, 0.1) !important;

    a
        padding: 0 2rem;
        line-height: 3.6rem;
        display: block;

        &.disabled
            color: #bbb;

.logo
    img
        height: 4rem;
        vertical-align: middle;

.navs
    padding-left: 2rem;

    a, .el-dropdown-link
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

i.active
    color: #10da10;
    opacity: 1;

.meta
    a
        color: #fff700;

        &.active
            color: #5bff68;
</style>