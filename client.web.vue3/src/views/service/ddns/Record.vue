<!--
 * @Author: snltty
 * @Date: 2021-10-24 19:40:41
 * @LastEditors: xr
 * @LastEditTime: 2022-01-23 14:14:16
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\service\ddns\Record.vue
-->
<template>
    <div class="h-100 flex flex-column flex-nowrap">
        <div class="head">
            <el-button type="info" size="mini" @click="handleAdd">新增解析</el-button>
            <el-button size="mini" @click="loadData">刷新列表</el-button>
        </div>
        <div class="body flex-1">
            <el-table v-loading="shareState.loading" :data="records.DomainRecords" border size="mini" height="100%">
                <el-table-column prop="RR" label="主机记录">
                    <template #default="scope">
                        <div class="flex">
                            <span>{{scope.row.RR}}</span>
                            <span class="flex-1"></span>
                            <el-popover placement="bottom-end" title="啥意思" trigger="hover" content="当IP变化时，是否更新此条解析记录">
                                <template #reference>
                                    <el-switch v-model="scope.row.autoUpdate" @change="handleRecordAutoUpdateChange(scope.row)" />
                                </template>
                            </el-popover>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column prop="Type" label="记录类型" width="80"></el-table-column>
                <el-table-column prop="Line" label="解析线路" width="80">
                    <template #default="scope">
                        <span>{{recordLinesJson[scope.row.Line]}}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="Value" label="记录值"></el-table-column>
                <el-table-column prop="Status" label="状态" width="50">
                    <template #default="scope">
                        <span>{{status[scope.row.Status]}}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="TTL" label="TTL" width="80"></el-table-column>
                <el-table-column prop="Remark" label="备注"></el-table-column>
                <el-table-column prop="todo" label="操作" width="165" fixed="right" class="t-c">
                    <template #default="scope">
                        <div class="op">
                            <a href="javascript:;" @click="handleEdit(scope.row)">编辑</a>
                            <a href="javascript:;" @click="handleSwitchStatus(scope.row)">{{statusBtn[scope.row.Status]}}</a>
                            <el-popconfirm title="删除不可逆，是否确认" @confirm="handleDel(scope.row)">
                                <template #reference>
                                    <a href="javascript:;">删除</a>
                                </template>
                            </el-popconfirm>
                            <a href="javascript:;" @click="handleRemark(scope.row)">备注</a>
                        </div>
                    </template>
                </el-table-column>
            </el-table>
        </div>
        <div class="pages t-c" v-if="records.TotalCount > 0">
            <el-pagination :total="records.TotalCount" v-model:currentPage="records.PageNumber" :page-size="records.PageSize" @current-change="loadData" background layout="total,prev, pager, next">
            </el-pagination>
        </div>
    </div>
    <el-dialog title="增加解析" v-model="showAdd" center :close-on-click-modal="false" width="50rem">
        <el-form ref="formDom" :model="form" :rules="rules" label-width="8rem">
            <el-form-item label="" :label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="记录类型" prop="Type">
                            <el-select v-model="form.Type">
                                <template v-for="(item) in recordTypes" :key="item">
                                    <el-option :value="item" :label="item"></el-option>
                                </template>
                            </el-select>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="解析线路" prop="Line">
                            <el-select v-model="form.Line">
                                <template v-for="(item) in recordLines" :key="item.LineCode">
                                    <el-option :value="item.LineCode" :label="item.LineName"></el-option>
                                </template>
                            </el-select>
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" :label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="主机记录" prop="RR">
                            <el-input v-model="form.RR" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="记录值" prop="Value">
                            <el-input v-model="form.Value" />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" :label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="TTL" prop="TTL">
                            <el-select v-model="form.TTL">
                                <template v-for="(item) in recordTTLs" :key="item.value">
                                    <el-option :value="item.value" :label="item.text"></el-option>
                                </template>
                            </el-select>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="优先级" prop="Priority">
                            <el-input v-model="form.Priority" />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
        </el-form>
        <template #footer>
            <el-button @click="showAdd = false">取 消</el-button>
            <el-button type="primary" :loading="shareState.loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { reactive, ref, toRefs } from '@vue/reactivity'
import { injectShareData } from './share-data'
import { onMounted, watch } from '@vue/runtime-core';
import {
    getRecords, setRecordStatus, delRecord, remarkRecord,
    getRecordTypes, addRecord, getRecordLines, switchRecord
} from '../../../apis/service/ddns'
import { ElMessageBox } from 'element-plus'

export default {
    setup () {

        const shareState = injectShareData();
        watch(() => shareState.domain, () => {
            loadData();
        });

        const state = reactive({
            records: {
                DomainRecords: [],
                PageNumber: 1,
                PageSize: 100,
                TotalCount: 0
            },
            status: { 'ENABLE': '正常', 'DISABLE': '禁用' },
            statusBtn: { 'ENABLE': '禁用', 'DISABLE': '启用' },

        });
        const loadData = () => {
            if (shareState.group.platform && shareState.domain.DomainName) {
                shareState.loading = true;
                getRecords({
                    Platform: shareState.group.platform,
                    Group: shareState.group.Name,
                    Domain: shareState.domain.DomainName,
                    PageSize: state.records.PageSize,
                    PageNumber: state.records.PageNumber,
                }).then((res) => {
                    shareState.loading = false;
                    res.DomainRecords.forEach(c => {
                        c.autoUpdate = shareState.domain.records.indexOf(c.RR) >= 0;
                    });
                    state.records = res;
                }).catch((e) => {
                    shareState.loading = false;
                });
                getRecordLines({
                    Platform: shareState.group.platform,
                    Domain: shareState.domain.DomainName,
                    Group: shareState.group.Name,
                }).then((res) => {
                    shareState.loading = false;
                    addState.form.Line = null;
                    addState.recordLines = res;
                    addState.recordLinesJson = res.reduce((json, item) => {
                        json[item.LineCode] = item.LineName;
                        return json;
                    }, {});
                }).catch((e) => {
                    shareState.loading = false;
                });
            }
        }
        const handleDel = (row) => {
            shareState.loading = true;
            delRecord({ 'RecordId': row.RecordId, 'Domain': shareState.domain.DomainName, 'Group': shareState.group.Name, 'Platform': shareState.group.platform }).then(() => {
                shareState.loading = false;
                loadData();
            }).catch((e) => {
                shareState.loading = false;
            });
        }
        const handleSwitchStatus = (row) => {
            shareState.loading = true;
            setRecordStatus({ 'RecordId': row.RecordId, 'Status': row.Status, 'Domain': shareState.domain.DomainName, 'Group': shareState.group.Name, 'Platform': shareState.group.platform }).then(() => {
                shareState.loading = false;
                loadData();
            }).catch((e) => {
                shareState.loading = false;
            });
        }
        const handleRemark = (row) => {
            ElMessageBox.prompt('备注', '备注', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                inputValue: row.Remark
            }).then(({ value }) => {
                if (value) {
                    shareState.loading = true;
                    remarkRecord({ 'RecordId': row.RecordId, 'Remark': value, 'Domain': shareState.domain.DomainName, 'Group': shareState.group.Name, 'Platform': shareState.group.platform }).then(() => {
                        shareState.loading = false;
                        loadData();
                    }).catch((e) => {
                        shareState.loading = false;
                    });
                }
            })
        }
        const handleRecordAutoUpdateChange = (row) => {
            shareState.loading = true;
            switchRecord({
                Platform: shareState.group.platform,
                Group: shareState.group.Name,
                Domain: shareState.domain.DomainName,
                Record: row.RR,
                AutoUpdate: row.autoUpdate,
            }).then(() => {
                shareState.updateFlag = Date.now();
                shareState.loading = false;
                //loadData();
            }).catch((e) => {
                shareState.loading = false;
            });
        }

        onMounted(() => {
            getRecordTypes().then((res) => {
                addState.recordTypes = res;
            });
        });

        const formDom = ref(null);
        const addState = reactive({
            showAdd: false,
            recordTypes: [],
            recordLines: [],
            recordLinesJson: {},
            recordTTLs: [
                { text: '10分钟', value: 600 }, { text: '30分钟', value: 1800 }, { text: '1小时', value: 3600 }, { text: '12小时', value: 43200 }, { text: '1天', value: 86400 }
            ],
            form: {
                RR: '',
                Type: 'A',
                Value: '',
                TTL: 600,
                Line: null,
                Priority: 10,
                DomainName: '',
                Platform: '',
                RecordId: '',
                AutoUpdate: false,
            },
            rules: {
                RR: [{ required: true, message: '必填', trigger: 'blur' }],
                Type: [{ required: true, message: '必填', trigger: 'blur' }],
                Line: [{ required: true, message: '必填', trigger: 'blur' }],
                Value: [{ required: true, message: '必填', trigger: 'blur' }],
            }
        });
        const handleAdd = () => {
            addState.form.RR = '';
            addState.form.RecordId = '';
            addState.showAdd = true;
        }
        const handleEdit = (row) => {
            addState.form.RR = row.RR;
            addState.form.RecordId = row.RecordId;
            addState.form.Type = row.Type;
            addState.form.Value = row.Value;
            addState.form.TTL = row.TTL;
            addState.form.Priority = row.Priority;
            addState.form.Line = row.Line;
            addState.showAdd = true;
        }
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                shareState.loading = true;
                addRecord({
                    Platform: shareState.group.platform,
                    Group: shareState.group.Name,
                    DomainName: shareState.domain.DomainName,
                    RecordId: addState.form.RecordId,
                    RR: addState.form.RR,
                    Type: addState.form.Type,
                    Value: addState.form.Value,
                    TTL: addState.form.TTL,
                    Priority: addState.form.Priority,
                    Line: addState.form.Line,
                }).then(() => {
                    shareState.loading = false;
                    addState.showAdd = false;
                    handleRecordAutoUpdateChange(addState.form.RR, addState.form.AutoUpdate);
                }).catch((e) => {
                    shareState.loading = false;
                });
            })
        }

        return {
            shareState, ...toRefs(state), loadData, handleDel, handleSwitchStatus, handleRemark, handleRecordAutoUpdateChange,
            ...toRefs(addState), formDom, handleAdd, handleEdit, handleSubmit
        }
    }
}
</script>
<style lang="stylus" scoped>
.head
    padding-bottom: 0.6rem;

.op
    a
        padding: 0 0.6rem;

.pages
    padding-top: 1rem;
</style>