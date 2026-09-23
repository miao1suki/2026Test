# Development content

This directory contains editable and disposable level-development content.

- `Levels/<LevelId>/Authoring` is the canonical source of level geometry and traversal data.
- `Levels/<LevelId>/Preview` is generated, writable piece/total-2D/folded-3D output. It is never a shipping source.
- `Levels/<LevelId>/Sandbox` is for temporary tests and experiments.
- `Sandbox/Systems/<SystemName>` is for reusable system test scenes unrelated to one level.
- Organize ownership by level, piece, face and content chunk—not by job title or person.

Do not add scenes from this directory to an enabled player build. The build guard rejects them.
