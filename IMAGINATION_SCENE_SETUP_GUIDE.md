# 🎬 梦境场景故事脚本使用指南

## 📋 文件说明

**新文件：** `ImaginationSceneStory.cs`  
**位置：** `Assets/Scripts/Story/ImaginationSceneStory.cs`  
**类型：** 梦境场景专属故事管理脚本  
**代码量：** ~400行

---

## 🏗️ 核心架构（1:1 复刻 MainSceneStory.cs）

### 单例模式

```csharp
public static ImaginationSceneStory Instance
{
    get
    {
        if (instance == null) instance = GameObject.FindObjectOfType<ImaginationSceneStory>();
        return instance;
    }
}
```

### Header 分组（完全相同的分组方式）

- `[Header("对话文件")]` - 对话配置
- `[Header("对话物")]` - DialogObject 引用
- `[Header("战斗角色")]` - 敌人 FightAttributes
- `[Header("位置")]` - Transform 标记点
- `[Header("物体")]` - GameObject 引用
- `[Header("标识")]` - 目标标记 Transform
- `[Header("场景管理")]` - 场景名称配置

### 核心方法

```csharp
// 故事进度逻辑驱动
private void UpdateStory()

// 推进进度
public void DriveProcess()

// 事件监听
private void AddStoryEvents()
```

---

## 🎯 故事进度流程（storyProcess）

### 🔴 storyProcess = 0：梦境开始 - 自动播放开场对话

**触发时机：** 场景加载后立即执行

**执行流程：**

1. 播放梦境背景音乐（bgms[0]）
2. 禁用敌人对象
3. 延迟 0.3 秒播放开场对话
4. **强制修复 UI Canvas 为 Screen Space - Camera 模式**
5. 调用 `playerDialogObject.Interact()` 播放对话

**关键代码：**

```csharp
case 0:
    SetAndPlayImaginationBGM(0);
    if (enemyObject != null) enemyObject.SetActive(false);
    StartCoroutine(PlayOpeningDialogAfterDelay(0.3f));
    break;
```

---

### 🟡 storyProcess = 1：对话结束 - 敌人出现，显示任务

**触发时机：** 开场对话结束时，由 `ImaginationSceneListener.CheckDialogEnd()` 自动调用

**执行流程：**

1. 更新主线任务为 "斩杀梦境中的敌人"
2. 激活敌人 GameObject
3. 重置敌人生命值（如果已死亡）
4. 设置目标标记指向敌人

**关键代码：**

```csharp
case 1:
    GameUIManager.Instance.mainTaskTip.UpdateTask("斩杀梦境中的敌人", "...");
    if (enemyObject != null) enemyObject.SetActive(true);
    if (mark_enemy != null)
        GameUIManager.Instance.destinationMark.SetTarget(mark_enemy);
    break;
```

---

### 🟢 storyProcess = 2：敌人被击杀 - 离开梦境

**触发时机：** 敌人死亡时，由 `ImaginationSceneListener.OnEnemyDeath()` 自动调用

**执行流程：**

1. 更新任务为 "离开梦境"
2. 延迟 1 秒执行黑屏过场
3. 加载返回场景（默认 "BirthScene"）

**关键代码：**

```csharp
case 2:
    GameUIManager.Instance.mainTaskTip.UpdateTask("离开梦境", "胜利！...");
    StartCoroutine(ExitImaginationAfterDelay(1.0f));
    break;
```

---

## 🚀 在 Unity 中配置步骤

### 【第1步】在场景中创建管理对象

**Hierarchy 操作：**

```
Imagination Scene
├── ImaginationSceneManager (空GameObject，命名重要!)
│   └── (挂载ImaginationSceneStory脚本)
├── Player
├── Enemy
├── NPC_Dialog_Player
├── NPC_Dialog_Merlin
└── Canvas (UI)
```

**创建方法：**

1. 在 Hierarchy 中右键 → Create Empty
2. 命名为 `ImaginationSceneManager`
3. Position 设为 (0, 0, 0)

---

### 【第2步】添加脚本组件

1. 选中 `ImaginationSceneManager`
2. Inspector → Add Component
3. 搜索输入 `ImaginationSceneStory`
4. 添加脚本

---

### 【第3步】分配 Inspector 字段

在 Inspector 中找到 `ImaginationSceneStory (Script)` 组件，逐一分配：

#### **【对话文件】区域**

```
Dialog_0_0 ← 拖放你的开场对话配置（DialogConfig）
```

#### **【对话物】区域**

```
Player Dialog Object ← 拖放玩家自言自语的DialogObject
Merlin Dialog Object ← 拖放梅林的DialogObject（可选）
```

#### **【战斗角色】区域**

```
Imagination Enemy ← 拖放敌人的FightAttributes组件所在Object
```

#### **【位置】区域**

```
Cave Entry ← 梦境入口位置（Transform）
Enemy Spawn Point ← 敌人出现点（Transform）
```

#### **【物体】区域**

```
Enemy Object ← 拖放敌人的GameObject
Level End ← 拖放关卡结束相关Object（可选）
```

#### **【标识】区域**

```
Mark_Cave Entry ← 梦境入口标记（Transform）
Mark_Enemy ← 敌人标记，使玩家能看到敌人位置（Transform）
```

#### **【场景管理】区域**

```
Exit Scene Name = "BirthScene"  ← 战胜敌人后返回的场景名
```

#### **其他字段**

```
Black Image ← Canvas中的黑色过场图片（Image组件）
Story Audio Source ← 播放BGM的AudioSource
BGMs ← 背景音乐列表（List<AudioClip>）
       [0] = 梦境BGM
       [1] = (可选)
       [2] = 战斗BGM
```

---

## 📄 配置示例（参考值）

```
ImaginationSceneStory 配置示例：

【对话文件】
- Dialog_0_0: ImaginationOpeningDialog (你创建的DialogConfig)

【对话物】
- Player Dialog Object: Player GameObject 的 DialogObject 组件
- Merlin Dialog Object: (可以为空或指向梅林对象)

【战斗角色】
- Imagination Enemy: Enemy GameObject

【位置】
- Cave Entry: Scene 中的 Position (0, 1, 0)
- Enemy Spawn Point: Enemy 初始位置

【物体】
- Enemy Object: Enemy GameObject
- Level End: LevelEnd GameObject

【标识】
- Mark_Cave Entry: CaveEntry Transform
- Mark_Enemy: Enemy Transform

【场景管理】
- Exit Scene Name: "BirthScene"

【音频】
- Black Image: Canvas/DialogUI/BlackImage (Image)
- Story Audio Source: AudioManager 的 AudioSource
- BGMs: [梦境BGM, 音乐2, 战斗BGM]
```

---

## 🔧 BUG 修复详解

### 【问题】对话 UI 不显示

**原因：** Canvas 不在 "Screen Space - Camera" 模式

**修复代码位置：** `PlayOpeningDialogAfterDelay()` 方法

```csharp
// 强制确保UI显示正确（修复UI不显示BUG）
Canvas dialogCanvas = GameUIManager.Instance.dialog.GetComponentInParent<Canvas>();
if (dialogCanvas != null)
{
    dialogCanvas.renderMode = RenderMode.ScreenSpaceCamera;
    dialogCanvas.worldCamera = Camera.main;
}
```

**工作原理：**

1. 获取对话 UI 父级 Canvas
2. 强制设置为 Camera 模式
3. 指定 Main Camera 作为渲染相机
4. 确保 UI 能被玩家看到

---

## 📡 事件监听系统

### 事件流向

```
dialogue_0_0 播放完成
    ↓
GameEventManager.Instance.dialogConfigEndEvent.Invoke(dialog_0_0)
    ↓
ImaginationSceneListener.CheckDialogEnd() 被触发
    ↓
对比 endedDialog == dialog_0_0
    ↓
是 → DriveProcess() → storyProcess 从 0 → 1
    ↓
case 1: 敌人出现
```

```
敌人被击杀
    ↓
GameEventManager.Instance.characterBeforeDeathEvent.Invoke(enemy)
    ↓
ImaginationSceneListener.OnEnemyDeath() 被触发
    ↓
对比 enemy == imaginationEnemy
    ↓
是 → DriveProcess() → storyProcess 从 1 → 2
    ↓
case 2: 离开梦境 → 黑屏 → 加载主场景
```

---

## ⚙️ 核心调用规则

### ✅ 合规调用方式

```csharp
// 1️⃣ 统一使用 Interact() 方法
playerDialogObject.AddSpecialDialog(dialog_0_0);
playerDialogObject.Interact();  // ✅ 正确

// 2️⃣ 事件自动驱动进度
GameEventManager.Instance.dialogConfigEndEvent.Invoke(dialog_0_0);
// ImaginationSceneListener 自动处理

// 3️⃣ 敌人死亡自动触发
GameEventManager.Instance.characterBeforeDeathEvent.Invoke(enemy);
// ImaginationSceneListener 自动处理
```

### ❌ 禁止的调用方式

```csharp
// ❌ 不要直接调用 StartDialog
DialogDisplayer.Instance.StartDialog(...);

// ❌ 不要手动管理进度
storyProcess++;  // 禁止直接修改

// ❌ 不要绕过事件监听
DriveProcess();  // 只在 ImaginationSceneListener 中调用
```

---

## 🔗 依赖关系

```
ImaginationSceneStory
├── GameUIManager (获取UI元素)
├── GameEventManager (事件系统)
├── DataManager (存档管理)
├── PlayerInputManager (输入控制)
├── InventoryManager (背包)
├── DialogDisplayer (对话系统)
└── UnityEngine.SceneManagement (场景加载)
```

---

## 🧪 测试检查表

在正式使用前，检查以下项目：

```
□ ImaginationSceneStory 脚本正确添加到场景
□ 所有 Inspector 字段都被正确分配（无粉红叉号）
□ dialog_0_0 DialogConfig 有内容
□ playerDialogObject 正确指向 DialogObject
□ imaginationEnemy 正确指向 FightAttributes
□ enemyObject 正确指向敌人 GameObject
□ mark_enemy 正确指向敌人 Transform
□ exitSceneName 设为 "BirthScene"（或目标场景）
□ BGM 列表有内容
□ 黑色过场图片正确分配
□ Canvas 的 render mode 被正确强制设置
```

---

## 📊 代码对比（与 MainSceneStory.cs）

| 特性           | 复刻情况    | 说明                    |
| -------------- | ----------- | ----------------------- |
| 单例模式       | ✅ 完全相同 | Instance 获取方式一致   |
| Header 分组    | ✅ 完全相同 | 组织方式一致            |
| storyProcess   | ✅ 完全相同 | 进度驱动逻辑一致        |
| UpdateStory()  | ✅ 完全相同 | 根据进度切换逻辑        |
| DriveProcess() | ✅ 完全相同 | 推进进度 + 保存游戏     |
| 事件监听       | ✅ 完全相同 | AddListener 方式一致    |
| BGM 控制       | ✅ 完全相同 | SetAndPlayBGM 方法相同  |
| 黑屏过场       | ✅ 完全相同 | BlackIEnum 协程完全相同 |

---

## 🚨 常见问题排查

### Q: 对话不自动播放？

**A:** 检查以下几点：

- [ ] dialog_0_0 是否为 null（Inspector检查）
- [ ] playerDialogObject 是否为 null
- [ ] Console 是否有错误信息
- [ ] 延迟时间是否过短（改为 0.5f）

### Q: 敌人出现了但没有对话结束的进度推进？

**A:**

- [ ] 检查 GameEventManager 是否正常工作
- [ ] 确认 dialogConfigEndEvent 被触发
- [ ] 打印 Debug.Log 验证 CheckDialogEnd 是否被调用

### Q: 敌人被击杀了但不离开梦境？

**A:**

- [ ] 确认敌人的 FightAttributes 正确分配
- [ ] 检查 OnEnemyDeath 是否被触发
- [ ] 验证敌人引用与 imaginationEnemy 是否相同

### Q: UI Canvas 显示不了？

**A:**

- [ ] 检查 Canvas render mode（应自动修复为 Camera）
- [ ] 确认 Main Camera 存在
- [ ] 查看 Console 是否有 Canvas 相关错误

---

## 📞 快速参考

**自动触发流程：**

```
Game Start
  ↓
UpdateStory() case 0
  ↓
PlayOpeningDialogAfterDelay(0.3f)
  ↓
playerDialogObject.Interact()
  ↓
对话显示 ← UI 已修复为 Screen Space - Camera
  ↓
玩家点击继续或回车
  ↓
dialogConfigEndEvent.Invoke()
  ↓
CheckDialogEnd() → DriveProcess()
  ↓
UpdateStory() case 1
  ↓
敌人出现，任务更新
  ↓
玩家战斗
  ↓
敌人被击杀
  ↓
characterBeforeDeathEvent.Invoke()
  ↓
OnEnemyDeath() → DriveProcess()
  ↓
UpdateStory() case 2
  ↓
ExitImaginationAfterDelay(1.0f)
  ↓
Black() + SceneManager.LoadScene("BirthScene")
```

---

祝你测试顺利！🎮
