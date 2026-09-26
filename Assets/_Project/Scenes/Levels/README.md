# Level scenes

The current collaboration rule is deliberately simple: every person creates and owns a separate Unity scene.

Recommended layout:

```text
Assets/_Project/Scenes/Levels/<LevelId>/Work/<owner-or-task>_<purpose>.unity
```

Rules:

- Use a unique scene filename and do not save another person's scene.
- Assemble geometry, ropes, ladders, platforms and gameplay objects directly in your scene.
- Share reusable scripts and prefabs, not one jointly edited scene file.
- There is no 2D-piece, total-2D or 2D-to-3D generated scene workflow.
- Push the scene and every referenced new asset on your own task branch.
- In the handoff, state the scene path and what it contains. Final scene combination is postponed until the integrator chooses a method.

Use `Tools/2026Test/路径机关/显示傻瓜式 Scene 工具` for rope, ladder and rope-platform authoring.
