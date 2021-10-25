<!--
 * @Author: snltty
 * @Date: 2021-10-23 21:16:42
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-25 20:06:05
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ddns\Domains.vue
-->
<template>
    <div>
        <el-select v-model="shareData.group" value-key="key" placeholder="选择平台" size="mini" style="width:20rem">
            <template v-for="(platform,index) in shareData.platforms" :key="index">
                <el-option-group v-for="group in platform.Groups" :key="group.key" :label="platform.Name">
                    <el-option :value="group" :label="group.key"></el-option>
                </el-option-group>
            </template>
        </el-select>
        <span class="split-pad"></span>
        <el-select v-model="shareData.domain" value-key="DomainName" placeholder="选择域名" size="mini" style="width:15rem;">
            <template v-for="(item,index) in shareData.domains.Domains" :key="index">
                <el-option :value="item" :label="item.DomainName">
                    <div class="flex">
                        <span>{{item.DomainName}}</span>
                        <span class="flex-1"></span>
                        <el-popconfirm title="删除不可逆，是否确认,且只可删除添加的域名,无法删除服务商域名" @confirm="handleDeleteDomain(item)">
                            <template #reference>
                                <a href="javascript:;" @click.stop>
                                    <el-icon :size="20" color="#ccc" class="middle">
                                        <folder-delete />
                                    </el-icon>
                                </a>
                            </template>
                        </el-popconfirm>
                    </div>
                </el-option>
            </template>
        </el-select>
        <span class="split-pad"></span>
        <el-button size="mini" @click="handleAddDomain" :loading="shareData.loading">增加域名</el-button>
    </div>
</template>

<script>
import { getPlatforms, getDoamins, deleteDomain, addDoamin } from '../../../apis/plugins/ddns'
import { injectShareData } from './share-data'
import { ElMessageBox } from 'element-plus'
import { onMounted, watch } from '@vue/runtime-core'
export default {
    setup () {

        const shareData = injectShareData();
        const loadPlatforms = () => {
            getPlatforms().then((res) => {
                res.forEach(platform => {
                    platform.Groups.forEach(group => {
                        group.platform = platform.Name;
                        group.key = `${platform.Name}_${group.Name}`
                        group.loading = false;
                    });
                });
                shareData.platforms = res;
                if (res.length > 0) {
                    let platform = res.filter(c => c.Name == shareData.group.platform)[0];
                    if (!platform) {
                        platform = res[0];
                    }
                    if (platform.Groups.length > 0) {
                        let group = platform.Groups.filter(c => c.Name == shareData.group.Name)[0];
                        if (!group) {
                            group = platform.Groups[0];
                        }
                        shareData.group = group;
                    }
                }
            });
        }

        watch(() => shareData.updateFlag, () => {
            loadPlatforms();
        });
        watch(() => shareData.group, () => {
            shareData.group.recordJson = shareData.group.Records.map(c => c.split('|')).reduce((json, item) => {
                if (!json[item[1]]) {
                    json[item[1]] = [];
                }
                json[item[1]].push(item[0]);
                return json;
            }, {});
            loadDomains();
        });
        watch(() => shareData.domain, () => {
            shareData.domain.records = shareData.group.recordJson[shareData.domain.DomainName] || [];
        });

        const loadDomains = () => {
            getDoamins({
                Platform: shareData.group.platform, Group: shareData.group.Name,
                PageSize: shareData.domains.PageSize,
                PageNumber: shareData.domains.PageNumber,
            }).then((res) => {
                shareData.domains = res;
                if (res.Domains.length > 0) {
                    const domain = res.Domains.filter(c => c.DomainName == shareData.domain.DomainName)[0];
                    if (domain) {
                        shareData.domain = domain;
                    } else {
                        shareData.domain = res.Domains[0];
                    }
                } else {
                    shareData.domain = {};
                }
            });
        }
        onMounted(() => {
            loadPlatforms();
        });
        const handleDeleteDomain = (domain) => {
            deleteDomain({
                Platform: shareData.group.platform,
                Group: shareData.group.Name,
                Domain: domain.DomainName
            }).then(() => {
                loadPlatforms();
            }).catch(() => {
                domain.loading = false;
            });
        }
        const handleAddDomain = () => {
            ElMessageBox.prompt('添加域名', '添加域名', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
            }).then(({ value }) => {
                if (value) {
                    addDoamin({
                        Platform: shareData.group.platform,
                        Group: shareData.group.Name,
                        Domain: value
                    }).then(() => {
                        loadDomains();
                    }).catch((e) => {

                    });
                }
            })
        }

        return {
            shareData, handleAddDomain, handleDeleteDomain
        }
    }
}
</script>

<style lang="stylus" scoped></style>