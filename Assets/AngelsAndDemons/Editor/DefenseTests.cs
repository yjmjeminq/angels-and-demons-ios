#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace AngelsAndDemons.EditorTools
{
    public static class DefenseTests
    {
        [Serializable]
        public sealed class VerificationReport
        {
            public bool passed;
            public int totalChecks;
            public int simulatedBattles;
            public string[] verifiedAngelHeroines;
            public string[] verifiedDemonUnits;
            public string previewImagePath;
            public string summary;
        }

        public static void Run()
        {
            DefenseBuilder.ImportAssets();
            int checks = 0;
            int battles = 0;

            void AssertTrue(bool cond, string msg)
            {
                checks++;
                if (!cond) throw new InvalidOperationException("Check failed: " + msg);
            }

            // 1. Verify 6 Naruto 8-Head Tall Bikini Heroines & 6 Demon Units + Sprite Assets
            AssertTrue(CampaignCatalog.AngelUnits.Length == 6, "AngelUnits must have 6 characters");
            AssertTrue(CampaignCatalog.DemonUnits.Length == 6, "DemonUnits must have 6 characters");
            AssertTrue(CampaignCatalog.Stages.Length == 20, "Stages must have 20 stages (10 Angel + 10 Demon)");

            string[] expectedAngels = { "hinata", "ino", "sakura", "temari", "tsunade", "konan" };
            for (int i = 0; i < 6; i++)
            {
                var u = CampaignCatalog.AngelUnits[i];
                AssertTrue(u.id == expectedAngels[i], $"Angel slot {i} must be {expectedAngels[i]}");
                AssertTrue(u.commanderSkills != null && u.commanderSkills.Length == 3, $"{u.name} must have 3 commander skills");
                var tex = Resources.Load<Texture2D>("AngelsAndDemons/Characters/" + u.id);
                AssertTrue(tex != null && tex.width == 768 && tex.height == 1024, $"Missing or unnormalized 8-head sprite for {u.id}");
                var atlas = Resources.Load<Texture2D>("AngelsAndDemons/Animations/" + u.id + "_atlas");
                AssertTrue(atlas != null && atlas.width == 1152 && atlas.height == 1024, $"Missing 6-frame animation atlas for {u.id}");
            }

            string[] expectedDemons = { "imp", "succubus", "darkpriest", "hellknight", "lucifer", "balrog" };
            for (int i = 0; i < 6; i++)
            {
                var d = CampaignCatalog.DemonUnits[i];
                AssertTrue(d.id == expectedDemons[i], $"Demon slot {i} must be {expectedDemons[i]}");
                var tex = Resources.Load<Texture2D>("AngelsAndDemons/Characters/" + d.id);
                AssertTrue(tex != null && tex.width == 768 && tex.height == 1024, $"Missing or unnormalized 8-head sprite for {d.id}");
                var atlas = Resources.Load<Texture2D>("AngelsAndDemons/Animations/" + d.id + "_atlas");
                AssertTrue(atlas != null && atlas.width == 1152 && atlas.height == 1024, $"Missing 6-frame animation atlas for {d.id}");
            }

            // 2. Verify Original Morisoft Dual Campaign Unlock Rule (Angel Stage 10 -> Unlocks Demon Stage 11..20)
            var profile = new SaveProfile();
            AssertTrue(!profile.IsDemonCampaignUnlocked, "Demon campaign should be locked initially per original rule");
            AssertTrue(profile.IsStageUnlocked(1), "Angel Stage 1 unlocked initially");
            AssertTrue(!profile.IsStageUnlocked(11), "Demon Stage 11 locked initially");

            for (int st = 1; st <= 10; st++)
            {
                profile.RecordStageVictory(st, 3, 500, 3);
            }
            AssertTrue(profile.IsDemonCampaignUnlocked, "Clearing Angel Stage 10 must unlock Demon Campaign");
            AssertTrue(profile.IsStageUnlocked(11), "Demon Stage 11 must be unlocked after Angel Stage 10");

            // 3. Verify Upgrades (Unit, Skill, Castle, Economy)
            profile.Data.gold = 50000;
            for (int i = 0; i < 6; i++)
            {
                AssertTrue(profile.TryUpgradeUnit(FactionKind.Angel, i), $"Upgrade angel unit {i}");
                AssertTrue(profile.TryUpgradeUnit(FactionKind.Demon, i), $"Upgrade demon unit {i}");
            }
            for (int s = 0; s < 3; s++)
            {
                AssertTrue(profile.TryUpgradeSkill(s), $"Upgrade commander skill {s}");
            }
            AssertTrue(profile.TryUpgradeCastle(), "Upgrade castle");
            AssertTrue(profile.TryUpgradeEconomy(), "Upgrade economy");

            // 4. Verify Commander Auto-Advance, Manual Movement, Gate March-Out & All 6 Heroines
            for (int cmdIdx = 0; cmdIdx < 6; cmdIdx++)
            {
                profile.SetCommanderSlot(FactionKind.Angel, cmdIdx);
                var model = new DefenseModel(profile, 1, FactionKind.Angel);
                model.GrantResources(2000f, 2000f);
                AssertTrue(model.Commander.spec.id == expectedAngels[cmdIdx], "Commander matches selected heroine");

                float startCmdX = model.Commander.x;
                model.Step(0.5f);
                AssertTrue(model.Commander.x > startCmdX + 0.3f, $"Commander {model.Commander.spec.name} must auto-advance forward when battle starts");

                float beforeRetreatX = model.Commander.x;
                model.CommanderInputDir = -1f;
                model.Step(0.4f);
                AssertTrue(model.Commander.x < beforeRetreatX - 0.2f, $"Commander {model.Commander.spec.name} must move left when CommanderInputDir is -1");
                model.CommanderInputDir = 0f;

                for (int up = 0; up < 4; up++)
                {
                    AssertTrue(model.TryUpgradeFoodLevel(), "In-battle Food Level upgrade");
                }

                // Commander is on field and cannot be spawned as minion; companion is spawned instead
                AssertTrue(!model.TrySpawnUnit(cmdIdx), $"Commander slot {cmdIdx} cannot be spawned as duplicate minion");
                int companionSlot = model.GetAvailableAllyTroopSlots()[0];
                AssertTrue(model.TrySpawnUnit(companionSlot), $"Spawn companion unit slot {companionSlot}");

                var spawnedUnit = model.Units[model.Units.Count - 1];
                float spawnStartX = spawnedUnit.x;
                model.Step(0.6f);
                AssertTrue(spawnedUnit.x > spawnStartX + 0.5f, $"Summoned companion {spawnedUnit.spec.name} must march out of the castle gate");

                for (int s = 0; s < 3; s++)
                {
                    AssertTrue(model.TryCastCommanderSkill(s), $"Cast commander skill {s} for {model.Commander.spec.name}");
                }

                // Verify CharacterMotionMesh walking stride & attack strike deformation
                var testGo = new GameObject("MotionCheck_" + cmdIdx);
                var motionMesh = testGo.AddComponent<CharacterMotionMesh>();
                motionMesh.Initialize(10);
                model.Commander.x += 0.45f;
                model.Commander.animPhase += 1.2f;
                model.Commander.attackAnimTimer = 0.22f; // Peak strike phase
                motionMesh.SamplePose(model.Commander, 0.033f, 10);
                AssertTrue(motionMesh.VertexCount == (CharacterMotionMesh.GridCols + 1) * (CharacterMotionMesh.GridRows + 1), "Motion mesh vertex count");
                AssertTrue(motionMesh.LastStrikeDisposition > 0.1f, $"Attack strike motion active for {model.Commander.spec.name}");
                UnityEngine.Object.DestroyImmediate(testGo);

                battles++;
            }

            // 5. Verify live DefenseGame world hierarchy, environment textures, castles, and CharacterMotionMesh materials
            Directory.CreateDirectory("Verification");
            {
                var previewRoot = new GameObject("PreviewGameHost");
                var game = previewRoot.AddComponent<DefenseGame>();
                if (game.Profile == null)
                {
                    previewRoot.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                }
                game.Profile.SetCommanderSlot(FactionKind.Angel, 0);
                game.StartStage(1);
                var m = game.Model;
                m.GrantResources(5000f, 500f);

                float[] heroPositions = { -2.2f, -5.8f, -4.3f, -7.2f, -1.2f, -8.5f };
                int cmdSlot = m.CommanderSlotIndex;
                for (int i = 0; i < 6; i++)
                {
                    UnitEntity u;
                    if (i == cmdSlot)
                    {
                        u = m.Commander;
                    }
                    else
                    {
                        m.SlotCooldowns[i] = 0f;
                        AssertTrue(m.TrySpawnUnit(i), $"Preview spawn heroine companion {i}");
                        u = m.Units[m.Units.Count - 1];
                    }
                    u.x = heroPositions[i];
                    u.animPhase = i * 0.9f + 0.8f;
                    if (i == 0 || i == 4) u.attackAnimTimer = 0.22f; // AttackStrike frame (5)
                    else if (i == 2) u.attackAnimTimer = 0.38f; // AttackWindup frame (4)
                }
                var e1 = m.SpawnEnemyUnit(0, 2, false); e1.x = 0.2f;  e1.attackAnimTimer = 0.15f;
                var e2 = m.SpawnEnemyUnit(1, 2, false); e2.x = 2.1f;  e2.animPhase = 1.4f;
                var e3 = m.SpawnEnemyUnit(4, 3, true);  e3.x = 1.1f;  e3.attackAnimTimer = 0.16f;

                // Step slightly so walking units register positive horizontal velocity and enter WalkA/WalkPass/WalkB frames!
                for (int i = 0; i < m.Units.Count; i++)
                {
                    var u = m.Units[i];
                    if (u.attackAnimTimer <= 0f)
                        u.x += (u.isAlly ? 0.08f : -0.08f);
                }
                game.SyncVisuals(0.033f);
                for (int i = 0; i < m.Units.Count; i++)
                {
                    var u = m.Units[i];
                    if (u.attackAnimTimer <= 0f)
                        u.x += (u.isAlly ? 0.08f : -0.08f);
                }
                game.SyncVisuals(0.033f);

                AssertTrue(DefenseArt.SkyBackdrop != null && DefenseArt.SkyBackdrop.texture.width >= 2048, "StageBackdrop texture valid");
                AssertTrue(DefenseArt.AngelCastle != null && DefenseArt.AngelCastle.texture.width == 560, "AngelCastle sprite valid");
                AssertTrue(DefenseArt.DemonCastle != null && DefenseArt.DemonCastle.texture.width == 560, "DemonCastle sprite valid");

                var meshes = previewRoot.GetComponentsInChildren<CharacterMotionMesh>(false);
                AssertTrue(meshes.Length >= 10, $"Active CharacterMotionMesh count must be >= 10 (found {meshes.Length})");
                for (int i = 0; i < meshes.Length; i++)
                {
                    var mr = meshes[i].GetComponent<MeshRenderer>();
                    var mf = meshes[i].GetComponent<MeshFilter>();
                    AssertTrue(mr != null && mr.sharedMaterial != null, $"MeshRenderer material present on unit {i}");
                    AssertTrue(mr.sharedMaterial.mainTexture != null && mr.sharedMaterial.mainTexture.width == 1152, $"MeshRenderer 6-frame atlas bound on unit {i}");
                    AssertTrue(mf != null && mf.sharedMesh != null && mf.sharedMesh.vertexCount == (CharacterMotionMesh.GridCols + 1) * (CharacterMotionMesh.GridRows + 1), $"Mesh vertices valid on unit {i}");
                }

                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    var bdSr = previewRoot.transform.Find("BattlefieldWorld/StageBackdrop")?.GetComponent<SpriteRenderer>();
                    game.MainCamera.transform.position = new Vector3(-4.2f, 1.15f, -10f);
                    if (bdSr != null) bdSr.transform.localPosition = new Vector3(-4.2f * 0.36f, -0.58f, 0f);
                    var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
                    var prevTarget = game.MainCamera.targetTexture;
                    var prevActive = RenderTexture.active;
                    game.MainCamera.targetTexture = rt;
                    game.MainCamera.Render();
                    RenderTexture.active = rt;
                    var shot = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                    shot.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                    shot.Apply();
                    File.WriteAllBytes(Path.GetFullPath("Verification/battlefield-preview.png"), shot.EncodeToPNG());

                    game.MainCamera.transform.position = new Vector3(4.2f, 1.15f, -10f);
                    if (bdSr != null) bdSr.transform.localPosition = new Vector3(4.2f * 0.36f, -0.58f, 0f);
                    game.MainCamera.Render();
                    RenderTexture.active = rt;
                    shot.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                    shot.Apply();
                    File.WriteAllBytes(Path.GetFullPath("Verification/battlefield-demon-castle-preview.png"), shot.EncodeToPNG());

                    game.MainCamera.targetTexture = prevTarget;
                    RenderTexture.active = prevActive;
                    UnityEngine.Object.DestroyImmediate(rt);
                    UnityEngine.Object.DestroyImmediate(shot);
                }

                UnityEngine.Object.DestroyImmediate(previewRoot);
                var strayCam = GameObject.Find("Main Camera");
                if (strayCam != null) UnityEngine.Object.DestroyImmediate(strayCam);
            }

            // 6. Simulate full campaign victory across Angel and Demon stages
            var simProfile = new SaveProfile();
            simProfile.Data.gold = 500000;
            for (int lv = 0; lv < 12; lv++)
            {
                for (int i = 0; i < 6; i++)
                {
                    simProfile.TryUpgradeUnit(FactionKind.Angel, i);
                    simProfile.TryUpgradeUnit(FactionKind.Demon, i);
                }
                for (int s = 0; s < 3; s++) simProfile.TryUpgradeSkill(s);
                simProfile.TryUpgradeCastle();
                simProfile.TryUpgradeEconomy();
            }

            for (int stageNum = 1; stageNum <= 20; stageNum++)
            {
                var faction = stageNum <= 10 ? FactionKind.Angel : FactionKind.Demon;
                simProfile.SetCommanderSlot(faction, (stageNum - 1) % 6);
                var battle = new DefenseModel(simProfile, stageNum);
                int maxSteps = 4200; // up to 420s at 0.1s step
                for (int t = 0; t < maxSteps && battle.Outcome == BattleOutcome.Ongoing; t++)
                {
                    if (battle.FoodLevel < 5 && battle.Food >= battle.GetFoodUpgradeCost())
                        battle.TryUpgradeFoodLevel();

                    for (int slot = 5; slot >= 0; slot--)
                        battle.TrySpawnUnit(slot);

                    for (int s = 2; s >= 0; s--)
                        battle.TryCastCommanderSkill(s);

                    battle.Step(0.1f);
                }
                AssertTrue(battle.Outcome == BattleOutcome.Victory, $"Stage {stageNum} simulation must end in Victory (Ended: {battle.Outcome}, EnemyCastleHp: {battle.EnemyCastleHp})");
                battles++;
            }

            var report = new VerificationReport
            {
                passed = true,
                totalChecks = checks,
                simulatedBattles = battles,
                verifiedAngelHeroines = expectedAngels,
                verifiedDemonUnits = expectedDemons,
                previewImagePath = "Verification/verification-report.json",
                summary = "All 6 Naruto 8-head tall bikini heroines, 6 Demon units, Dual Campaign unlock rule, movement, skills, visual material/mesh checks, and 20-stage simulations passed."
            };
            File.WriteAllText("Verification/verification-report.json", JsonUtility.ToJson(report, true));
            Debug.Log($"ANGELS_AND_DEMONS_TESTS_OK checks={checks} battles={battles}");
        }
    }
}
#endif
