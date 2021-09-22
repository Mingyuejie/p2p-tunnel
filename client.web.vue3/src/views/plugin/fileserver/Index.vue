<!--
 * @Author: snltty
 * @Date: 2021-09-05 13:25:54
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-06 14:52:16
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\fileserver\Index.vue
-->
<template>
    <div class="fileserver-wrap h-100 flex flex-column flex-nowrap">
        <div class="flex-1">
            <el-row class="h-100">
                <el-col :span="12" class="h-100">
                    <Local ref="local"></Local>
                </el-col>
                <el-col :span="12" class="h-100" style="padding-left:0.6rem;">
                    <Remote ref="remote"></Remote>
                </el-col>
            </el-row>
        </div>
        <div class="queue scrollbar" style="padding-top:0.6rem;">
            <el-row class="h-100">
                <el-col :span="16" class="h-100">
                    <Online @on-change="onChange"></Online>
                </el-col>
                <el-col :span="8" class="h-100">
                    <Server></Server>
                </el-col>
            </el-row>
        </div>
    </div>
</template>
<script>
import Local from './Local.vue'
import Remote from './Remote.vue'
import Server from './Server.vue'
import Online from './Online.vue'
import { provide, reactive, ref } from '@vue/runtime-core'
export default {
    components: { Local, Remote, Server, Online },
    setup () {

        const remote = ref(null);
        const remoteState = reactive({ files: [], clientId: 0 })
        provide('remote', remoteState);

        const local = ref(null);
        const localState = reactive({ files: [] })
        provide('local', localState);

        const onChange = () => {
            remote.value.reload();
            local.value.reload();
        }

        return {
            remote, local, onChange
        }
    }
}
</script>

<style lang="stylus" scoped>
.fileserver-wrap
    padding: 1rem;
    box-sizing: border-box;
    min-height: 76.8rem;

.queue
    height: 30rem;
</style>