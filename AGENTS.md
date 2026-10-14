# VP515 development instructions

- This is a four-person course project: Windows PC, 2D side-scrolling platform puzzles, Universal 2D/URP, Unity 6000.3.23f1.
- Current stage is project preparation. Do not invent final character art, narrative changes or gameplay rules.
- Preserve the template SampleScene. Use Assets/_VP515/Scenes/Foundation.unity for the baseline and separate scenes for gameplay experiments.
- Use the Unity Editor API or live Unity CLI for scenes, prefabs and settings; do not hand-edit their YAML.
- Use Unity Package Manager APIs for package changes. Commit manifest.json and packages-lock.json together.
- Every asset must keep its .meta file. Move and rename assets inside Unity.
- Use Git feature branches, small commits, clear ownership and review. Do not force-push or discard uncommitted work.
- LFS tracks binary artwork/audio. Build output, Library, Temp, logs and credentials stay out of Git.
- Pixel characters/tiles use 24 PPU and a provisional 960x540 reference. Background art has separate import rules; do not globally force all images into pixel-art settings.
- Keep code and work attribution accurate: human authored, AI assisted and teammate authored are distinct.
- No public publication, paid asset acquisition, monetization or cloud services unless requested.
