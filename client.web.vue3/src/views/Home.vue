<!--
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-05 12:44:14
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\Home.vue
-->
<template>
    <div class="home">
        <el-table :data="clients" border size="mini">
            <el-table-column prop="Name" label="客户端"></el-table-column>
            <el-table-column prop="Mac" label="Mac"></el-table-column>
            <el-table-column prop="UDP" label="UDP" width="80">
                <template #default="scope">
                    <el-switch disabled @click.stop v-model="scope.row.Connected"></el-switch>
                </template>
            </el-table-column>
            <el-table-column prop="TCP" label="TCP" width="80">
                <template #default="scope">
                    <el-switch disabled @click.stop v-model="scope.row.TcpConnected"></el-switch>
                </template>
            </el-table-column>
            <el-table-column prop="todo" label="操作" width="280" fixed="right" class="t-c">
                <template #default="scope">
                    <div class="t-c">
                        <el-button :disabled="scope.row.Connected && scope.row.TcpConnected" :loading="scope.row.Connecting || scope.row.TcpConnecting" size="mini" @click="handleConnect(scope.row)">连它</el-button>
                        <el-button :disabled="scope.row.Connected && scope.row.TcpConnected" :loading="scope.row.Connecting || scope.row.TcpConnecting" size="mini" @click="handleConnectReverse(scope.row)">连我</el-button>
                        <el-button :loading="scope.row.Connecting || scope.row.TcpConnecting" size="mini" @click="handleReset(scope.row)">重启它</el-button>
                    </div>
                </template>
            </el-table-column>
        </el-table>
        <div class="remark">
            <el-alert title="说明" type="info" show-icon :closable="false">
                <p style="line-height:2rem">1、注册信息里 [<strong>客户信息</strong>]的<strong>【TCP端口】</strong>与 [<strong>注册信息</strong>]的<strong>【TCP端口】</strong>一致时，连接别人的成功概率高</p>
                <p style="line-height:2rem">2、所以会有 【连它】和 【连我】 之分，尽量让两个TCP端口一致的一方连接另一方</p>
            </el-alert>
        </div>
    </div>
</template>

<script>
import { toRefs } from '@vue/reactivity';
import { injectClients } from '../states/clients'
import { sendClientConnect, sendClientConnectReverse } from '../apis/clients'
import { sendReset } from '../apis/reset'
export default {
    name: 'Home',
    components: {},
    setup () {
        const clientsState = injectClients();

        const handleConnect = (row) => {
            sendClientConnect(row.Id);
        }
        const handleConnectReverse = (row) => {
            sendClientConnectReverse(row.Id);
        }
        const handleReset = (row) => {
            sendReset(row.Id);
        }

        return {
            ...toRefs(clientsState), handleConnect, handleReset, handleConnectReverse
        }

    }
}
</script>
<style lang="stylus" scoped>
.home
    padding: 2rem;

.remark
    margin-top: 1rem;
</style>