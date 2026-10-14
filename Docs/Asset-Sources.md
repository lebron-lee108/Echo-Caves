# 资源来源

工程包含 Unity Universal 2D 模板资源，以及首节切片的 AI 辅助原创素材。用户参考截图和视频没有用作运行时美术。

## 2026年10月15日切片素材

- `Assets/_VP515/Art/Slice/ForestVista.png`：内置图像生成工具生成的原创远景，冷灰枯木与暖金雾光。
- `Assets/_VP515/Art/Slice/SceneryAtlas.png`：同工具生成的透明场景物件；Unity Sprite Data Provider 切为树、根桥、岩石、草、灯、门、符印与蝴蝶，保持原始 atlas 可替换。
- `Assets/_VP515/Art/Slice/TravelerFrames.png`：同工具生成的八姿态角色图；是 AI 帧组，不冒称专业手绘逐帧动画。Unity 内校正裁切和脚底 pivot，后续由美术同学审阅和替换。
- `Assets/_VP515/Art/Slice/Thornling.png`：同工具生成的原创枯木小怪。当前移动/攻击靠代码姿态表现，没有整套专属逐帧动画。
- `SliceAudio.cs`：程序合成的原创音阶环境循环、独立低通噪声风床、噪声挥击、木质命中、砂砾脚步和符印音，没有采样参考视频的声音。采样端点/有限幅值检查不是人耳听感或最终输出混音验收。
- TextMeshPro Essentials：Unity uGUI 包附带资源，通过官方非交互导入入口安装；许可随资源保留。
- `EffectWhite.asset` 和 `SliceGlow.shader`：代码定义的白色效果载体与软光着色器，不是美术插画。

参考视频本地抽帧位于 `Evidence/ReferenceVideo-20261015/`，已忽略，不上传至公开仓库。图像生成提示词见 `Docs/Slice/Art-Prompts.md`。素材用于本次可评估切片，不将生成器输出等同于团队画风验收或来源权利审查已完成。

## 夜间美术融合返工

`ForestVistaV3.png` 与 `RootBridgeV3.png` 是内置图像工具基于用户参考图派生的背景层和透明连续桥素材，不直接把整张截图当游戏背景，也不据此声称来源权利已审核。当前只用于本地制作候选，未经来源审阅不推送公开仓库。

`TravelerWalkV3.png` 与 `ThornlingFramesV3.png` 基于前述AI角色素材派生走路与敌人八帧组，Unity内校正脚底、矩形与密度；是AI帧组，不冒称专业手绘或自然动作已验收。`LeavesV3.png` 为独立生成的四片透明落叶，用于三层动态环境。上述新素材保留旧版，提示词记录在切片文档。

后续每项资源记录：项目内路径、原作者、来源URL、许可/购买凭证、修改情况、团队共享范围和公开展示范围。参考图片不能自动当作有授权的游戏资产。
