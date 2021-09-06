<!--
 * @Author: snltty
 * @Date: 2021-09-05 12:43:00
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-06 14:48:26
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\fileserver\Remote.vue
-->
<template>
    <div class="h-100 flex flex-column">
        <div class="header">
            <el-select v-model="remoteState.clientId" placeholder="请选择目标客户端" size="mini" @change="handleClientChange">
                <el-option v-for="item in clients" :key="item.Id" :label="item.Name" :value="item.Id">
                </el-option>
            </el-select>
            <el-button size="mini" @click="handleClientChange" :loading="loading">刷新列表</el-button>
        </div>
        <div class="body flex-1 relative">
            <div class="absolute">
                <el-table v-loading="loading" :data="list" border size="mini" height="100%" :span-method="objectSpanMethod" @row-dblclick="handleRowDblclick">
                    <el-table-column prop="Name" label="名称">
                        <template #default="scope">
                            <span v-if="scope.row.Image">
                                <img :src="'data:image/jpg;base64,'+scope.row.Image" height="30" class="img" />
                            </span>
                            <span>{{scope.row.Name}}</span>
                            <span v-if="scope.row.Type ==-1">({{scope.row.FullName}})</span>
                        </template>
                    </el-table-column>
                    <el-table-column prop="Length" label="大小" width="120 ">
                        <template #default="scope">
                            <span v-if="scope.row.Type !=0">{{(scope.row.Length/1024/1024).toFixed(2)}} MB</span>
                        </template>
                    </el-table-column>
                    <el-table-column prop="todo" label="操作" width="80" fixed="right" class="t-c">
                        <template #default="scope">
                            <el-button size="mini" v-if="scope.row.Type ==1" @click="handleDownload(scope.row)">下载</el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
    </div>
</template>
<script>
import { computed, reactive, toRefs } from '@vue/reactivity';
import { getRemoteFiles, sendDownload } from '../../apis/file-server'
import { injectClients } from '../../states/clients'
import { inject, onMounted } from '@vue/runtime-core';
import { ElMessageBox } from 'element-plus'
export default {
    setup () {
        const remoteState = inject('remote');
        const localState = inject('local');
        const clientsState = injectClients();
        const clients = computed(() => [{ Name: '请选择目标客户端', Id: 0 }].concat(clientsState.clients));
        const state = reactive({
            list: [],
            loading: false
        });
        const loadDir = (path = '') => {
            state.loading = true;
            getRemoteFiles(path, +remoteState.clientId).then((res) => {
                remoteState.files = state.list = [{
                    Name: '..上级',
                    FullName: '..',
                    Type: 0,
                    Length: 0,
                }, ...JSON.parse(res)];
                state.loading = false;
            }).catch((msg) => {
                state.loading = false;
            });
        }
        const handleRowDblclick = (row) => {
            if (row.Type != 1) {
                if (row.FullName == '..') {
                    loadDir(row.FullName)
                } else {
                    loadDir(row.Name)
                }
            }
        }
        onMounted(() => {
            loadDir();
        });

        const handleClientChange = () => {
            loadDir();
        }
        const reload = handleClientChange;
        const handleDownload = (row) => {
            if (localState.files.filter(c => c.Name == row.Name).length > 0) {
                ElMessageBox.confirm('文件已存在，是否确定下载?', '提示', {
                    confirmButtonText: '确定',
                    cancelButtonText: '取消',
                    type: 'warning'
                }).then(() => {
                    sendDownload(row.Name, remoteState.clientId);
                }).catch(() => {
                });
            } else {
                sendDownload(row.Name, remoteState.clientId);
            }
        }
        const objectSpanMethod = ({ row, column, rowIndex, columnIndex }) => {
            if (column.property == 'Name' && row.Type == 0) {
                return { colspan: 3, rowspan: 1 }
            }
        }
        return {
            ...toRefs(state), clients, remoteState, reload,
            handleRowDblclick, objectSpanMethod, handleClientChange, handleDownload
        }
    }
}
</script>
<style lang="stylus" scoped>
.header
    padding: 0 0 0.6rem 0;

.img
    vertical-align: middle;
</style>