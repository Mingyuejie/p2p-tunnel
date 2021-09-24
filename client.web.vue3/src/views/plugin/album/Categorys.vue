<!--
 * @Author: snltty
 * @Date: 2021-09-24 15:24:48
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-24 17:19:03
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\album\Categorys.vue
-->
<template>
    <div class="flex flex-column flex-nowrap h-100">
        <div class="head t-c">
            <el-button size="mini" @click="loadData" :loading="loading">刷新相册</el-button>
            <el-button size="mini" type="primary" @click="loadData" v-if="shareData.token" :loading="loading">创建相册</el-button>
        </div>
        <div class="body flex-1 scrollbar">
            <template v-for="(item,index) in categorys" :key="index">
                <div class="item" @click="handleClick(item)">
                    <el-image :src="item.cover">
                        <template #error>
                            <div class="image-slot">
                                <i class="el-icon-picture-outline"></i>
                            </div>
                        </template>
                    </el-image>
                    <div class="name">{{item.name}}</div>
                    <div class="time">{{item.addTime}}</div>
                </div>
            </template>
        </div>
        <div class="pages t-c">
            <el-pagination background layout="prev, pager, next,sizes" :total="1000"></el-pagination>
        </div>
    </div>
</template>

<script>
import { reactive, toRefs } from '@vue/reactivity';
import { inject, watch } from '@vue/runtime-core';
import { ElMessage } from 'element-plus'
export default {
    setup (props) {

        const shareData = inject('share-data');
        const state = reactive({
            categorys: [],
            loading: false
        });
        watch(() => shareData.axios, () => {
            loadData();
        });

        const loadData = () => {
            if (shareData.axios == null) {
                return;
            }
            state.loading = true;
            shareData.axios.get({
                url: 'category/list',
                headers: { token: shareData.token }
            }).then((res) => {
                state.loading = false;
                if (res.data.code == 0) {
                    state.categorys = res.data.data.map(c => {
                        c.addTime = new Date(c.addTime * 1000).format('yyyy-MM-dd hh:mm:ss');
                        return c;
                    });
                } else {
                    ElMessage.error(res.data.msg);
                }
            }).catch((e) => {
                state.loading = false;
                ElMessage.error(e.response.data);
            });
        }
        loadData();
        const handleClick = (item) => {
            shareData.cid = 0;
            shareData.cid = item.id;
        }

        return {
            ...toRefs(state), loadData, handleClick, shareData
        }
    }
}
</script>
<style lang='stylus' scoped>
.body
    padding: 1rem 2rem 1rem 1rem;

.item
    width: 14.92rem;
    text-align: center;
    border: 1px solid #ddd;
    display: inline-block;
    padding: 0.6rem;
    margin: 0 0 1rem 1rem;
    transition: 0.3s;
    cursor: pointer;

    &:hover
        background-color: #f5f5f5;

    .name, .time
        line-height: 2rem;
        color: #666;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;

    .el-image
        width: 100%;
        height: 15rem;
        background-color: #f5f5f5;

        .image-slot
            display: flex;
            justify-content: center;
            align-items: center;
            width: 100%;
            height: 100%;
            background: #f5f7fa;
            font-size: 5rem;
            color: #ddd;

.pages
    padding: 1rem 0;
</style>