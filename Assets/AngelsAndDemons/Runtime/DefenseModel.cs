using System;
using System.Collections.Generic;
using UnityEngine;

namespace AngelsAndDemons
{
    public enum BattleOutcome { Ongoing = 0, Victory = 1, Defeat = 2 }
    public enum CueKind { None = 0, Spawn = 1, Hit = 2, Critical = 3, Heal = 4, Skill = 5, WaveWarn = 6, CastleHit = 7, EconomyUp = 8, Victory = 9, Defeat = 10 }

    public struct BattleCue
    {
        public CueKind kind;
        public float x;
        public float y;
        public float amount;
        public int style;
        public string label;
    }

    public sealed class UnitEntity
    {
        public int instanceId;
        public bool isAlly;
        public bool isCommander;
        public bool isBoss;
        public UnitSpec spec;
        public int level;
        public float x;
        public float yOffset;
        public float rangeOffset;
        public float hp;
        public float maxHp;
        public float attack;
        public float defense;
        public float moveSpeed;
        public float attackCooldown;
        public float shieldTimer;
        public float animPhase;
        public float attackAnimTimer;
        public float hurtFlashTimer;
        public float respawnTimer;
        public bool IsAlive => hp > 0f && respawnTimer <= 0f;
    }

    public sealed class ProjectileEntity
    {
        public bool isAlly;
        public bool isHeal;
        public float x;
        public float y;
        public float targetX;
        public float targetY = 0.90f;
        public float speed;
        public float damage;
        public float splashRadius;
        public float knockback;
        public Color color;
        public int style;
    }

    public sealed class SkillWaveFx
    {
        public float x;
        public float radius;
        public float timer;
        public float duration;
        public int vfxStyle;
        public string title;
        public Color color;
    }

    public sealed class DefenseModel
    {
        public const float AllyCastleX = -11.8f;
        public const float EnemyCastleX = 11.8f;
        public const float FieldMinX = -10.8f;
        public const float FieldMaxX = 10.8f;

        public SaveProfile Profile { get; private set; }
        public StageSpec Stage { get; private set; }
        public FactionKind PlayerFaction { get; private set; }
        public FactionKind EnemyFaction => PlayerFaction == FactionKind.Angel ? FactionKind.Demon : FactionKind.Angel;

        public BattleOutcome Outcome { get; private set; }
        public float ElapsedTime { get; private set; }

        public float AllyCastleHp { get; private set; }
        public float AllyCastleMaxHp { get; private set; }
        public float EnemyCastleHp { get; private set; }
        public float EnemyCastleMaxHp { get; private set; }

        public int FoodLevel { get; private set; } // 1..8 in-game resource level
        public float Food { get; private set; }
        public float MaxFood { get; private set; }
        public float Mana { get; private set; }
        public float MaxMana { get; private set; }
        public int MaxAllyUnits { get; private set; }

        public readonly float[] SlotCooldowns = new float[6];
        public readonly float[] SkillCooldowns = new float[3];

        public UnitEntity Commander { get; private set; }
        public int CommanderSlotIndex => Profile != null ? Profile.GetCommanderSlot(PlayerFaction) : 0;

        public int[] GetAvailableAllyTroopSlots()
        {
            int cmd = CommanderSlotIndex;
            var list = new List<int>(5);
            for (int i = 0; i < 6; i++)
            {
                if (i != cmd) list.Add(i);
            }
            return list.ToArray();
        }

        public float CommanderInputDir { get; set; }
        public bool AutoAdvanceCommander { get; set; } = true;
        public bool HasCommanderTargetX { get; set; }
        public float CommanderTargetX { get; set; }
        public bool RetreatHold { get; set; }

        public readonly List<UnitEntity> Units = new List<UnitEntity>(64);
        public readonly List<ProjectileEntity> Projectiles = new List<ProjectileEntity>(64);
        public readonly List<SkillWaveFx> ActiveSkillWaves = new List<SkillWaveFx>(16);
        public readonly List<BattleCue> Cues = new List<BattleCue>(32);

        public string ActiveBannerText { get; private set; } = "";
        public float ActiveBannerTimer { get; private set; }

        int nextInstanceId = 1;
        float enemySpawnTimer;
        int waveCount;
        bool triggered75;
        bool triggered45;
        bool triggered18;
        float allyTurretCooldown;
        float enemyTurretCooldown;
        uint rngState = 0xA53C9E17u;

        public DefenseModel(SaveProfile profile, int stageNumber, FactionKind? overrideFaction = null)
        {
            Profile = profile ?? new SaveProfile();
            Stage = CampaignCatalog.GetStage(stageNumber);
            PlayerFaction = overrideFaction ?? Stage.campaignFaction;
            ResetBattle();
        }

        float NextRandom01()
        {
            rngState ^= rngState << 13;
            rngState ^= rngState >> 17;
            rngState ^= rngState << 5;
            return (rngState & 0xFFFFFF) / (float)0x1000000;
        }

        public void ResetBattle()
        {
            Outcome = BattleOutcome.Ongoing;
            ElapsedTime = 0f;
            Units.Clear();
            Projectiles.Clear();
            ActiveSkillWaves.Clear();
            Cues.Clear();

            int castleLv = Mathf.Max(1, Profile.Data.castleLevel);
            int econLv = Mathf.Max(1, Profile.Data.economyMetaLevel);

            AllyCastleMaxHp = 1800f + (castleLv - 1) * 280f;
            AllyCastleHp = AllyCastleMaxHp;
            EnemyCastleMaxHp = Stage.enemyCastleHp;
            EnemyCastleHp = EnemyCastleMaxHp;

            FoodLevel = 1;
            UpdateFoodCapacity();
            Food = 135f + (econLv - 1) * 20f;
            MaxMana = 150f + (econLv - 1) * 15f;
            Mana = 75f + (econLv - 1) * 10f;
            MaxAllyUnits = 12 + (econLv - 1) / 2;

            Array.Clear(SlotCooldowns, 0, SlotCooldowns.Length);
            Array.Clear(SkillCooldowns, 0, SkillCooldowns.Length);

            int cmdSlot = Profile.GetCommanderSlot(PlayerFaction);
            var cmdSpec = CampaignCatalog.GetUnit(PlayerFaction, cmdSlot);
            int cmdLv = Profile.GetUnitLevel(PlayerFaction, cmdSlot);
            Commander = CreateEntity(cmdSpec, cmdLv, isAlly: true, isCommander: true, isBoss: false);
            Commander.x = AllyCastleX + 2.0f;
            Units.Add(Commander);

            // Spawn initial vanguard enemy so the battlefield is immediately alive
            int initEnemyLv = Mathf.Max(1, (Stage.stageNumber - 1) % 10 + 1);
            var firstEnemy = SpawnEnemyUnit(0, initEnemyLv, isBoss: false);
            firstEnemy.x = EnemyCastleX - 2.2f;

            enemySpawnTimer = 2.2f;
            waveCount = 1;
            triggered75 = false;
            triggered45 = false;
            triggered18 = false;
            allyTurretCooldown = 1.0f;
            enemyTurretCooldown = 1.2f;
            RetreatHold = false;
            AutoAdvanceCommander = true;
            HasCommanderTargetX = false;
            CommanderTargetX = Commander.x;
            CommanderInputDir = 0f;

            ShowBanner($"★ 지휘관 [{Commander.spec.name}] 출격! (◀/▶ 또는 바닥 터치로 지휘관 이동 · 하단 카드로 병사 소환)", 4.0f);
        }

        void UpdateFoodCapacity()
        {
            int econLv = Mathf.Max(1, Profile.Data.economyMetaLevel);
            MaxFood = 240f + (FoodLevel - 1) * 140f + (econLv - 1) * 35f;
        }

        public int GetFoodUpgradeCost()
        {
            if (FoodLevel >= 8) return 0;
            return 85 + (FoodLevel - 1) * 65;
        }

        public float GetFoodGainPerSecond()
        {
            int econLv = Mathf.Max(1, Profile.Data.economyMetaLevel);
            return 24f + (FoodLevel - 1) * 11f + (econLv - 1) * 2.5f;
        }

        public bool TryUpgradeFoodLevel()
        {
            if (Outcome != BattleOutcome.Ongoing || FoodLevel >= 8) return false;
            int cost = GetFoodUpgradeCost();
            if (Food < cost) return false;
            Food -= cost;
            FoodLevel++;
            UpdateFoodCapacity();
            Cues.Add(new BattleCue { kind = CueKind.EconomyUp, x = AllyCastleX + 1.5f, y = 1.5f, amount = FoodLevel, label = $"자원 Lv.{FoodLevel}" });
            return true;
        }

        public int GetActiveAllyTroopCount()
        {
            int count = 0;
            for (int i = 0; i < Units.Count; i++)
            {
                var u = Units[i];
                if (u.isAlly && !u.isCommander && u.IsAlive) count++;
            }
            return count;
        }

        public bool TrySpawnUnit(int slotIndex)
        {
            if (Outcome != BattleOutcome.Ongoing) return false;
            slotIndex = Mathf.Clamp(slotIndex, 0, 5);
            // Selected Commander is already leading on the battlefield — no duplicate minion spawn!
            if (slotIndex == CommanderSlotIndex) return false;
            if (SlotCooldowns[slotIndex] > 0f) return false;
            if (GetActiveAllyTroopCount() >= MaxAllyUnits) return false;

            var spec = CampaignCatalog.GetUnit(PlayerFaction, slotIndex);
            if (Food < spec.foodCost) return false;

            Food -= spec.foodCost;
            SlotCooldowns[slotIndex] = spec.spawnCooldown;

            int lv = Profile.GetUnitLevel(PlayerFaction, slotIndex);
            var unit = CreateEntity(spec, lv, isAlly: true, isCommander: false, isBoss: false);
            unit.x = AllyCastleX + 0.95f + (NextRandom01() - 0.5f) * 0.25f;
            Units.Add(unit);

            ActiveSkillWaves.Add(new SkillWaveFx
            {
                x = unit.x + 0.3f,
                radius = 1.35f,
                timer = 0f,
                duration = 0.42f,
                vfxStyle = slotIndex,
                title = spec.name,
                color = spec.auraColor
            });

            Cues.Add(new BattleCue { kind = CueKind.Spawn, x = unit.x, y = 1.2f, style = slotIndex, label = $"★ {spec.name} 출격!" });
            return true;
        }

        public bool TryCastCommanderSkill(int skillIndex)
        {
            if (Outcome != BattleOutcome.Ongoing || Commander == null || !Commander.IsAlive) return false;
            skillIndex = Mathf.Clamp(skillIndex, 0, 2);
            if (SkillCooldowns[skillIndex] > 0f) return false;

            var skills = Commander.spec.commanderSkills;
            if (skills == null || skillIndex >= skills.Length) return false;
            var skill = skills[skillIndex];
            if (Mana < skill.manaCost) return false;

            Mana -= skill.manaCost;
            int skillLv = Profile.GetSkillLevel(skillIndex);
            float cdReduction = Mathf.Clamp((skillLv - 1) * 0.025f, 0f, 0.28f);
            SkillCooldowns[skillIndex] = skill.cooldown * (1f - cdReduction);
            Commander.attackAnimTimer = 0.42f;

            float skillScale = 1f + (skillLv - 1) * 0.14f;
            float basePower = Commander.attack * skill.powerMultiplier * skillScale;
            float dir = 1f;
            float centerX = Commander.x + dir * (skill.range * 0.45f);

            // Apply heal & shield to allies if applicable
            if (skill.healRatio > 0f || skill.shieldSeconds > 0f)
            {
                for (int i = 0; i < Units.Count; i++)
                {
                    var u = Units[i];
                    if (!u.isAlly || !u.IsAlive) continue;
                    if (Mathf.Abs(u.x - Commander.x) <= skill.range + 3.0f)
                    {
                        if (skill.healRatio > 0f)
                        {
                            float heal = u.maxHp * (skill.healRatio + (skillLv - 1) * 0.015f);
                            u.hp = Mathf.Min(u.maxHp, u.hp + heal);
                            Cues.Add(new BattleCue { kind = CueKind.Heal, x = u.x, y = 1.5f, amount = heal, label = $"+{Mathf.RoundToInt(heal)}" });
                        }
                        if (skill.shieldSeconds > 0f)
                        {
                            u.shieldTimer = Mathf.Max(u.shieldTimer, skill.shieldSeconds);
                        }
                    }
                }
            }

            // Apply AoE damage & knockback to enemies in range
            for (int i = 0; i < Units.Count; i++)
            {
                var e = Units[i];
                if (e.isAlly || !e.IsAlive) continue;
                float dist = e.x - Commander.x;
                if (dist >= -1.8f && dist <= skill.range + skill.radius * 0.5f)
                {
                    ApplyDamageToUnit(e, basePower, isCritical: true, knockbackDir: 1f, knockbackForce: skill.knockback);
                }
            }

            // Also damage enemy castle if within skill reach
            if (EnemyCastleX - Commander.x <= skill.range + 1.5f)
            {
                DamageCastle(isAllyCastle: false, basePower * 0.55f);
            }

            ActiveSkillWaves.Add(new SkillWaveFx
            {
                x = Mathf.Clamp(centerX, FieldMinX, FieldMaxX),
                radius = skill.radius,
                timer = 0f,
                duration = 0.75f,
                vfxStyle = skill.vfxStyle,
                title = skill.name,
                color = Commander.spec.auraColor
            });

            Cues.Add(new BattleCue { kind = CueKind.Skill, x = Commander.x, y = 2.0f, style = skill.vfxStyle, label = skill.name });
            ShowBanner($"★ {Commander.spec.name}: {skill.name}!", 1.5f);
            return true;
        }

        UnitEntity CreateEntity(UnitSpec spec, int level, bool isAlly, bool isCommander, bool isBoss)
        {
            int id = nextInstanceId++;
            float lvMult = 1f + (level - 1) * 0.13f;
            float cmdHpMult = isCommander ? 3.25f : isBoss ? 2.65f : 1f;
            float cmdAtkMult = isCommander ? 1.75f : isBoss ? 1.50f : 1f;
            float maxHp = spec.maxHp * lvMult * cmdHpMult;
            float atk = spec.attack * lvMult * cmdAtkMult;

            float laneY;
            if (spec.isFlying)
                laneY = 0.48f + ((id % 3) - 1) * 0.08f;
            else if (isCommander)
                laneY = -0.05f;
            else
                laneY = ((id % 5) - 2) * 0.10f;

            float rangeJitter = isCommander ? 0f : ((id % 4) - 1.5f) * 0.18f;

            return new UnitEntity
            {
                instanceId = id,
                isAlly = isAlly,
                isCommander = isCommander,
                isBoss = isBoss,
                spec = spec,
                level = level,
                yOffset = laneY,
                rangeOffset = rangeJitter,
                hp = maxHp,
                maxHp = maxHp,
                attack = atk,
                defense = spec.defense + (level - 1) * 1.5f,
                moveSpeed = spec.moveSpeed,
                attackCooldown = 0.18f,
                animPhase = NextRandom01() * 6.28f
            };
        }

        public void Step(float dt)
        {
            if (Outcome != BattleOutcome.Ongoing || dt <= 0f) return;

            ElapsedTime += dt;
            if (ActiveBannerTimer > 0f) ActiveBannerTimer = Mathf.Max(0f, ActiveBannerTimer - dt);

            // 1. Regenerate Food & Mana
            Food = Mathf.Min(MaxFood, Food + GetFoodGainPerSecond() * dt);
            float manaRegen = 8.0f + (Profile.Data.economyMetaLevel - 1) * 0.65f;
            Mana = Mathf.Min(MaxMana, Mana + manaRegen * dt);

            for (int i = 0; i < SlotCooldowns.Length; i++)
                if (SlotCooldowns[i] > 0f) SlotCooldowns[i] = Mathf.Max(0f, SlotCooldowns[i] - dt);
            for (int i = 0; i < SkillCooldowns.Length; i++)
                if (SkillCooldowns[i] > 0f) SkillCooldowns[i] = Mathf.Max(0f, SkillCooldowns[i] - dt);

            for (int i = ActiveSkillWaves.Count - 1; i >= 0; i--)
            {
                ActiveSkillWaves[i].timer += dt;
                if (ActiveSkillWaves[i].timer >= ActiveSkillWaves[i].duration)
                    ActiveSkillWaves.RemoveAt(i);
            }

            // 2. Enemy Wave Director
            StepEnemyDirector(dt);

            // 3. Castle Defense Turrets
            StepCastleTurrets(dt);

            // 4. Update Commander & Units
            StepUnits(dt);

            // 5. Update Projectiles
            StepProjectiles(dt);

            // 6. Clean up dead non-commander units
            for (int i = Units.Count - 1; i >= 0; i--)
            {
                var u = Units[i];
                if (!u.isCommander && u.hp <= 0f)
                {
                    Units.RemoveAt(i);
                }
            }
        }

        void StepEnemyDirector(float dt)
        {
            enemySpawnTimer -= dt;
            if (enemySpawnTimer <= 0f)
            {
                waveCount++;
                enemySpawnTimer = Stage.enemySpawnInterval * Mathf.Clamp(1.05f - ElapsedTime * 0.002f, 0.62f, 1.05f);

                int maxTier = Mathf.Clamp(Stage.maxEnemyTier, 0, 5);
                int tier = Mathf.Clamp((int)(NextRandom01() * (maxTier + 1)), 0, maxTier);
                if (waveCount % 5 == 0) tier = maxTier;

                int enemyLv = Mathf.Max(1, (Stage.stageNumber - 1) % 10 + 1);
                SpawnEnemyUnit(tier, enemyLv, isBoss: false);

                if (waveCount % 4 == 0)
                {
                    SpawnEnemyUnit(0, enemyLv, isBoss: false);
                }
            }

            float hpRatio = EnemyCastleHp / Mathf.Max(1f, EnemyCastleMaxHp);
            int baseLv = Mathf.Max(1, (Stage.stageNumber - 1) % 10 + 1);
            if (!triggered75 && hpRatio <= 0.75f)
            {
                triggered75 = true;
                SpawnEnemyUnit(Mathf.Min(5, Stage.maxEnemyTier), baseLv, isBoss: false);
                SpawnEnemyUnit(0, baseLv, isBoss: false);
                ShowBanner("⚠ 적 성채 수비대 증원 출격!", 2.0f);
                Cues.Add(new BattleCue { kind = CueKind.WaveWarn, x = EnemyCastleX - 1.5f, y = 2.0f, label = "수비대 증원!" });
            }
            if (!triggered45 && hpRatio <= 0.45f)
            {
                triggered45 = true;
                SpawnEnemyUnit(Stage.bossSlotIndex, baseLv + 1, isBoss: true);
                SpawnEnemyUnit(1, baseLv, isBoss: false);
                ShowBanner("⚠ 적군 군단장(보스) 직접 출격!", 2.5f);
                Cues.Add(new BattleCue { kind = CueKind.WaveWarn, x = EnemyCastleX - 1.5f, y = 2.2f, label = "보스 출격!" });
            }
            if (!triggered18 && hpRatio <= 0.18f)
            {
                triggered18 = true;
                SpawnEnemyUnit(Stage.bossSlotIndex, baseLv + 1, isBoss: true);
                SpawnEnemyUnit(Mathf.Min(5, Stage.maxEnemyTier), baseLv, isBoss: false);
                ShowBanner("⚠ 적 본진 최후의 총공세!", 2.5f);
                Cues.Add(new BattleCue { kind = CueKind.WaveWarn, x = EnemyCastleX - 1.5f, y = 2.2f, label = "최후의 총공세!" });
            }
        }

        public UnitEntity SpawnEnemyUnit(int slotIndex, int level, bool isBoss)
        {
            var spec = CampaignCatalog.GetUnit(EnemyFaction, slotIndex);
            var unit = CreateEntity(spec, level, isAlly: false, isCommander: false, isBoss: isBoss);
            unit.x = EnemyCastleX - 0.95f - (NextRandom01() * 0.25f);
            Units.Add(unit);
            return unit;
        }

        void StepCastleTurrets(float dt)
        {
            int castleLv = Mathf.Max(1, Profile.Data.castleLevel);
            allyTurretCooldown -= dt;
            if (allyTurretCooldown <= 0f)
            {
                var target = FindNearestTarget(isAlly: true, fromX: AllyCastleX, maxRange: 6.2f);
                if (target != null)
                {
                    allyTurretCooldown = 1.35f;
                    Projectiles.Add(new ProjectileEntity
                    {
                        isAlly = true,
                        isHeal = false,
                        x = AllyCastleX + 0.6f,
                        y = 3.2f,
                        targetX = target.x,
                        targetY = 0.85f + target.yOffset * 0.5f,
                        speed = 13.5f,
                        damage = 48f + (castleLv - 1) * 14f,
                        splashRadius = 0.8f,
                        knockback = 0.4f,
                        color = new Color(1f, 0.92f, 0.45f),
                        style = 1
                    });
                }
            }

            enemyTurretCooldown -= dt;
            if (enemyTurretCooldown <= 0f)
            {
                var target = FindNearestTarget(isAlly: false, fromX: EnemyCastleX, maxRange: 6.0f);
                if (target != null)
                {
                    enemyTurretCooldown = 1.5f;
                    Projectiles.Add(new ProjectileEntity
                    {
                        isAlly = false,
                        isHeal = false,
                        x = EnemyCastleX - 0.6f,
                        y = 3.2f,
                        targetX = target.x,
                        targetY = 0.85f + target.yOffset * 0.5f,
                        speed = 12.5f,
                        damage = 42f + Stage.stageNumber * 6f,
                        splashRadius = 0.8f,
                        knockback = 0.3f,
                        color = new Color(1f, 0.3f, 0.4f),
                        style = 2
                    });
                }
            }
        }

        void StepUnits(float dt)
        {
            // Find foremost ally X for healer formation tracking
            float foremostAllyX = AllyCastleX + 2.5f;
            for (int i = 0; i < Units.Count; i++)
            {
                var a = Units[i];
                if (a.isAlly && a.IsAlive && !a.spec.isHealer && a.x > foremostAllyX)
                    foremostAllyX = a.x;
            }

            for (int i = 0; i < Units.Count; i++)
            {
                var u = Units[i];
                if (u.isCommander && u.respawnTimer > 0f)
                {
                    u.respawnTimer -= dt;
                    if (u.respawnTimer <= 0f)
                    {
                        u.respawnTimer = 0f;
                        u.hp = u.maxHp;
                        u.x = AllyCastleX + 1.8f;
                        u.shieldTimer = 3.0f;
                        ShowBanner($"★ 지휘관 {u.spec.name} 전장 재합류!", 1.8f);
                    }
                    continue;
                }
                if (!u.IsAlive) continue;

                u.animPhase += dt * (u.moveSpeed * 2.5f);
                if (u.attackAnimTimer > 0f) u.attackAnimTimer = Mathf.Max(0f, u.attackAnimTimer - dt);
                if (u.hurtFlashTimer > 0f) u.hurtFlashTimer = Mathf.Max(0f, u.hurtFlashTimer - dt);
                if (u.shieldTimer > 0f) u.shieldTimer = Mathf.Max(0f, u.shieldTimer - dt);
                if (u.attackCooldown > 0f) u.attackCooldown = Mathf.Max(0f, u.attackCooldown - dt);

                // Sanctuary regeneration when near Ally Castle + passive Commander chakra recovery
                if (u.isAlly && u.hp < u.maxHp)
                {
                    if (u.x <= AllyCastleX + 3.2f)
                    {
                        float sanctuaryRegen = (u.isCommander ? 42f : 20f) * dt;
                        u.hp = Mathf.Min(u.maxHp, u.hp + sanctuaryRegen);
                    }
                    else if (u.isCommander)
                    {
                        u.hp = Mathf.Min(u.maxHp, u.hp + 8f * dt);
                    }
                }

                float dir = u.isAlly ? 1f : -1f;
                float effectiveRange = Mathf.Max(1.0f, u.spec.attackRange + u.rangeOffset);

                // Mandatory Gate March-Out: newly spawned units must step out of the castle doorway first!
                bool mustMarchOutOfGate = u.isAlly
                    ? (u.x < AllyCastleX + 2.0f)
                    : (u.x > EnemyCastleX - 2.0f);

                // Manual Commander input ALWAYS moves the Commander immediately
                bool manualMovingCommander = false;
                if (u.isCommander)
                {
                    if (Mathf.Abs(CommanderInputDir) > 0.05f)
                    {
                        u.x = Mathf.Clamp(u.x + CommanderInputDir * (u.moveSpeed * 1.45f) * dt, FieldMinX, FieldMaxX);
                        HasCommanderTargetX = false;
                        // If player manually retreats left, pause auto-advance so she doesn't immediately walk back into danger;
                        // if player manually advances right, resume auto-advance!
                        if (CommanderInputDir < -0.05f) AutoAdvanceCommander = false;
                        else if (CommanderInputDir > 0.05f) AutoAdvanceCommander = true;
                        manualMovingCommander = true;
                    }
                    else if (HasCommanderTargetX)
                    {
                        float diff = CommanderTargetX - u.x;
                        if (Mathf.Abs(diff) > 0.18f)
                        {
                            float step = Mathf.Sign(diff) * (u.moveSpeed * 1.40f) * dt;
                            if (Mathf.Abs(step) >= Mathf.Abs(diff))
                            {
                                u.x = Mathf.Clamp(CommanderTargetX, FieldMinX, FieldMaxX);
                                HasCommanderTargetX = false;
                            }
                            else
                            {
                                u.x = Mathf.Clamp(u.x + step, FieldMinX, FieldMaxX);
                            }
                            manualMovingCommander = true;
                        }
                        else
                        {
                            HasCommanderTargetX = false;
                        }
                    }
                }

                // Healer support check
                bool healedThisStep = false;
                if (u.spec.isHealer && !mustMarchOutOfGate)
                {
                    var injured = FindInjuredAlly(u);
                    if (injured != null && u.attackCooldown <= 0f)
                    {
                        u.attackCooldown = u.spec.attackInterval;
                        u.attackAnimTimer = 0.48f;
                        float healAmount = u.attack * 1.15f;
                        injured.hp = Mathf.Min(injured.maxHp, injured.hp + healAmount);
                        ActiveSkillWaves.Add(new SkillWaveFx
                        {
                            x = injured.x,
                            radius = 1.25f,
                            timer = 0f,
                            duration = 0.42f,
                            vfxStyle = 2,
                            title = "치유",
                            color = new Color(0.38f, 1.0f, 0.65f)
                        });
                        Cues.Add(new BattleCue { kind = CueKind.Heal, x = injured.x, y = 1.4f, amount = healAmount, label = $"+{Mathf.RoundToInt(healAmount)}" });
                        healedThisStep = true;
                    }
                }

                var enemyTarget = mustMarchOutOfGate ? null : FindNearestTarget(u.isAlly, u.x, effectiveRange);
                float distToEnemyCastle = u.isAlly ? (EnemyCastleX - u.x) : (u.x - AllyCastleX);
                bool canHitCastle = !mustMarchOutOfGate && (distToEnemyCastle <= effectiveRange + 0.5f);

                if (enemyTarget != null || canHitCastle)
                {
                    if (!healedThisStep && u.attackCooldown <= 0f)
                    {
                        u.attackCooldown = u.spec.attackInterval;
                        u.attackAnimTimer = 0.48f;
                        float targetX = enemyTarget != null ? enemyTarget.x : (u.isAlly ? EnemyCastleX : AllyCastleX);
                        ExecuteUnitAttack(u, enemyTarget, targetX);
                    }
                }
                else if (!manualMovingCommander)
                {
                    // Movement when no enemy is blocking inside effectiveRange
                    if (u.isAlly && RetreatHold)
                    {
                        float rallyX = AllyCastleX + 2.1f + (u.isCommander ? 0.3f : 0f);
                        if (u.x > rallyX) u.x = Mathf.Max(rallyX, u.x - u.moveSpeed * 1.15f * dt);
                    }
                    else if (u.isCommander)
                    {
                        if (AutoAdvanceCommander)
                        {
                            u.x = Mathf.Clamp(u.x + dir * u.moveSpeed * dt, FieldMinX, FieldMaxX);
                        }
                    }
                    else if (u.spec.isHealer && u.isAlly)
                    {
                        // Healer marches forward up to 1.1 units behind the foremost front-line ally (or advances freely if she is the only one)
                        float targetFollowX = Mathf.Max(u.x + 1.0f, foremostAllyX - 1.1f);
                        if (u.x < targetFollowX || mustMarchOutOfGate)
                        {
                            u.x = Mathf.Clamp(u.x + dir * u.moveSpeed * dt, FieldMinX, FieldMaxX);
                        }
                    }
                    else
                    {
                        u.x = Mathf.Clamp(u.x + dir * u.moveSpeed * dt, FieldMinX, FieldMaxX);
                    }
                }
            }
        }

        void ExecuteUnitAttack(UnitEntity attacker, UnitEntity primaryTarget, float targetX)
        {
            float dir = attacker.isAlly ? 1f : -1f;
            if (attacker.spec.isRanged)
            {
                Projectiles.Add(new ProjectileEntity
                {
                    isAlly = attacker.isAlly,
                    isHeal = false,
                    x = attacker.x + dir * 0.45f,
                    y = 0.95f + attacker.yOffset * 0.5f,
                    targetX = targetX,
                    targetY = primaryTarget != null ? (0.88f + primaryTarget.yOffset * 0.5f) : 1.35f,
                    speed = 11.5f,
                    damage = attacker.attack,
                    splashRadius = attacker.spec.splashRadius,
                    knockback = attacker.spec.knockback,
                    color = attacker.spec.auraColor,
                    style = attacker.spec.slotIndex
                });
            }
            else
            {
                bool hitAny = false;
                float splash = Mathf.Max(0.75f, attacker.spec.splashRadius);
                for (int i = 0; i < Units.Count; i++)
                {
                    var other = Units[i];
                    if (other.isAlly == attacker.isAlly || !other.IsAlive) continue;
                    if (Mathf.Abs(other.x - targetX) <= splash)
                    {
                        ApplyDamageToUnit(other, attacker.attack, isCritical: false, knockbackDir: dir, knockbackForce: attacker.spec.knockback);
                        hitAny = true;
                    }
                }

                float castleX = attacker.isAlly ? EnemyCastleX : AllyCastleX;
                if (!hitAny && Mathf.Abs(castleX - attacker.x) <= attacker.spec.attackRange + 0.85f)
                {
                    DamageCastle(isAllyCastle: !attacker.isAlly, attacker.attack);
                }
            }
        }

        void StepProjectiles(float dt)
        {
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                var p = Projectiles[i];
                float dx = p.targetX - p.x;
                float dy = p.targetY - p.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float step = p.speed * dt;
                bool reached = dist <= step || Mathf.Abs(dx) <= 0.08f;

                if (!reached && dist > 0.001f)
                {
                    p.x += (dx / dist) * step;
                    p.y += (dy / dist) * step;
                }
                else
                {
                    p.x = p.targetX;
                    p.y = p.targetY;
                    float splash = Mathf.Max(0.70f, p.splashRadius);
                    bool hitUnit = false;
                    for (int uIdx = 0; uIdx < Units.Count; uIdx++)
                    {
                        var u = Units[uIdx];
                        if (u.isAlly == p.isAlly || !u.IsAlive) continue;
                        if (Mathf.Abs(u.x - p.targetX) <= splash)
                        {
                            ApplyDamageToUnit(u, p.damage, isCritical: false, knockbackDir: p.isAlly ? 1f : -1f, knockbackForce: p.knockback);
                            hitUnit = true;
                        }
                    }

                    float castleX = p.isAlly ? EnemyCastleX : AllyCastleX;
                    if (!hitUnit && Mathf.Abs(p.targetX - castleX) <= 1.8f)
                    {
                        DamageCastle(isAllyCastle: !p.isAlly, p.damage);
                    }

                    ActiveSkillWaves.Add(new SkillWaveFx
                    {
                        x = p.targetX,
                        radius = Mathf.Clamp(splash * 0.85f, 0.75f, 1.8f),
                        timer = 0f,
                        duration = 0.25f,
                        vfxStyle = p.style,
                        title = "",
                        color = p.color
                    });

                    Projectiles.RemoveAt(i);
                }
            }
        }

        void ApplyDamageToUnit(UnitEntity target, float rawDamage, bool isCritical, float knockbackDir, float knockbackForce)
        {
            if (!target.IsAlive) return;
            float mitigation = 100f / (100f + Mathf.Max(0f, target.defense));
            float dmg = Mathf.Max(4f, rawDamage * mitigation);
            if (target.shieldTimer > 0f) dmg *= 0.38f;

            target.hp -= dmg;
            target.hurtFlashTimer = 0.16f;

            if (!target.spec.superArmor && knockbackForce > 0f)
            {
                target.x = Mathf.Clamp(target.x + knockbackDir * knockbackForce * 0.32f, FieldMinX, FieldMaxX);
            }

            Cues.Add(new BattleCue
            {
                kind = isCritical ? CueKind.Critical : CueKind.Hit,
                x = target.x,
                y = 1.25f,
                amount = dmg,
                label = Mathf.RoundToInt(dmg).ToString()
            });

            if (target.hp <= 0f)
            {
                target.hp = 0f;
                if (target.isCommander)
                {
                    target.respawnTimer = 8.0f;
                    ShowBanner($"⚠ 지휘관 {target.spec.name} 부상 후퇴! (8초 후 재출격)", 2.0f);
                }
                else if (!target.isAlly)
                {
                    Mana = Mathf.Min(MaxMana, Mana + (target.isBoss ? 35f : 8f));
                    Food = Mathf.Min(MaxFood, Food + (target.isBoss ? 50f : 12f));
                }
            }
        }

        void DamageCastle(bool isAllyCastle, float rawDamage)
        {
            if (Outcome != BattleOutcome.Ongoing) return;
            float dmg = Mathf.Max(5f, rawDamage * 0.85f);
            if (isAllyCastle)
            {
                AllyCastleHp = Mathf.Max(0f, AllyCastleHp - dmg);
                Cues.Add(new BattleCue { kind = CueKind.CastleHit, x = AllyCastleX + 0.6f, y = 2.2f, amount = dmg, label = $"-{Mathf.RoundToInt(dmg)}" });
                if (AllyCastleHp <= 0f)
                {
                    Outcome = BattleOutcome.Defeat;
                    Cues.Add(new BattleCue { kind = CueKind.Defeat, x = AllyCastleX, y = 2.0f, label = "성소 함락..." });
                }
            }
            else
            {
                EnemyCastleHp = Mathf.Max(0f, EnemyCastleHp - dmg);
                Cues.Add(new BattleCue { kind = CueKind.CastleHit, x = EnemyCastleX - 0.6f, y = 2.2f, amount = dmg, label = $"-{Mathf.RoundToInt(dmg)}" });
                if (EnemyCastleHp <= 0f)
                {
                    Outcome = BattleOutcome.Victory;
                    int stars = AllyCastleHp >= AllyCastleMaxHp * 0.8f ? 3 : AllyCastleHp >= AllyCastleMaxHp * 0.4f ? 2 : 1;
                    Profile.RecordStageVictory(Stage.stageNumber, stars, Stage.rewardGold, Stage.rewardGems);
                    Profile.Save();
                    Cues.Add(new BattleCue { kind = CueKind.Victory, x = EnemyCastleX, y = 2.0f, amount = stars, label = "적 성채 파괴 승리!" });
                }
            }
        }

        UnitEntity FindNearestTarget(bool isAlly, float fromX, float maxRange)
        {
            UnitEntity best = null;
            float bestDist = maxRange;
            for (int i = 0; i < Units.Count; i++)
            {
                var u = Units[i];
                if (u.isAlly == isAlly || !u.IsAlive) continue;
                float dist = isAlly ? (u.x - fromX) : (fromX - u.x);
                if (dist >= -0.65f && dist <= bestDist)
                {
                    bestDist = dist;
                    best = u;
                }
            }
            return best;
        }

        UnitEntity FindInjuredAlly(UnitEntity healer)
        {
            UnitEntity best = null;
            float lowestRatio = 0.90f;
            for (int i = 0; i < Units.Count; i++)
            {
                var u = Units[i];
                if (u.isAlly != healer.isAlly || !u.IsAlive) continue;
                float dist = Mathf.Abs(u.x - healer.x);
                if (dist <= healer.spec.attackRange + 1.4f)
                {
                    float ratio = u.hp / Mathf.Max(1f, u.maxHp);
                    if (ratio < lowestRatio)
                    {
                        lowestRatio = ratio;
                        best = u;
                    }
                }
            }
            return best;
        }

        public void ShowBanner(string text, float duration)
        {
            ActiveBannerText = text;
            ActiveBannerTimer = duration;
        }

        public void GrantResources(float food, float mana)
        {
            MaxFood = Mathf.Max(MaxFood, food);
            Food = food;
            MaxMana = Mathf.Max(MaxMana, mana);
            Mana = mana;
        }
    }
}
