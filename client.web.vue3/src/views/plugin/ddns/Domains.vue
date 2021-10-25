<!--
 * @Author: snltty
 * @Date: 2021-10-23 21:16:42
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-25 11:32:37
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ddns\Domains.vue
-->
<template>
    <div>
        <el-select v-model="shareData.group" value-key="key" placeholder="选择平台" size="mini" style="width:20rem">
            <template v-slot:prefix>
                <template v-if="shareData.group.AutoUpdate">更新</template>
                <template v-else>不更新</template>
            </template>
            <template v-for="(platform,index) in shareData.platforms" :key="index">
                <el-option-group v-for="group in platform.Groups" :key="group.key" :label="platform.Name">
                    <el-option :value="group" :label="group.key">
                        <el-popover placement="bottom-end" title="啥意思" trigger="hover" content="当IP变化时，自动更新此平台下域名">
                            <template #reference>
                                <el-switch :loading="group.loading" v-model="group.AutoUpdate" @click.stop @change="handleGroupAutoUpdateChange(group)" />
                            </template>
                        </el-popover>
                        <span class="split-pad"></span>
                        <span>{{group.Name}}</span>
                    </el-option>
                </el-option-group>
            </template>
        </el-select>
        <span class="split-pad"></span>
        <el-select v-model="shareData.domain" value-key="Domain" placeholder="选择域名" size="mini" style="width:15rem;">
            <template v-slot:prefix>
                <template v-if="shareData.domain.AutoUpdate">更新</template>
                <template v-else>不更新</template>
            </template>
            <template v-for="(item,index) in shareData.group.Domains" :key="index">
                <el-option :value="item" :label="item.Domain">
                    <el-popover placement="bottom-end" title="啥意思" trigger="hover" content="当IP变化时，自动更新此域名下的解析">
                        <template #reference>
                            <el-switch :loading="item.loading" v-model="item.AutoUpdate" @click.stop @change="handleDomainAutoUpdateChange(item)" />
                        </template>
                    </el-popover>
                    <span class="split-pad"></span>
                    <span>{{item.Domain}}</span>
                    <span class="split-pad"></span>
                    <el-popconfirm title="删除不可逆，是否确认" @confirm="handleDeleteDomain(item)">
                        <template #reference>
                            <a href="javascript:;" @click.stop>
                                <el-icon :size="20" color="#ccc" class="middle">
                                    <folder-delete />
                                </el-icon>
                            </a>
                        </template>
                    </el-popconfirm>

                </el-option>
            </template>
        </el-select>
        <span class="split-pad"></span>
        <el-button size="mini" @click="showAdd = true">增加域名</el-button>
    </div>
    <el-dialog title="增加域名" v-model="showAdd" center :close-on-click-modal="false" width="30rem">
        <el-form ref="formDom" :model="form" :rules="rules" label-width="80">
            <el-form-item label="域名" prop="Domain">
                <el-input v-model="form.Domain" />
            </el-form-item>
            <el-form-item label="自动更新" prop="AutoUpdate">
                <el-switch v-model="form.AutoUpdate" />
            </el-form-item>
        </el-form>
        <template #footer>
            <el-button @click="showAdd = false">取 消</el-button>
            <el-button type="primary" :loading="loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { getDoamins, switchGroup, switchDomain, deleteDomain, addDoamin } from '../../../apis/plugins/ddns'
import { injectShareData } from './share-data'
import { ElMessage } from 'element-plus'
import { onMounted, reactive, ref, toRefs } from '@vue/runtime-core'
export default {
    setup (props, { emit }) {

        const shareData = injectShareData();
        const loadData = () => {
            getDoamins().then((res) => {
                res.forEach(platform => {
                    platform.Groups.forEach(group => {
                        group.platform = platform.Name;
                        group.key = `${platform.Name}_${group.Name}`
                        group.loading = false;
                        group.Domains.forEach(domain => {
                            domain.group = group.Name;
                            domain.loading = false;
                        });
                    });
                });
                shareData.platforms = res;
                if (!shareData.group.Name && res.length > 0 && res[0].Groups.length > 0) {
                    shareData.group = res[0].Groups[0];
                    if (res[0].Groups[0].Domains.length > 0) {
                        shareData.domain = res[0].Groups[0].Domains[0];
                    }
                } else if (shareData.group.Name) {
                    const platform = res.filter(c => c.Name == shareData.group.platform)[0];
                    if (platform) {
                        const group = platform.Groups.filter(c => c.Name == shareData.group.Name)[0];
                        if (group) {
                            shareData.group = group;
                        }
                    }
                }
            });
        }
        onMounted(() => {
            loadData();
        });

        const handleGroupAutoUpdateChange = (group) => {
            group.loading = true;
            switchGroup({
                Platform: group.platform,
                Group: group.Name,
                AutoUpdate: group.AutoUpdate,
            }).then(() => {
                group.loading = false;
            }).catch(() => {
                ElMessage.error('失败!');
                group.loading = false;
                group.AutoUpdate = !group.AutoUpdate;
            });
        }
        const handleDomainAutoUpdateChange = (domain) => {
            domain.loading = true;
            switchDomain({
                Platform: shareData.group.platform,
                Group: shareData.group.Name,
                Domain: domain.Domain,
                AutoUpdate: domain.AutoUpdate,
            }).then(() => {
                domain.loading = false;
            }).catch(() => {
                ElMessage.error('失败!');
                domain.loading = false;
                domain.AutoUpdate = !domain.AutoUpdate;
            });
        }
        const handleDeleteDomain = (domain) => {
            domain.loading = true;
            deleteDomain({
                Platform: shareData.group.platform,
                Group: shareData.group.Name,
                Domain: domain.Domain
            }).then(() => {
                domain.loading = false;
                loadData();
            }).catch(() => {
                ElMessage.error('失败!');
                domain.loading = false;
            });
        }

        const formDom = ref(null);
        const state = reactive({
            loading: false,
            showAdd: false,
            form: {
                Domain: '',
                AutoUpdate: false,
            },
            rules: {
                Domain: [{ required: true, message: '必填', trigger: 'blur' }],
            }
        });
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                state.loading = true;
                addDoamin({
                    Platform: shareData.group.platform,
                    Group: shareData.group.Name,
                    Domain: state.form.Domain, AutoUpdate: state.form.AutoUpdate
                }).then(() => {
                    state.loading = false;
                    state.showAdd = false;
                    loadData();
                }).catch((e) => {
                    state.loading = false;
                });
            })
        }

        return {
            shareData, handleGroupAutoUpdateChange, handleDomainAutoUpdateChange, handleDeleteDomain,
            ...toRefs(state), formDom, handleSubmit
        }
    }
}
</script>

<style lang="stylus" scoped></style>