<!--
 * @Author: snltty
 * @Date: 2021-09-24 10:29:45
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-25 00:57:57
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\album\Index.vue
-->
<template>
    <div class="flex flex-column flex-nowrap h-100">
        <div class="head t-c flex">
            <el-select v-model="clientIndex" placeholder="请选择目标" size="mini" @change="onClientChange">
                <el-option v-for="(item,index) in clients" :key="index" :label="`${item.SourcePort} -> 【${item.TargetName}】${item.TargetPort}`" :value="index">
                </el-option>
            </el-select>
            <SettingModal className="AlbumSettingPlugin" @success="loadClients">
                <el-button size="mini">配置插件</el-button>
            </SettingModal>
            <el-button size="mini" @click="loadClients" :loading="loading">刷新目标</el-button>
            <div class="flex-1"></div>
            <el-button type="warning" size="mini" @click="handleVerify" :loading="loading">管理验证</el-button>
        </div>
        <div class="body flex-1 scrollbar">
            <Categorys v-if="category == null"></Categorys>
            <Album v-else>
                <el-button size="mini" @click="category = null">返回相册列表</el-button>
            </Album>
        </div>
    </div>
</template>

<script>
import { reactive, toRefs } from '@vue/reactivity'
import { loadSetting } from '../../../apis/plugins/album'
import Axios from '../../../apis/axios'
import { onMounted, provide } from '@vue/runtime-core'
import SettingModal from '../SettingModal.vue'
import Categorys from './Categorys.vue'
import Album from './Album.vue'
import { ElMessageBox, ElMessage } from 'element-plus'
export default {
    components: { SettingModal, Categorys, Album },
    setup () {
        const state = reactive({
            clients: [],
            clientIndex: null,
            serverPort: 0,
            loading: false
        });
        const shareData = reactive({
            category: null,
            axios: null,
            token: sessionStorage.getItem('token') || '',
            baseURL: ''
        });
        provide('share-data', shareData);

        const loadClients = () => {
            state.loading = true;
            loadSetting().then((res) => {
                state.loading = false;
                state.clients = res.Clients;
                if (state.clients.length > 0 && state.clientIndex == null) {
                    state.clientIndex = 0;
                }
                state.serverPort = res.ServerPort;
                onClientChange();
            });
        }
        const onClientChange = () => {
            let url = '';
            if (process.env.NODE_ENV == 'development' || state.clientIndex === null) {
                url = `http://127.0.0.1:${state.serverPort}`
            } else {
                url = `http://127.0.0.1:${state.clients[state.clientIndex].SourcePort}`
            }
            shareData.baseURL = url;
            shareData.axios = new Axios({ baseURL: url })
        }
        const handleVerify = () => {
            ElMessageBox.prompt('输入目标端配置的口令，验证管理权限', '管理验证', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
            }).then(({ value }) => {
                state.loading = true;
                shareData.axios.post({
                    url: '/verify/verify',
                    params: { password: value }
                }).then((res) => {
                    state.loading = false;
                    if (res.data.code == 0) {
                        if (res.data.data) {
                            shareData.token = res.data.data;
                            sessionStorage.setItem('token', shareData.token);
                            ElMessage.success('管理验证成功！');
                        } else {
                            ElMessage.error('口令不正确，验证失败');
                        }
                    } else {
                        ElMessage.error(res.data.msg);
                    }
                }).catch((e) => {
                    state.loading = false;
                    ElMessage.error(e.response.data);
                });
            })
        }

        onMounted(() => {
            loadClients();
        });

        return {
            ...toRefs(state), ...toRefs(shareData), loadClients, onClientChange, handleVerify
        }
    }
}
</script>
<style lang="stylus" scoped>
.head
    padding: 2rem;

    .el-button
        margin-left: 0.4rem;
</style>