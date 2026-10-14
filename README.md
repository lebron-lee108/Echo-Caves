# VP515 Unity 项目

四人 Game Development 课程项目。Windows PC、2D 横版平台解谜；少女与蝴蝶双形态。当前版本是开发前置工程，不代表第一章玩法已经实现。

团队公开仓库：https://github.com/lebron-lee108/VP515 。公开访问允许查看和克隆，不代表可以直接推送。三名组员必须用自己的账号接受 Collaborator 邀请，获得 Write 权限后再参与提交。

首次加入和日常操作请照着 [组员加入与日常 Git 操作](Docs/Team-Onboarding.md) 执行。李泊良本机继续使用 `E:\VP515`，不需要重复克隆或新建项目。

## 打开工程

1. 四名成员统一安装 **Unity 6000.3.23f1**。不要自动升级工程或包版本。
2. 在 Unity Hub 中选择添加已有项目，选择包含 `Assets`、`Packages`、`ProjectSettings` 的仓库根目录。
3. 等待首次导入结束，在 Project 窗口打开 `Assets/_VP515/Scenes/Foundation.unity`。
4. 当前场景包含相机、2D 全局灯、背景分层根节点和空 Tilemap，没有角色或正式美术。

## 已配置的基础

- Universal 2D 模板，URP 2D Renderer；Unity 负责生成场景和设置文件。
- 正交相机；暂定 24 PPU、960×540 参考分辨率，默认 Windows 窗口1920×1080。
- URP 自带 Pixel Perfect Camera，像素对齐、不启用低分辨率 RenderTexture 放大、不启用默认后处理。
- `Force Text` 序列化、`Visible Meta Files`，适合 Git 协作。
- `Foundation` 已作为构建入口；原始模板的 `SampleScene` 保留。
- Git 忽略生成目录；`Assets`、`.meta`、`ProjectSettings`、`manifest.json`、`packages-lock.json`都提交。
- Git LFS 跟踪二进制美术、音频和视频；运行录屏、构建产物放在被忽略的目录中。
- Unity CLI 连接包 `com.unity.pipeline` 固定为 `0.8.0-exp.1`，用于编辑器自动化；属于实验包，更新前须团队评估。

参考分辨率与 PPU 是沿用初版计划的准备基准，应在批量制作角色和 TileSet 之前由团队确认。

## 目录

- `Assets/_VP515/Scenes`：正式场景；`Sandboxes`：独立机制验证。
- `Assets/_VP515/Scripts`：Core、Player、Gameplay、Presentation。
- `Assets/_VP515/Art/Pixel`：像素角色和地形。新 PNG 默认 Sprite、24 PPU、Point、无压缩、无 Mipmap。
- `Assets/_VP515/Art/Backgrounds`：多层背景。新图默认 Sprite、Bilinear；按真实素材尺寸设置 PPU 和缩放。
- `Assets/_VP515/Art/UI`：界面资源；其他目录分别放动画、材质、音频、Prefabs、Data、Settings。
- `Assets/Editor/VP515Setup`：显式运行的前置工具，仅在编辑器中使用，不进入游戏程序。
- `Docs`：协作规则、研究和贡献记录；`Tools`：本地协作配置脚本。

## 背景层与碰撞层

视觉 Sorting Layer 从后往前：Sky、DistantBackground、FarBackground、MidBackground、WorldBack、Ground、Actors、Effects、Foreground、UI。

Physics Layer 预留：Player、Butterfly、Shell、TerrainSolid、TerrainData、Interactable、Enemy、GameplayTrigger。当前只建立名称，双形态碰撞矩阵和切换行为将在玩法规格确定后配置。

背景层级是供美术和视差组件挂载的空节点，当前尚无背景图片或视差运行脚本。远景慢、近景快的系数应在实际镜头测试中设置；视差背景不承担平台碰撞。

## 每天协作

开始同步前，在 Unity 保存并关闭工程，在 GitHub Desktop 提交未完成工作或暂存更改；仅保存场景不等于 Git 工作区已经干净。切回 `main`，Fetch origin 后如有更新则 Pull origin，再创建 `feature/简短功能名` 分支。提交时附上相关 `.meta`，完成后推送分支并通过 Pull Request 评审合并到 `main`。

同一个 `.unity` 场景或复杂 Prefab 同时只由一个人编辑。其他人使用独立场景或 Prefab 交接。不要在电脑文件管理器里单独移动图片、脚本或 `.meta`。

首次克隆后运行 `git lfs install --local` 和 `git lfs pull`。仓库本地设置通过 `Tools/Setup-Git.ps1` 配置；克隆不会继承创建者的 `.git/config`。GitHub Desktop 是可选图形界面，不影响本地 Git 的使用。

本机已配置GitHub隐私邮箱作为仓库内提交邮箱，没有修改全局Git身份。组员应使用自己的姓名和GitHub隐私邮箱。第一份工程准备提交标注AI协助，不代表手写玩法代码。

## 验证与构建

编辑器菜单 `VP515 > Preparation`：验证基础配置、构建 Windows 基础版本。`Apply baseline settings` 不应在未保存场景时运行，它会切换到 Foundation 场景。

Windows 基础构建输出到 `Builds/Windows/Baseline/VP515.exe`，仅供验证工具链。`Docs/Setup` 保存基础配置和构建验证报告；不把空场景构建成功当成游戏完成。

首次Windows基础构建成功，0个错误、1条警告。该警告说明Unity Pipeline没有配置Player运行端连接，因此在导出的游戏中禁用；编辑器CLI连接不受影响，当前也不需要为玩家版本启用远程自动化。

## English project brief

VP515 is a four-person Windows PC course project built with Unity 6000.3.23f1 and Universal 2D. The planned game is a side-scrolling platform puzzler with human and butterfly forms. This repository currently provides the project foundation, version-control setup and collaboration conventions. Gameplay, final art and player testing remain subsequent work.
