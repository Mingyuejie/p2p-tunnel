<!--
 * @Author: snltty
 * @Date: 2021-08-20 00:47:21
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-23 15:24:23
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
                    <el-button size="mini" @click="handleEdit(scope.row)">配置</el-button>
                </template>
            </el-table-column>
        </el-table>
        <el-dialog title="配置" destroy-on-close v-model="showAdd" center :close-on-click-modal="false" width="80rem">
            <el-form ref="formDom" :model="form" :rules="rules" label-width="0">
                <el-form-item label="" prop="Content" label-width="0">
                    <vue-json-editor ref="editor" :value="form.Content" style="height:400px" mode="code" :exapndedOnStart="true" />
                </el-form-item>
            </el-form>
            <template #footer>
                <el-button @click="showAdd = false">取 消</el-button>
                <el-button type="primary" :loading="loading" @click="handleSubmit">确 定</el-button>
            </template>
        </el-dialog>
    </div>
</template>
<script>
import { reactive, ref, toRefs } from '@vue/reactivity'
import { getPlugins, loadSetting, saveSetting } from '../../apis/plugins/setting'
import { ElMessage } from 'element-plus'
import vueJsonEditor from 'vue-json-editor'
export default {
    components: { vueJsonEditor },
    setup () {

        const editor = ref(null);
        const state = reactive({
            loading: false,
            showAdd: false,
            list: [],
            form: {
                Code: 0,
                Content: { a: 111 }
            },
            rules: {
            }
        });
        const getData = () => {
            getPlugins().then((res) => {
                state.list = res;
            });
        };
        getData();

        const handleEdit = (row) => {
            state.form.Code = row.Code;
            loadSetting(row.Code).then((res) => {
                state.form.Content = res;
                state.showAdd = true;
            });
        }
        const formDom = ref(null);
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                state.loading = true;
                saveSetting(state.form.Code, JSON.stringify(editor.value.json)).then(() => {
                    state.loading = false;
                    state.showAdd = false;
                    getData();
                    ElMessage.success('以保存');
                }).catch((e) => {
                    ElMessage.error(e);
                    state.loading = false;
                });
            })
        }
        return {
            ...toRefs(state), editor, getData, handleEdit,
            formDom, handleSubmit
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
<style lang="stylus">
.jsoneditor-vue
    height: 100%;
</style>