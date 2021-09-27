<!--
 * @Author: snltty
 * @Date: 2021-09-26 19:43:21
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-27 20:49:26
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ftp\Progress.vue
-->
<template>
    <div class="progress flex">
        <div class="upload flex-1 relative">
            <div class="absolute">
                <el-table :data="upload" size="mini" height="100%">
                    <el-table-column prop="FileName" label="文件名（上传）"></el-table-column>
                    <el-table-column prop="TotalLength" label="大小" width="100">
                        <template #default="scope">
                            <span>{{scope.row.TotalLength.sizeFormat()}} </span>
                        </template>
                    </el-table-column>
                    <el-table-column prop="IndexLength" label="进度" width="100">
                        <template #default="scope">
                            <span>{{((scope.row.IndexLength/scope.row.TotalLength)*100).toFixed(2)}} </span>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
        <div class="split"></div>
        <div class="download flex-1 relative">
            <div class="absolute">
                <el-table :data="download" size="mini" height="100%">
                    <el-table-column prop="FileName" label="文件名（下载）"></el-table-column>
                    <el-table-column prop="TotalLength" label="大小" width="100">
                        <template #default="scope">
                            <span>{{scope.row.TotalLength.sizeFormat()}} </span>
                        </template>
                    </el-table-column>
                    <el-table-column prop="IndexLength" label="进度" width="100">
                        <template #default="scope">
                            <span>{{((scope.row.IndexLength/scope.row.TotalLength)*100).toFixed(2)}} </span>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
    </div>
</template>

<script>
import { reactive, toRefs } from '@vue/reactivity'
import { subNotifyMsg, unsubNotifyMsg } from '../../../apis/request'
import { onMounted, onUnmounted } from '@vue/runtime-core';
export default {
    setup () {
        const state = reactive({
            upload: [],
            download: []
        });

        const subFunc = (info) => {
            state.upload = info.Uploads;
            state.download = info.Downloads;
        }
        onMounted(() => {
            subNotifyMsg('ftp/info', subFunc);
        });
        onUnmounted(() => {
            unsubNotifyMsg('ftp/info', subFunc);
        });

        return {
            ...toRefs(state)
        }
    }
}
</script>

<style lang="stylus" scoped>
.progress
    height: 30rem;
    width: 100%;

    .split
        width: 0.6rem;

    .upload, .download
        height: 100%;
        border: 1px solid #ddd;
</style>