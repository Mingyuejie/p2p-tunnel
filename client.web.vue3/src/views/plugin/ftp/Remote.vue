<!--
 * @Author: snltty
 * @Date: 2021-09-26 19:51:49
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-26 23:49:31
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ftp\Remote.vue
-->
<template>
    <div class="flex flex-column h-100">
        <div class="head flex flex-nowrap">
            <SettingModal className="FtpSettingPlugin">
                <el-button size="mini">配置插件</el-button>
            </SettingModal>
            <span class="split"></span>
            <el-select v-model="clientId" placeholder="请选择已连接的目标客户端" @change="getFiles" size="mini">
                <template v-for="client in clients" :key="client.Id">
                    <el-option :label="client.Name" :value="item.Id">
                    </el-option>
                </template>
            </el-select>
            <span class="split"></span>
            <el-button size="mini" :loading="loading" @click="getFiles('')">刷新列表</el-button>
        </div>
        <div class="body flex-1 relative">
            <div class="absolute">
                <el-table :data="data" border size="mini" height="100%" @row-dblclick="handleRowDblClick" @row-contextmenu="handleContextMenu">
                    <el-table-column prop="Label" label="文件名（远程）"></el-table-column>
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
import { geRemoteList, sendRemoteCreate, sendRemoteDelete } from '../../../apis/plugins/ftp'
import { onMounted } from '@vue/runtime-core';
import FileTree from './FileTree.vue'
import ContextMenu from './ContextMenu.vue'
import { ElMessageBox } from 'element-plus'
import { injectClients } from '../../../states/clients'
import SettingModal from '../SettingModal.vue'
export default {
    components: { FileTree, ContextMenu, SettingModal },
    setup () {
        const clientState = injectClients();
        const state = reactive({
            data: [],
            loading: false,
            clientId: null
        });
        const getFiles = (path = '') => {
            state.loading = true;
            geRemoteList(state.clientId || 0, path).then((res) => {
                state.loading = false;
                state.data = [{ Name: '..', Label: '.. 上一级', Length: 0, Type: 0 }].concat(res.map(c => {
                    c.Label = c.Name;
                    return c;
                }));
            }).catch((err) => {
                console.log(err);
                state.loading = false;
            });
        }
        onMounted(() => {
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
                                sendRemoteCreate(state.clientId || 0, value).then(() => {
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
                                sendRemoteDelete(state.clientId || 0, row.Name).then(() => {
                                    getFiles();
                                });
                            });
                        }
                    },
                ]);
            }
            event.preventDefault();
        }

        return {
            ...toRefs(state), ...toRefs(clientState), getFiles, contextMenu, handleRowDblClick, handleContextMenu
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