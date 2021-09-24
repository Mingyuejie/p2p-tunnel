<!--
 * @Author: snltty
 * @Date: 2021-09-24 15:24:48
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-24 17:18:53
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\album\Album.vue
-->
<template>
    <div class="flex flex-column flex-nowrap h-100">
        <div class="head t-c">
            <el-button size="mini" @click="loadData" :loading="loading">刷新图片列表</el-button>
            <el-button size="mini" type="primary" @click="loadData" v-if="shareData.token" :loading="loading">上传图片</el-button>
            <slot></slot>
        </div>
        <div class="body flex-1 scrollbar"></div>
        <div class="pages t-c">
            <el-pagination background layout="prev, pager, next,sizes" :total="1000"></el-pagination>
        </div>
    </div>
</template>

<script>
import { reactive, toRefs } from '@vue/reactivity';
import { inject } from '@vue/runtime-core';
import { ElMessage } from 'element-plus'
export default {
    setup (props) {

        const shareData = inject('share-data');
        const state = reactive({
            data: [],
            loading: false
        });
        const loadData = () => {
            if (shareData.axios == null) {
                return;
            }
            state.loading = true;
            shareData.axios.get({
                url: 'album/list',
                params: { cid: shareData.cid },
                headers: { token: shareData.token }
            }).then((res) => {
                state.loading = false;
                if (res.data.code == 0) {
                    state.data = res.data.data;
                } else {
                    ElMessage.error(res.data.msg);
                }
            }).catch((e) => {
                state.loading = false;
                ElMessage.error(e.response.data);
            });
        }
        loadData();

        return {
            ...toRefs(state), loadData, shareData
        }
    }
}
</script>
<style lang='stylus' scoped>
.pages
    padding: 1rem 0;
</style>