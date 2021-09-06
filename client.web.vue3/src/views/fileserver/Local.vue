<!--
 * @Author: snltty
 * @Date: 2021-09-05 12:43:00
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-06 16:27:48
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\fileserver\Local.vue
-->
<template>
    <div class="h-100 flex flex-column">
        <div class="header">
            <el-dropdown size="mini" trigger="click" @command="handleSpecialFolderCommand">
                <el-button size="mini">
                    {{specialFolderModel}} <i class="el-icon-arrow-down el-icon--right"></i>
                </el-button>
                <template #dropdown>
                    <FolderTree :childs="specialFolder"></FolderTree>
                </template>
            </el-dropdown>
            <el-button size="mini" @click="reload" :loading="loading">刷新列表</el-button>
        </div>
        <div class="body flex-1 relative">
            <div class="absolute">
                <el-table v-loading="loading" :data="list" border size="mini" height="100%" :span-method="objectSpanMethod" @row-dblclick="handleRowDblclick">
                    <el-table-column prop="Name" label="名称">
                        <template #default="scope">
                            <div class="file-name" :title="scope.row.Name">
                                <span v-if="scope.row.Image">
                                    <img :src="'data:image/jpg;base64,'+scope.row.Image" height="30" class="img" />
                                </span>
                                <span>{{scope.row.Name}}</span>
                                <span v-if="scope.row.Type ==-1">({{scope.row.FullName}})</span>
                            </div>
                        </template>
                    </el-table-column>
                    <el-table-column prop="Length" label="大小" width="120">
                        <template #default="scope">
                            <span v-if="scope.row.Type !=0">{{(scope.row.Length/1024/1024).toFixed(2)}} MB</span>
                        </template>
                    </el-table-column>
                    <el-table-column prop="todo" label="操作" width="80" fixed="right" class="t-c">
                        <template #default="scope">
                            <el-button size="mini" v-if="scope.row.Type ==1" @click="handleUpload(scope.row)">上传</el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
    </div>

</template>
<script>
import { reactive, toRefs } from '@vue/reactivity';
import { getLocalFiles, sendUpload, getSpecialFolder } from '../../apis/file-server'
import { inject, onMounted, watch } from '@vue/runtime-core';
import { ElMessage, ElMessageBox } from 'element-plus'
import FolderTree from './FolderTree.vue'
export default {
    components: { FolderTree },
    setup () {
        const remoteState = inject('remote');
        const localState = inject('local');
        const state = reactive({
            list: [],
            loading: false,
            specialFolder: [],
            specialFolderModel: '特殊文件夹',
        });
        const loadDir = (path = '', toid = 0) => {
            state.loading = true;
            getLocalFiles(path, toid).then((res) => {
                localState.files = state.list = [{
                    Name: '..上级',
                    FullName: '..',
                    Type: 0,
                    Length: 0,
                }, ...JSON.parse(res)];
                state.loading = false;
            }).catch(() => {
                state.loading = false;
            });
        }
        const handleRowDblclick = (row) => {
            if (row.Type != 1) {
                if (row.FullName == '..' || row.Type == -1) {
                    loadDir(row.FullName)
                } else {
                    loadDir(row.Name)
                }
            }
        }
        const handleUpload = (row) => {
            if (remoteState.clientId <= 0) {
                return ElMessage.error('请选择目标客户端');
            }
            if (remoteState.files.filter(c => c.Name == row.Name).length > 0) {
                ElMessageBox.confirm('文件已存在，是否确定上传?', '提示', {
                    confirmButtonText: '确定',
                    cancelButtonText: '取消',
                    type: 'warning'
                }).then(() => {
                    sendUpload(row.Name, remoteState.clientId);
                }).catch(() => {
                });
            } else {
                sendUpload(row.Name, remoteState.clientId);
            }
        }
        onMounted(() => {
            getSpecialFolder().then((msg) => {
                state.specialFolder = [JSON.parse(msg)];
            });
            reload();
        });
        const reload = () => {
            loadDir();
        }
        const handleSpecialFolderCommand = (item) => {
            if (item.FullName) {
                loadDir(item.FullName, -1);
                state.specialFolderModel = item.Name;
            }
        }

        const objectSpanMethod = ({ row, column, rowIndex, columnIndex }) => {
            if (column.property == 'Name' && row.Type == 0) {
                return { colspan: 3, rowspan: 1 }
            }
        }
        return {
            ...toRefs(state), reload, handleRowDblclick, objectSpanMethod, handleUpload, handleSpecialFolderCommand
        }
    }
}
</script>
<style lang="stylus" scoped>
.file-name
    white-space: nowrap;
    text-overflow: ellipsis;
    overflow: hidden;

.header
    padding: 0 0 0.6rem 0;

.img
    vertical-align: middle;
</style>