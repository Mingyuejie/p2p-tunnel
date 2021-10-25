<!--
 * @Author: snltty
 * @Date: 2021-09-03 14:39:29
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-25 17:20:14
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\ddns.md
-->
## 域名解析

```
{
  "Enable": true, //是否启用
  "Interval": 60000, //定时更新间隔 ms
  "Platforms": [
    {
      "Name": "Aliyun", //阿里云平台  可以多个分组，不同分组不同秘钥管理不同域名
      "Groups": [
        {
          "Name": "默认分组",
          "Key": "", //秘钥  对应各平台的第一个  要么叫key  要么叫id 要么 SecretId
          "Secret": "", //秘钥  对应各平台的第二个  要么叫 Secret  要么叫 SecretKey
          "Records": [] //此项不填
        }
      ]
    },
    {
      "Name": "Tencent", //腾讯平台  可以多个分组，不同分组不同秘钥管理不同域名
      "Groups": [
        {
          "Name": "默认分组",
          "Key": "",
          "Secret": "",
          "Records": [] //此项不填
        }
      ]
    }
  ]
}