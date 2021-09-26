<!--
 * @Author: snltty
 * @Date: 2021-09-26 19:43:21
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-26 20:22:24
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ftp\Progress.vue
-->
<template>
    <div class="progress flex">
        <div class="upload flex-1">
            <el-table :data="upload" border size="mini" height="100%">
                <el-table-column prop="FileName" label="文件名（上传）"></el-table-column>
                <el-table-column prop="TotalLength" label="大小" width="100"></el-table-column>
            </el-table>
        </div>
        <div class="split"></div>
        <div class="download flex-1">
            <el-table :data="download" border size="mini" height="100%">
                <el-table-column prop="FileName" label="文件名（下载）"></el-table-column>
                <el-table-column prop="TotalLength" label="大小" width="100"></el-table-column>
            </el-table>
        </div>
    </div>

</template>

<script>
import { reactive, toRefs } from '@vue/reactivity'
export default {
    setup () {
        const state = reactive({
            upload: [],
            download: []
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
</style>