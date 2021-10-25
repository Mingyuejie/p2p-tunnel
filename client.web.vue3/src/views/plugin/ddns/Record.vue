<!--
 * @Author: snltty
 * @Date: 2021-10-24 19:40:41
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-25 11:33:03
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ddns\Record.vue
-->
<template>
    <div class="h-100 flex flex-column flex-nowrap">
        <div class="head">
            <el-button type="info" size="mini" @click="handleAdd">新增解析</el-button>
            <el-button size="mini" @click="loadData">刷新列表</el-button>
        </div>
        <div class="body flex-1">
            <el-table v-loading="loading" :data="list" border size="mini" height="100%">
                <el-table-column prop="RR" label="主机记录">
                    <template #default="scope">
                        <div class="flex">
                            <span>{{scope.row.RR}}</span>
                            <span class="flex-1"></span>
                            <el-popover placement="bottom-end" title="啥意思" trigger="hover" content="当IP变化时，是否更新此条解析记录">
                                <template #reference>
                                    <el-switch v-model="scope.row.autoUpdate" @change="handleRecordAutoUpdateChange(scope.row.RR,form.autoUpdate)" />
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
                <el-table-column prop="TTL" label="TTL" width="50"></el-table-column>
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
import { reactive, ref, toRefs } from '@vue/reactivity'
import { injectShareData } from './share-data'
import { onMounted, watch } from '@vue/runtime-core';
import {
    getRecords, setRecordStatus, delRecord, remarkRecord,
    getRecordTypes, addRecord, getRecordLines, switchRecord
} from '../../../apis/plugins/ddns'
import { ElMessageBox } from 'element-plus'

export default {
    setup () {

        const shareData = injectShareData();
        watch(() => shareData.group, () => {
            loadData();
        });

        const state = reactive({
            loading: false,
            list: [],
            status: { 'ENABLE': '正常', 'DISABLE': '禁用' },
            statusBtn: { 'ENABLE': '禁用', 'DISABLE': '启用' },

        });
        const loadData = () => {
            if (shareData.group.platform && shareData.domain.Domain) {
                getRecords({
                    Platform: shareData.group.platform,
                    Domain: shareData.domain.Domain
                }).then((res) => {
                    const arr = res.DomainRecords || [];
                    arr.forEach(c => {
                        c.autoUpdate = shareData.domain.Records.indexOf(c.RR) >= 0;
                    });
                    state.list = arr;
                });
                getRecordLines({
                    Platform: shareData.group.platform,
                    Domain: shareData.domain.Domain
                }).then((res) => {
                    addState.recordLines = res;
                    addState.recordLinesJson = res.reduce((json, item) => {
                        json[item.LineCode] = item.LineName;
                        return json;
                    }, {});
                })
            }
        }
        const handleDel = (row) => {
            delRecord({ 'RecordId': row.RecordId, 'Domain': row.DomainName, 'Platform': shareData.group.platform }).then(() => {
                loadData();
            });
        }
        const handleSwitchStatus = (row) => {
            setRecordStatus({ 'RecordId': row.RecordId, 'Status': row.Status, 'Domain': row.DomainName, 'Platform': shareData.group.platform }).then(() => {
                loadData();
            });
        }
        const handleRemark = (row) => {
            ElMessageBox.prompt('备注', '备注', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                inputValue: row.Remark
            }).then(({ value }) => {
                if (value) {
                    remarkRecord({ 'RecordId': row.RecordId, 'Remark': value, 'Domain': row.DomainName, 'Platform': shareData.group.platform }).then(() => {
                        loadData();
                    });
                }
            })
        }
        const handleRecordAutoUpdateChange = (rr, autoUpdate) => {
            switchRecord({
                Platform: shareData.group.platform,
                Group: shareData.group.Name,
                Domain: shareData.domain.Domain,
                Record: rr,
                AutoUpdate: autoUpdate,
            }).then(() => {
                loadData();
            })
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
                Line: 'default',
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
                state.loading = true;
                addRecord({
                    Platform: shareData.group.platform,
                    DomainName: shareData.domain.Domain,
                    RecordId: addState.form.RecordId,
                    RR: addState.form.RR,
                    Type: addState.form.Type,
                    Value: addState.form.Value,
                    TTL: addState.form.TTL,
                    Priority: addState.form.Priority,
                    Line: addState.form.Line,
                }).then(() => {
                    state.loading = false;
                    addState.showAdd = false;
                    handleRecordAutoUpdateChange(addState.form.RR, addState.form.AutoUpdate);
                }).catch((e) => {
                    state.loading = false;
                });
            })
        }

        return {
            ...toRefs(state), loadData, handleDel, handleSwitchStatus, handleRemark, handleRecordAutoUpdateChange,
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
</style>