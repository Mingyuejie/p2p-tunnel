<!--
 * @Author: snltty
 * @Date: 2021-10-24 19:40:41
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-24 20:15:27
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ddns\Record.vue
-->
<template>
    <div class="h-100">
        <el-table v-loading="loading" :data="list" border size="mini" height="100%">
            <el-table-column prop="RR" label="主机记录"></el-table-column>
            <el-table-column prop="Type" label="记录类型" width="80"></el-table-column>
            <el-table-column prop="Line" label="解析线路" width="80"></el-table-column>
            <el-table-column prop="Value" label="记录值"></el-table-column>
            <el-table-column prop="Status" label="状态" width="50">
                <template #default="scope">
                    <span>{{status[scope.row.Status]}}</span>
                </template>
            </el-table-column>
            <el-table-column prop="TTL" label="TTL" width="50"></el-table-column>
            <el-table-column prop="Remark" label="备注"></el-table-column>
            <el-table-column prop="todo" label="操作" width="165" fixed="right" class="t-c">
                <template #default="scope">

                    <div class="op">
                        <a href="javascript:;">编辑</a>
                        <a href="javascript:;">暂停</a>
                        <a href="javascript:;">删除</a>
                        <a href="javascript:;">备注</a>
                    </div>
                    <!-- <el-popconfirm title="删除不可逆，是否确认" @confirm="handleDel(scope.row)">
                        <template #reference>
                            <el-button type="danger" size="mini" icon="el-icon-delete"></el-button>
                        </template>
                    </el-popconfirm> -->
                </template>
            </el-table-column>
        </el-table>
    </div>
</template>

<script>
import { reactive, toRefs } from '@vue/reactivity'
import { injectShareData } from './share-data'
import { watch } from '@vue/runtime-core';
import { getRecords } from '../../../apis/plugins/ddns'

export default {
    setup () {

        const shareData = injectShareData();
        watch(() => shareData.group, () => {
            loadData();
        });

        const state = reactive({
            loading: false,
            list: [],
            status: { 'ENABLE': '正常', 'DISABLE': '禁用' }
        });
        const loadData = () => {
            getRecords({
                Platform: shareData.group.platform,
                Domain: shareData.domain.Domain
            }).then((res) => {
                state.list = res.DomainRecords || [];
            })
        }
        const handleDel = (row) => {
        }

        return {
            ...toRefs(state), handleDel
        }
    }
}
</script>
<style lang="stylus" scoped>
.op
    a
        padding: 0 0.6rem;
</style>