# Forsaken King's Vessel

《弃王之躯》是一款以暗黑奇幻为基调的 Unity 3D 动作冒险游戏。玩家将在破败王国、梦境场景与最终战区域之间推进剧情，完成探索、对话、战斗与 Boss 挑战。

## 项目简介

- 引擎：Unity
- 类型：第三人称动作冒险 / 剧情战斗
- 核心内容：场景探索、NPC 对话、背包系统、敌人 AI、Boss 战、任务提示与 UI 交互

当前项目包含多个主要场景与系统脚本，适合作为课程作业展示、团队协作开发与后续版本迭代的基础仓库。

## 主要内容

- `Assets/Scenes/`：菜单、主场景、梦境场景、出生场景、最终场景等
- `Assets/Scripts/UI/`：主菜单、游戏内 HUD、任务与提示 UI
- `Assets/Scripts/Combat/`：战斗角色管理、攻击判定、掉落与属性系统
- `Assets/Scripts/AI/`：普通敌人与 Boss 的行为逻辑
- `Assets/Scripts/Inventory/`：背包、物品、装备展示
- `Assets/Scripts/Dialog/`：对话配置与对话显示系统

## 运行方式

1. 使用 Unity Hub 打开本项目根目录。
2. 等待依赖导入完成。
3. 从 `Assets/Scenes/MenuScene.unity` 或主要关卡场景启动运行。

## 仓库说明

本仓库已配置适用于 Unity 项目的 `.gitignore`，不会提交 `Library/`、`Temp/`、`Logs/` 等本地生成目录。实际协作时，只需要同步 `Assets/`、`Packages/` 与 `ProjectSettings/` 即可。

## 项目状态

这是一个持续开发中的课程游戏项目。后续可以继续补充：

- 更完整的剧情与任务说明
- 操作按键表
- 角色、场景与 Boss 截图
- 版本更新记录
