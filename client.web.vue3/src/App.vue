<!--
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-26 09:41:30
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\App.vue
-->
<template>
    <el-config-provider :locale="locale">
        <auth-wrap>
            <div class="body absolute">
                <div class="wrap flex flex-column flex-nowrap h-100">
                    <div class="menu">
                        <Menu></Menu>
                    </div>
                    <div class="content flex-1 relative scrollbar-10">
                        <router-view />
                    </div>
                    <div class="copyright">
                        @snltty
                    </div>
                </div>
            </div>
        </auth-wrap>
    </el-config-provider>
</template>
<script>
import Menu from './components/Menu.vue'
import { provideRegister } from './states/register'
import { provideWebsocket } from './states/websocket'
import { provideClients } from './states/clients'
import { provideTcpForward } from './states/tcpForward'
import { provideFileserver } from './states/fileserver'
import { ElConfigProvider } from 'element-plus'
import zhCn from 'element-plus/lib/locale/lang/zh-cn'
export default {
    components: { Menu, ElConfigProvider },
    setup () {
        provideRegister();
        provideWebsocket();
        provideClients();
        provideTcpForward();
        provideFileserver();
        return {
            locale: zhCn
        }
    }
}
</script>
<style lang="stylus" scoped>
.body
    background-image: url('./assets/bg.png');
    background-size: 100% 100%;
    background-position: 0 0;
    background-repeat: no-repeat;

.wrap
    width: 80%;
    max-width: 90rem;
    margin: 0 auto;

.content
    background-color: #fff;
    border-radius: 0.4rem;

.copyright
    padding: 2rem 0;
    text-align: center;
    color: #fff;
    opacity: 0.8;
</style>
