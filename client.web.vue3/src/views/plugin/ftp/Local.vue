<!--
 * @Author: snltty
 * @Date: 2021-09-26 19:51:49
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-26 23:31:31
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ftp\Local.vue
-->
<template>
    <div class="flex flex-column h-100">
        <div class="head flex flex-nowrap">
            <el-dropdown size="mini" trigger="click" @command="handleSpecialFolderCommand" class=" flex-1">
                <el-input size="mini" :title="specialFolderModel" :value="specialFolderModel" suffix-icon="el-icon-arrow-down"></el-input>
                <template #dropdown>
                    <FileTree :childs="specialFolder"></FileTree>
                </template>
            </el-dropdown>
            <span class="split"></span>
            <el-button size="mini" :loading="loading" @click="getFiles('')">刷新列表</el-button>
        </div>
        <div class="body flex-1 relative">
            <div class="absolute">
                <el-table :data="data" border size="mini" height="100%" @row-dblclick="handleRowDblClick" @row-contextmenu="handleContextMenu">
                    <el-table-column prop="Label" label="文件名（本地）"></el-table-column>
                    <el-table-column prop="Length" label="大小" width="100">
                        <template #default="scope">
                            <span v-if="scope.row.Type !=0">{{scope.row.Length.sizeFormat()}} </span>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
    </div>
    <ContextMenu ref="contextMenu"></ContextMenu>
</template>

<script>
import { reactive, ref, toRefs } from '@vue/reactivity'
import { getLocalSpecialList, getLocalList, sendLocalCreate, sendLocalDelete, sendSetLocalPath } from '../../../apis/plugins/ftp'
import { onMounted } from '@vue/runtime-core';
import FileTree from './FileTree.vue'
import ContextMenu from './ContextMenu.vue'
import { ElMessageBox } from 'element-plus'
export default {
    components: { FileTree, ContextMenu },
    setup () {
        const state = reactive({
            data: [],
            loading: false,
            specialFolder: [],
            specialFolderModel: '特殊文件夹'
        });
        const getSpecial = () => {
            getLocalSpecialList().then((res) => {
                state.specialFolder = [res];
            });
        }
        const getFiles = (path = '') => {
            state.loading = true;
            getLocalList(path).then((res) => {
                state.loading = false;
                state.specialFolderModel = res.Current;
                state.data = [{ Name: '..', Label: '.. 上一级', Length: 0, Type: 0 }].concat(res.Data.map(c => {
                    c.Label = c.Name;
                    return c;
                }));
            }).catch(() => {
                state.loading = false;
            });
        }
        onMounted(() => {
            getSpecial();
            getFiles();
        });

        const handleRowDblClick = (row) => {
            if (!state.loading && row.Type == 0) {
                getFiles(row.Name);
            }
        }
        const contextMenu = ref(null);
        const handleContextMenu = (row, column, event) => {
            if (!state.loading && row.Name != '..') {
                contextMenu.value.show(event, [
                    { text: '上传', handle: () => { } },
                    {
                        text: '创建文件夹', handle: () => {
                            ElMessageBox.prompt('输入文件夹名称', '创建文件夹', {
                                confirmButtonText: '确定',
                                cancelButtonText: '取消',
                                inputValue: '新建文件夹'
                            }).then(({ value }) => {
                                sendLocalCreate(value).then(() => {
                                    getFiles();
                                });
                            });

                        }
                    },
                    {
                        text: '删除', handle: () => {
                            ElMessageBox.confirm('删除不可逆，是否确认', '删除', {
                                confirmButtonText: '确定',
                                cancelButtonText: '取消',
                                type: 'warning'
                            }).then(() => {
                                sendLocalDelete(row.Name).then(() => {
                                    getFiles();
                                });
                            });
                        }
                    },
                ]);
            }
            event.preventDefault();
        }
        const handleSpecialFolderCommand = (item) => {
            if (!state.loading && item.FullName) {
                sendSetLocalPath(item.FullName).then(() => {
                    getFiles();
                });
            }
        }

        return {
            ...toRefs(state), getFiles, contextMenu, handleRowDblClick, handleContextMenu, handleSpecialFolderCommand
        }
    }
}
</script>

<style lang="stylus" scoped>
.head
    padding-bottom: 0.4rem;

    .split
        width: 0.2rem;
</style>