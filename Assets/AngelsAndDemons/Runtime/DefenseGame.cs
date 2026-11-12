using System.Collections.Generic;
using UnityEngine;

namespace AngelsAndDemons
{
    public sealed class FloatingTextEntry
    {
        public float x;
        public float y;
        public string text;
        public Color color;
        public float timer;
        public float duration;
        public int size;
    }

    public sealed class DefenseGame : MonoBehaviour
    {
        public SaveProfile Profile { get; private set; }
        public DefenseModel Model { get; private set; }
        public DefenseAudio Audio { get; private set; }
        public DefenseHaptics Haptics { get; private set; }
        public DefenseHud Hud { get; private set; }

        public bool InBattle { get; private set; }
        public bool IsPaused { get; set; }
        public float BattleSpeed { get; set; } = 1.0f;
        public int SelectedStageNumber { get; set; } = 1;

        Camera mainCam;
        Transform worldRoot;
        SpriteRenderer stageBackdropRenderer;
        SpriteRenderer allyCastleRenderer;
        SpriteRenderer enemyCastleRenderer;
        SpriteRenderer allyTurretOrb;
        SpriteRenderer enemyTurretOrb;
        SpriteRenderer allyCastleHpBg;
        SpriteRenderer allyCastleHpFill;
        SpriteRenderer enemyCastleHpBg;
        SpriteRenderer enemyCastleHpFill;

        sealed class UnitView
        {
            public GameObject root;
            public SpriteRenderer shadow;
            public SpriteRenderer aura;
            public SpriteRenderer commanderBeacon;
            public CharacterMotionMesh motionMesh;
            public SpriteRenderer strikeFx;
            public SpriteRenderer shield;
            public SpriteRenderer hpBg;
            public SpriteRenderer hpFill;
        }

        readonly List<UnitView> unitViews = new List<UnitView>(64);
        readonly List<SpriteRenderer> projectileViews = new List<SpriteRenderer>(64);
        readonly List<SpriteRenderer> skillWaveViews = new List<SpriteRenderer>(16);
        public readonly List<FloatingTextEntry> FloatingTexts = new List<FloatingTextEntry>(48);

        public float CameraOffsetX { get; set; }
        public Camera MainCamera => mainCam;

        float screenShakeTimer;
        float screenShakeIntensity;

        public void TriggerScreenShake(float intensity, float duration)
        {
            screenShakeIntensity = Mathf.Max(screenShakeIntensity, intensity);
            screenShakeTimer = Mathf.Max(screenShakeTimer, duration);
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            DefenseArt.Initialize();

            Profile = new SaveProfile();
            Profile.Load();

            Audio = gameObject.AddComponent<DefenseAudio>();
            Haptics = new DefenseHaptics();
            Hud = new DefenseHud(this);

            SetupCameraAndWorld();
            EnterLobby();
        }

        void SetupCameraAndWorld()
        {
            mainCam = Camera.main;
            if (mainCam == null)
            {
                var camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camObj.tag = "MainCamera";
                mainCam = camObj.GetComponent<Camera>();
            }
            mainCam.orthographic = true;
            mainCam.orthographicSize = 4.5f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.08f, 0.12f, 0.22f);
            mainCam.transform.position = new Vector3(0f, 1.15f, -10f);

            worldRoot = new GameObject("BattlefieldWorld").transform;
            worldRoot.SetParent(transform, false);

            // 1. Full Painted 2D Side-Scrolling Stage Panorama (2560x720)
            stageBackdropRenderer = CreateWorldSprite("StageBackdrop", worldRoot, DefenseArt.SkyBackdrop, -30);
            stageBackdropRenderer.transform.localPosition = new Vector3(0f, -0.58f, 0f);
            stageBackdropRenderer.transform.localScale = Vector3.one;

            // 2. Painted Gothic Angel Sanctuary & Demon Citadel Gatehouse Towers + Turret Orbs + World HP Bars
            allyCastleRenderer = CreateWorldSprite("AllyCastle", worldRoot, DefenseArt.AngelCastle, -6);
            allyCastleRenderer.flipX = false;
            allyCastleRenderer.transform.localPosition = new Vector3(DefenseModel.AllyCastleX + 0.45f, -0.65f, 0f);
            allyCastleRenderer.transform.localScale = Vector3.one;

            enemyCastleRenderer = CreateWorldSprite("EnemyCastle", worldRoot, DefenseArt.DemonCastle, -6);
            enemyCastleRenderer.flipX = false;
            enemyCastleRenderer.transform.localPosition = new Vector3(DefenseModel.EnemyCastleX - 0.45f, -0.65f, 0f);
            enemyCastleRenderer.transform.localScale = Vector3.one;

            allyTurretOrb = CreateWorldSprite("AllyTurretOrb", worldRoot, DefenseArt.Spark, -7);
            allyTurretOrb.transform.localPosition = new Vector3(DefenseModel.AllyCastleX + 0.45f, 4.80f, 0f);
            allyTurretOrb.color = new Color(0.45f, 0.92f, 1f, 0.55f);

            enemyTurretOrb = CreateWorldSprite("EnemyTurretOrb", worldRoot, DefenseArt.Spark, -7);
            enemyTurretOrb.transform.localPosition = new Vector3(DefenseModel.EnemyCastleX - 0.45f, 4.40f, 0f);
            enemyTurretOrb.color = new Color(1f, 0.38f, 0.20f, 0.55f);

            allyCastleHpBg = CreateWorldSprite("AllyCastleHpBg", worldRoot, DefenseArt.Pixel, 28);
            allyCastleHpBg.transform.localPosition = new Vector3(DefenseModel.AllyCastleX + 0.85f, 3.15f, 0f);
            allyCastleHpBg.transform.localScale = new Vector3(2.1f, 0.18f, 1f);
            allyCastleHpBg.color = new Color(0.05f, 0.08f, 0.14f, 0.88f);

            allyCastleHpFill = CreateWorldSprite("AllyCastleHpFill", worldRoot, DefenseArt.Pixel, 29);
            allyCastleHpFill.transform.localPosition = new Vector3(DefenseModel.AllyCastleX + 0.85f, 3.15f, 0f);
            allyCastleHpFill.transform.localScale = new Vector3(2.02f, 0.13f, 1f);
            allyCastleHpFill.color = new Color(0.25f, 0.92f, 0.55f, 1f);

            enemyCastleHpBg = CreateWorldSprite("EnemyCastleHpBg", worldRoot, DefenseArt.Pixel, 28);
            enemyCastleHpBg.transform.localPosition = new Vector3(DefenseModel.EnemyCastleX - 0.85f, 3.15f, 0f);
            enemyCastleHpBg.transform.localScale = new Vector3(2.1f, 0.18f, 1f);
            enemyCastleHpBg.color = new Color(0.14f, 0.05f, 0.08f, 0.88f);

            enemyCastleHpFill = CreateWorldSprite("EnemyCastleHpFill", worldRoot, DefenseArt.Pixel, 29);
            enemyCastleHpFill.transform.localPosition = new Vector3(DefenseModel.EnemyCastleX - 0.85f, 3.15f, 0f);
            enemyCastleHpFill.transform.localScale = new Vector3(2.02f, 0.13f, 1f);
            enemyCastleHpFill.color = new Color(1.0f, 0.30f, 0.36f, 1f);
        }

        static SpriteRenderer CreateWorldSprite(string name, Transform parent, Sprite sprite, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        public void EnterLobby()
        {
            InBattle = false;
            IsPaused = false;
            worldRoot.gameObject.SetActive(false);
            Audio.SetMusic(inBattle: false);
            FloatingTexts.Clear();
        }

        public void StartStage(int stageNumber)
        {
            SelectedStageNumber = Mathf.Clamp(stageNumber, 1, 20);
            var stage = CampaignCatalog.GetStage(SelectedStageNumber);
            Model = new DefenseModel(Profile, SelectedStageNumber, stage.campaignFaction);
            InBattle = true;
            IsPaused = false;
            CameraOffsetX = 0f;
            FloatingTexts.Clear();

            worldRoot.gameObject.SetActive(true);
            stageBackdropRenderer.sprite = DefenseArt.GetStageBackdrop(SelectedStageNumber);
            stageBackdropRenderer.color = Color.white;
            mainCam.backgroundColor = stage.skyTop;

            bool playerIsAngel = Model.PlayerFaction == FactionKind.Angel;
            allyCastleRenderer.sprite = playerIsAngel ? DefenseArt.AngelCastle : DefenseArt.DemonCastle;
            enemyCastleRenderer.sprite = playerIsAngel ? DefenseArt.DemonCastle : DefenseArt.AngelCastle;

            mainCam.transform.position = new Vector3(-3.8f, 1.15f, -10f);
            SyncVisuals(0.016f);
            Audio.SetMusic(inBattle: true);
        }

        void Update()
        {
            Hud.HandleKeyboardAndTouchInput();

            if (!InBattle || Model == null) return;

            float unscaledDt = Time.unscaledDeltaTime;
            for (int i = FloatingTexts.Count - 1; i >= 0; i--)
            {
                FloatingTexts[i].timer += unscaledDt;
                FloatingTexts[i].y += unscaledDt * 0.85f;
                if (FloatingTexts[i].timer >= FloatingTexts[i].duration)
                    FloatingTexts.RemoveAt(i);
            }

            float simDt = 0f;
            if (!IsPaused && Model.Outcome == BattleOutcome.Ongoing)
            {
                simDt = Mathf.Min(0.05f, Time.deltaTime) * Mathf.Clamp(BattleSpeed, 1f, 2f);
                Model.Step(simDt);
                ProcessCues();
            }

            UpdateCamera();
            SyncVisuals(simDt);
        }

        void ProcessCues()
        {
            for (int i = 0; i < Model.Cues.Count; i++)
            {
                var cue = Model.Cues[i];
                Audio.PlayCue(cue, Haptics);

                Color c = Color.white;
                int size = 18;
                switch (cue.kind)
                {
                    case CueKind.Spawn:
                        c = new Color(0.55f, 0.95f, 1f);
                        size = 19;
                        break;
                    case CueKind.Hit:
                        c = new Color(1f, 0.92f, 0.65f);
                        size = 17;
                        break;
                    case CueKind.Critical:
                        c = new Color(1f, 0.45f, 0.25f);
                        size = 22;
                        TriggerScreenShake(0.35f, 0.22f);
                        break;
                    case CueKind.Heal:
                        c = new Color(0.45f, 1f, 0.65f);
                        size = 19;
                        break;
                    case CueKind.Skill:
                        c = new Color(0.55f, 0.92f, 1f);
                        size = 24;
                        TriggerScreenShake(0.40f, 0.28f);
                        break;
                    case CueKind.EconomyUp:
                        c = new Color(1f, 0.88f, 0.3f);
                        size = 22;
                        break;
                    case CueKind.CastleHit:
                        c = new Color(1f, 0.35f, 0.35f);
                        size = 20;
                        TriggerScreenShake(0.32f, 0.20f);
                        break;
                }

                if (!string.IsNullOrEmpty(cue.label))
                {
                    FloatingTexts.Add(new FloatingTextEntry
                    {
                        x = cue.x,
                        y = cue.y + 1.05f,
                        text = cue.label,
                        color = c,
                        timer = 0f,
                        duration = 0.95f,
                        size = size
                    });
                }
            }
            Model.Cues.Clear();
        }

        void UpdateCamera()
        {
            float frontAllyX = DefenseModel.AllyCastleX + 2.5f;
            for (int i = 0; i < Model.Units.Count; i++)
            {
                var u = Model.Units[i];
                if (u.isAlly && u.IsAlive && u.x > frontAllyX)
                    frontAllyX = u.x;
            }

            float cmdX = (Model.Commander != null && Model.Commander.IsAlive) ? Model.Commander.x : frontAllyX;
            float focusX = Mathf.Lerp(cmdX, frontAllyX, 0.35f) * 0.72f + CameraOffsetX;
            float targetX = Mathf.Clamp(focusX, -4.6f, 4.6f);

            var pos = mainCam.transform.position;
            pos.x = Mathf.Lerp(pos.x, targetX, Time.unscaledDeltaTime * 7.0f);
            pos.y = 1.15f;

            Vector3 shakeOffset = Vector3.zero;
            if (screenShakeTimer > 0f)
            {
                screenShakeTimer -= Time.unscaledDeltaTime;
                float currentInt = screenShakeIntensity * Mathf.Clamp01(screenShakeTimer / 0.35f);
                shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 40f, 0f) - 0.5f) * currentInt * 0.40f,
                    (Mathf.PerlinNoise(0f, Time.time * 40f) - 0.5f) * currentInt * 0.40f,
                    0f);
            }
            mainCam.transform.position = pos + shakeOffset;

            // Subtle parallax on the wide 2560x720 stage panorama
            if (stageBackdropRenderer != null)
                stageBackdropRenderer.transform.localPosition = new Vector3(pos.x * 0.36f, -0.58f, 0f);
        }

        public void SyncVisuals(float simDt)
        {
            if (Model == null) return;

            // Update Castle World HP Bars & Turret Orbs (show world bar over portal when castle takes damage)
            float allyHpRatio = Mathf.Clamp01(Model.AllyCastleHp / Mathf.Max(1f, Model.AllyCastleMaxHp));
            bool showAllyCastleBar = allyHpRatio < 0.995f;
            allyCastleHpBg.gameObject.SetActive(showAllyCastleBar);
            allyCastleHpFill.gameObject.SetActive(showAllyCastleBar);
            if (showAllyCastleBar)
            {
                allyCastleHpFill.transform.localPosition = new Vector3((DefenseModel.AllyCastleX + 0.85f) - 1.01f * (1f - allyHpRatio), 3.15f, 0f);
                allyCastleHpFill.transform.localScale = new Vector3(2.02f * allyHpRatio, 0.13f, 1f);
            }

            float enemyHpRatio = Mathf.Clamp01(Model.EnemyCastleHp / Mathf.Max(1f, Model.EnemyCastleMaxHp));
            bool showEnemyCastleBar = enemyHpRatio < 0.995f;
            enemyCastleHpBg.gameObject.SetActive(showEnemyCastleBar);
            enemyCastleHpFill.gameObject.SetActive(showEnemyCastleBar);
            if (showEnemyCastleBar)
            {
                enemyCastleHpFill.transform.localPosition = new Vector3((DefenseModel.EnemyCastleX - 0.85f) - 1.01f * (1f - enemyHpRatio), 3.15f, 0f);
                enemyCastleHpFill.transform.localScale = new Vector3(2.02f * enemyHpRatio, 0.13f, 1f);
            }

            float pulse = 0.42f + Mathf.Sin(Time.unscaledTime * 5f) * 0.08f;
            allyTurretOrb.transform.localScale = new Vector3(pulse, pulse, 1f);
            enemyTurretOrb.transform.localScale = new Vector3(pulse, pulse, 1f);

            while (unitViews.Count < Model.Units.Count)
            {
                unitViews.Add(CreateUnitView(unitViews.Count));
            }

            for (int i = 0; i < unitViews.Count; i++)
            {
                var view = unitViews[i];
                if (i >= Model.Units.Count || !Model.Units[i].IsAlive)
                {
                    if (view.root.activeSelf) view.root.SetActive(false);
                    continue;
                }

                var u = Model.Units[i];
                if (!view.root.activeSelf) view.root.SetActive(true);

                float bob = Mathf.Abs(Mathf.Sin(u.animPhase)) * 0.04f;
                float baseScale = u.isCommander ? 1.12f : u.isBoss ? 1.20f : 0.96f;
                float faceDir = u.isAlly ? 1f : -1f;

                float lungeX = 0f;
                float lungeY = 0f;
                float charRot = 0f;

                if (u.attackAnimTimer > 0f)
                {
                    float p = 1f - Mathf.Clamp01(u.attackAnimTimer / 0.48f); // 0.0 -> 1.0
                    switch (u.spec.id)
                    {
                        case "hinata":
                            // Hinata: Gentle Step Twin Lion Fists dash thrust
                            if (p < 0.25f)
                            {
                                lungeX = -faceDir * (p / 0.25f) * 0.16f;
                            }
                            else if (p < 0.70f)
                            {
                                float sp = (p - 0.25f) / 0.45f;
                                lungeX = faceDir * Mathf.Sin(sp * Mathf.PI * 0.5f) * 0.72f;
                                charRot = -faceDir * Mathf.Sin(sp * Mathf.PI) * 6f;
                            }
                            else
                            {
                                float rp = (p - 0.70f) / 0.30f;
                                lungeX = faceDir * (1f - rp) * 0.72f;
                            }
                            break;

                        case "ino":
                            // Ino: Acrobatic jump spin & golden kunai throw
                            lungeY = Mathf.Sin(p * Mathf.PI) * 0.46f;
                            lungeX = faceDir * Mathf.Sin(p * Mathf.PI) * 0.32f;
                            charRot = -faceDir * Mathf.Sin(p * Mathf.PI * 2f) * 14f;
                            break;

                        case "sakura":
                            // Sakura: Cherry Blossom Ground Smash
                            if (p < 0.38f)
                            {
                                lungeY = Mathf.Sin((p / 0.38f) * Mathf.PI * 0.5f) * 0.52f;
                                lungeX = faceDir * 0.20f * (p / 0.38f);
                            }
                            else if (p < 0.65f)
                            {
                                float sp = (p - 0.38f) / 0.27f;
                                lungeY = (1f - sp) * 0.52f;
                                lungeX = faceDir * 0.48f;
                                if (sp > 0.4f && sp < 0.7f) TriggerScreenShake(0.38f, 0.22f);
                            }
                            else
                            {
                                float rp = (p - 0.65f) / 0.35f;
                                lungeX = faceDir * 0.48f * (1f - rp);
                            }
                            break;

                        case "temari":
                            // Temari: Giant Fan Wind Gale heavy swing
                            if (p < 0.30f)
                            {
                                lungeX = -faceDir * (p / 0.30f) * 0.22f;
                                charRot = faceDir * 18f * (p / 0.30f);
                            }
                            else
                            {
                                float sp = (p - 0.30f) / 0.70f;
                                lungeX = faceDir * Mathf.Sin(sp * Mathf.PI) * 0.68f;
                                charRot = -faceDir * Mathf.Cos(sp * Mathf.PI) * 22f;
                            }
                            break;

                        case "tsunade":
                            // Tsunade: Legendary Heavenly Foot Meteor Axe Kick
                            if (p < 0.42f)
                            {
                                lungeY = Mathf.Sin((p / 0.42f) * Mathf.PI * 0.5f) * 0.64f;
                                lungeX = faceDir * 0.25f * (p / 0.42f);
                                charRot = -faceDir * 12f * (p / 0.42f);
                            }
                            else if (p < 0.68f)
                            {
                                float sp = (p - 0.42f) / 0.26f;
                                lungeY = (1f - sp) * 0.64f;
                                lungeX = faceDir * 0.55f;
                                charRot = faceDir * 18f * (1f - sp);
                                if (sp > 0.4f && sp < 0.7f) TriggerScreenShake(0.55f, 0.28f);
                            }
                            else
                            {
                                float rp = (p - 0.68f) / 0.32f;
                                lungeX = faceDir * 0.55f * (1f - rp);
                            }
                            break;

                        case "konan":
                            // Konan: Paper Angel Wings Flight & Lance Storm
                            lungeY = Mathf.Sin(p * Mathf.PI) * 0.55f;
                            lungeX = faceDir * Mathf.Sin(p * Mathf.PI * 0.5f) * 0.38f;
                            charRot = -faceDir * 8f * Mathf.Sin(p * Mathf.PI);
                            break;

                        case "balrog":
                            // Balrog: Molten earth ground slam with screen shake
                            if (p < 0.40f)
                            {
                                lungeY = (p / 0.40f) * 0.45f;
                            }
                            else
                            {
                                float sp = (p - 0.40f) / 0.60f;
                                lungeY = Mathf.Max(0f, (1f - sp * 2.2f) * 0.45f);
                                lungeX = faceDir * Mathf.Sin(sp * Mathf.PI) * 0.50f;
                                if (sp > 0.2f && sp < 0.5f) TriggerScreenShake(0.50f, 0.26f);
                            }
                            break;

                        default:
                            // Generic demon / companion kinematic lunge
                            lungeX = Mathf.Sin(p * Mathf.PI) * (faceDir * 0.42f);
                            lungeY = Mathf.Sin(p * Mathf.PI) * 0.12f;
                            charRot = -faceDir * Mathf.Sin(p * Mathf.PI) * 8f;
                            break;
                    }
                }

                view.root.transform.localPosition = new Vector3(u.x + lungeX, -0.25f + u.yOffset + lungeY, 0f);
                view.root.transform.localRotation = Quaternion.Euler(0f, 0f, charRot);

                int orderBase = Mathf.RoundToInt(-u.yOffset * 25f) + (u.isCommander ? 16 : 6);
                view.motionMesh.transform.localPosition = new Vector3(0f, bob, 0f);
                view.motionMesh.transform.localScale = new Vector3(baseScale, baseScale, 1f);
                view.motionMesh.SamplePose(u, simDt > 0f ? simDt : 0.016f, orderBase);

                // Distinct character strike VFX
                bool showStrike = u.attackAnimTimer > 0.04f && u.attackAnimTimer < 0.40f;
                view.strikeFx.gameObject.SetActive(showStrike);
                if (showStrike)
                {
                    float p = 1f - Mathf.Clamp01(u.attackAnimTimer / 0.48f);
                    float dir = u.isAlly ? 1f : -1f;
                    Sprite fxSprite;
                    float fxScale = baseScale;
                    float fxRot = 0f;
                    Color sc = u.spec.auraColor;

                    switch (u.spec.id)
                    {
                        case "hinata":
                            fxSprite = DefenseArt.PalmWave;
                            fxScale *= Mathf.Lerp(0.85f, 1.45f, p);
                            fxRot = dir * 12f;
                            sc = new Color(0.35f, 0.75f, 1f, 0.85f);
                            break;
                        case "ino":
                            fxSprite = DefenseArt.KunaiLance;
                            fxScale *= Mathf.Lerp(0.70f, 1.20f, p);
                            fxRot = -dir * 25f;
                            sc = new Color(0.95f, 0.85f, 0.35f, 0.90f);
                            break;
                        case "sakura":
                            fxSprite = DefenseArt.Ring;
                            fxScale *= Mathf.Lerp(0.6f, 1.6f, p);
                            sc = new Color(1f, 0.45f, 0.75f, 0.95f);
                            break;
                        case "temari":
                            fxSprite = DefenseArt.SlashCrescent;
                            fxScale *= Mathf.Lerp(0.9f, 1.7f, p);
                            fxRot = dir * Mathf.Lerp(45f, -35f, p);
                            sc = new Color(0.35f, 1f, 0.65f, 0.90f);
                            break;
                        case "tsunade":
                            fxSprite = DefenseArt.Ring;
                            fxScale *= Mathf.Lerp(0.8f, 1.85f, p);
                            sc = new Color(1f, 0.85f, 0.25f, 0.95f);
                            break;
                        case "konan":
                            fxSprite = DefenseArt.KunaiLance;
                            fxScale *= Mathf.Lerp(0.75f, 1.35f, p);
                            fxRot = -dir * 35f;
                            sc = new Color(0.85f, 0.92f, 1f, 0.92f);
                            break;
                        default:
                            fxSprite = (u.spec.id == "imp" || u.spec.id == "succubus") ? DefenseArt.KunaiLance : DefenseArt.SlashCrescent;
                            fxScale *= Mathf.Lerp(0.75f, 1.25f, p);
                            fxRot = fxSprite == DefenseArt.SlashCrescent ? Mathf.Lerp(28f, -32f, p) * dir : 0f;
                            sc.a = 0.85f;
                            break;
                    }

                    view.strikeFx.sprite = fxSprite;
                    view.strikeFx.flipX = !u.isAlly;
                    view.strikeFx.sortingOrder = orderBase + 4;
                    float fxOffsetX = dir * (0.65f + p * 0.35f) * baseScale;
                    float fxOffsetY = (u.spec.id == "sakura" || u.spec.id == "tsunade" || u.spec.id == "balrog") ? 0.30f : 1.05f * baseScale;
                    view.strikeFx.transform.localPosition = new Vector3(fxOffsetX, fxOffsetY, 0f);
                    view.strikeFx.transform.localRotation = Quaternion.Euler(0f, 0f, fxRot);
                    view.strikeFx.transform.localScale = new Vector3(fxScale, fxScale, 1f);
                    sc.a *= Mathf.Clamp01(Mathf.Sin(p * Mathf.PI) * 1.2f);
                    view.strikeFx.color = sc;
                }

                view.aura.gameObject.SetActive(u.isCommander || u.isBoss);
                if (u.isCommander || u.isBoss)
                {
                    Color ac = u.isCommander ? new Color(1f, 0.86f, 0.22f, 0.42f) : u.spec.auraColor;
                    if (!u.isCommander) ac.a = 0.26f + Mathf.Sin(Time.unscaledTime * 4f) * 0.06f;
                    view.aura.color = ac;
                    view.aura.transform.localScale = new Vector3(1.45f * baseScale, 0.46f * baseScale, 1f);
                }

                view.commanderBeacon.gameObject.SetActive(u.isCommander);
                if (u.isCommander)
                {
                    float beaconBob = Mathf.Sin(Time.unscaledTime * 6f) * 0.06f;
                    view.commanderBeacon.transform.localPosition = new Vector3(0f, 2.66f * baseScale + beaconBob, 0f);
                    view.commanderBeacon.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
                    view.commanderBeacon.color = new Color(1f, 0.88f, 0.22f, 0.95f);
                }

                view.shield.gameObject.SetActive(u.shieldTimer > 0f);
                if (u.shieldTimer > 0f)
                {
                    Color sc = u.spec.auraColor;
                    sc.a = 0.50f;
                    view.shield.color = sc;
                    view.shield.transform.localPosition = new Vector3(0f, 1.15f * baseScale, 0f);
                    view.shield.transform.localScale = new Vector3(1.6f * baseScale, 1.9f * baseScale, 1f);
                }

                float hpRatio = Mathf.Clamp01(u.hp / Mathf.Max(1f, u.maxHp));
                bool showHpBar = hpRatio < 0.995f || u.isCommander || u.isBoss;
                view.hpBg.gameObject.SetActive(showHpBar);
                view.hpFill.gameObject.SetActive(showHpBar);
                if (showHpBar)
                {
                    float barY = 2.40f * baseScale;
                    view.hpBg.transform.localPosition = new Vector3(0f, barY, 0f);
                    view.hpFill.transform.localPosition = new Vector3(-0.44f * (1f - hpRatio), barY, 0f);
                    view.hpFill.transform.localScale = new Vector3(0.88f * hpRatio, 0.10f, 1f);
                    view.hpFill.color = u.isAlly ? new Color(0.28f, 0.95f, 0.52f) : new Color(1.0f, 0.32f, 0.36f);
                }
            }

            // Projectiles
            while (projectileViews.Count < Model.Projectiles.Count)
            {
                var sr = CreateWorldSprite("Proj_" + projectileViews.Count, worldRoot, DefenseArt.Spark, 30);
                projectileViews.Add(sr);
            }
            for (int i = 0; i < projectileViews.Count; i++)
            {
                var sr = projectileViews[i];
                if (i >= Model.Projectiles.Count)
                {
                    if (sr.gameObject.activeSelf) sr.gameObject.SetActive(false);
                    continue;
                }
                if (!sr.gameObject.activeSelf) sr.gameObject.SetActive(true);
                var p = Model.Projectiles[i];
                sr.sprite = p.style == 3 ? DefenseArt.SlashCrescent : p.style == 1 || p.style == 5 ? DefenseArt.KunaiLance : DefenseArt.Spark;
                sr.flipX = false;
                float angle = Mathf.Atan2(p.targetY - p.y, p.targetX - p.x) * Mathf.Rad2Deg;
                sr.transform.localPosition = new Vector3(p.x, p.y, 0f);
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                sr.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
                sr.color = p.color;
            }

            // Skill waves & impact/summon/heal rings
            while (skillWaveViews.Count < Model.ActiveSkillWaves.Count)
            {
                var sr = CreateWorldSprite("SkillWave_" + skillWaveViews.Count, worldRoot, DefenseArt.Ring, 35);
                skillWaveViews.Add(sr);
            }
            for (int i = 0; i < skillWaveViews.Count; i++)
            {
                var sr = skillWaveViews[i];
                if (i >= Model.ActiveSkillWaves.Count)
                {
                    if (sr.gameObject.activeSelf) sr.gameObject.SetActive(false);
                    continue;
                }
                if (!sr.gameObject.activeSelf) sr.gameObject.SetActive(true);
                var w = Model.ActiveSkillWaves[i];
                float t = Mathf.Clamp01(w.timer / Mathf.Max(0.01f, w.duration));
                if (w.duration <= 0.28f)
                {
                    // Projectile impact burst at chest height
                    float s = Mathf.Lerp(0.35f, w.radius * 0.95f, t);
                    sr.transform.localPosition = new Vector3(w.x, 0.90f, 0f);
                    sr.transform.localScale = new Vector3(s, s, 1f);
                    Color c = w.color;
                    c.a = (1f - t) * 0.72f;
                    sr.color = c;
                }
                else if (w.duration < 0.5f)
                {
                    // Ground summoning / healing magic circle on the stone road
                    float s = Mathf.Lerp(0.70f, 1.38f, t);
                    sr.transform.localPosition = new Vector3(w.x, -0.32f, 0f);
                    sr.transform.localScale = new Vector3(s, s * 0.38f, 1f);
                    Color c = w.color;
                    c.a = (1f - t) * 0.58f;
                    sr.color = c;
                }
                else
                {
                    float scale = Mathf.Lerp(0.8f, w.radius * 1.15f, t);
                    sr.transform.localPosition = new Vector3(w.x, 0.85f, 0f);
                    sr.transform.localScale = new Vector3(scale, scale, 1f);
                    Color c = w.color;
                    c.a = (1f - t) * 0.65f;
                    sr.color = c;
                }
            }
        }

        UnitView CreateUnitView(int index)
        {
            var root = new GameObject("Unit_" + index);
            root.transform.SetParent(worldRoot, false);

            var shadow = CreateWorldSprite("Shadow", root.transform, DefenseArt.Shadow, 0);
            shadow.transform.localScale = new Vector3(0.88f, 0.55f, 1f);

            var aura = CreateWorldSprite("Aura", root.transform, DefenseArt.Circle, 1);
            var commanderBeacon = CreateWorldSprite("CommanderBeacon", root.transform, DefenseArt.Spark, 27);
            commanderBeacon.gameObject.SetActive(false);

            var meshGo = new GameObject("MotionMesh");
            meshGo.transform.SetParent(root.transform, false);
            var motionMesh = meshGo.AddComponent<CharacterMotionMesh>();
            motionMesh.Initialize(10);

            var strikeFx = CreateWorldSprite("StrikeFx", root.transform, DefenseArt.SlashCrescent, 18);
            strikeFx.gameObject.SetActive(false);

            var shield = CreateWorldSprite("Shield", root.transform, DefenseArt.Ring, 22);

            var hpBg = CreateWorldSprite("HpBg", root.transform, DefenseArt.Pixel, 25);
            hpBg.transform.localScale = new Vector3(0.92f, 0.14f, 1f);
            hpBg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);

            var hpFill = CreateWorldSprite("HpFill", root.transform, DefenseArt.Pixel, 26);
            hpFill.transform.localScale = new Vector3(0.88f, 0.10f, 1f);

            return new UnitView
            {
                root = root,
                shadow = shadow,
                aura = aura,
                commanderBeacon = commanderBeacon,
                motionMesh = motionMesh,
                strikeFx = strikeFx,
                shield = shield,
                hpBg = hpBg,
                hpFill = hpFill
            };
        }

        void OnGUI()
        {
            Hud.DrawGui();
        }
    }
}
