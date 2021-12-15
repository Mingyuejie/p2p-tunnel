/*
 * @Author: wwping
 * @Date: 2021-01-09 22:32:31
 * @LastEditors: wwping
 * @LastEditTime: 2021-01-10 01:34:34
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \qbcode.vclass.client.vue\.electron-vue\build-upload.js
 */
const path = require('path')
const fs = require('fs')
const OSS = require('ali-oss');
const slog = require('single-line-log').stdout;

const client = new OSS({
    accessKeyId: 'LTAIl07QZx9hwJ4P',
    accessKeySecret: 'fYoSmsiLomNc2jbfDNkyISZAud6iIM',
    bucket: 'ide-qbcode',
    region: 'oss-cn-shenzhen',
    timeout: 1 * 60 * 60 * 1000
});

const distRootPath = '/downloads/vclass/update/';
const sourceRootPath = '../build';

const joinPath = (...paths) => {
    return path.join(...paths).replace(/\\/g, '/');
}
const formatError = (msg) => {
    return `\x1B[31m${msg}\x1B[39m`;
}
const formatSuccess = (msg) => {
    return `\x1B[32m${msg}\x1B[39m`;
}
const formatWarn = (msg) => {
    return `\x1B[33m${msg}\x1B[39m`;
}

let args = process.argv.slice(2);
let sourcePath = (args.map(c => c.split('=')).filter(c => c[0] == '--sp')[0] || [])[1];
let distPath = (args.map(c => c.split('=')).filter(c => c[0] == '--dp')[0] || [])[1];
if (!sourcePath || !distPath) {
    console.error('请设置 [--sp] 和[--dp]');
    process.exit(0);
}
sourcePath = path.join(__dirname, sourceRootPath, sourcePath);
distPath = joinPath(distRootPath, distPath);

const files = fs.readdirSync(sourcePath).map((filename) => {
    let filePath = path.join(sourcePath, filename);
    let file = fs.statSync(filePath);
    return {
        fullName: filePath,
        filename: filename,
        size: file.size,
        distPath: joinPath(distPath, filename),
        isdir: file.isDirectory()
    }
}).filter(c => c.isdir == false).sort((a, b) => b.size - a.size);

console.log(formatWarn('开始上传---------------------------------'));
const fn = (index = 0) => {
    if (index >= files.length) {
        console.log(formatSuccess('上传成功~~~~~~~~~~~~~~~~~~~~~~~~~~~~'));
        return;
    }
    console.log(`${files[index].fullName.replace(path.join(__dirname, sourceRootPath), '')} -> ${files[index].distPath}`);
    client.multipartUpload(files[index].distPath, files[index].fullName, {
        async progress (percentage, cpt) {
            slog(`${(percentage * 100).toFixed(2)}%`);
        },
    }).then(() => {
        console.log('\n')
        fn(++index);
    }).catch(() => {
        console.log(formatError('上传失败!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!'));
    });
}
fn();
