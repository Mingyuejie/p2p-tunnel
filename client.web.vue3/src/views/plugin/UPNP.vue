<!--
 * @Author: snltty
 * @Date: 2021-08-20 00:47:21
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-22 17:30:56
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\UPNP.vue
-->
<template>
    <div class="upnp-wrap">
        <div class="head">
            <el-button type="primary" size="mini" style="margin-right:.6rem" @click="handleAdd">增加映射</el-button>
            <el-select v-model="deviceIndex" placeholder="请选择设备" size="mini" @change="deviceChange">
                <el-option v-for="item in devices" :key="item.index" :label="item.text" :value="item.index">
                </el-option>
            </el-select>
            <span style="margin-left:.6rem">添加时，有效期为秒数,0为系统默认，尚不确定具体有效期</span>
        </div>
        <el-table v-loading="loading" :data="list" border size="mini">
            <el-table-column prop="PrivatePort" label="内网端口" width="100">
                <template #default="scope">
                    <template v-if="scope.row.add">
                        <el-input v-model="scope.row.PrivatePort" size="mini"></el-input>
                    </template>
                    <template v-else>{{scope.row.PrivatePort}}</template>
                </template>
            </el-table-column>
            <el-table-column prop="PublicPort" label="外网端口" width="100">
                <template #default="scope">
                    <template v-if="scope.row.add">
                        <el-input v-model="scope.row.PublicPort" size="mini"></el-input>
                    </template>
                    <template v-else>{{scope.row.PublicPort}}</template>
                </template>
            </el-table-column>
            <el-table-column prop="Expiration" label="有效期" width="150">
                <template #default="scope">
                    <template v-if="scope.row.add">
                        <el-input v-model="scope.row.Lifetime" size="mini"></el-input>
                    </template>
                    <template v-else>{{scope.row.Expiration}}</template>
                </template>
            </el-table-column>
            <el-table-column prop="Protocol" label="协议" width="100">
                <template #default="scope">
                    <template v-if="scope.row.add">
                        <el-select v-model="scope.row.Protocol" placeholder="选择协议" size="mini">
                            <el-option v-for="(item,index) in protocols" :key="index" :label="item" :value="index">
                            </el-option>
                        </el-select>
                    </template>
                    <template v-else>{{scope.row.Protocol}}</template>
                </template>
            </el-table-column>
            <el-table-column prop="Description" label="说明">
                <template #default="scope">
                    <template v-if="scope.row.add">
                        <el-input v-model="scope.row.Description" size="mini"></el-input>
                    </template>
                    <template v-else>{{scope.row.Description}}</template>
                </template>
            </el-table-column>
            <el-table-column prop="todo" label="操作" width="145" fixed="right" class="t-c">
                <template #default="scope">
                    <template v-if="scope.row.add">
                        <el-button type="primary" size="mini" @click="handleSave(scope)" :loading="scope.row.loading">保存</el-button>
                        <el-button v-if="!scope.row.loading" size="mini" @click="handleCancel(scope)" :loading="scope.row.loading">取消</el-button>
                    </template>
                    <template v-else>
                        <el-popconfirm title="删除不可逆，是否确认" @confirm="handleDel(scope)">
                            <template #reference>
                                <el-button type="danger" size="mini" icon="el-icon-delete"></el-button>
                            </template>
                        </el-popconfirm>
                    </template>
                </template>
            </el-table-column>
        </el-table>
        <div class="remark">
            <el-alert title="说明" type="info" show-icon :closable="false">
                <p style="line-height:2rem">1、当你拥有公网IP时可用</p>
            </el-alert>
        </div>
    </div>
</template>
<script>
import { reactive, toRefs } from '@vue/reactivity'
import { getUpnpDevices, getUpnpMappings, sendUpnpDelMapping, sendUpnpAddMapping } from '../../apis/upnp'
import { ElMessage } from 'element-plus'
export default {
    setup () {

        const state = reactive({
            loading: false,
            list: [],
            devices: [],
            deviceIndex: null,
            protocols: ['TCP', 'UDP']
        });

        getUpnpDevices().then((res) => {
            console.log(res);
            state.devices = res.map((v, i) => { return { text: v, index: i } });
        });
        const deviceChange = () => {
            getUpnpMappings(state.deviceIndex).then((res) => {
                state.list = JSON.parse(res).map(c => {
                    c.Expiration = new Date(c.Expiration).format('yyyy-MM-dd hh:mm:ss');
                    c.Protocol = state.protocols[c.Protocol];
                    return c;
                });
            });
        };
        const handleDel = (scope) => {
            sendUpnpDelMapping(scope.row.DeviceIndex, scope.$index).then(() => {
                deviceChange();
            });
        }
        const handleAdd = () => {
            if (state.deviceIndex == null) {
                return ElMessage.error('请选择设备');
            }
            if (state.list.filter(c => c.add === true).length > 0) return;

            state.list.push({
                PrivatePort: 0, PublicPort: 0, Lifetime: 0, Protocol: 0,
                Description: '',
                add: true, loading: false
            });
        }
        const handleCancel = (scope) => {
            state.list.splice(scope.$index, 1);
        }
        const handleSave = (scope) => {
            scope.row.PrivatePort = + scope.row.PrivatePort;
            scope.row.PublicPort = + scope.row.PublicPort;
            scope.row.Lifetime = + scope.row.Lifetime;
            if (!scope.row.PrivatePort || isNaN(scope.row.PrivatePort) || !scope.row.PublicPort || isNaN(scope.row.PublicPort)) {
                return ElMessage.error('请正确填写端口号');
            }
            if (!scope.row.Lifetime || isNaN(scope.row.Lifetime)) {
                return ElMessage.error('请正确填写有效期');
            }
            scope.row.loading = true;
            sendUpnpAddMapping(scope.row).then(() => {
                deviceChange();
            }).catch((e) => {
                ElMessage.error(e);
                scope.row.loading = false;
            });
        }

        return {
            ...toRefs(state), deviceChange, handleDel, handleAdd, handleCancel, handleSave
        }
    }
}
</script>
<style lang="stylus" scoped>
.upnp-wrap
    padding: 2rem;

    .head
        margin-bottom: 1rem;

.remark
    margin-top: 1rem;
</style>