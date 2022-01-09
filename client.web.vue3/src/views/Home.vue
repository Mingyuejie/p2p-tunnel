<!--
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: xr
 * @LastEditTime: 2022-01-08 17:22:22
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\Home.vue
-->
<template>
    <div class="home">
        <el-table :data="clients" border size="mini">
            <el-table-column prop="Name" label="客户端">
                <template #default="scope">
                    <div @click="handleClientClick(scope.row)">
                        <span style="margin-right:.6rem">{{scope.row.Name}}</span>
                        <el-tag v-if="scope.row.islocal" size="mini" effect="plain">局域网({{scope.row.Ip}})</el-tag>
                        <el-tag v-else size="mini" effect="plain" type="success">广域网({{scope.row.Ip}})</el-tag>
                    </div>
                </template>
            </el-table-column>
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
                <p style="line-height:2rem">1、注册信息里 [<strong>客户信息</strong>]的<strong>【TCP端口】</strong>与 [<strong>注册信息</strong>]的<strong>【TCP端口】</strong>一致时，被连接成功概率高</p>
                <p style="line-height:2rem">2、所以会有 【连它】和 【连我】 之分，尽量让两个TCP端口一致的一方作为被连接的一方</p>
            </el-alert>
        </div>
    </div>
    <el-dialog title="试一下发送数据效率" v-model="showTest" center :close-on-click-modal="false" width="50rem">
        <el-form ref="formDom" :model="form" :rules="rules" label-width="10rem">
            <el-form-item label="包数量" prop="Count">
                <el-input v-model="form.Count" />
            </el-form-item>
            <el-form-item label="包大小(KB)" prop="KB">
                <el-input v-model="form.KB" />
            </el-form-item>
            <el-form-item label="结果" prop="">
                {{result}}
            </el-form-item>
        </el-form>
        <template #footer>
            <el-button @click="showTest = false">取 消</el-button>
            <el-button type="primary" :loading="loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { computed, reactive, ref, toRefs } from '@vue/reactivity';
import { injectClients } from '../states/clients'
import { injectRegister } from '../states/register'
import { sendClientConnect, sendClientConnectReverse } from '../apis/clients'
import { sendReset } from '../apis/reset'
import { sendPacketTest } from '../apis/test'
export default {
    name: 'Home',
    components: {},
    setup () {
        const clientsState = injectClients();
        const registerState = injectRegister();
        const localIp = computed(() => registerState.LocalInfo.LocalIp.split('.').slice(0, 3).join('.'));

        const handleConnect = (row) => {
            sendClientConnect(row.Id);
        }
        const handleConnectReverse = (row) => {
            sendClientConnectReverse(row.Id);
        }
        const handleReset = (row) => {
            sendReset(row.Id);
        }

        const state = reactive({
            showTest: false,
            loading: false,
            Id: 0,
            result: '',
            form: {
                Count: 10000,
                KB: 1
            },
            rules: {
                Count: [
                    { required: true, message: '必填', trigger: 'blur' },
                ],
                KB: [
                    { required: true, message: '必填', trigger: 'blur' }
                ]
            }
        });
        const formDom = ref(null);
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                state.loading = true;
                sendPacketTest(state.Id, state.form.Count, state.form.KB).then((res) => {
                    state.loading = false;
                    state.result = `${res.Ms} ms、${res.Us} us、${res.Ticks} ticks`;
                }).catch((err) => {
                    state.loading = false;
                });
            });
        }
        const handleClientClick = (row) => {
            state.Id = row.Id;
            state.showTest = true;
        }

        return {
            ...toRefs(state), handleSubmit, formDom, handleClientClick,
            ...toRefs(clientsState), handleConnect, handleReset, handleConnectReverse, localIp
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