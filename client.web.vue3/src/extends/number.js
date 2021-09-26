/*
 * @Author: snltty
 * @Date: 2021-09-26 20:43:28
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-26 21:01:10
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\extends\number.js
 */
Number.prototype.sizeFormat = function () {
    let unites = ['B', 'KB', 'MB', 'GB', 'TB'];
    let unit = unites[0], size = this;
    while ((unit = unites.shift()) && size > 1024) {
        size /= 1024;
    }
    return unit == 'B' ? size + unit : size.toFixed(2) + unit;
}