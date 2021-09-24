<!--
 * @Author: snltty
 * @Date: 2021-09-24 14:36:58
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-24 14:58:37
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\SettingModal.vue
-->
<template>
    <span @click="handleEdit">
        <slot>
            <el-button size="mini">配置</el-button>
        </slot>
    </span>
    <el-dialog title="配置" destroy-on-close v-model="showAdd" center :close-on-click-modal="false" width="80rem">
        <el-form ref="formDom" :model="form" :rules="rules" label-width="0">
            <el-form-item label="" prop="Content" label-width="0">
                <vue-json-editor v-if="showAdd" ref="editor" :value="form.Content" style="height:400px" mode="code" :exapndedOnStart="true" />
            </el-form-item>
        </el-form>
        <template #footer>
            <el-button @click="showAdd = false">取 消</el-button>
            <el-button type="primary" :loading="loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { toRefs, reactive, ref } from '@vue/reactivity';
import { loadSetting, saveSetting } from '../../apis/plugins/setting'
import { ElMessage } from 'element-plus'
import vueJsonEditor from 'vue-json-editor'
export default {
    props: ['className'],
    emits: ['success'],
    components: { vueJsonEditor },
    setup (props, { emit }) {
        const state = reactive({
            loading: false,
            showAdd: false,
            form: {
                ClassName: props.className,
                Content: { a: 111 }
            },
            rules: {
            }
        });
        const handleEdit = () => {
            loadSetting(state.form.ClassName).then((res) => {
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
                saveSetting(state.form.ClassName, JSON.stringify(editor.value.json)).then(() => {
                    state.loading = false;
                    state.showAdd = false;
                    ElMessage.success('以保存');
                    emit('success')
                }).catch((e) => {
                    ElMessage.error(e);
                    state.loading = false;
                });
            })
        }

        return {
            ...toRefs(state), formDom, handleEdit, handleSubmit
        }
    }
}
</script>
<style lang="stylus" scoped></style>
<style lang="stylus">
.jsoneditor-vue
    height: 100%;

div.jsoneditor
    border-color: var(--main-color);

div.jsoneditor-menu
    background-color: var(--main-color);
    border-color: var(--main-color);
</style>