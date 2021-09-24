<!--
 * @Author: snltty
 * @Date: 2021-09-24 15:24:48
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-25 01:32:42
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\album\Categorys.vue
-->
<template>
    <div class="flex flex-column flex-nowrap h-100">
        <div class="head t-c">
            <el-button size="mini" @click="loadData" :loading="loading">刷新相册</el-button>
            <el-button size="mini" type="primary" @click="handleAdd" v-if="shareData.token" :loading="loading">创建相册</el-button>
        </div>
        <div class="body flex-1 scrollbar">
            <template v-for="(item,index) in page.data" :key="index">
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
                    <div class="oper" @click.stop>
                        <el-dropdown size="mini">
                            <el-button size="mini">
                                <i class="el-icon-arrow-down"></i>
                            </el-button>
                            <template #dropdown>
                                <el-dropdown-menu>
                                    <el-dropdown-item @click="handleEdit(item)">编辑</el-dropdown-item>
                                    <el-dropdown-item @click="handleDel(item)">删除</el-dropdown-item>
                                </el-dropdown-menu>
                            </template>
                        </el-dropdown>
                    </div>
                </div>
            </template>
        </div>
        <div class="pages t-c">
            <el-pagination :total="page.count" v-model:currentPage="page.page" :page-size="page.pageSize" @current-change="loadData" background layout="total,prev, pager, next">
            </el-pagination>
        </div>
    </div>
</template>

<script>
import { reactive, toRefs } from '@vue/reactivity';
import { inject, watch } from '@vue/runtime-core';
import { ElMessage, ElMessageBox } from 'element-plus'
export default {
    setup (props) {

        const shareData = inject('share-data');
        const state = reactive({
            page: { page: 1, pageSize: 15, count: 0, data: [] },
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
                params: { p: state.page.page, ps: state.page.pageSize },
                headers: { token: shareData.token }
            }).then((res) => {
                state.loading = false;
                if (res.data.code == 0) {
                    res.data.data.data.forEach(c => {
                        c.addTime = new Date(c.addTime * 1000).format('yyyy-MM-dd hh:mm:ss');
                        if (c.cover) {
                            c.cover = shareData.baseURL + c.cover;
                        }
                    });
                    state.page = res.data.data;
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
            shareData.category = null;
            shareData.category = item;
        }

        const addEditModal = (data) => {
            ElMessageBox.prompt('输入名称', data.id == 0 ? '新增相册' : '编辑相册', {
                confirmButtonText: '确定',
                inputPattern: /.{1,100}/,
                inputErrorMessage: '1-20个字符',
                cancelButtonText: '取消',
                inputValue: data.name
            }).then(({ value }) => {
                shareData.axios.post({
                    url: `/Category/${data.id == 0 ? 'add' : 'editname'}`,
                    data: data.id == 0 ? { name: value, cover: '' } : { id: data.id, name: value },
                    headers: { token: shareData.token }
                }).then((res) => {
                    if (res.data.code == 0) {
                        loadData();
                    } else {
                        ElMessage.error(res.data.msg);
                    }
                }).catch((e) => {
                    ElMessage.error(e.response.data);
                });
            })
        }
        const handleAdd = () => {
            addEditModal({ id: 0, name: '' });
        }
        const handleEdit = (item) => {
            addEditModal(item);
        }
        const handleDel = (item) => {
            ElMessageBox.confirm('删除不可逆，是否确认?', '删除相册',
                {
                    confirmButtonText: '确定',
                    cancelButtonText: '取消',
                    type: 'warning',
                }).then(() => {
                    shareData.axios.post({
                        url: '/Category/del',
                        params: { ids: item.id },
                        headers: { token: shareData.token }
                    }).then((res) => {
                        if (res.data.code == 0) {
                            loadData();
                        } else {
                            ElMessage.error(res.data.msg);
                        }
                    }).catch((e) => {
                        ElMessage.error(e.response.data);
                    });
                });
        }

        return {
            ...toRefs(state), loadData, handleClick, shareData, handleAdd, handleEdit, handleDel
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
    position: relative;

    &:hover
        background-color: #f5f5f5;

        .oper
            display: block;

    .oper
        display: none;
        position: absolute;
        right: 0.6rem;
        top: 0.6rem;

        .el-button
            padding: 0.3rem 0.4rem;
            min-height: auto;

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