using UnityEngine;

namespace AngelsAndDemons
{
    public sealed class DefenseHud
    {
        public const float DesignWidth = 1600f;
        public const float DesignHeight = 900f;

        static readonly Rect LeftMovePadRect = new Rect(28, 696, 108, 92);
        static readonly Rect RightMovePadRect = new Rect(142, 696, 108, 92);
        static readonly Rect MinimapRect = new Rect(372, 22, 580, 56);

        readonly DefenseGame game;
        GUIStyle boxStyle;
        GUIStyle titleStyle;
        GUIStyle labelStyle;
        GUIStyle centerLabelStyle;
        GUIStyle buttonStyle;
        GUIStyle smallLabelStyle;
        GUIStyle bigStartButtonStyle;
        bool stylesInitialized;

        bool holdMoveLeft;
        bool holdMoveRight;
        public int LobbyTabIndex { get; set; } = 0; // 0 = Commander Select & Battle Launch, 1 = Upgrade Workshop
        public bool ShowBattleHelp { get; set; } = false;

        public DefenseHud(DefenseGame game)
        {
            this.game = game;
        }

        static Rect GetSafeRect()
        {
            Rect safe = Screen.safeArea;
            if (safe.width <= 0f || safe.height <= 0f)
                safe = new Rect(0f, 0f, Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            return safe;
        }

        static Vector2 ScreenToDesignPosition(Vector2 screenPos)
        {
            Rect safe = GetSafeRect();
            float guiY = Screen.height - screenPos.y;
            float dx = (screenPos.x - safe.x) / Mathf.Max(1f, safe.width) * DesignWidth;
            float dy = (guiY - safe.y) / Mathf.Max(1f, safe.height) * DesignHeight;
            return new Vector2(dx, dy);
        }

        void EnsureStyles()
        {
            if (stylesInitialized) return;
            stylesInitialized = true;

            var font = DefenseArt.Font;
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                font = font,
                fontSize = 18,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.95f, 0.72f) }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 20,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            centerLabelStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            smallLabelStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 17,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.96f, 1f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            bigStartButtonStyle = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
        }

        void DrawShadowLabel(Rect rect, string text, GUIStyle style, Color textColor, float shadowOffset = 2f)
        {
            Color prev = style.normal.textColor;
            style.normal.textColor = new Color(0.02f, 0.04f, 0.08f, 0.96f);
            GUI.Label(new Rect(rect.x + shadowOffset, rect.y + shadowOffset, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x - shadowOffset, rect.y + shadowOffset, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + shadowOffset, rect.y - shadowOffset, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x - shadowOffset, rect.y - shadowOffset, rect.width, rect.height), text, style);
            style.normal.textColor = textColor;
            GUI.Label(rect, text, style);
            style.normal.textColor = prev;
        }

        public void HandleKeyboardAndTouchInput()
        {
            if (!game.InBattle || game.Model == null) return;

            holdMoveLeft = false;
            holdMoveRight = false;

            // Check multi-touch on iPhone
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
                Vector2 dp = ScreenToDesignPosition(t.position);
                if (LeftMovePadRect.Contains(dp))
                {
                    holdMoveLeft = true;
                    game.CameraOffsetX = 0f;
                }
                else if (RightMovePadRect.Contains(dp))
                {
                    holdMoveRight = true;
                    game.CameraOffsetX = 0f;
                }
                else if (MinimapRect.Contains(dp))
                {
                    float nx = Mathf.Clamp01((dp.x - (MinimapRect.x + 12f)) / (MinimapRect.width - 24f));
                    game.CameraOffsetX = Mathf.Lerp(-4.6f, 4.6f, nx);
                }
                else if (dp.y > 110f && dp.y < 640f && !game.IsPaused && !ShowBattleHelp && game.Model.Outcome == BattleOutcome.Ongoing)
                {
                    // Tapping or holding on the battlefield sets the Commander's move destination X!
                    var cam = game.MainCamera;
                    if (cam != null && game.Model.Commander != null)
                    {
                        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(t.position.x, t.position.y, 10f));
                        float targetX = Mathf.Clamp(wp.x, DefenseModel.FieldMinX, DefenseModel.FieldMaxX);
                        game.Model.CommanderTargetX = targetX;
                        game.Model.HasCommanderTargetX = true;
                        game.Model.AutoAdvanceCommander = targetX > game.Model.Commander.x + 0.5f;
                        game.CameraOffsetX = 0f;
                    }
                }
            }

            // Also check mouse press (PC / single-touch fallback)
            if (Input.GetMouseButton(0))
            {
                Vector2 dp = ScreenToDesignPosition(Input.mousePosition);
                if (LeftMovePadRect.Contains(dp))
                {
                    holdMoveLeft = true;
                    game.CameraOffsetX = 0f;
                }
                else if (RightMovePadRect.Contains(dp))
                {
                    holdMoveRight = true;
                    game.CameraOffsetX = 0f;
                }
                else if (MinimapRect.Contains(dp))
                {
                    float nx = Mathf.Clamp01((dp.x - (MinimapRect.x + 12f)) / (MinimapRect.width - 24f));
                    game.CameraOffsetX = Mathf.Lerp(-4.6f, 4.6f, nx);
                }
                else if (dp.y > 110f && dp.y < 640f && !game.IsPaused && !ShowBattleHelp && game.Model.Outcome == BattleOutcome.Ongoing)
                {
                    var cam = game.MainCamera;
                    if (cam != null && game.Model.Commander != null)
                    {
                        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
                        float targetX = Mathf.Clamp(wp.x, DefenseModel.FieldMinX, DefenseModel.FieldMaxX);
                        game.Model.CommanderTargetX = targetX;
                        game.Model.HasCommanderTargetX = true;
                        game.Model.AutoAdvanceCommander = targetX > game.Model.Commander.x + 0.5f;
                        game.CameraOffsetX = 0f;
                    }
                }
            }

            float keyDir = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyDir -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyDir += 1f;

            if (holdMoveLeft) keyDir = -1f;
            else if (holdMoveRight) keyDir = 1f;

            game.Model.CommanderInputDir = keyDir;

            var troopSlots = game.Model.GetAvailableAllyTroopSlots();
            if (Input.GetKeyDown(KeyCode.Alpha1) && troopSlots.Length > 0) game.Model.TrySpawnUnit(troopSlots[0]);
            if (Input.GetKeyDown(KeyCode.Alpha2) && troopSlots.Length > 1) game.Model.TrySpawnUnit(troopSlots[1]);
            if (Input.GetKeyDown(KeyCode.Alpha3) && troopSlots.Length > 2) game.Model.TrySpawnUnit(troopSlots[2]);
            if (Input.GetKeyDown(KeyCode.Alpha4) && troopSlots.Length > 3) game.Model.TrySpawnUnit(troopSlots[3]);
            if (Input.GetKeyDown(KeyCode.Alpha5) && troopSlots.Length > 4) game.Model.TrySpawnUnit(troopSlots[4]);

            if (Input.GetKeyDown(KeyCode.Q)) game.Model.TryCastCommanderSkill(0);
            if (Input.GetKeyDown(KeyCode.W)) game.Model.TryCastCommanderSkill(1);
            if (Input.GetKeyDown(KeyCode.E)) game.Model.TryCastCommanderSkill(2);

            if (Input.GetKeyDown(KeyCode.R)) game.Model.TryUpgradeFoodLevel();
            if (Input.GetKeyDown(KeyCode.Tab)) game.Model.RetreatHold = !game.Model.RetreatHold;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) game.IsPaused = !game.IsPaused;
        }

        public void DrawGui()
        {
            EnsureStyles();

            Rect safe = GetSafeRect();
            Matrix4x4 prevMatrix = GUI.matrix;
            float scaleX = safe.width / DesignWidth;
            float scaleY = safe.height / DesignHeight;
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, safe.y, 0f), Quaternion.identity, new Vector3(scaleX, scaleY, 1f));

            if (!game.InBattle)
                DrawLobby();
            else
                DrawBattleHud();

            GUI.matrix = prevMatrix;
        }

        void DrawPanel(Rect rect, Color bgColor, Color borderColor)
        {
            Color prev = GUI.color;
            GUI.color = borderColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = bgColor;
            GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        bool DrawColoredButton(Rect rect, string text, Color bgColor, bool enabled = true, GUIStyle customStyle = null)
        {
            bool prevEnabled = GUI.enabled;
            Color prevColor = GUI.backgroundColor;
            GUI.enabled = enabled;
            GUI.backgroundColor = enabled ? bgColor : new Color(0.32f, 0.34f, 0.38f, 0.85f);
            bool clicked = GUI.Button(rect, text, customStyle ?? buttonStyle);
            GUI.backgroundColor = prevColor;
            GUI.enabled = prevEnabled;
            return clicked;
        }

        void DrawLobby()
        {
            var profile = game.Profile;
            var faction = profile.ActiveFaction;
            bool isAngel = faction == FactionKind.Angel;

            // Main Background
            DrawPanel(new Rect(0, 0, DesignWidth, DesignHeight), new Color(0.06f, 0.09f, 0.16f, 1f), new Color(0.25f, 0.38f, 0.62f, 1f));

            // Top Navigation Header (y = 12..88)
            DrawPanel(new Rect(18, 12, 1564, 76), new Color(0.10f, 0.15f, 0.25f, 0.98f), new Color(0.95f, 0.78f, 0.32f, 0.9f));
            GUI.Label(new Rect(34, 18, 430, 34), "천사와 악마 · 나루토 8등신 비키니 에디션", titleStyle);
            GUI.Label(new Rect(34, 50, 430, 28), $"◆ 보유 골드: {profile.Data.gold:N0} G    ★ 성석: {profile.Data.gems} 개", labelStyle);

            // Two Main Mode Tabs: [① 지휘관 선택 & 전투 출격] vs [② 캐릭터·스킬·성채 강화소]
            if (DrawColoredButton(new Rect(475, 20, 260, 60), "① ★ 지휘관 선택 & 전투 출격\n(메인 플레이 화면)", LobbyTabIndex == 0 ? new Color(0.95f, 0.58f, 0.16f) : new Color(0.20f, 0.28f, 0.42f)))
            {
                LobbyTabIndex = 0;
            }
            if (DrawColoredButton(new Rect(745, 20, 250, 60), "② ▲ 캐릭터·성채 강화소\n(유닛/인술/성채 레벨업)", LobbyTabIndex == 1 ? new Color(0.22f, 0.72f, 0.46f) : new Color(0.20f, 0.28f, 0.42f)))
            {
                LobbyTabIndex = 1;
            }

            // Faction Toggle (1부: 천사 진영 vs 2부: 악마 진영)
            if (DrawColoredButton(new Rect(1015, 20, 195, 60), "★ 1부: 천사 캠페인\n(나루토 비키니 6인)", isAngel ? new Color(0.25f, 0.65f, 1f) : new Color(0.22f, 0.28f, 0.4f)))
            {
                profile.Data.selectedFaction = 0;
                if (game.SelectedStageNumber > 10) game.SelectedStageNumber = 1;
                profile.Save();
            }

            string demonLabel = profile.IsDemonCampaignUnlocked
                ? "◆ 2부: 악마 캠페인\n(심연의 마왕군)"
                : "[잠김] 2부: 악마 캠페인\n(천사 10장 클리어 시)";
            if (DrawColoredButton(new Rect(1218, 20, 195, 60), demonLabel, !isAngel ? new Color(0.9f, 0.28f, 0.38f) : new Color(0.32f, 0.2f, 0.3f), profile.IsDemonCampaignUnlocked))
            {
                profile.Data.selectedFaction = 1;
                if (game.SelectedStageNumber <= 10) game.SelectedStageNumber = 11;
                profile.Save();
            }

            if (DrawColoredButton(new Rect(1421, 20, 146, 60), profile.IsDemonCampaignUnlocked ? "원작 2부 해금됨" : "2부 즉시해금\n(테스트용)", new Color(0.55f, 0.38f, 0.85f)))
            {
                profile.Data.demonUnlockedOverride = !profile.Data.demonUnlockedOverride;
                profile.Save();
            }

            if (LobbyTabIndex == 0)
            {
                DrawLobbyCommanderSelectAndLaunch(profile, faction, isAngel);
            }
            else
            {
                DrawLobbyUpgradeWorkshop(profile, faction, isAngel);
            }
        }

        void DrawLobbyCommanderSelectAndLaunch(SaveProfile profile, FactionKind faction, bool isAngel)
        {
            int cmdSlot = profile.GetCommanderSlot(faction);
            var activeCmd = CampaignCatalog.GetUnit(faction, cmdSlot);

            // =========================================================================
            // STEP 1 PANEL (Left-Center Top: x=18..1122, y=98..548):
            // 6 Large Full-Body 8-Head Tall Character Cards — Tap Any Card to Pick Commander!
            // =========================================================================
            DrawPanel(new Rect(18, 98, 1104, 452), new Color(0.09f, 0.14f, 0.24f, 0.98f), new Color(0.42f, 0.68f, 1f, 0.9f));
            DrawShadowLabel(new Rect(34, 106, 760, 32),
                isAngel
                    ? "STEP 1. 전장에서 직접 조종할 [내 지휘관] 캐릭터를 선택하세요!"
                    : "STEP 1. 전장에서 직접 조종할 [악마 군단장] 캐릭터를 선택하세요!",
                titleStyle, new Color(1f, 0.95f, 0.72f));
            DrawShadowLabel(new Rect(765, 108, 345, 28), "★ 선택된 지휘관은 상시 출전! (나머지 5명만 식량 소환)", smallLabelStyle, new Color(1f, 0.88f, 0.32f));

            for (int i = 0; i < 6; i++)
            {
                var spec = CampaignCatalog.GetUnit(faction, i);
                int lv = profile.GetUnitLevel(faction, i);
                bool isSelectedCmd = (cmdSlot == i);

                Rect card = new Rect(30 + i * 180, 144, 172, 394);
                Color cardBg = isSelectedCmd ? new Color(0.22f, 0.32f, 0.52f, 0.99f) : new Color(0.11f, 0.16f, 0.27f, 0.96f);
                Color cardBorder = isSelectedCmd ? new Color(1f, 0.86f, 0.25f, 1f) : spec.primaryColor;
                DrawPanel(card, cardBg, cardBorder);

                // Top Role Tag
                DrawPanel(new Rect(card.x + 6, card.y + 6, card.width - 12, 26),
                    isSelectedCmd ? new Color(0.95f, 0.68f, 0.14f, 0.95f) : new Color(0.16f, 0.22f, 0.36f, 0.95f),
                    cardBorder);
                GUI.Label(new Rect(card.x + 6, card.y + 6, card.width - 12, 26),
                    isSelectedCmd ? "★ 내 조작 지휘관" : spec.angelClassOrigin,
                    smallLabelStyle);

                // Full 8-Head Tall Bikini Standing Art
                Rect imgRect = new Rect(card.x + 8, card.y + 36, card.width - 16, 246);
                DrawPanel(imgRect, new Color(0.06f, 0.09f, 0.15f, 1f), new Color(0.3f, 0.42f, 0.62f, 0.5f));
                var fullTex = DefenseArt.GetCharacterTexture(spec.id);
                if (fullTex != null)
                {
                    GUI.DrawTexture(new Rect(imgRect.x + 4, imgRect.y + 4, imgRect.width - 8, imgRect.height - 8), fullTex, ScaleMode.ScaleToFit, true);
                }

                // Name + Level + Stats
                GUI.Label(new Rect(card.x + 4, card.y + 286, card.width - 8, 26), $"{spec.name} (Lv.{lv})", centerLabelStyle);
                GUI.Label(new Rect(card.x + 4, card.y + 310, card.width - 8, 36),
                    $"HP {Mathf.RoundToInt(spec.maxHp * (1f + (lv - 1) * 0.13f))} · 공격 {Mathf.RoundToInt(spec.attack * (1f + (lv - 1) * 0.13f))}\n병사 소환 식량: {spec.foodCost}",
                    smallLabelStyle);

                // Select Button at bottom of card
                if (DrawColoredButton(
                    new Rect(card.x + 8, card.y + 348, card.width - 16, 38),
                    isSelectedCmd ? "✓ 지휘관 선택됨" : "지휘관으로 선택",
                    isSelectedCmd ? new Color(0.96f, 0.66f, 0.14f) : new Color(0.26f, 0.50f, 0.84f)))
                {
                    profile.SetCommanderSlot(faction, i);
                    profile.Save();
                }

                // Also allow tapping anywhere on the character illustration to select her immediately!
                if (GUI.Button(imgRect, GUIContent.none, GUIStyle.none))
                {
                    profile.SetCommanderSlot(faction, i);
                    profile.Save();
                }
            }

            // =========================================================================
            // STEP 2 PANEL (Left-Center Bottom: x=18..1122, y=558..886):
            // Selected Commander's 3 Active Skills + Clear "What to Do After Picking Commander" Guide!
            // =========================================================================
            DrawPanel(new Rect(18, 558, 1104, 328), new Color(0.09f, 0.14f, 0.24f, 0.98f), activeCmd.primaryColor);
            GUI.Label(new Rect(34, 566, 620, 32),
                $"★ 현재 선택된 내 지휘관: [{activeCmd.name}] — 전용 필살 인술 3종 & 전투 조작법",
                titleStyle);

            // Left Half: 3 Exclusive Commander Skills used in battle
            for (int s = 0; s < 3; s++)
            {
                var skill = activeCmd.commanderSkills[s];
                int sLv = profile.GetSkillLevel(s);
                Rect sBox = new Rect(32 + s * 184, 606, 176, 178);
                DrawPanel(sBox, new Color(0.14f, 0.19f, 0.32f, 0.98f), new Color(0.55f, 0.42f, 0.95f, 0.9f));
                string slotTitle = s == 0 ? "인술 1 (우측하단 1번)" : s == 1 ? "인술 2 (우측하단 2번)" : "궁극 오의 (우측하단 3번)";
                GUI.Label(new Rect(sBox.x + 6, sBox.y + 8, sBox.width - 12, 22), slotTitle, smallLabelStyle);
                GUI.Label(new Rect(sBox.x + 6, sBox.y + 34, sBox.width - 12, 28), $"{skill.name} (Lv.{sLv})", centerLabelStyle);
                GUI.Label(new Rect(sBox.x + 8, sBox.y + 66, sBox.width - 16, 72), skill.description, smallLabelStyle);
                GUI.Label(new Rect(sBox.x + 8, sBox.y + 144, sBox.width - 16, 24), $"소모 MP: {skill.manaCost}  |  쿨 {skill.cooldown:F0}초", smallLabelStyle);
            }

            // Bottom quick action buttons under skills
            if (DrawColoredButton(new Rect(32, 796, 270, 74), $"▲ [{activeCmd.name}] 및 부대 강화하기\n(골드 보유: {profile.Data.gold:N0}G)", new Color(0.22f, 0.70f, 0.45f)))
            {
                LobbyTabIndex = 1;
            }
            if (DrawColoredButton(new Rect(312, 796, 128, 74), game.Audio.Muted ? "◎ 사운드\n꺼짐" : "● 사운드\n켜짐", new Color(0.28f, 0.40f, 0.58f)))
            {
                game.Audio.ToggleMute();
            }
            if (DrawColoredButton(new Rect(450, 796, 126, 74), game.Haptics.Enabled ? "● 진동\n켜짐" : "◎ 진동\n꺼짐", new Color(0.28f, 0.40f, 0.58f)))
            {
                game.Haptics.Toggle();
            }

            // Right Half of Bottom Panel: Crystal-Clear 3-Step Guide ("지휘관을 고른 뒤 어떻게 플레이하나요?")
            Rect guideBox = new Rect(592, 606, 516, 264);
            DrawPanel(guideBox, new Color(0.12f, 0.18f, 0.30f, 0.98f), new Color(1f, 0.84f, 0.28f, 0.95f));
            DrawShadowLabel(new Rect(guideBox.x + 16, guideBox.y + 10, guideBox.width - 32, 30),
                "★ 지휘관을 고른 뒤 이렇게 플레이하세요! (원작 천사와 악마 방식)",
                titleStyle, new Color(1f, 0.92f, 0.45f));
            DrawShadowLabel(new Rect(guideBox.x + 16, guideBox.y + 46, guideBox.width - 32, 64),
                $"① [내 지휘관 직접 이동 & 공격]: 전투 시작 시 머리 위에 [★ 내 지휘관] 표시가 뜬 {activeCmd.name}가 상시 출전합니다! 전장 바닥 터치나 좌측 [◀후퇴 / ▶전진]으로 직접 조종하세요.",
                labelStyle, Color.white);
            DrawShadowLabel(new Rect(guideBox.x + 16, guideBox.y + 114, guideBox.width - 32, 64),
                "② [동료 5인 식량 소환]: 지휘관 외 5명의 동료 카드가 하단 중앙에 배치됩니다. 식량이 차오르면 터치하여 성문에서 동료들을 소환해 함께 진격하세요!",
                labelStyle, Color.white);
            DrawShadowLabel(new Rect(guideBox.x + 16, guideBox.y + 182, guideBox.width - 32, 68),
                $"③ [지휘관 인술 3종 발동]: 차오르는 MP로 우측 하단의 [보라색 인술 버튼 3개]를 눌러 {activeCmd.name}의 필살기를 발동하고 우측 적 성채 HP를 0으로 만들면 승리!",
                labelStyle, Color.white);

            // =========================================================================
            // STEP 3 PANEL (Right Column: x=1132..1582, y=98..886):
            // Stage Selection (1..10 or 11..20) + Giant Pulsing [★ 전투 시작!] Button!
            // =========================================================================
            DrawPanel(new Rect(1132, 98, 450, 788), new Color(0.09f, 0.14f, 0.24f, 0.98f), new Color(0.95f, 0.55f, 0.28f, 0.95f));
            GUI.Label(new Rect(1148, 106, 418, 32), "STEP 2. 출격 스테이지 선택 & 전투 시작!", titleStyle);

            int baseStage = isAngel ? 1 : 11;
            for (int i = 0; i < 10; i++)
            {
                int stNum = baseStage + i;
                var st = CampaignCatalog.GetStage(stNum);
                bool unlocked = profile.IsStageUnlocked(stNum);
                bool selected = game.SelectedStageNumber == stNum;
                int stars = profile.Data.stageStars[stNum - 1];
                string starStr = stars == 3 ? "★★★" : stars == 2 ? "★★☆" : stars == 1 ? "★☆☆" : "☆☆☆";

                Rect stRect = new Rect(1148, 146 + i * 54, 418, 48);
                Color btnCol = selected ? new Color(0.95f, 0.58f, 0.18f) : unlocked ? new Color(0.18f, 0.28f, 0.45f) : new Color(0.15f, 0.16f, 0.2f);
                if (DrawColoredButton(stRect, $"{st.title}   [{starStr}]  보상 {st.rewardGold}G", btnCol, unlocked))
                {
                    game.SelectedStageNumber = stNum;
                }
            }

            var chosenStage = CampaignCatalog.GetStage(game.SelectedStageNumber);
            DrawPanel(new Rect(1148, 694, 418, 72), new Color(0.14f, 0.19f, 0.32f, 0.98f), new Color(0.95f, 0.78f, 0.32f, 0.8f));
            GUI.Label(new Rect(1156, 700, 402, 60),
                $"출격 지휘관: ★ {activeCmd.name}   |   전장: {chosenStage.title}\n적 성채 HP {chosenStage.enemyCastleHp:N0} · 승리 보상 +{chosenStage.rewardGold}G",
                centerLabelStyle);

            float pulse = 0.92f + Mathf.Sin(Time.unscaledTime * 5f) * 0.08f;
            Color launchCol = new Color(0.98f * pulse, 0.26f, 0.30f, 1f);
            if (DrawColoredButton(
                new Rect(1148, 776, 418, 96),
                $"▶ [{activeCmd.name}] 지휘관으로\n{chosenStage.title} 전투 시작! (터치)",
                launchCol,
                true,
                bigStartButtonStyle))
            {
                game.StartStage(game.SelectedStageNumber);
            }
        }

        void DrawLobbyUpgradeWorkshop(SaveProfile profile, FactionKind faction, bool isAngel)
        {
            int cmdSlot = profile.GetCommanderSlot(faction);
            var activeCmd = CampaignCatalog.GetUnit(faction, cmdSlot);

            // Left Panel: 6 Unit Upgrades
            DrawPanel(new Rect(18, 98, 820, 788), new Color(0.09f, 0.14f, 0.24f, 0.98f), new Color(0.35f, 0.68f, 0.95f, 0.9f));
            GUI.Label(new Rect(36, 108, 780, 32),
                isAngel ? "▲ 천사 진영 6인 캐릭터 레벨 강화 (지휘관 & 소환 병사 능력치 상승)" : "▲ 악마 진영 6병종 레벨 강화",
                titleStyle);

            for (int i = 0; i < 6; i++)
            {
                var spec = CampaignCatalog.GetUnit(faction, i);
                int lv = profile.GetUnitLevel(faction, i);
                int upCost = profile.GetUnitUpgradeCost(faction, i);
                bool isCmd = cmdSlot == i;

                Rect row = new Rect(34, 150 + i * 118, 788, 110);
                DrawPanel(row, isCmd ? new Color(0.18f, 0.27f, 0.44f, 0.98f) : new Color(0.12f, 0.17f, 0.28f, 0.95f), isCmd ? new Color(1f, 0.84f, 0.32f) : spec.primaryColor);

                var tex = DefenseArt.GetPortraitTexture(spec.id);
                if (tex != null)
                {
                    GUI.DrawTextureWithTexCoords(new Rect(row.x + 10, row.y + 8, 88, 94), tex, new Rect(0.16f, 0.53f, 0.68f, 0.45f));
                }

                GUI.Label(new Rect(row.x + 112, row.y + 10, 420, 28), $"{spec.name}  (Lv.{lv})   [{spec.angelClassOrigin}]", titleStyle);
                GUI.Label(new Rect(row.x + 112, row.y + 42, 420, 28),
                    $"체력 {Mathf.RoundToInt(spec.maxHp * (1f + (lv - 1) * 0.13f))}  ·  공격력 {Mathf.RoundToInt(spec.attack * (1f + (lv - 1) * 0.13f))}  ·  소환 식량 {spec.foodCost}",
                    labelStyle);
                GUI.Label(new Rect(row.x + 112, row.y + 72, 420, 26), spec.costumeSummary, smallLabelStyle);

                if (DrawColoredButton(new Rect(row.x + 545, row.y + 10, 228, 40), isCmd ? "★ 현재 지휘관" : "지휘관으로 지정", isCmd ? new Color(0.95f, 0.68f, 0.18f) : new Color(0.28f, 0.48f, 0.78f)))
                {
                    profile.SetCommanderSlot(faction, i);
                    profile.Save();
                }

                if (DrawColoredButton(new Rect(row.x + 545, row.y + 58, 228, 42), $"▲ 레벨 강화 ({upCost}G)", new Color(0.22f, 0.72f, 0.45f), profile.Data.gold >= upCost && lv < 20))
                {
                    if (profile.TryUpgradeUnit(faction, i))
                    {
                        game.Audio.Play("research", 0.5f, 1.1f);
                        profile.Save();
                    }
                }
            }

            // Right Panel: Commander Skills + Castle & Economy Meta Upgrades + Back to Launch
            DrawPanel(new Rect(852, 98, 730, 788), new Color(0.09f, 0.14f, 0.24f, 0.98f), new Color(0.95f, 0.78f, 0.32f, 0.9f));
            GUI.Label(new Rect(872, 108, 690, 32), $"★ [{activeCmd.name}] 지휘관 인술 3종 & 성채 영구 강화", titleStyle);

            for (int s = 0; s < 3; s++)
            {
                var skill = activeCmd.commanderSkills[s];
                int sLv = profile.GetSkillLevel(s);
                int sCost = profile.GetSkillUpgradeCost(s);
                Rect sRect = new Rect(872, 152 + s * 112, 690, 102);
                DrawPanel(sRect, new Color(0.13f, 0.18f, 0.30f, 0.96f), new Color(0.42f, 0.72f, 1f, 0.8f));
                GUI.Label(new Rect(sRect.x + 16, sRect.y + 10, 440, 28), $"인술 {s + 1}: {skill.name} (Lv.{sLv})", titleStyle);
                GUI.Label(new Rect(sRect.x + 16, sRect.y + 44, 440, 46), skill.description, labelStyle);
                if (DrawColoredButton(new Rect(sRect.x + 470, sRect.y + 22, 204, 58), $"▲ 인술 강화\n({sCost}G)", new Color(0.32f, 0.58f, 0.92f), profile.Data.gold >= sCost && sLv < 15))
                {
                    if (profile.TryUpgradeSkill(s))
                    {
                        game.Audio.Play("research", 0.5f, 1.15f);
                        profile.Save();
                    }
                }
            }

            // Castle & Economy Meta Upgrades
            Rect metaRect = new Rect(872, 496, 690, 260);
            DrawPanel(metaRect, new Color(0.12f, 0.16f, 0.26f, 0.96f), new Color(0.88f, 0.72f, 0.32f, 0.8f));
            GUI.Label(new Rect(metaRect.x + 16, metaRect.y + 12, 650, 30), "◆ 아군 닌자 마을 방어력 & 군량 생산력 영구 강화", titleStyle);

            int castleCost = profile.GetCastleUpgradeCost();
            GUI.Label(new Rect(metaRect.x + 18, metaRect.y + 56, 430, 56),
                $"마을 대관문 & 인술 포탑 레벨: Lv.{profile.Data.castleLevel}\n아군 마을 최대 체력 {1800 + (profile.Data.castleLevel - 1) * 280} / 마을 수호 인술 포탑 공격력 증가",
                labelStyle);
            if (DrawColoredButton(new Rect(metaRect.x + 470, metaRect.y + 56, 204, 56), $"▲ 마을 관문 강화 ({castleCost}G)", new Color(0.85f, 0.55f, 0.2f), profile.Data.gold >= castleCost))
            {
                if (profile.TryUpgradeCastle())
                {
                    game.Audio.Play("research", 0.5f, 1.05f);
                    profile.Save();
                }
            }

            int econCost = profile.GetEconomyUpgradeCost();
            GUI.Label(new Rect(metaRect.x + 18, metaRect.y + 132, 430, 56),
                $"닌자 군량 & 최대 병력 레벨: Lv.{profile.Data.economyMetaLevel}\n전투 중 초당 닌자 군량(차크라) 회복 속도 & 최대 소환 가능 병력 수 증가",
                labelStyle);
            if (DrawColoredButton(new Rect(metaRect.x + 470, metaRect.y + 132, 204, 56), $"▲ 군량 생산력 강화 ({econCost}G)", new Color(0.24f, 0.68f, 0.58f), profile.Data.gold >= econCost))
            {
                if (profile.TryUpgradeEconomy())
                {
                    game.Audio.Play("research", 0.5f, 1.05f);
                    profile.Save();
                }
            }

            if (DrawColoredButton(
                new Rect(872, 774, 690, 96),
                $"▶ 강화 완료 — [{activeCmd.name}] 지휘관 출격 화면으로 돌아가기",
                new Color(0.95f, 0.55f, 0.18f),
                true,
                bigStartButtonStyle))
            {
                LobbyTabIndex = 0;
            }
        }

        void DrawBattleHud()
        {
            var m = game.Model;
            if (m == null) return;

            // 1. Top Bar: Ally Castle HP | Minimap | Enemy Castle HP | Help, Speed & Pause
            DrawPanel(new Rect(16, 12, 1568, 78), new Color(0.06f, 0.09f, 0.15f, 0.90f), new Color(0.45f, 0.62f, 0.9f, 0.8f));

            // Ally Castle HP Bar + Dedicated Commander HP Bar on Top-Left
            float allyRatio = Mathf.Clamp01(m.AllyCastleHp / Mathf.Max(1f, m.AllyCastleMaxHp));
            DrawPanel(new Rect(28, 18, 330, 30), new Color(0.12f, 0.16f, 0.22f), new Color(0.3f, 0.8f, 1f));
            GUI.color = new Color(0.22f, 0.85f, 0.52f);
            GUI.DrawTexture(new Rect(30, 20, 326 * allyRatio, 26), Texture2D.whiteTexture);
            GUI.color = Color.white;
            DrawShadowLabel(new Rect(28, 18, 330, 30), $"★ 나뭇잎 마을 관문 HP: {Mathf.CeilToInt(m.AllyCastleHp)} / {Mathf.CeilToInt(m.AllyCastleMaxHp)}", centerLabelStyle, Color.white);

            float cmdHpRatio = m.Commander != null ? Mathf.Clamp01(m.Commander.hp / Mathf.Max(1f, m.Commander.maxHp)) : 0f;
            DrawPanel(new Rect(28, 52, 330, 30), new Color(0.10f, 0.14f, 0.24f), new Color(1f, 0.84f, 0.28f));
            GUI.color = new Color(0.28f, 0.72f, 1f);
            GUI.DrawTexture(new Rect(30, 54, 326 * cmdHpRatio, 26), Texture2D.whiteTexture);
            GUI.color = Color.white;
            string cmdBarText = (m.Commander != null && m.Commander.respawnTimer > 0f)
                ? $"★ 내 지휘관 부활 대기: {m.Commander.respawnTimer:F1}초"
                : $"★ 내 지휘관({m.Commander.spec.name}) HP: {Mathf.CeilToInt(m.Commander.hp)}/{Mathf.CeilToInt(m.Commander.maxHp)}";
            DrawShadowLabel(new Rect(28, 52, 330, 30), cmdBarText, centerLabelStyle, new Color(1f, 0.95f, 0.72f));

            // Center Minimap (Tap/Drag to Pan Battlefield Camera)
            Rect mapRect = MinimapRect;
            DrawPanel(mapRect, new Color(0.08f, 0.12f, 0.20f, 0.95f), new Color(0.65f, 0.75f, 0.95f, 0.7f));
            float mapSpan = DefenseModel.EnemyCastleX - DefenseModel.AllyCastleX;
            if (game.MainCamera != null)
            {
                float camCenterN = Mathf.Clamp01((game.MainCamera.transform.position.x - DefenseModel.AllyCastleX) / mapSpan);
                float viewW = 148f;
                float vx = Mathf.Clamp(mapRect.x + 12f + camCenterN * (mapRect.width - 24f) - viewW * 0.5f, mapRect.x + 4f, mapRect.xMax - viewW - 4f);
                GUI.color = new Color(1f, 0.92f, 0.45f, 0.18f);
                GUI.DrawTexture(new Rect(vx, mapRect.y + 3f, viewW, mapRect.height - 6f), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            for (int i = 0; i < m.Units.Count; i++)
            {
                var u = m.Units[i];
                if (!u.IsAlive) continue;
                float nx = Mathf.Clamp01((u.x - DefenseModel.AllyCastleX) / mapSpan);
                float px = mapRect.x + 12f + nx * (mapRect.width - 24f);
                float py = u.isCommander ? mapRect.y + 8f : mapRect.y + 24f;
                float sz = u.isCommander ? 14f : u.isBoss ? 12f : 8f;
                GUI.color = u.isCommander ? new Color(1f, 0.9f, 0.2f) : u.isAlly ? new Color(0.35f, 0.9f, 1f) : new Color(1f, 0.3f, 0.35f);
                GUI.DrawTexture(new Rect(px - sz * 0.5f, py, sz, sz), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            DrawPanel(new Rect(mapRect.x + 8, mapRect.y + 30, mapRect.width - 16, 22), new Color(0.04f, 0.06f, 0.12f, 0.88f), new Color(0.35f, 0.50f, 0.75f, 0.6f));
            DrawShadowLabel(new Rect(mapRect.x, mapRect.y + 31, mapRect.width, 20), $"{m.Stage.title}  (경과 {Mathf.FloorToInt(m.ElapsedTime)}초 · 미니맵 터치 시 시점 이동)", smallLabelStyle, Color.white);

            // Enemy Castle HP Bar + Active Troop Status on Top-Right
            float enemyRatio = Mathf.Clamp01(m.EnemyCastleHp / Mathf.Max(1f, m.EnemyCastleMaxHp));
            DrawPanel(new Rect(966, 18, 330, 32), new Color(0.18f, 0.10f, 0.14f), new Color(1f, 0.35f, 0.4f));
            GUI.color = new Color(0.95f, 0.28f, 0.34f);
            GUI.DrawTexture(new Rect(968, 20, 326 * enemyRatio, 28), Texture2D.whiteTexture);
            GUI.color = Color.white;
            DrawShadowLabel(new Rect(966, 18, 330, 32), $"◆ 탈주 닌자 요새 HP: {Mathf.CeilToInt(m.EnemyCastleHp)} / {Mathf.CeilToInt(m.EnemyCastleMaxHp)}", centerLabelStyle, Color.white);
            DrawShadowLabel(new Rect(966, 54, 330, 26), $"출전 병력: {m.GetActiveAllyTroopCount()}/{m.MaxAllyUnits}  |  전장 바닥 터치: 지휘관 이동", smallLabelStyle, new Color(0.92f, 0.96f, 1f));

            // Help, Speed & Pause Buttons
            if (DrawColoredButton(new Rect(1308, 20, 82, 60), "[?] 조작법", ShowBattleHelp ? new Color(0.95f, 0.65f, 0.18f) : new Color(0.28f, 0.55f, 0.52f)))
            {
                ShowBattleHelp = !ShowBattleHelp;
            }
            if (DrawColoredButton(new Rect(1396, 20, 88, 60), game.BattleSpeed > 1.2f ? "▶▶ 2.0x" : "▶ 1.0x", new Color(0.25f, 0.48f, 0.78f)))
            {
                game.BattleSpeed = game.BattleSpeed > 1.2f ? 1.0f : 2.0f;
            }
            if (DrawColoredButton(new Rect(1490, 20, 84, 60), game.IsPaused ? "▶ 계속" : "|| 정지", new Color(0.45f, 0.35f, 0.65f)))
            {
                game.IsPaused = !game.IsPaused;
            }

            // 2. Overhead Golden Tag on the Commander + Move Target Marker + Floating Combat Numbers
            var cam = game.MainCamera;
            if (cam != null)
            {
                // Draw unmistakable floating golden badge over the player's Commander!
                if (m.Commander != null && m.Commander.IsAlive)
                {
                    Vector3 cmdVp = cam.WorldToViewportPoint(new Vector3(m.Commander.x, 2.75f, 0f));
                    if (cmdVp.z > 0f && cmdVp.x >= -0.05f && cmdVp.x <= 1.05f)
                    {
                        float cx = Mathf.Clamp(cmdVp.x * DesignWidth, 110f, DesignWidth - 110f);
                        float cy = Mathf.Clamp((1f - cmdVp.y) * DesignHeight, 102f, 620f);
                        Rect badgeRect = new Rect(cx - 102f, cy - 34f, 204f, 32f);
                        DrawPanel(badgeRect, new Color(0.12f, 0.16f, 0.28f, 0.94f), new Color(1f, 0.86f, 0.22f, 1f));
                        DrawShadowLabel(badgeRect, $"★ 내 지휘관: {m.Commander.spec.name} ▼", smallLabelStyle, new Color(1f, 0.92f, 0.35f));
                    }
                }

                // Draw destination marker on the stone road when player taps the battlefield to move Commander
                if (m.HasCommanderTargetX && m.Commander != null && m.Commander.IsAlive)
                {
                    Vector3 tgtVp = cam.WorldToViewportPoint(new Vector3(m.CommanderTargetX, -0.35f, 0f));
                    if (tgtVp.z > 0f && tgtVp.x >= 0f && tgtVp.x <= 1f)
                    {
                        float tx = tgtVp.x * DesignWidth;
                        float ty = Mathf.Clamp((1f - tgtVp.y) * DesignHeight, 420f, 635f);
                        Rect tgtRect = new Rect(tx - 68f, ty - 16f, 136f, 28f);
                        DrawPanel(tgtRect, new Color(0.10f, 0.28f, 0.48f, 0.92f), new Color(0.45f, 0.95f, 1f, 1f));
                        DrawShadowLabel(tgtRect, "▼ 지휘관 이동 목표", smallLabelStyle, new Color(0.6f, 0.95f, 1f));
                    }
                }

                for (int i = 0; i < game.FloatingTexts.Count; i++)
                {
                    var ft = game.FloatingTexts[i];
                    Vector3 vp = cam.WorldToViewportPoint(new Vector3(ft.x, ft.y, 0f));
                    if (vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f)
                    {
                        float sx = vp.x * DesignWidth;
                        float sy = (1f - vp.y) * DesignHeight;
                        DrawShadowLabel(new Rect(sx - 95f, sy - 18f, 190f, 36f), ft.text, centerLabelStyle, ft.color);
                    }
                }
            }

            // 3. Active Wave / Skill Banner
            if (m.ActiveBannerTimer > 0f && !string.IsNullOrEmpty(m.ActiveBannerText))
            {
                DrawPanel(new Rect(350, 98, 900, 42), new Color(0.08f, 0.12f, 0.24f, 0.95f), new Color(1f, 0.82f, 0.3f));
                DrawShadowLabel(new Rect(350, 98, 900, 42), m.ActiveBannerText, centerLabelStyle, new Color(1f, 0.95f, 0.65f));
            }

            // 3B. Contextual Control Callout Strip (Always visible for first 12s of Stage 1, or when [[?] 조작법] is toggled!)
            if (ShowBattleHelp || (m.Stage.stageNumber == 1 && m.ElapsedTime < 12.0f))
            {
                DrawPanel(new Rect(16, 646, 376, 36), new Color(0.10f, 0.22f, 0.40f, 0.94f), new Color(0.45f, 0.85f, 1f));
                DrawShadowLabel(new Rect(16, 646, 376, 36), "① 내 지휘관 이동 (◀/▶ 또는 바닥 터치) & 군량Lv", smallLabelStyle, Color.white);

                DrawPanel(new Rect(400, 646, 760, 36), new Color(0.10f, 0.22f, 0.40f, 0.94f), new Color(1f, 0.84f, 0.28f));
                DrawShadowLabel(new Rect(400, 646, 760, 36), "② 동료 닌자 5인 소환 (군량이 차면 원하는 카드를 터치해 출격시키세요!)", smallLabelStyle, new Color(1f, 0.94f, 0.65f));

                DrawPanel(new Rect(1168, 646, 416, 36), new Color(0.18f, 0.12f, 0.34f, 0.94f), new Color(0.75f, 0.52f, 1f));
                DrawShadowLabel(new Rect(1168, 646, 416, 36), $"③ [{m.Commander.spec.name}] 지휘관 전용 필살 인술 3종 (MP)", smallLabelStyle, new Color(0.92f, 0.85f, 1f));
            }

            // 4. Bottom HUD Bar (Height 202px: y = 686..888)
            DrawPanel(new Rect(16, 686, 1568, 202), new Color(0.06f, 0.09f, 0.16f, 0.94f), new Color(0.42f, 0.58f, 0.86f, 0.85f));

            // 4A. Left Controls: Hold Pads for Commander Move Left/Right + Rally/Auto toggle + Economy Upgrade
            DrawPanel(LeftMovePadRect, holdMoveLeft ? new Color(0.28f, 0.68f, 1f, 0.98f) : new Color(0.16f, 0.30f, 0.52f, 0.96f), new Color(0.55f, 0.82f, 1f));
            GUI.Label(LeftMovePadRect, "◀ 내 지휘관\n후퇴·대기\n(누르기)", centerLabelStyle);

            DrawPanel(RightMovePadRect, holdMoveRight ? new Color(0.28f, 0.68f, 1f, 0.98f) : new Color(0.16f, 0.30f, 0.52f, 0.96f), new Color(0.55f, 0.82f, 1f));
            GUI.Label(RightMovePadRect, "내 지휘관 ▶\n전진·돌격\n(누르기)", centerLabelStyle);

            if (DrawColoredButton(new Rect(28, 794, 222, 82), m.RetreatHold ? "◀ 전군 본진 집결 중\n(터치: 전군 총진격)" : "▶ 전군 진격 중\n(터치: 본진 후퇴집결)", m.RetreatHold ? new Color(0.95f, 0.52f, 0.2f) : new Color(0.22f, 0.58f, 0.42f)))
            {
                m.RetreatHold = !m.RetreatHold;
            }

            // Economy Upgrade Button
            int foodUpCost = m.GetFoodUpgradeCost();
            bool canUpFood = m.FoodLevel < 8 && m.Food >= foodUpCost;
            string econText = m.FoodLevel >= 8
                ? $"★ 군량 Lv.MAX\n보유 {Mathf.FloorToInt(m.Food)}/{Mathf.FloorToInt(m.MaxFood)}"
                : $"▲ 군량 Lv.{m.FoodLevel} 강화\n비용: {foodUpCost}\n보유 {Mathf.FloorToInt(m.Food)}/{Mathf.FloorToInt(m.MaxFood)}";
            if (DrawColoredButton(new Rect(258, 696, 134, 180), econText, canUpFood ? new Color(0.92f, 0.68f, 0.16f) : new Color(0.25f, 0.30f, 0.40f), m.FoodLevel < 8))
            {
                if (!m.TryUpgradeFoodLevel())
                {
                    m.ShowBanner($"군량이 부족합니다! (군량 Lv 강화 필요: {foodUpCost})", 1.2f);
                }
            }

            // 4B. Center Controls: 5 Companion Summon Slots (Commander is already on the field!)
            var allyTroopSlots = m.GetAvailableAllyTroopSlots();
            for (int s = 0; s < allyTroopSlots.Length; s++)
            {
                int slotIdx = allyTroopSlots[s];
                var spec = CampaignCatalog.GetUnit(m.PlayerFaction, slotIdx);
                float cd = m.SlotCooldowns[slotIdx];
                bool canSpawn = cd <= 0f && m.Food >= spec.foodCost && m.GetActiveAllyTroopCount() < m.MaxAllyUnits;

                Rect card = new Rect(404 + s * 152, 696, 146, 180);
                DrawPanel(card, canSpawn ? new Color(0.14f, 0.22f, 0.36f, 0.98f) : new Color(0.10f, 0.12f, 0.18f, 0.92f), canSpawn ? spec.primaryColor : new Color(0.3f, 0.32f, 0.38f));

                var pTex = DefenseArt.GetPortraitTexture(spec.id);
                if (pTex != null)
                {
                    GUI.DrawTextureWithTexCoords(new Rect(card.x + 10, card.y + 6, 126, 92), pTex, new Rect(0.16f, 0.53f, 0.68f, 0.45f));
                }

                DrawShadowLabel(new Rect(card.x + 4, card.y + 100, card.width - 8, 22), $"[{s + 1}] {spec.name}", centerLabelStyle, Color.white);
                string btnText = cd > 0f ? $"쿨 {cd:F1}s" : $"출격 ({spec.foodCost})";
                DrawPanel(new Rect(card.x + 6, card.y + 124, card.width - 12, 48), canSpawn ? spec.primaryColor : new Color(0.25f, 0.27f, 0.32f), Color.white);
                DrawShadowLabel(new Rect(card.x + 6, card.y + 124, card.width - 12, 48), btnText, centerLabelStyle, Color.white);

                // Invisible full-card button so tapping portrait OR button summons immediately
                if (GUI.Button(card, GUIContent.none, GUIStyle.none))
                {
                    if (!m.TrySpawnUnit(slotIdx))
                    {
                        if (cd > 0f) m.ShowBanner($"{spec.name} 소환 쿨타임 ({cd:F1}초)", 1.0f);
                        else if (m.Food < spec.foodCost) m.ShowBanner($"군량 부족! ({spec.name} 출격 군량: {spec.foodCost})", 1.1f);
                        else m.ShowBanner("최대 출전 병력 한도에 도달했습니다!", 1.1f);
                    }
                }
            }

            // 4C. Right Controls: 3 Commander Active Skills + Mana Bar (x = 1170..1572)
            float manaRatio = Mathf.Clamp01(m.Mana / Mathf.Max(1f, m.MaxMana));
            DrawPanel(new Rect(1172, 696, 398, 28), new Color(0.10f, 0.14f, 0.26f), new Color(0.4f, 0.75f, 1f));
            GUI.color = new Color(0.28f, 0.62f, 1f);
            GUI.DrawTexture(new Rect(1174, 698, 394 * manaRatio, 24), Texture2D.whiteTexture);
            GUI.color = Color.white;
            DrawShadowLabel(new Rect(1172, 696, 398, 28), $"◆ [{m.Commander.spec.name}] 인술 차크라(MP): {Mathf.FloorToInt(m.Mana)} / {Mathf.FloorToInt(m.MaxMana)}", centerLabelStyle, Color.white);

            var skills = m.Commander.spec.commanderSkills;
            for (int s = 0; s < 3; s++)
            {
                var skill = skills[s];
                float scd = m.SkillCooldowns[s];
                bool canCast = m.Commander.IsAlive && scd <= 0f && m.Mana >= skill.manaCost;
                Rect sBtn = new Rect(1172 + s * 134, 732, 128, 144);
                string keyHint = s == 0 ? "인술1" : s == 1 ? "인술2" : "궁극오의";
                string sText = scd > 0f
                    ? $"[{keyHint}]\n{skill.name}\n쿨 {scd:F1}초"
                    : $"[{keyHint}]\n{skill.name}\nMP {skill.manaCost} 발동";
                if (DrawColoredButton(sBtn, sText, canCast ? new Color(0.55f, 0.28f, 0.88f) : new Color(0.28f, 0.24f, 0.36f)))
                {
                    if (!m.TryCastCommanderSkill(s))
                    {
                        if (scd > 0f) m.ShowBanner($"{skill.name} 쿨타임 ({scd:F1}초)", 1.0f);
                        else if (m.Mana < skill.manaCost) m.ShowBanner($"MP가 부족합니다! (필요 MP: {skill.manaCost})", 1.1f);
                    }
                }
            }

            // 5. Pause or Victory/Defeat Overlay
            if (game.IsPaused && m.Outcome == BattleOutcome.Ongoing)
            {
                DrawModalPause();
            }
            else if (m.Outcome != BattleOutcome.Ongoing)
            {
                DrawModalOutcome(m);
            }
        }

        void DrawModalPause()
        {
            DrawPanel(new Rect(500, 220, 600, 410), new Color(0.07f, 0.11f, 0.20f, 0.98f), new Color(0.95f, 0.78f, 0.32f));
            GUI.Label(new Rect(500, 240, 600, 40), "|| 전투 일시정지", centerLabelStyle);
            if (DrawColoredButton(new Rect(560, 300, 480, 58), "▶ 전투 계속하기", new Color(0.24f, 0.72f, 0.48f)))
                game.IsPaused = false;
            if (DrawColoredButton(new Rect(560, 372, 480, 58), "★ 현재 스테이지 재도전", new Color(0.28f, 0.52f, 0.85f)))
                game.StartStage(game.SelectedStageNumber);
            if (DrawColoredButton(new Rect(560, 444, 480, 58), "◀ 지휘관 변경 / 스테이지 선택으로 이동", new Color(0.85f, 0.55f, 0.20f)))
            {
                LobbyTabIndex = 0;
                game.EnterLobby();
            }
            if (DrawColoredButton(new Rect(560, 516, 480, 58), "▲ 닌자·마을 강화소로 이동", new Color(0.45f, 0.38f, 0.75f)))
            {
                LobbyTabIndex = 1;
                game.EnterLobby();
            }
        }

        void DrawModalOutcome(DefenseModel m)
        {
            bool win = m.Outcome == BattleOutcome.Victory;
            DrawPanel(new Rect(480, 190, 640, 480), new Color(0.08f, 0.12f, 0.22f, 0.98f), win ? new Color(1f, 0.84f, 0.28f) : new Color(1f, 0.32f, 0.36f));
            DrawShadowLabel(new Rect(480, 210, 640, 44), win ? "★ 탈주 닌자 요새 함락 — 작전 대승리! ★" : "⚠ 아군 닌자 마을 함락 — 작전 실패", centerLabelStyle, win ? new Color(1f, 0.92f, 0.45f) : new Color(1f, 0.45f, 0.45f));

            if (win)
            {
                string unlockMsg = (m.Stage.stageNumber == 10)
                    ? "\n★ 특전: 나뭇잎 마을 방어전 완주로 [2부: 탈주 닌자 세력 침공 캠페인]이 정식 해금되었습니다!"
                    : "";
                DrawShadowLabel(new Rect(500, 260, 600, 95),
                    $"{m.Stage.title} 작전 성공!\n획득 골드: +{m.Stage.rewardGold}G   |   획득 성석: +{m.Stage.rewardGems}개{unlockMsg}\n★ 획득한 골드로 닌자·인술·마을 관문을 강화하세요!",
                    centerLabelStyle, Color.white);

                // Prominent Workshop Button!
                if (DrawColoredButton(new Rect(540, 370, 520, 62), "▲ [강화소로 이동] 번 골드로 닌자/인술/마을 레벨업!", new Color(0.95f, 0.65f, 0.18f)))
                {
                    LobbyTabIndex = 1;
                    game.EnterLobby();
                }

                if (m.Stage.stageNumber < 20 && DrawColoredButton(new Rect(540, 442, 520, 58), "▶ 다음 스테이지 바로 출격", new Color(0.24f, 0.74f, 0.46f)))
                {
                    game.StartStage(m.Stage.stageNumber + 1);
                }
            }
            else
            {
                DrawShadowLabel(new Rect(500, 265, 600, 90),
                    "방어선이 무너졌습니다!\n강화소에서 나루토 닌자들과 지휘관 인술·마을 관문을 강화한 뒤 다시 도전하세요!",
                    centerLabelStyle, Color.white);

                // Prominent Workshop Button on Defeat!
                if (DrawColoredButton(new Rect(540, 370, 520, 62), "▲ [강화소로 이동] 닌자 부대 스펙업하고 재도전!", new Color(0.22f, 0.72f, 0.48f)))
                {
                    LobbyTabIndex = 1;
                    game.EnterLobby();
                }

                if (DrawColoredButton(new Rect(540, 442, 520, 58), "▶ 현재 스테이지 즉시 다시 도전", new Color(0.28f, 0.56f, 0.88f)))
                {
                    game.StartStage(m.Stage.stageNumber);
                }
            }

            if (DrawColoredButton(new Rect(540, 514, 520, 56), "◀ 메인 로비 / 스테이지 선택 화면으로", new Color(0.35f, 0.42f, 0.60f)))
            {
                LobbyTabIndex = 0;
                game.EnterLobby();
            }
        }
    }
}
