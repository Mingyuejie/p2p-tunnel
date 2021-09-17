<!--
 * @Author: snltty
 * @Date: 2021-09-05 19:42:26
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-17 21:13:20
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\fileserver\Server.vue
-->
<template>
    <div class="wrap">
        <el-form label-width="0" ref="formDom">
            <el-form-item>
                <el-input type="textarea" v-model="Root" placeholder="填写文件服务根路径" resize="none" rows="5"></el-input>
            </el-form-item>
            <el-form-item label="" label-width="0" class="t-c">
                <el-button type="primary" :loading="loading" v-if="!IsStart" @click="handleStartServer" size="mini">开启服务</el-button>
                <el-button type="danger" :loading="loading" v-else @click="handleStopServer" size="mini">关闭</el-button>

                <el-button :loading="loading" @click="handleSaveServer" size="mini">保存设置</el-button>
            </el-form-item>
        </el-form>
    </div>
</template>

<script>
import { ref, toRefs } from '@vue/reactivity'
import { injectFileserver } from '../../states/fileserver'
import { sendStartServer, sendStopServer, sendUpdateServerConfig, getServerConfig } from '../../apis/file-server'
import { ElMessage } from 'element-plus'
export default {
    setup () {

        const loading = ref(false);
        const state = injectFileserver();

        getServerConfig().then((json) => {
            state.Root = json.Root;
        });

        const handleStartServer = () => {
            loading.value = true;
            sendUpdateServerConfig(state.Root).then(() => {
                sendStartServer();
                loading.value = false;
            }).catch(() => {
                loading.value = false;
            });
        };
        const handleStopServer = () => {
            loading.value = true;
            sendStopServer().then(() => {
                loading.value = false;
            }).catch(() => {
                loading.value = false;
            });
        };

        const handleSaveServer = () => {
            loading.value = true;
            sendUpdateServerConfig(state.Root, state.IsStart).then(() => {
                loading.value = false;
                ElMessage.success('操作成功');
            }).catch((msg) => {
                loading.value = false;
                ElMessage.error(`操作失败:${msg}`);
            });
        }

        return {
            ...toRefs(state), loading, handleStartServer, handleStopServer, handleSaveServer
        }
    }
}
</script>
<style lang="stylus" scoped>
.wrap
    padding: 0 0 0 0.6rem;
</style>