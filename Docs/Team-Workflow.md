# 四人协作与交接

## 仓库与权限

使用一个团队私有仓库。仓库管理员将三名组员加入 Collaborators；每人使用自己的 GitHub 账号，勿共享密码或令牌。`main`保留可以打开、编译、运行的版本。课程项目当前没有公开发布或商业化承诺。

## 每次交接

1. 更新自己的分支，确认工程版本仍为6000.3.23f1。
2. 开始编辑场景前在团队看板记录场景 Owner，避免同场景并行编辑。
3. 在Unity中保存，确认Console没有编译错误。
4. 提交功能及对应.meta；在提交说明里写明改了什么、如何检查。
5. 推送分支，提交PR；至少另一位成员复核后合并。

提交说明示例：`feat: add shell pressure plate prototype`、`fix: restore checkpoint after recall`、`art: add mountain far background`。

## 基础检查

- 可打开Foundation或本次功能场景，无编译错误。
- 仓库中没有Library、Temp、Builds、Logs、UserSettings。
- PNG、PSD、音频等二进制资源进入LFS；组员已经拉取实际LFS资源。
- manifest.json与packages-lock.json一致，资源.meta完整。
- 禁止强推main、直接覆盖队友场景或用删除Library代替诊断。

## 资产归档

在 `Docs/Asset-Sources.md` 记录来源、作者、许可和是否允许团队使用/公开展示。录屏放到 `Recordings`，课程Build放到 `Builds`，不把它们反复提交到源工程仓库。可编辑美术源文件可以用LFS存储。

## 个人贡献

每人记录自己负责的设计、代码和验证结果。AI生成后本人阅读/修改/调试、本人手写、队友完成应分别标注。组内验收与真实外部玩家测试分别记录，不虚构测试数据。
