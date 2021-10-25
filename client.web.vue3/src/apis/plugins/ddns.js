/*
 * @Author: snltty
 * @Date: 2021-08-20 16:06:04
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-25 10:53:16
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\plugins\ddns.js
 */
import { sendWebsocketMsg } from "../request";

export const switchGroup = (json = { Platform: '', Group: '', AutoUpdate: false, }) => {
    return sendWebsocketMsg(`ddns/SwitchGroup`, json);
}

export const getDoamins = () => {
    return sendWebsocketMsg(`ddns/domains`);
}
export const addDoamin = (json = { Platform: '', Group: '', Domain: '', AutoUpdate: false }) => {
    return sendWebsocketMsg(`ddns/AddDomain`, json);
}
export const switchDomain = (json = { Platform: '', Group: '', Domain: '', AutoUpdate: false, }) => {
    return sendWebsocketMsg(`ddns/SwitchDomain`, json);
}
export const deleteDomain = (json = { Platform: '', Group: '', Domain: '' }) => {
    return sendWebsocketMsg(`ddns/DeleteDomain`, json);
}
export const getRecords = (json = { Platform: '', Domain: '' }) => {
    return sendWebsocketMsg(`ddns/GetRecords`, json);
}
export const setRecordStatus = (json = { Platform: '', Domain: '', RecordId: '', Status: '' }) => {
    return sendWebsocketMsg(`ddns/SetRecordStatus`, json);
}
export const delRecord = (json = { Platform: '', Domain: '', RecordId: '' }) => {
    return sendWebsocketMsg(`ddns/DelRecord`, json);
}
export const remarkRecord = (json = { Platform: '', Domain: '', RecordId: '', Remark: '' }) => {
    return sendWebsocketMsg(`ddns/RemarkRecord`, json);
}
export const getRecordTypes = () => {
    return sendWebsocketMsg(`ddns/GetRecordTypes`);
}
export const getRecordLines = (json = { Platform: '', Domain: '' }) => {
    return sendWebsocketMsg(`ddns/GetRecordLines`, json);
}
export const addRecord = (json = {
    Platform: '',
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
    json.TTL = +json.TTL;
    json.Priority = +json.Priority;
    if (json.Priority == 0) {
        json.Priority = 1;
    }
    return sendWebsocketMsg(`ddns/SwitchRecord`, json);
}



