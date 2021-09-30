<!--
 * @Author: snltty
 * @Date: 2021-09-30 14:25:13
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-30 16:03:36
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\cmd\Index.vue
-->
<template>
    <div class="cmd-setting-wrap flex flex-column h-100">
        <div class="head">
            <SettingModal className="CmdSettingPlugin">
                <el-button size="mini">配置插件</el-button>
            </SettingModal>
            <el-select v-model="clientId" placeholder="请选择已连接的目标客户端" size="mini">
                <template v-for="client in clients" :key="client.Id">
                    <el-option :label="client.Name" :value="client.Id">
                    </el-option>
                </template>
            </el-select>
        </div>
        <div class="body flex-1">
            <el-tabs type="border-card" class="h-100 flex flex-column">
                <el-tab-pane label="命令行" class="absolute">
                    <Cmd></Cmd>
                </el-tab-pane>
                <el-tab-pane label="服务" class="absolute">服务</el-tab-pane>
                <el-tab-pane label="进程" class="absolute">进程</el-tab-pane>
            </el-tabs>
        </div>
    </div>
</template>

<script>
import SettingModal from '../SettingModal.vue'
import Cmd from './Cmd.vue'
import { injectClients } from '../../../states/clients'
import { provideCmd } from '../../../states/cmd'
import { reactive, toRefs } from '@vue/reactivity'
provideCmd
export default {
    components: { SettingModal, Cmd },
    setup () {

        const clientState = injectClients();
        const state = reactive({
            clientId: null
        });
        provideCmd(state);
        return {
            ...toRefs(state), ...toRefs(clientState)
        }
    }
}
</script>

<style lang="stylus" scoped>
.cmd-setting-wrap
    padding: 2rem;
    box-sizing: border-box;

    .head
        margin-bottom: 1rem;
</style>

<style lang="stylus">
.cmd-setting-wrap
    .el-tabs__content
        flex: 1 1 0%;
</style>
