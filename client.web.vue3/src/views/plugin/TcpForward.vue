<!--
 * @Author: snltty
 * @Date: 2021-08-20 00:47:21
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-23 21:12:21
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\TcpForward.vue
-->
<template>
    <div class="forward-wrap">
        <div class="head flex">
            <el-button type="primary" size="mini" @click="handleAdd">增加转发</el-button>
            <el-button size="mini" @click="getData">刷新列表</el-button>
            <span class="flex-1"></span>
            <SettingModal className="TcpForwardPlugin">
                <el-button size="mini">配置插件</el-button>
            </SettingModal>
        </div>
        <el-table v-loading="loading" :data="list" border size="mini">
            <el-table-column prop="Desc" label="说明"></el-table-column>
            <el-table-column prop="Sopurce" label="来源">
                <template #default="scope">
                    <span>{{scope.row.SourceIp}}:{{scope.row.SourcePort}}</span>
                </template>
            </el-table-column>
            <el-table-column prop="Desc" label="目标">
                <template #default="scope">
                    <span>【{{scope.row.TargetName}}】</span>
                    <span>{{scope.row.TargetIp}}:{{scope.row.TargetPort}}</span>
                </template>
            </el-table-column>
            <el-table-column prop="AliveType" label="连接类型" width="80">
                <template #default="scope"><span>{{aliveTypes[scope.row.AliveType]}}</span></template>
            </el-table-column>
            <el-table-column prop="Listening" label="状态" width="65">
                <template #default="scope">
                    <el-switch :disabled="!scope.row.Editable" @click.stop @change="onListeningChange(scope.row)" v-model="scope.row.Listening"></el-switch>
                </template>
            </el-table-column>
            <el-table-column prop="todo" label="操作" width="145" fixed="right" class="t-c">
                <template #default="scope">
                    <el-button size="mini" :disabled="!scope.row.Editable" @click="handleEdit(scope.row)">编辑</el-button>
                    <el-popconfirm title="删除不可逆，是否确认" @confirm="handleDel(scope.row)">
                        <template #reference>
                            <el-button type="danger" :disabled="!scope.row.Editable" size="mini" icon="el-icon-delete"></el-button>
                        </template>
                    </el-popconfirm>
                </template>
            </el-table-column>
        </el-table>
        <div class="remark">
            <el-alert title="说明" type="info" show-icon :closable="false">
                <p style="line-height:2rem">1、源端是你的，目标是对方的</p>
                <p style="line-height:2rem">2、例如 源是<strong>127.0.0.1:8080</strong>，目标是<strong>【B客户端】</strong>的<strong>127.0.0.1:12138</strong> ，则在你电脑访问 127.0.0.1:8080则会访问到 B客户端的127.0.0.1:12138服务</p>
                <p style="line-height:2rem">3、web是短连接，其它服务长链接</p>
            </el-alert>
        </div>
        <el-dialog title="转发" destroy-on-close v-model="showAdd" center :close-on-click-modal="false" width="600px">
            <el-form ref="formDom" :model="form" :rules="rules" label-width="80px">
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :span="12">
                            <el-form-item label="源IP" prop="SourceIp">
                                <el-input v-model="form.SourceIp"></el-input>
                            </el-form-item>
                        </el-col>
                        <el-col :span="12">
                            <el-form-item label="源端口" prop="SourcePort">
                                <el-input v-model="form.SourcePort"></el-input>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :span="12">
                            <el-form-item label="目标" prop="TargetName">
                                <el-select v-model="form.TargetName" placeholder="选择类型">
                                    <el-option v-for="(item,index) in clients" :key="index" :label="item.Name" :value="item.Name">
                                    </el-option>
                                </el-select>
                            </el-form-item>
                        </el-col>
                        <el-col :span="12">
                            <el-form-item label="连接类型" prop="AliveType">
                                <el-select v-model="form.AliveType" placeholder="选择类型">
                                    <el-option v-for="(item,index) in aliveTypes" :key="index" :label="item" :value="index">
                                    </el-option>
                                </el-select>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :span="12">
                            <el-form-item label="目标IP" prop="TargetIp">
                                <el-input v-model="form.TargetIp"></el-input>
                            </el-form-item>
                        </el-col>
                        <el-col :span="12">
                            <el-form-item label="目标端口" prop="TargetPort">
                                <el-input v-model="form.TargetPort"></el-input>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="说明" prop="Desc">
                    <el-input v-model="form.Desc"></el-input>
                </el-form-item>
            </el-form>
            <template #footer>
                <el-button @click="showAdd = false">取 消</el-button>
                <el-button type="primary" :loading="loading" @click="handleSubmit">确 定</el-button>
            </template>
        </el-dialog>
    </div>
</template>
<script>
import { reactive, ref, toRefs } from '@vue/reactivity'
import { getTcpForwards, sendTcpForwardAdd, sendTcpForwardDel, sendTcpForwardStart, sendTcpForwardStop } from '../../apis/plugins/tcp-forward'
import { ElMessage } from 'element-plus'
import { injectClients } from '../../states/clients'
import SettingModal from './setting/SettingModal.vue'
export default {
    components: { SettingModal },
    setup () {

        const clientsState = injectClients();
        const state = reactive({
            loading: false,
            showAdd: false,
            list: [],
            aliveTypes: ['长连接', '短连接'],
            form: {
                ID: 0,
                SourceIp: '0.0.0.0', SourcePort: 0,
                TargetName: 'B客户端', TargetIp: '127.0.0.1', TargetPort: 0,
                AliveType: 0,
                Desc: ''
            },
            rules: {
                SourceIp: [{ required: true, message: '必填', trigger: 'blur' }],
                SourcePort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
                TargetIp: [{ required: true, message: '必填', trigger: 'blur' }],
                TargetPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
            }
        });

        const getData = () => {
            getTcpForwards().then((res) => {
                state.list = res;
            });
        };
        getData();

        const handleDel = (row) => {
            sendTcpForwardDel(row.ID).then(() => {
                getData();
            });
        }
        const defaultForm = JSON.parse(JSON.stringify(state.form));
        const handleAdd = () => {
            state.showAdd = true;
            for (let j in defaultForm) {
                state.form[j] = defaultForm[j];
            }
        }
        const handleEdit = (row) => {
            state.showAdd = true;
            for (let j in row) {
                state.form[j] = row[j];
            }
        }
        const formDom = ref(null);
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                state.loading = true;
                state.form.SourcePort = Number(state.form.SourcePort)
                state.form.TargetPort = Number(state.form.TargetPort);
                sendTcpForwardAdd(state.form).then(() => {
                    state.loading = false;
                    state.showAdd = false;
                    getData();
                }).catch((e) => {
                    ElMessage.error(e);
                    state.loading = false;
                });
            })
        }

        const onListeningChange = (row) => {
            if (!row.Listening) {
                sendTcpForwardStop(row.ID).then(getData).catch(getData);
            } else {
                sendTcpForwardStart(row.ID).then(getData).catch(getData);
            }
        }

        return {
            ...toRefs(state), ...toRefs(clientsState), getData, handleDel, handleAdd, handleEdit, onListeningChange,
            formDom, handleSubmit
        }
    }
}
</script>
<style lang="stylus" scoped>
.forward-wrap
    padding: 2rem;

    .head
        margin-bottom: 1rem;

.remark
    margin-top: 1rem;
</style>