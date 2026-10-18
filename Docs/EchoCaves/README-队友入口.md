# Echo Caves 第一章交接入口

冻结日期：2026-10-19。此套文档为当前C岗位交接快照；历史问题仅见《只读审阅记录》。最新原生结果已写入《最新原生检查.txt》（EchoVerification.RunAll），由主实施运行，本轮文档助手只读核对文件、源码和CSV，未运行Editor或Git。

## 唯一交接入口

| 项目 | 当前值 |
| --- | --- |
| Unity / ProductName | 6000.3.23f1 / `Echo Caves`（含空格） |
| 场景 | `Assets/_EchoCaves/Scenes/Chapter01_Continuous.unity` |
| UI Prefab | 仅 `Assets/_EchoCaves/UI/Prefabs/EchoCavesUI-v03.prefab` |
| 模块 | 仅 `Assets/_EchoCaves/Prefabs/Modules-v04/` 内9个Prefab |
| 世界参考 | 已按16PPU、768×432装配；UI参考1600×900 |
| 内容 | 12项可替换AI环境源、12相机书签、103原生锚点、54语义映射 |
| 原生检查（主实施报告） | Structure46/46、Presentation12/12、World16/16、Route18/18、GPU tint PASS；UI13视图161标签PASS |

场景UI根与上述UI Prefab已在源码打磨后重新原生构建并通过契约检查。最新报告还通过2389个持久场景Sprite引用、外场景锚点排除及9个模块原生打开/关闭回读。新章节已启用为构建入口，旧场景保留但禁用（主实施报告）。默认HUD干净，F1切换审阅dock。集成画面位于 `Evidence/EchoCaves/Integrated-v04/`（13张），环境画面位于 `Evidence/EchoCaves/Final-C/`（12张）。两批画面均已原生重拍；D—E侧壁、缩放后的MossLip位置与地表/G路线板已更新（主实施报告）。截图及检查不是完整游戏验收，也不承诺参考图1:1还原。

## 陈旭 / 美术接手

1. 仓库已更名为 [Echo-Caves](https://github.com/lebron-lee108/Echo-Caves)，公开、沿用原历史；本地仍为E:/VP515。最新C内容提交、推送及最终SHA待主实施交付，当前不能凭远端旧HEAD接手。
2. 收到 `feature/chapter-one-slice` 的完整交接SHA后，再按下面命令取精确快照。不合并main；已有未提交修改先另行保存，不执行强制覆盖。
3. 用指定Unity版本打开项目。保留.meta、LFS对象、Packages/manifest.json与packages-lock.json、Noto OFL及共享TMP着色器依赖；不要只复制新根目录。
4. 打开上述场景，不重复导入UI，不增加第二个EventSystem。模块是复用件，不是要求把9个再次堆入已装配场景。旧版本仅本地历史，排除本次提交，不作为接手入口。

```powershell
git fetch origin
git switch --detach <主实施交付的完整SHA>
git lfs pull
git rev-parse HEAD
```

先读《程序美术交接》《C-制作规格》，再用《第一章点位》《语义原生锚点映射》《实际梯子端点》接入。《实际场景锚点》是Editor原生导出，不手填坐标。

## 尚未完成

B控制器、跳跃/整段攀爬、AI、解谜、音频事件、经济、存档与完整游玩未验收；D最终角色、生物、动画、VFX和音频待交。当前没有角色或动画Boss占位，只有锚点/Gizmo。真实Play输入进入Settings且1个EventSystem通过，仅是自动化UI烟雾测试，不是人工玩家测试。
