<!--
 * @Author: snltty
 * @Date: 2021-09-30 14:25:13
 * @LastEditors: xr
 * @LastEditTime: 2022-01-23 14:20:33
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\service\cmd\Index.vue
-->
<template>
    <div class="cmd-setting-wrap flex flex-column h-100">
        <div class="head flex">
            <ConfigureModal className="CmdClientConfigure">
                <el-button size="mini">配置插件</el-button>
            </ConfigureModal>
            <span class="split"></span>
            <el-select v-model="clientId" placeholder="请选择已连接的目标客户端" size="mini">
                <template v-for="client in clients" :key="client.Id">
                    <el-option :label="client.Name" :value="client.Id">
                    </el-option>
                </template>
            </el-select>
        </div>
        <div class="body flex-1 relative">
            <Cmd></Cmd>
        </div>
    </div>
</template>

<script>
import ConfigureModal from '../configure/ConfigureModal.vue'
import Cmd from './Cmd.vue'
import { injectClients } from '../../../states/clients'
import { provideCmd } from '../../../states/cmd'
import { reactive, toRefs } from '@vue/reactivity'
export default {
    components: { ConfigureModal, Cmd },
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
