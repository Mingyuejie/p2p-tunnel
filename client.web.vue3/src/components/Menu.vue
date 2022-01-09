<!--
 * @Author: snltty
 * @Date: 2021-08-19 22:05:47
 * @LastEditors: xr
 * @LastEditTime: 2022-01-09 15:31:53
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
            <router-link :to="{name:'PluginSetting'}">插件配置</router-link>
            <el-dropdown>
                <span class="el-dropdown-link">
                    <span>应用插件</span>
                    <span>{{routeName}}</span>
                    <i class="el-icon-arrow-down el-icon--right"></i>
                </span>
                <template #dropdown>
                    <el-dropdown-menu>
                        <template v-if="websocketState.connected">
                            <template v-for="(item,index) in menus" :key="index">
                                <auth-item :name="item.plugin">
                                    <el-dropdown-item>
                                        <router-link :to="{name:item.name}">{{item.text}}</router-link>
                                    </el-dropdown-item>
                                </auth-item>
                            </template>
                        </template>
                        <template v-else>
                            <template v-for="(item,index) in menus" :key="index">
                                <el-dropdown-item>
                                    <router-link :to="{name:item.name}" class="disabled">{{item.text}}</router-link>
                                </el-dropdown-item>
                            </template>
                        </template>
                    </el-dropdown-menu>
                </template>
            </el-dropdown>
            <router-link :to="{name:'About'}">说明文档</router-link>
        </div>
        <div class="meta">
            <a href="javascript:;" @click="editWsUrl" title="点击修改" :class="{active:websocketState.connected}">{{wsUrl}} {{connectStr}}<i class="el-icon-refresh"></i></a>
            <Theme></Theme>
        </div>
    </div>
</template>
<script>
import { computed, ref, toRefs } from '@vue/reactivity';
import { onMounted } from '@vue/runtime-core'
import { injectRegister } from '../states/register'
import { injectWebsocket } from '../states/websocket'
import { injectTcpForward } from '../states/tcpForward'
import { injectFileserver } from '../states/fileserver'
import { initWebsocket } from '../apis/request'
import Theme from './Theme.vue'
import AuthItem from './auth/AuthItem.vue';
import { useRoute } from 'vue-router'
import { ElMessageBox } from 'element-plus'
export default {
    components: { Theme, AuthItem },
    setup () {
        const registerState = injectRegister();
        const websocketState = injectWebsocket();
        const connectStr = computed(() => `${['未连接', '已连接'][Number(websocketState.connected)]}`);

        const tcpForwardState = injectTcpForward();
        const tcpForwardConnected = computed(() => tcpForwardState.connected);

        const fileserverState = injectFileserver();
        const fileServerStarted = computed(() => fileserverState.IsStart);

        const route = useRoute();
        const routeName = computed(() => {
            if (route.matched.length > 0 && route.matched[0].name == 'Pugins') {
                return `-${route.meta.name}`;
            }
            return '';
        });

        const menus = [
            { name: 'PluginTcpForward', text: 'TCP转发', plugin: 'TcpForwardPlugin' },
            { name: 'PluginAlbum', text: '图片相册', plugin: 'AlbumSettingPlugin' },
            { name: 'PluginFtp', text: '文件服务', plugin: 'FtpPlugin' },
            { name: 'PluginCmd', text: '远程命令', plugin: 'CmdsPlugin' },
            { name: 'PluginUPNP', text: 'UPNP映射', plugin: 'UpnpPlugin' },
            { name: 'PluginDdns', text: '域名解析', plugin: 'DdnsPlugin' },
            { name: 'PluginWakeUp', text: '幻数据包', plugin: 'WakeUpPlugin' },
            { name: 'PluginLogger', text: '日志信息', plugin: 'LoggerPlugin' }
        ];

        const editWsUrl = () => {
            ElMessageBox.prompt('修改连接地址', '修改连接地址', {
                inputValue: wsUrl.value
            }).then(({ value }) => {
                localStorage.setItem('wsurl', value);
                wsUrl.value = value;
                initWebsocket(wsUrl.value);
            })
        }
        const wsUrl = ref(localStorage.getItem('wsurl') || 'ws://127.0.0.1:59411');
        onMounted(() => {
            initWebsocket(wsUrl.value);
        });

        return {
            ...toRefs(registerState),
            websocketState, connectStr, tcpForwardConnected, fileServerStarted,
            routeName, menus,
            wsUrl, editWsUrl
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