using System;
using UnityEngine;

namespace AngelsAndDemons
{
    public enum FactionKind { Angel = 0, Demon = 1 }
    public enum UnitRole { Vanguard = 0, Marksman = 1, Priest = 2, Caster = 3, Paladin = 4, Archangel = 5 }

    [Serializable]
    public sealed class SkillSpec
    {
        public string id;
        public string name;
        public string description;
        public int manaCost;
        public float cooldown;
        public float range;
        public float radius;
        public float powerMultiplier;
        public float healRatio;
        public float shieldSeconds;
        public float knockback;
        public int vfxStyle;
    }

    [Serializable]
    public sealed class UnitSpec
    {
        public int slotIndex;
        public string id;
        public FactionKind faction;
        public UnitRole role;
        public string name;
        public string title;
        public string angelClassOrigin;
        public string costumeSummary;
        public int foodCost;
        public float spawnCooldown;
        public float maxHp;
        public float attack;
        public float defense;
        public float moveSpeed;
        public float attackRange;
        public float attackInterval;
        public float splashRadius;
        public float knockback;
        public bool isRanged;
        public bool isHealer;
        public bool isFlying;
        public bool superArmor;
        public Color primaryColor;
        public Color accentColor;
        public Color auraColor;
        public SkillSpec[] commanderSkills;
    }

    [Serializable]
    public sealed class StageSpec
    {
        public int stageNumber; // 1..20 (1..10 Angel Campaign, 11..20 Demon Campaign)
        public FactionKind campaignFaction;
        public string title;
        public string subtitle;
        public string environmentTheme;
        public float enemyCastleHp;
        public float enemySpawnInterval;
        public int maxEnemyTier;
        public int bossSlotIndex;
        public int rewardGold;
        public int rewardGems;
        public Color skyTop;
        public Color skyBottom;
        public Color groundColor;
    }

    public static class CampaignCatalog
    {
        public static readonly UnitSpec[] AngelUnits = CreateAngelUnits();
        public static readonly UnitSpec[] DemonUnits = CreateDemonUnits();
        public static readonly StageSpec[] Stages = CreateStages();

        public static UnitSpec GetUnit(FactionKind faction, int slotIndex)
        {
            var list = faction == FactionKind.Angel ? AngelUnits : DemonUnits;
            return list[Mathf.Clamp(slotIndex, 0, list.Length - 1)];
        }

        public static StageSpec GetStage(int stageNumber)
        {
            return Stages[Mathf.Clamp(stageNumber - 1, 0, Stages.Length - 1)];
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }

        static UnitSpec[] CreateAngelUnits()
        {
            return new[]
            {
                new UnitSpec
                {
                    slotIndex = 0,
                    id = "hinata",
                    faction = FactionKind.Angel,
                    role = UnitRole.Vanguard,
                    name = "휴우가 히나타",
                    title = "유권의 성전사 (8등신 비키니)",
                    angelClassOrigin = "Tier 1 · 성전사 (Swordsman)",
                    costumeSummary = "라벤더 & 화이트 투피스 비키니 · 금빛 천사 날개 머리핀 · 청색 쌍사권 오라",
                    foodCost = 60,
                    spawnCooldown = 2.4f,
                    maxHp = 440f,
                    attack = 52f,
                    defense = 12f,
                    moveSpeed = 2.85f,
                    attackRange = 1.25f,
                    attackInterval = 0.82f,
                    splashRadius = 0.8f,
                    knockback = 0.45f,
                    isRanged = false,
                    isHealer = false,
                    isFlying = false,
                    superArmor = false,
                    primaryColor = Hex("8d7be8"),
                    accentColor = Hex("f4f6ff"),
                    auraColor = Hex("5cdbff"),
                    commanderSkills = new[]
                    {
                        new SkillSpec { id = "hinata_s1", name = "유보 쌍사권", description = "푸른 사자 차크라를 두른 채 전방 적군을 관통 연타하고 밀어냅니다.", manaCost = 35, cooldown = 6.0f, range = 4.2f, radius = 2.2f, powerMultiplier = 3.2f, knockback = 1.6f, vfxStyle = 0 },
                        new SkillSpec { id = "hinata_s2", name = "수호팔괘 육십사장", description = "회전 차크라 방벽을 펼쳐 주변 아군을 치유하고 5초간 보호막을 부여합니다.", manaCost = 50, cooldown = 11.0f, range = 5.0f, radius = 4.5f, powerMultiplier = 1.5f, healRatio = 0.22f, shieldSeconds = 5.0f, vfxStyle = 1 },
                        new SkillSpec { id = "hinata_s3", name = "팔괘 공벽장·천광", description = "성광이 깃든 거대 장풍으로 전장 전체 적군에게 대피해와 경직을 입힙니다.", manaCost = 85, cooldown = 18.0f, range = 12.0f, radius = 8.0f, powerMultiplier = 5.4f, knockback = 2.8f, vfxStyle = 2 }
                    }
                },
                new UnitSpec
                {
                    slotIndex = 1,
                    id = "ino",
                    faction = FactionKind.Angel,
                    role = UnitRole.Marksman,
                    name = "야마나카 이노",
                    title = "성광의 발키리 사수 (8등신 비키니)",
                    angelClassOrigin = "Tier 2 · 발키리 궁수 (Valkyrie Archer)",
                    costumeSummary = "로얄 퍼플 & 골드 투피스 비키니 · 꽃 장식 · 빛의 차크라 쿠나이",
                    foodCost = 110,
                    spawnCooldown = 3.8f,
                    maxHp = 310f,
                    attack = 68f,
                    defense = 6f,
                    moveSpeed = 2.60f,
                    attackRange = 3.35f,
                    attackInterval = 0.90f,
                    splashRadius = 0.5f,
                    knockback = 0.25f,
                    isRanged = true,
                    isHealer = false,
                    isFlying = false,
                    superArmor = false,
                    primaryColor = Hex("9b4de0"),
                    accentColor = Hex("ffd859"),
                    auraColor = Hex("e59bff"),
                    commanderSkills = new[]
                    {
                        new SkillSpec { id = "ino_s1", name = "성광 쿠나이 폭우", description = "전방에 황금빛 차크라 쿠나이 세례를 퍼부어 다수의 적을 관통합니다.", manaCost = 35, cooldown = 5.5f, range = 7.5f, radius = 3.2f, powerMultiplier = 3.1f, knockback = 1.1f, vfxStyle = 3 },
                        new SkillSpec { id = "ino_s2", name = "감각전달·성소의 축복", description = "아군 전체의 신경을 연결해 체력을 회복하고 6초간 보호막을 부여합니다.", manaCost = 50, cooldown = 11.5f, range = 6.5f, radius = 5.5f, powerMultiplier = 1.2f, healRatio = 0.25f, shieldSeconds = 6.0f, vfxStyle = 4 },
                        new SkillSpec { id = "ino_s3", name = "심전신·천벌의 꽃보라", description = "전장 전역에 보라빛 성광 꽃보라를 터뜨려 적 전체의 방어를 무너뜨립니다.", manaCost = 80, cooldown = 17.5f, range = 13.0f, radius = 9.0f, powerMultiplier = 5.1f, knockback = 2.0f, vfxStyle = 5 }
                    }
                },
                new UnitSpec
                {
                    slotIndex = 2,
                    id = "sakura",
                    faction = FactionKind.Angel,
                    role = UnitRole.Priest,
                    name = "하루노 사쿠라",
                    title = "백호의 세라핌 사제 (8등신 비키니)",
                    angelClassOrigin = "Tier 3 · 세라핌 사제 (High Priest)",
                    costumeSummary = "체리 핑크 & 화이트 투피스 비키니 · 이마 백호의 인 · 치유 차크라 글러브",
                    foodCost = 175,
                    spawnCooldown = 5.8f,
                    maxHp = 480f,
                    attack = 64f,
                    defense = 11f,
                    moveSpeed = 2.50f,
                    attackRange = 2.85f,
                    attackInterval = 1.15f,
                    splashRadius = 2.4f,
                    knockback = 0.8f,
                    isRanged = true,
                    isHealer = true,
                    isFlying = false,
                    superArmor = false,
                    primaryColor = Hex("ff6f9c"),
                    accentColor = Hex("fff0f5"),
                    auraColor = Hex("67f5a7"),
                    commanderSkills = new[]
                    {
                        new SkillSpec { id = "sakura_s1", name = "앵화충·대지분쇄", description = "괴력을 담은 주먹으로 지면을 강타해 전방 적군을 크게 띄우고 분쇄합니다.", manaCost = 40, cooldown = 6.5f, range = 4.6f, radius = 3.0f, powerMultiplier = 3.6f, knockback = 2.4f, vfxStyle = 6 },
                        new SkillSpec { id = "sakura_s2", name = "백호의 인·성광 치유진", description = "전장 아군들의 체력을 대량 회복시키고 성스러운 재생 보호막을 펼칩니다.", manaCost = 55, cooldown = 10.5f, range = 8.0f, radius = 6.5f, powerMultiplier = 1.0f, healRatio = 0.36f, shieldSeconds = 5.5f, vfxStyle = 7 },
                        new SkillSpec { id = "sakura_s3", name = "극·금강괴력 벚꽃폭풍", description = "백호 해방 상태로 전장 전체를 강타하여 모든 적에게 파괴적인 충격을 줍니다.", manaCost = 90, cooldown = 19.0f, range = 12.5f, radius = 8.5f, powerMultiplier = 5.8f, knockback = 3.2f, vfxStyle = 8 }
                    }
                },
                new UnitSpec
                {
                    slotIndex = 3,
                    id = "temari",
                    faction = FactionKind.Angel,
                    role = UnitRole.Caster,
                    name = "테마리",
                    title = "천공의 풍둔 마법사 (8등신 비키니)",
                    angelClassOrigin = "Tier 4 · 천공 마법사 (Sky Caster)",
                    costumeSummary = "다크 틸 & 골드 투피스 비키니 · 삼성 거대 철선 부채 · 신성 돌풍",
                    foodCost = 250,
                    spawnCooldown = 7.6f,
                    maxHp = 520f,
                    attack = 96f,
                    defense = 13f,
                    moveSpeed = 2.45f,
                    attackRange = 3.65f,
                    attackInterval = 1.30f,
                    splashRadius = 2.2f,
                    knockback = 1.45f,
                    isRanged = true,
                    isHealer = false,
                    isFlying = false,
                    superArmor = false,
                    primaryColor = Hex("1f8a8c"),
                    accentColor = Hex("f5c85b"),
                    auraColor = Hex("88ffe6"),
                    commanderSkills = new[]
                    {
                        new SkillSpec { id = "temari_s1", name = "풍둔·대족제비", description = "삼성 부채를 크게 휘둘러 전방 적군을 베어 넘기는 진공 칼날 폭풍을 날립니다.", manaCost = 40, cooldown = 6.0f, range = 8.5f, radius = 3.8f, powerMultiplier = 3.4f, knockback = 2.6f, vfxStyle = 9 },
                        new SkillSpec { id = "temari_s2", name = "천공 순풍의 장막", description = "순풍 결계로 아군을 치유하고 원거리 피해를 막는 바람 보호막을 부여합니다.", manaCost = 50, cooldown = 11.5f, range = 7.0f, radius = 5.5f, powerMultiplier = 1.4f, healRatio = 0.22f, shieldSeconds = 5.0f, vfxStyle = 10 },
                        new SkillSpec { id = "temari_s3", name = "소환·참참무 천공열풍", description = "초대형 회오리가 전장을 휩쓸며 적 진영 전체를 적 본진까지 밀어냅니다.", manaCost = 85, cooldown = 18.5f, range = 14.0f, radius = 9.5f, powerMultiplier = 5.5f, knockback = 3.8f, vfxStyle = 11 }
                    }
                },
                new UnitSpec
                {
                    slotIndex = 4,
                    id = "tsunade",
                    faction = FactionKind.Angel,
                    role = UnitRole.Paladin,
                    name = "츠나데",
                    title = "창조재생의 성기사장 (8등신 비키니)",
                    angelClassOrigin = "Tier 5 · 가디언 성기사 (Paladin Tank)",
                    costumeSummary = "에메랄드 그린 & 골드 투피스 비키니 · 음봉인 · 황금 성광 건틀릿",
                    foodCost = 360,
                    spawnCooldown = 10.5f,
                    maxHp = 1180f,
                    attack = 128f,
                    defense = 28f,
                    moveSpeed = 2.40f,
                    attackRange = 1.45f,
                    attackInterval = 1.05f,
                    splashRadius = 1.6f,
                    knockback = 1.3f,
                    isRanged = false,
                    isHealer = false,
                    isFlying = false,
                    superArmor = true,
                    primaryColor = Hex("24a86b"),
                    accentColor = Hex("ffcf4d"),
                    auraColor = Hex("ffd966"),
                    commanderSkills = new[]
                    {
                        new SkillSpec { id = "tsunade_s1", name = "통천각·성광파", description = "하늘에서 내리꽂는 금빛 발차기로 전방 중장갑 적들을 단숨에 분쇄합니다.", manaCost = 40, cooldown = 6.0f, range = 4.8f, radius = 3.2f, powerMultiplier = 3.8f, knockback = 2.5f, vfxStyle = 12 },
                        new SkillSpec { id = "tsunade_s2", name = "인법·창조재생 백호", description = "음봉인을 해방해 아군 전체 체력을 대폭 회복하고 6.5초간 무적급 보호막을 두릅니다.", manaCost = 60, cooldown = 12.0f, range = 8.0f, radius = 6.5f, powerMultiplier = 1.5f, healRatio = 0.40f, shieldSeconds = 6.5f, vfxStyle = 13 },
                        new SkillSpec { id = "tsunade_s3", name = "오의·천지개벽 일격", description = "호카게의 전력을 담은 일격으로 대지를 갈라 적군 전체에게 치명타를 입힙니다.", manaCost = 90, cooldown = 20.0f, range = 13.0f, radius = 9.0f, powerMultiplier = 6.2f, knockback = 3.4f, vfxStyle = 14 }
                    }
                },
                new UnitSpec
                {
                    slotIndex = 5,
                    id = "konan",
                    faction = FactionKind.Angel,
                    role = UnitRole.Archangel,
                    name = "코난",
                    title = "식지의 대천사 (8등신 비키니)",
                    angelClassOrigin = "Tier 6 · 대천사 거신 (Archangel Titan)",
                    costumeSummary = "네이비 블루 & 순백 투피스 비키니 · 종이 꽃 장식 · 거대 순백 종이 천사 날개",
                    foodCost = 580,
                    spawnCooldown = 16.0f,
                    maxHp = 1450f,
                    attack = 175f,
                    defense = 24f,
                    moveSpeed = 2.55f,
                    attackRange = 3.85f,
                    attackInterval = 1.15f,
                    splashRadius = 2.8f,
                    knockback = 1.6f,
                    isRanged = true,
                    isHealer = false,
                    isFlying = true,
                    superArmor = true,
                    primaryColor = Hex("3b55a8"),
                    accentColor = Hex("ffffff"),
                    auraColor = Hex("a8d8ff"),
                    commanderSkills = new[]
                    {
                        new SkillSpec { id = "konan_s1", name = "식지의 술·천사의 창", description = "순백의 종이 날개에서 수백 개의 빛의 창을 사출해 전열을 관통합니다.", manaCost = 45, cooldown = 6.5f, range = 9.0f, radius = 3.6f, powerMultiplier = 3.9f, knockback = 2.2f, vfxStyle = 15 },
                        new SkillSpec { id = "konan_s2", name = "신의 날개·성소 가호", description = "성스러운 종이 날개로 아군 전체를 감싸 치유하고 6초간 보호막을 부여합니다.", manaCost = 55, cooldown = 12.0f, range = 8.5f, radius = 7.0f, powerMultiplier = 1.6f, healRatio = 0.30f, shieldSeconds = 6.0f, vfxStyle = 16 },
                        new SkillSpec { id = "konan_s3", name = "신의 지자·6천억 기폭찰", description = "전장 전체를 갈라버리는 성광 기폭찰의 바다로 모든 악마 군단을 소멸시킵니다.", manaCost = 95, cooldown = 21.0f, range = 15.0f, radius = 10.5f, powerMultiplier = 6.8f, knockback = 3.6f, vfxStyle = 17 }
                    }
                }
            };
        }

        static UnitSpec[] CreateDemonUnits()
        {
            return new[]
            {
                new UnitSpec
                {
                    slotIndex = 0,
                    id = "imp",
                    faction = FactionKind.Demon,
                    role = UnitRole.Vanguard,
                    name = "블러드 임프 전사",
                    title = "심연의 선봉 투창병",
                    angelClassOrigin = "Tier 1 · 악마 보병",
                    costumeSummary = "진홍빛 피부와 흑요석 갑주 · 마그마 창과 박쥐 날개",
                    foodCost = 55,
                    spawnCooldown = 2.3f,
                    maxHp = 410f,
                    attack = 50f,
                    defense = 10f,
                    moveSpeed = 2.70f,
                    attackRange = 1.3f,
                    attackInterval = 0.84f,
                    splashRadius = 0.7f,
                    knockback = 0.4f,
                    isRanged = false,
                    isHealer = false,
                    isFlying = false,
                    superArmor = false,
                    primaryColor = Hex("c93434"),
                    accentColor = Hex("2d1418"),
                    auraColor = Hex("ff6a3d"),
                    commanderSkills = CreateDemonCommanderSkills("블러드 임프")
                },
                new UnitSpec
                {
                    slotIndex = 1,
                    id = "succubus",
                    faction = FactionKind.Demon,
                    role = UnitRole.Marksman,
                    name = "서큐버스 암흑사수",
                    title = "밤의 그림자 궁수",
                    angelClassOrigin = "Tier 2 · 악마 궁수",
                    costumeSummary = "다크 고딕 바이올렛 갑주 · 박쥐 날개 · 암흑 마력궁",
                    foodCost = 105,
                    spawnCooldown = 3.7f,
                    maxHp = 300f,
                    attack = 66f,
                    defense = 6f,
                    moveSpeed = 2.55f,
                    attackRange = 3.30f,
                    attackInterval = 0.92f,
                    splashRadius = 0.6f,
                    knockback = 0.25f,
                    isRanged = true,
                    isHealer = false,
                    isFlying = false,
                    superArmor = false,
                    primaryColor = Hex("8128a8"),
                    accentColor = Hex("ff4d94"),
                    auraColor = Hex("d65cff"),
                    commanderSkills = CreateDemonCommanderSkills("서큐버스")
                },
                new UnitSpec
                {
                    slotIndex = 2,
                    id = "darkpriest",
                    faction = FactionKind.Demon,
                    role = UnitRole.Priest,
                    name = "네크로맨서 리치",
                    title = "심연의 암흑 사제",
                    angelClassOrigin = "Tier 3 · 악마 사제",
                    costumeSummary = "칠흑과 핏빛 로브 · 해골 지팡이 · 영혼 불꽃 보주",
                    foodCost = 170,
                    spawnCooldown = 5.8f,
                    maxHp = 450f,
                    attack = 62f,
                    defense = 10f,
                    moveSpeed = 2.40f,
                    attackRange = 2.85f,
                    attackInterval = 1.20f,
                    splashRadius = 2.2f,
                    knockback = 0.6f,
                    isRanged = true,
                    isHealer = true,
                    isFlying = false,
                    superArmor = false,
                    primaryColor = Hex("4b1e6d"),
                    accentColor = Hex("a8ff52"),
                    auraColor = Hex("9e47ff"),
                    commanderSkills = CreateDemonCommanderSkills("네크로맨서")
                },
                new UnitSpec
                {
                    slotIndex = 3,
                    id = "hellknight",
                    faction = FactionKind.Demon,
                    role = UnitRole.Paladin,
                    name = "헬파이어 데스나이트",
                    title = "지옥불 중장갑 기사",
                    angelClassOrigin = "Tier 4 · 악마 중기병",
                    costumeSummary = "칠흑 가시 판금 갑주 · 심연의 방패 · 업화의 대검",
                    foodCost = 250,
                    spawnCooldown = 7.8f,
                    maxHp = 920f,
                    attack = 92f,
                    defense = 24f,
                    moveSpeed = 2.15f,
                    attackRange = 1.6f,
                    attackInterval = 1.05f,
                    splashRadius = 1.4f,
                    knockback = 1.15f,
                    isRanged = false,
                    isHealer = false,
                    isFlying = false,
                    superArmor = true,
                    primaryColor = Hex("331c24"),
                    accentColor = Hex("ff3d3d"),
                    auraColor = Hex("ff5226"),
                    commanderSkills = CreateDemonCommanderSkills("데스나이트")
                },
                new UnitSpec
                {
                    slotIndex = 4,
                    id = "lucifer",
                    faction = FactionKind.Demon,
                    role = UnitRole.Caster,
                    name = "타락천사 루시퍼",
                    title = "심연의 마왕 군단장",
                    angelClassOrigin = "Tier 5 · 마왕 지휘관",
                    costumeSummary = "여섯 장의 칠흑 날개 · 흑금 마왕 갑주 · 암흑 마검",
                    foodCost = 370,
                    spawnCooldown = 11.0f,
                    maxHp = 1150f,
                    attack = 134f,
                    defense = 26f,
                    moveSpeed = 2.25f,
                    attackRange = 3.8f,
                    attackInterval = 1.00f,
                    splashRadius = 2.1f,
                    knockback = 1.5f,
                    isRanged = true,
                    isHealer = false,
                    isFlying = true,
                    superArmor = true,
                    primaryColor = Hex("59153a"),
                    accentColor = Hex("ffae33"),
                    auraColor = Hex("ff2a6d"),
                    commanderSkills = CreateDemonCommanderSkills("루시퍼")
                },
                new UnitSpec
                {
                    slotIndex = 5,
                    id = "balrog",
                    faction = FactionKind.Demon,
                    role = UnitRole.Archangel,
                    name = "심연의 군주 발록",
                    title = "지옥불 최종 거신",
                    angelClassOrigin = "Tier 6 · 마신 거신",
                    costumeSummary = "용암이 흐르는 거대 마신 갑주 · 화염 날개 · 지옥 파멸검",
                    foodCost = 580,
                    spawnCooldown = 16.5f,
                    maxHp = 1520f,
                    attack = 180f,
                    defense = 26f,
                    moveSpeed = 2.10f,
                    attackRange = 2.4f,
                    attackInterval = 1.20f,
                    splashRadius = 2.9f,
                    knockback = 1.9f,
                    isRanged = false,
                    isHealer = false,
                    isFlying = false,
                    superArmor = true,
                    primaryColor = Hex("8f1d14"),
                    accentColor = Hex("ff7b1c"),
                    auraColor = Hex("ff3c00"),
                    commanderSkills = CreateDemonCommanderSkills("발록")
                }
            };
        }

        static SkillSpec[] CreateDemonCommanderSkills(string prefix)
        {
            return new[]
            {
                new SkillSpec { id = "demon_s1", name = prefix + "·지옥불 참격", description = "암흑 화염 파동을 날려 전방 적군을 불태우고 밀어냅니다.", manaCost = 40, cooldown = 6.0f, range = 6.5f, radius = 3.2f, powerMultiplier = 3.5f, knockback = 2.2f, vfxStyle = 12 },
                new SkillSpec { id = "demon_s2", name = prefix + "·피의 갈망", description = "아군 악마 군단 전체의 체력을 회복하고 암흑 보호막을 부여합니다.", manaCost = 55, cooldown = 11.5f, range = 8.0f, radius = 6.5f, powerMultiplier = 1.4f, healRatio = 0.32f, shieldSeconds = 5.5f, vfxStyle = 13 },
                new SkillSpec { id = "demon_s3", name = prefix + "·심연 메테오", description = "하늘에서 지옥불 운석을 쏟아부어 적 진영 전체를 초토화합니다.", manaCost = 90, cooldown = 19.5f, range = 14.0f, radius = 9.5f, powerMultiplier = 6.0f, knockback = 3.2f, vfxStyle = 14 }
            };
        }

        static StageSpec[] CreateStages()
        {
            var stages = new StageSpec[20];
            string[] angelTitles =
            {
                "제1장 · 천공의 성소 외곽",
                "제2장 · 별빛의 회랑",
                "제3장 · 유권과 백호의 숲",
                "제4장 · 모래폭풍의 협곡",
                "제5장 · 무너진 황혼의 성당",
                "제6장 · 혈월의 전초기지",
                "제7장 · 타오르는 용암 협곡",
                "제8장 · 칠흑의 데스나이트 요새",
                "제9장 · 타락천사의 내성",
                "제10장 · 심연의 왕좌: 루시퍼 결전"
            };
            string[] demonTitles =
            {
                "악마편 1장 · 심연의 반격",
                "악마편 2장 · 성소 외벽 돌파",
                "악마편 3장 · 히나타의 수호진",
                "악마편 4장 · 이노의 꽃보라 회랑",
                "악마편 5장 · 사쿠라의 치유 성전",
                "악마편 6장 · 테마리의 폭풍 협곡",
                "악마편 7장 · 츠나데의 금강 요새",
                "악마편 8장 · 코난의 종이천사 제단",
                "악마편 9장 · 천상의 대성당 진입",
                "악마편 10장 · 최종결전: 빛의 대신전 함락"
            };

            for (int i = 0; i < 10; i++)
            {
                stages[i] = new StageSpec
                {
                    stageNumber = i + 1,
                    campaignFaction = FactionKind.Angel,
                    title = angelTitles[i],
                    subtitle = $"천사 진영(나루토 비키니 군단) 작전 #{i + 1}",
                    environmentTheme = i < 3 ? "SkySanctuary" : i < 6 ? "TwilightForest" : i < 8 ? "LavaGorge" : "DemonCitadel",
                    enemyCastleHp = 3200f + i * 850f,
                    enemySpawnInterval = Mathf.Max(1.8f, 3.2f - i * 0.14f),
                    maxEnemyTier = Mathf.Min(5, 1 + i / 2),
                    bossSlotIndex = Mathf.Min(5, 1 + i / 2),
                    rewardGold = 350 + i * 130,
                    rewardGems = 3 + i / 3,
                    skyTop = i < 5 ? Hex("1e3563") : Hex("361428"),
                    skyBottom = i < 5 ? Hex("4a74b5") : Hex("752538"),
                    groundColor = i < 5 ? Hex("2b3c4d") : Hex("2d1e29")
                };
            }

            for (int i = 0; i < 10; i++)
            {
                int stageNum = 11 + i;
                stages[10 + i] = new StageSpec
                {
                    stageNumber = stageNum,
                    campaignFaction = FactionKind.Demon,
                    title = demonTitles[i],
                    subtitle = $"악마 진영(2부 해금 캠페인) 작전 #{i + 1}",
                    environmentTheme = i < 4 ? "TwilightForest" : i < 7 ? "SkySanctuary" : "HolyCathedral",
                    enemyCastleHp = 4200f + i * 950f,
                    enemySpawnInterval = Mathf.Max(1.6f, 2.8f - i * 0.12f),
                    maxEnemyTier = Mathf.Min(5, 2 + i / 2),
                    bossSlotIndex = Mathf.Min(5, 1 + i / 2),
                    rewardGold = 500 + i * 150,
                    rewardGems = 4 + i / 2,
                    skyTop = Hex("2c1742"),
                    skyBottom = Hex("6b388c"),
                    groundColor = Hex("29233b")
                };
            }

            return stages;
        }
    }
}
