<!--
 * @Author: snltty
 * @Date: 2021-09-26 19:43:21
 * @LastEditors: xr
 * @LastEditTime: 2022-01-23 14:14:23
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\service\ftp\Progress.vue
-->
<template>
    <div class="progress flex">
        <div class="upload flex-1 relative">
            <div class="absolute">
                <el-table :data="upload" size="mini" height="100%" @row-contextmenu="handleLocalContextMenu">
                    <el-table-column prop="FileName" label="文件名（上传）"></el-table-column>
                    <el-table-column prop="TotalLength" label="大小" width="100">
                        <template #default="scope">
                            <p>{{scope.row.TotalLength.sizeFormat()}} </p>
                        </template>
                    </el-table-column>
                    <el-table-column prop="State" label="状态" width="100">
                        <template #default="scope">
                            <span>{{states[scope.row.State]}} </span>
                        </template>
                    </el-table-column>
                    <el-table-column prop="IndexLength" label="进度" width="100">
                        <template #default="scope">
                            <p>{{((scope.row.IndexLength/scope.row.TotalLength)*100).toFixed(2)}}%</p>
                            <p>{{scope.row.Speed.sizeFormat()}}/s</p>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
        <span class="split"></span>
        <div class="download flex-1 relative">
            <div class="absolute">
                <el-table :data="download" size="mini" height="100%" @row-contextmenu="handleRemoteContextMenu">
                    <el-table-column prop="FileName" label="文件名（下载）"></el-table-column>
                    <el-table-column prop="TotalLength" label="大小" width="100">
                        <template #default="scope">
                            <span>{{scope.row.TotalLength.sizeFormat()}} </span>
                        </template>
                    </el-table-column>
                    <el-table-column prop="IndexLength" label="进度" width="100">
                        <template #default="scope">
                            <p>{{((scope.row.IndexLength/scope.row.TotalLength)*100).toFixed(2)}}%</p>
                            <p>{{scope.row.Speed.sizeFormat()}}/s</p>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
        <ContextMenu ref="contextMenu"></ContextMenu>
    </div>
</template>

<script>
import { reactive, toRefs, ref } from '@vue/reactivity'
import { subNotifyMsg, unsubNotifyMsg, pushListener } from '../../../apis/request'
import { sendLocalCancel, sendRemoteCancel } from '../../../apis/service/ftp'
import { onMounted, onUnmounted } from '@vue/runtime-core';
import ContextMenu from './ContextMenu.vue'
import { injectFilesData } from './list-share-data'
import { ElMessageBox } from 'element-plus'
import { injectCmd } from '../../../states/cmd'
export default {
    components: { ContextMenu },
    setup () {

        const listShareData = injectFilesData();
        const stateCmd = injectCmd();
        const state = reactive({
            upload: [],
            download: [],
            states: ['等待中', '上传中', '正在取消', '错误的']
        });

        const subFunc = (info) => {
            if (info.Uploads.length < state.upload.length) {
                pushListener.push('ftp.progress.upload');
            }
            if (info.Downloads.length < state.download.length) {
                pushListener.push('ftp.progress.download');
            }

            state.upload = info.Uploads;
            state.download = info.Downloads;
        }
        onMounted(() => {
            subNotifyMsg('ftp/info', subFunc);
        });
        onUnmounted(() => {
            unsubNotifyMsg('ftp/info', subFunc);
        });


        const contextMenu = ref(null);
        const handleLocalContextMenu = (row, column, event) => {
            if (!state.loading && row.Name != '..') {
                contextMenu.value.show(event, [
                    {
                        text: '取消上传', handle: () => {
                            ElMessageBox.confirm(`取消上传,【${row.FileName}】`, '取消上传', {
                                confirmButtonText: '确定',
                                cancelButtonText: '取消',
                                type: 'warning'
                            }).then(() => {
                                state.loading = true;
                                sendLocalCancel(stateCmd.clientId || 0, row.Md5).then(() => {
                                    state.loading = false;
                                }).catch(() => {
                                    state.loading = false;
                                });;
                            });
                        }
                    }
                ]);
            }
            event.preventDefault();
        }
        const handleRemoteContextMenu = (row, column, event) => {
            if (!state.loading && row.Name != '..') {
                contextMenu.value.show(event, [
                    {
                        text: '取消下载', handle: () => {
                            ElMessageBox.confirm(`取消下载,【${row.FileName}】`, '取消下载', {
                                confirmButtonText: '确定',
                                cancelButtonText: '取消',
                                type: 'warning'
                            }).then(() => {
                                state.loading = true;
                                sendRemoteCancel(stateCmd.clientId || 0, row.Md5).then((res) => {
                                    state.loading = false;
                                }).catch(() => {
                                    state.loading = false;
                                });;
                            });
                        }
                    }
                ]);
            }
            event.preventDefault();
        }

        return {
            ...toRefs(state), contextMenu, handleLocalContextMenu, handleRemoteContextMenu
        }
    }
}
</script>

<style lang="stylus" scoped>
.el-table::before
    height: 0;

.progress
    height: 30rem;
    width: 100%;

    .upload, .download
        height: 100%;
        border: 1px solid #ddd;
</style>