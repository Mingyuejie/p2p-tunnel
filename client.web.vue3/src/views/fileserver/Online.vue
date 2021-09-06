<!--
 * @Author: snltty
 * @Date: 2021-09-05 19:49:43
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-06 15:00:12
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\fileserver\Online.vue
-->
<template>
    <el-table v-loading="loading" :data="list" border size="mini" height="100%">
        <el-table-column prop="FileName" label="文件名">
            <template #default="scope">
                <span>{{scope.row.FileType}} {{scope.row.FileName}}</span>
            </template>
        </el-table-column>
        <el-table-column prop="TotalLength" label="大小" width="120">
            <template #default="scope">
                <span>{{(scope.row.TotalLength/1024/1024).toFixed(2)}} MB</span>
            </template>
        </el-table-column>
        <el-table-column prop="Label" label="已下载" width="200">
            <template #default="scope">
                <span>{{(scope.row.IndexLength/1024/1024).toFixed(2)}} MB，{{Math.floor((scope.row.IndexLength/scope.row.TotalLength)*100)}}%</span>
            </template>
        </el-table-column>
    </el-table>
</template>

<script>
import { toRefs, reactive } from '@vue/reactivity';
import { subNotifyMsg, unsubNotifyMsg } from '../../apis/request'
import { onMounted, onUnmounted } from '@vue/runtime-core';
export default {
    emits: ['on-change'],
    setup (props, { emit }) {
        const state = reactive({
            list: [],
            loading: false
        });
        const onlineMsg = (msg) => {
            let arr = JSON.parse(msg);
            arr.forEach(c => {
                c.FileType = { 'DOWNLOAD': '↓', 'UPLOAD': '↑' }[c.FileType];
            });
            if (arr.length < state.list.length) {
                emit('on-change');
            }
            state.list = arr;
        }
        onMounted(() => {
            subNotifyMsg('fileserver/online', onlineMsg);
        });
        onUnmounted(() => {
            unsubNotifyMsg('fileserver/online', onlineMsg);
        })

        return {
            ...toRefs(state)
        }
    }
}
</script>

<style lang="stylus" scoped></style>