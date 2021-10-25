<!--
 * @Author: snltty
 * @Date: 2021-10-23 20:36:48
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-25 09:47:49
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ddns\Index.vue
-->
<template>
    <div class="h-100 ddns-wrap flex flex-column flex-nowrap">
        <div class="head flex">
            <Domains></Domains>
            <span class="split"></span>
            <span class="flex-1"></span>
            <SettingModal className="DdnsSettingPlugin">
                <el-button size="mini">配置插件</el-button>
            </SettingModal>
        </div>
        <div class="flex-1">
            <Record></Record>
        </div>
    </div>
</template>

<script>
import { reactive } from '@vue/reactivity'
import SettingModal from '../setting/SettingModal.vue'
import Domains from './Domains.vue'
import Record from './Record.vue'
import { provideShareData } from './share-data'
export default {
    components: { SettingModal, Domains, Record },
    setup () {
        const shareState = reactive({
            platforms: [],
            group: {
                AutoUpdate: false,
                Name: '',
                platform: '',
                Domains: []
            },
            domain: {
                AutoUpdate: false,
            }
        });
        provideShareData(shareState);

        return {}
    }
}
</script>

<style lang="stylus" scoped>
.ddns-wrap
    padding: 2rem;
    box-sizing: border-box;

    .head
        margin-bottom: 1rem;
</style>