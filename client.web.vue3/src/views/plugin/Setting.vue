<!--
 * @Author: snltty
 * @Date: 2021-08-20 00:47:21
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-24 14:57:29
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\Setting.vue
-->
<template>
    <div class="plugin-setting-wrap">
        <div class="head">
            <el-button size="mini" @click="getData">刷新列表</el-button>
        </div>
        <el-table v-loading="loading" :data="list" border size="mini">
            <el-table-column prop="Name" label="插件名"></el-table-column>
            <el-table-column prop="Desc" label="描述"></el-table-column>
            <el-table-column prop="Author" label="作者"></el-table-column>
            <el-table-column prop="todo" label="操作" width="80" fixed="right" class="t-c">
                <template #default="scope">
                    <SettingModal :className="scope.row.ClassName" @success=" getData">
                        <el-button size="mini">配置</el-button>
                    </SettingModal>
                </template>
            </el-table-column>
        </el-table>
    </div>
</template>
<script>
import { reactive, ref, toRefs } from '@vue/reactivity'
import { getPlugins } from '../../apis/plugins/setting'
import SettingModal from './SettingModal.vue'
export default {
    components: { SettingModal },
    setup () {
        const editor = ref(null);
        const state = reactive({
            loading: false,
            showAdd: false,
            list: [],
            rules: {
            }
        });
        const getData = () => {
            getPlugins().then((res) => {
                state.list = res;
            });
        };
        getData();

        return {
            ...toRefs(state), editor, getData
        }
    }
}
</script>
<style lang="stylus" scoped>
.plugin-setting-wrap
    padding: 2rem;

.head
    margin-bottom: 0.6rem;
</style>
