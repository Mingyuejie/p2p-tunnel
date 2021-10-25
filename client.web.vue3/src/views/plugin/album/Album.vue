<!--
 * @Author: snltty
 * @Date: 2021-09-24 15:24:48
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-25 16:30:14
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\album\Album.vue
-->
<template>
    <div class="flex flex-column flex-nowrap h-100">
        <div class="head t-c">
            <slot></slot>
            <el-button size="mini" @click="loadData" :loading="loading">刷新图片列表</el-button>
            <el-button size="mini" type="primary" @click="handleUpload" v-if="shareData.token" :loading="loading">上传图片</el-button>
        </div>
        <div class="body flex-1 scrollbar">
            <template v-for="(item,index) in page.data" :key="index">
                <div class="item" @click="isShowImages =true">
                    <el-image :src="item.absolutePath">
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
                                    <el-dropdown-item @click="handleSetCover(item)">设为封面</el-dropdown-item>
                                    <el-dropdown-item @click="handleDel(item)">删除</el-dropdown-item>
                                </el-dropdown-menu>
                            </template>
                        </el-dropdown>
                    </div>
                </div>
            </template>
        </div>
        <div class="pages t-c" v-if="page.count > 0">
            <el-pagination :total="page.count" v-model:currentPage="page.page" :page-size="page.pageSize" @current-change="loadData" background layout="total,prev, pager, next">
            </el-pagination>
        </div>
    </div>
    <el-dialog title="上传图片" destroy-on-close v-model="showUpload" center :close-on-click-modal="false" width="69rem">
        <el-form ref="formDom" label-width="0">
            <el-form-item>
                <template v-for="(item,index) in images" :key="index">
                    <div class="el-upload el-upload--picture-card" style="margin-right:1rem;margin-bottom:1rem;">
                        <div>
                            <div class="bg" :style="{'background-image':`url(${item.url})`}"></div>
                            <span class="actions">
                                <span class="delete" @click.stop="handleDelImage(index)">
                                    <i class="el-icon-delete"></i>
                                </span>
                            </span>
                        </div>
                    </div>
                </template>
                <SelectFile @change="onFileChange" :multiple="true">
                    <template #default>
                        <div class="el-upload el-upload--picture-card">
                            <i class="el-icon-plus"></i>
                        </div>
                    </template>
                </SelectFile>
            </el-form-item>
        </el-form>
        <template #footer>
            <el-button @click="showUpload = false" :loading="loading">取 消</el-button>
            <el-button type="primary" :loading="loading" @click="handleSubmitUpload">确定上传</el-button>
        </template>
    </el-dialog>
    <el-image-viewer v-if="isShowImages" :url-list="showImages" @close="isShowImages=false"></el-image-viewer>
</template>

<script>
import { reactive, toRefs } from '@vue/reactivity';
import { inject, onMounted } from '@vue/runtime-core';
import { ElMessage, ElMessageBox } from 'element-plus'
import SelectFile from './SelectFile.vue'
export default {
    components: { SelectFile },
    setup (props) {

        const shareData = inject('share-data');
        const state = reactive({
            page: { page: 1, pageSize: 15, count: 0, data: [] },
            loading: false,
            showUpload: false,
            images: [],
            acceptTypes: ['image/jpg', 'image/png', 'image/gif', 'image/jpeg'],
            maxSize: 100,
            isShowImages: false,
            showImages: []
        });
        const loadData = () => {
            if (shareData.axios == null) {
                return;
            }
            state.loading = true;
            shareData.axios.get({
                url: 'album/list',
                params: { p: state.page.page, ps: state.page.pageSize, cid: shareData.category.id },
                headers: { token: shareData.token }
            }).then((res) => {
                state.loading = false;
                if (res.data.code == 0) {
                    res.data.data.data.forEach(c => {
                        c.addTime = new Date(c.addTime * 1000).format('yyyy-MM-dd hh:mm:ss');
                        if (c.path) {
                            c.absolutePath = shareData.baseURL + c.path;
                        }
                    });
                    state.showImages = res.data.data.data.filter(c => c.absolutePath.length > 0).map(c => c.absolutePath);
                    state.page = res.data.data;
                } else {
                    ElMessage.error(res.data.msg);
                }
            }).catch((e) => {
                state.loading = false;
                ElMessage.error(e.response.data);
            });
        }
        onMounted(() => { loadData(); });
        const handleDel = (item) => {
            ElMessageBox.confirm('删除不可逆，是否确认?', '删除相册',
                {
                    confirmButtonText: '确定',
                    cancelButtonText: '取消',
                    type: 'warning',
                }).then(() => {
                    shareData.axios.post({
                        url: '/album/del',
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
        const handleSetCover = (item) => {
            shareData.axios.post({
                url: `/category/editcover`,
                data: { id: shareData.category.id, cover: item.path },
                headers: { token: shareData.token }
            }).then((res) => {
                if (res.data.code == 0) {
                    ElMessage.success('设置成功');
                } else {
                    ElMessage.error(res.data.msg);
                }
            }).catch((e) => {
                ElMessage.error(e.response.data);
            });
        }
        const handleEdit = (data) => {
            ElMessageBox.prompt('输入名称', '编辑图片', {
                confirmButtonText: '确定',
                inputPattern: /.{1,100}/,
                inputErrorMessage: '1-20个字符',
                cancelButtonText: '取消',
                inputValue: data.name
            }).then(({ value }) => {
                shareData.axios.post({
                    url: `/album/editname`,
                    data: { id: data.id, name: value },
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


        const handleUpload = () => {
            state.showUpload = true;
        }
        const onFileChange = (file) => {
            if (file.size / 1024 / 1024 > state.maxSize) {
                ElMessage.error(`请上传 ${state.maxSize}M 以内的图片`);
                return;
            }
            if (!state.acceptTypes.includes(file.type)) {
                ElMessage.error(`仅支持 ${state.acceptTypes.join(',')}`);
                return;
            }
            state.images.push({
                url: window.URL.createObjectURL(file),
                file: file
            });
        }
        const handleDelImage = (index) => {
            state.images.splice(index, 1);
        }
        const handleSubmitUpload = () => {
            state.loading = true;
            const fn = () => {
                if (state.images.length == 0) {
                    state.loading = false;
                    state.showUpload = false;
                    loadData();
                    return;
                }
                var fd = new FormData();
                fd.append('cid', shareData.category.id);
                fd.append('file', state.images[0].file);
                shareData.axios.post({
                    url: '/album/add',
                    data: fd,
                    headers: {
                        token: shareData.token,
                        'content-type': 'multipart/form-data'
                    }
                }).then((res) => {
                    if (res.data.code == 0) {
                        state.images.splice(0, 1);
                        fn();
                    } else {
                        state.loading = false;
                        ElMessage.error(res.data.msg);
                    }
                }).catch((e) => {
                    state.loading = false;
                    ElMessage.error(e.response.data);
                });
            }
            fn();
        }

        return {
            ...toRefs(state), loadData, shareData, handleDel, handleEdit, handleSetCover,
            handleUpload, onFileChange, handleDelImage, handleSubmitUpload
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

.el-upload--picture-card
    position: relative;

    &:hover
        .actions
            display: block;

    .actions
        position: relative;
        top: 0%;
        left: 0%;
        display: none;

    .bg
        position: absolute;
        left: 0;
        top: 0;
        right: 0;
        bottom: 0;
        background-size: cover;
        border-radius: 0.6rem;
        background-position: center;
</style>