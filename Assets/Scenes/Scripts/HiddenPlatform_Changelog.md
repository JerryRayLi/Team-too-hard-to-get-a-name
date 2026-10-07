# HiddenPlatform 更新日志

记录规则：第一次保留完整代码；第二次起只记录变更代码和行号，说明、使用与验证保持简短。行号均指 HiddenPlatform.cs 在该次更新完成后的版本；删除操作标注旧版本行号，后续更新不重排行号记录。

## 第一次更新：隐藏平台与踩踏显现

说明：平台开始时隐藏但保留碰撞，从上方踩到后显现。此处补录第二次更新前的代码，原始日期未确认。

### 完整代码

```csharp
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class HiddenPlatform : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private bool revealed;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Hide the visual while keeping the collider active.
        spriteRenderer.enabled = false;
        revealed = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryReveal(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryReveal(collision);
    }

    private void TryReveal(Collision2D collision)
    {
        if (revealed ||
            collision.gameObject.GetComponent<PlayerController>() == null)
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            // On the platform side, a top contact has a downward normal.
            if (collision.GetContact(i).normal.y < -0.5f)
            {
                revealed = true;
                spriteRenderer.enabled = true;
                return;
            }
        }
    }
}
```

## 第二次更新：下一跳触发左右反转（2026-10-06，历史版本）

说明：踩中名称含 `change_direction` 的平台后，下一次起跳时反转左右操作，并持续保持。第三次更新已替换此触发时机。

使用：将特殊预制体放进关卡，踩中后按空格。

验证：编译及 17 项模拟逻辑检查通过，未实际试玩。

### 变更代码（第一版 → 第二版）

**新增：第二版第 2 行，导入输入系统。**

```csharp
using UnityEngine.InputSystem;
```

**新增：第二版第 10–11 行，保存待触发的角色。**

```csharp
    private PlayerController pendingPlayer;
    private Rigidbody2D pendingBody;
```

**新增：第二版第 32–78 行，等待起跳后反转。**

```csharp
    private void LateUpdate()
    {
        if (pendingPlayer == null || pendingBody == null)
        {
            pendingPlayer = null;
            pendingBody = null;
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (!pendingPlayer.isActiveAndEnabled || keyboard == null ||
            !keyboard.spaceKey.wasPressedThisFrame)
        {
            return;
        }

        // PlayerController.Update sets vertical velocity to jumpForce on takeoff.
        // Wait until after Update so an unsuccessful jump request stays pending.
        if (!Mathf.Approximately(pendingBody.linearVelocity.y, pendingPlayer.jumpForce))
        {
            return;
        }

        float previousMoveSpeed = pendingPlayer.moveSpeed;
        pendingPlayer.moveSpeed = -Mathf.Abs(previousMoveSpeed);

        // Correct this frame's input velocity too, preserving platform movement.
        // Match PlayerController's input priority when both directions are held.
        float direction = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            direction = -1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            direction = 1f;
        }

        Vector2 velocity = pendingBody.linearVelocity;
        velocity.x += direction * (pendingPlayer.moveSpeed - previousMoveSpeed);
        pendingBody.linearVelocity = velocity;

        // Keep moveSpeed negative after landing; another special tile won't toggle it.
        pendingPlayer = null;
        pendingBody = null;
    }
```

**修改：第一版第 31–35 行 → 第二版第 82–91 行，取得角色引用。**

```csharp
        if (revealed)
        {
            return;
        }

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player == null)
        {
            return;
        }
```

**新增：第二版第 101–105 行，特殊平台记录待触发效果。**

```csharp
                if (gameObject.name.Contains("change_direction"))
                {
                    pendingPlayer = player;
                    pendingBody = player.GetComponent<Rigidbody2D>();
                }
```

## 第三次更新：踩到平台立即反转（2026-10-06）

说明：从上方踩中特殊平台就立即反转左右操作，无需按空格；离开平台或跳跃后保持，重复触发不切回正常，重开关卡恢复。

使用：沿用名称含 `change_direction` 的预制体和现有组件。

验证：编译及 20 项模拟逻辑检查通过，未实际试玩。

### 变更代码（第二版 → 第三版）

删除第二版第 2 行的输入系统导入、第 10–11 行的等待字段，以及第 32–78 行整个 `LateUpdate()` 方法。

**修改：第二版第 103–104 行 → 第三版第 52–53 行，将等待下一跳改为踩踏时立即反转。**

```csharp
                    // Reverse controls on contact and keep them reversed after leaving.
                    player.moveSpeed = -Mathf.Abs(player.moveSpeed);
```

## 第四次更新：每块特殊砖首次踩踏切换一次方向（2026-10-06）

说明：依次踩中不同特殊砖时，左右操作在正常和反向之间切换；同一块砖只触发一次，离开或跳跃后保持当前方向。

使用：沿用名称含 `change_direction` 的预制体，每个实例独立触发；重开关卡后重置。

验证：编译及 23 项模拟逻辑检查通过，包含连续踩三块砖和返回旧砖，未实际试玩。

### 变更代码（第三版 → 第四版）

**修改：第四版第 52–53 行，将固定反向改为切换当前方向。**

```csharp
                    // Toggle controls once per tile; revealed prevents repeat triggers.
                    player.moveSpeed = -player.moveSpeed;
```
