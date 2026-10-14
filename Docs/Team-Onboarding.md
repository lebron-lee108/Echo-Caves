# VP515 组员加入与日常 Git 操作

四名成员共同使用 https://github.com/lebron-lee108/VP515 ，统一 Unity 6000.3.23f1。李泊良使用已有的 `E:\VP515`；其他三人各自在自己的电脑克隆一份工程。不要重新创建 Unity 项目，也不要用微信群压缩包覆盖工程。

## 三名组员接受邀请

邀请对象是 `Everglow0513`、`cccwww456`、`ApicnicCLOTH`，权限为 Write，可以向团队仓库提交代码和资源。管理员发送邀请后，每位组员仍需亲自接受。

1. 在浏览器登录自己对应的 GitHub 账号，确认右上角头像下的用户名正确。
2. 打开 https://github.com/lebron-lee108/VP515/invitations ，点击接受邀请。如果页面没有显示邀请，检查 GitHub 邮件和右上角通知，或让管理员核对账号及邀请状态。
3. 接受后告诉李泊良，由管理员检查 Collaborators 列表。公开仓库即使没接受邀请也能浏览和下载，所以“能打开仓库”不能证明已经加入团队。

## 第一次下载工程

1. 安装 GitHub Desktop，点击 Sign in to GitHub.com，登录自己的账号。Unity Hub 用自己的 Unity 账号登录，不共享账号。
2. 在 Desktop 点击 `File > Clone repository > URL`，填入 `https://github.com/lebron-lee108/VP515.git`。
3. 选择一个本地空目录，例如最终工程路径 `D:\Projects\VP515`，然后点击 Clone。不要选已有其他 Unity 工程的目录，不要放进 OneDrive 等自动同步目录。这里的路径仅是示例，可自行选择磁盘。
4. 如果弹出 Initialize Git LFS，点击初始化。若提示创建 Fork，先停止，检查是否登录了受邀账号且已经接受邀请；团队直接使用原仓库，不另建 Fork。
5. Desktop 的 `Repository > Repository settings > Git` 中确认提交姓名和邮箱是自己的；可使用 GitHub 邮箱设置中提供的隐私邮箱，不填李泊良的邮箱。

Windows 组员还应安装 Git for Windows 并包含 Git LFS，以便使用项目配置脚本。首次克隆后，在工程根目录右键打开 Git Bash，依次运行以下两行：

```bash
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./Tools/Setup-Git.ps1
git lfs pull
```

第一行只配置这个仓库的 Git/LFS、换行和同步选项；执行策略参数只作用于本次 PowerShell 进程。第二行下载实际美术等大文件。看到报错就保留完整错误并联系李泊良，不要忽略错误继续覆盖文件。日常操作仍可用 Desktop，不需要每天运行这些命令。

## 第一次在 Unity 打开

1. 在 Unity Hub 安装 **6000.3.23f1**，不要用另一个版本自动升级工程。课程 Windows 打包需求对应 Windows 构建支持；Mac 成员的平台模块需另外确认。
2. 在 Hub 的 Projects 页面添加已有工程，选择刚克隆的根目录。正确目录中可以直接看到 `Assets`、`Packages`、`ProjectSettings`。
3. 等待导入和依赖安装。打开 `Assets/_VP515/Scenes/Foundation.unity`，查看 Console 是否有红色编译错误。
4. 当前只是基础场景，没有角色、正式背景或玩法；空场景是当前版本的正常状态，不代表资源丢失。

每位成员完成后，在群里确认“邀请已接受、工程已克隆、Unity 版本正确、Foundation 能打开、Console 无红色编译错误”。仅确认自己实际完成的步骤。

## 每天开始工作

先在群里约定本次谁编辑哪个场景或 Prefab，同一个文件同时只由一个人修改。

1. 如 Unity 已打开，保存场景和资源后关闭工程。
2. Desktop 左上角确认仓库为 VP515。如果 Changes 有自己的修改，先在原工作分支 Commit 保存；不需要丢弃，也不要随手点击 Discard changes。
3. 将 Current branch 切到 `main`。点击 Fetch origin；如果随后出现 Pull origin，再点击拉取。Fetch 只是检查更新，Pull 才把更新合入本地。
4. 点击 `Branch > New branch`，创建自己的功能分支，例如 `feature/yourname-parallax`。分支名描述实际工作，这个例子不代表已指定个人分工。
5. 再从 Hub 打开同一份工程，开始开发。创建分支不会生成第二份 Unity 工程，它让当前目录切换到另一条工作记录。

已有未完成的功能分支可以继续使用。若需要把最新 main 合入该分支，先保存、关闭 Unity，并提交当前修改，再在 Desktop 的 Branch 菜单选择合并 main 到当前功能分支。遇到冲突停止，不要凭感觉覆盖队友版本。

## 保存并上传自己的工作

1. 在 Unity 保存场景和资源，确认 Console 没有编译错误。新资源及对应 `.meta` 都要保留；移动和重命名资源在 Unity 内进行。
2. 切到 Desktop 的 Changes，逐项看看准备提交什么。不要提交密码、API Key、个人隐私、无公开许可的素材，也不要把 Library、Temp、Builds 或录屏加进仓库。
3. 在左下角 Summary 写清修改，例如 `art: add mountain far background`；Description 可写验证场景和结果。
4. 点击 `Commit to 当前分支`。这只在自己电脑保存版本，没有上传。
5. 第一次推送新分支点击 Publish branch；以后点击 Push origin。上传成功后，队友才能从远程获取该分支。

不必等一天结束才提交；完成一个可以检查的小变化就提交一次。AI 辅助代码和个人修改调试的范围记录到贡献文档中。

## 将功能合并进团队主版本

1. 推送功能分支后，在 Desktop 点击 Create Pull Request，或到 GitHub 仓库 Pull requests 页面点击 New pull request。
2. 核对 `base: main`，`compare: 自己的功能分支`。填写改了什么、如何在 Unity 验证、是否有已知问题。
3. 请另一位组员检查。确认无冲突且验证通过后，由有权限的成员点击 Squash and merge，并确认合并。不要在检查完成前合并。
4. 本机保存并关闭 Unity，确认没有未提交更改，切回 main，Fetch origin 后 Pull origin。下一次功能从更新后的 main 开始。

当前流程依靠团队约定执行。不要强推 main，不直接覆盖别人的场景，不把 Git 冲突标记带进 Unity。遇到无法 Pull、Push 或合并的情况，截图错误，保留本地工作，由团队一起处理。

## 你需要记住的按钮

| 按钮 | 实际作用 |
| --- | --- |
| Clone | 第一次把远程工程下载到自己电脑 |
| Fetch origin | 检查远程是否有新提交，不把更新合入当前工作 |
| Pull origin | 将远程更新同步到本地当前分支 |
| Commit | 在自己电脑保存一次可追踪的修改 |
| Publish branch 或 Push origin | 把本地提交上传到 GitHub |
| Pull Request | 请求团队检查并将分支合入 main |

`No local changes` 只表示没有未提交修改，不自动代表已经上传。还需检查是否有待 Push 提交，以及远程是否更新。

## 官方操作参考

GitHub Desktop 的克隆入口和操作步骤见 [GitHub 官方克隆说明](https://docs.github.com/en/desktop/adding-and-cloning-repositories/cloning-and-forking-repositories-from-github-desktop)。
