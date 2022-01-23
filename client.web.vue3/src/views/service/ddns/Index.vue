<!--
 * @Author: snltty
 * @Date: 2021-10-23 20:36:48
 * @LastEditors: xr
 * @LastEditTime: 2022-01-23 14:18:17
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\service\ddns\Index.vue
-->
<template>
    <div class="h-100 ddns-wrap flex flex-column flex-nowrap">
        <div class="head flex">
            <Domains></Domains>
            <span class="split"></span>
            <span class="flex-1"></span>
            <ConfigureModal className="DdnsClientConfigure">
                <el-button size="mini">配置插件</el-button>
            </ConfigureModal>
        </div>
        <div class="flex-1">
            <Record></Record>
        </div>
    </div>
</template>

<script>
import { reactive } from '@vue/reactivity'
import ConfigureModal from '../configure/ConfigureModal.vue'
import Domains from './Domains.vue'
import Record from './Record.vue'
import { provideShareData } from './share-data'

export default {
    components: { ConfigureModal, Domains, Record },
    setup () {
        const shareState = reactive({
            platforms: [],
            group: {
                Name: '',
                platform: '',
                Records: [],
                recordJson: {}
            },
            domains: {
                Domains: [],
                PageNumber: 1,
                PageSize: 100,
                TotalCount: 0
            },
            domain: {
                records: []
            },
            updateFlag: 1,
            loading: false
        });
        provideShareData(shareState);

        return {
            shareState
        }
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