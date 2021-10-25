/*
 * @Author: snltty
 * @Date: 2021-08-20 16:06:04
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-25 14:54:01
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\plugins\ddns.js
 */
import { sendWebsocketMsg } from "../request";

export const getPlatforms = (json = { Platform: '', Group: '' }) => {
    return sendWebsocketMsg(`ddns/Platforms`, json);
}
export const getDoamins = (json = {
    Platform: '', Group: '',
    PageNumber: 1, PageSize: 500
}) => {
    return sendWebsocketMsg(`ddns/Domains`, json);
}
export const addDoamin = (json = { Platform: '', Group: '', Domain: '' }) => {
    return sendWebsocketMsg(`ddns/AddDomain`, json);
}
export const deleteDomain = (json = { Platform: '', Group: '', Domain: '' }) => {
    return sendWebsocketMsg(`ddns/DeleteDomain`, json);
}
export const getRecords = (json = { Platform: '', Group: '', Domain: '', PageNumber: 1, PageSize: 500 }) => {
    return sendWebsocketMsg(`ddns/GetRecords`, json);
}
export const setRecordStatus = (json = { Platform: '', Group: '', Domain: '', RecordId: '', Status: '' }) => {
    return sendWebsocketMsg(`ddns/SetRecordStatus`, json);
}
export const delRecord = (json = { Platform: '', Group: '', Domain: '', RecordId: '' }) => {
    return sendWebsocketMsg(`ddns/DelRecord`, json);
}
export const remarkRecord = (json = { Platform: '', Group: '', Domain: '', RecordId: '', Remark: '' }) => {
    return sendWebsocketMsg(`ddns/RemarkRecord`, json);
}
export const getRecordTypes = () => {
    return sendWebsocketMsg(`ddns/GetRecordTypes`);
}
export const getRecordLines = (json = { Platform: '', Group: '', Domain: '' }) => {
    return sendWebsocketMsg(`ddns/GetRecordLines`, json);
}
export const addRecord = (json = {
    Platform: '',
    Group: '',
    DomainName: '',
    RecordId: '',
    RR: '',
    Type: '',
    Value: '',
    TTL: 600,
    Priority: 10,
    Line: '',
}) => {
    json.TTL = +json.TTL;
    json.Priority = +json.Priority;
    if (json.Priority == 0) {
        json.Priority = 1;
    }
    return sendWebsocketMsg(`ddns/AddRecord`, json);
}
export const switchRecord = (json = {
    Platform: '',
    Group: '',
    Domain: '',
    Record: '',
    AutoUpdate: false,
}) => {
    console.log(json);
    return sendWebsocketMsg(`ddns/SwitchRecord`, json);
}



