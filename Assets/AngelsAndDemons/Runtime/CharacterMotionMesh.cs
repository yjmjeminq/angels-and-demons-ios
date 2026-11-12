using UnityEngine;

namespace AngelsAndDemons
{
    /// <summary>
    /// Renders each 8-head tall character using its 6-frame articulated animation atlas
    /// (<id>_atlas.png: 1152x1024, 3x2 grid of 384x512 frames: Idle, WalkA, WalkPass, WalkB, AttackWindup, AttackStrike)
    /// combined with smooth continuous breathing, running lean, combat lunge, and hit-flash on a 14x22 UV mesh.
    /// </summary>
    public sealed class CharacterMotionMesh : MonoBehaviour
    {
        public const int GridCols = 14;
        public const int GridRows = 22;

        static readonly int[] WalkSequence = { 1, 2, 3, 2 };

        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        Mesh mesh;
        Material instanceMaterial;

        Vector3[] baseVertices;
        Vector3[] deformedVertices;
        Vector2[] uvs;
        Color32[] colors;
        int[] triangles;

        string currentId;
        int lastFrameIndex = -1;
        float walkCycle;
        float lastX;
        bool hasLastX;
        float smoothMoveWeight;
        float smoothLean;

        public int VertexCount => deformedVertices != null ? deformedVertices.Length : 0;
        public int ActiveFrameIndex { get; private set; }
        public float LastStrikeDisposition { get; private set; }
        public float LastFootStrideOffset { get; private set; }

        public void Initialize(int sortingOrder)
        {
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            meshRenderer.sortingOrder = sortingOrder;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            int vCount = (GridCols + 1) * (GridRows + 1);
            baseVertices = new Vector3[vCount];
            deformedVertices = new Vector3[vCount];
            uvs = new Vector2[vCount];
            colors = new Color32[vCount];
            triangles = new int[GridCols * GridRows * 6];

            // Each frame cell is 384x512 (aspect = 0.75 width to 1.0 height); character height = 2.35 world units
            float worldH = 2.35f;
            float worldW = worldH * 0.75f;
            float pivotY = 44f / 1024f; // feet line

            int vIdx = 0;
            for (int y = 0; y <= GridRows; y++)
            {
                float ny = y / (float)GridRows;
                float py = (ny - pivotY) * worldH;
                for (int x = 0; x <= GridCols; x++)
                {
                    float nx = x / (float)GridCols;
                    float px = (nx - 0.5f) * worldW;
                    baseVertices[vIdx] = new Vector3(px, py, 0f);
                    deformedVertices[vIdx] = baseVertices[vIdx];
                    uvs[vIdx] = new Vector2(nx / 3f, 0.5f + ny * 0.5f);
                    colors[vIdx] = new Color32(255, 255, 255, 255);
                    vIdx++;
                }
            }

            int tIdx = 0;
            for (int y = 0; y < GridRows; y++)
            {
                for (int x = 0; x < GridCols; x++)
                {
                    int i0 = y * (GridCols + 1) + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + (GridCols + 1);
                    int i3 = i2 + 1;

                    triangles[tIdx++] = i0;
                    triangles[tIdx++] = i2;
                    triangles[tIdx++] = i1;

                    triangles[tIdx++] = i1;
                    triangles[tIdx++] = i2;
                    triangles[tIdx++] = i3;
                }
            }

            mesh = new Mesh { name = "CharacterMotionMesh" };
            mesh.MarkDynamic();
            mesh.vertices = baseVertices;
            mesh.uv = uvs;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            meshFilter.sharedMesh = mesh;
        }

        public void SamplePose(UnitEntity u, float dt, int sortingOrder)
        {
            if (mesh == null) Initialize(sortingOrder);
            meshRenderer.sortingOrder = sortingOrder;

            var tex = DefenseArt.GetCharacterAtlasTexture(u.spec.id);
            if ((currentId != u.spec.id || instanceMaterial == null) && tex != null)
            {
                currentId = u.spec.id;
                lastFrameIndex = -1;
                if (instanceMaterial == null)
                {
                    instanceMaterial = DefenseArt.CreateCharacterMaterial(tex);
                    meshRenderer.sharedMaterial = instanceMaterial;
                }
                else
                {
                    instanceMaterial.mainTexture = tex;
                    if (instanceMaterial.HasProperty("_MainTex"))
                        instanceMaterial.SetTexture("_MainTex", tex);
                }
            }

            if (instanceMaterial != null && instanceMaterial.HasProperty("_Flash"))
            {
                instanceMaterial.SetFloat("_Flash", u.hurtFlashTimer > 0f ? 0.55f : 0f);
            }

            if (!hasLastX)
            {
                lastX = u.x;
                hasLastX = true;
            }
            float dx = u.x - lastX;
            lastX = u.x;

            float speed = dt > 0.0001f ? Mathf.Abs(dx) / dt : 0f;
            float targetMove = Mathf.Clamp01(speed / Mathf.Max(0.35f, u.moveSpeed * 0.55f));
            if (u.attackAnimTimer > 0.02f) targetMove = 0f;
            smoothMoveWeight = Mathf.Lerp(smoothMoveWeight, targetMove, 1f - Mathf.Exp(-16f * Mathf.Max(0.01f, dt)));
            walkCycle = u.animPhase;

            // All 12 character atlases are normalized so the character faces RIGHT (+X)!
            bool wantsFaceRight = u.isAlly;
            if (u.isCommander && Mathf.Abs(dx) > 0.002f)
            {
                wantsFaceRight = dx > 0f;
            }
            float flipX = wantsFaceRight ? 1f : -1f;
            float faceDir = wantsFaceRight ? 1f : -1f;

            // Attack phase: attackAnimTimer counts down from 0.48 -> 0
            float atkProgress = u.attackAnimTimer > 0f ? 1f - Mathf.Clamp01(u.attackAnimTimer / 0.48f) : 0f;
            float windup = 0f;
            float strike = 0f;
            int frameIndex = 0;

            if (u.attackAnimTimer > 0f)
            {
                if (atkProgress < 0.38f)
                {
                    windup = Mathf.SmoothStep(0f, 1f, atkProgress / 0.38f);
                    frameIndex = 4; // AttackWindup frame in atlas
                }
                else
                {
                    strike = Mathf.Sin(((atkProgress - 0.38f) / 0.62f) * Mathf.PI);
                    frameIndex = 5; // AttackStrike frame in atlas
                }
            }
            else if (smoothMoveWeight > 0.12f || speed > 0.08f)
            {
                int step = Mathf.FloorToInt(walkCycle * 1.45f) & 3;
                frameIndex = WalkSequence[step]; // 1: WalkA -> 2: WalkPass -> 3: WalkB -> 2: WalkPass
            }
            else
            {
                frameIndex = 0; // Idle
            }

            ActiveFrameIndex = frameIndex;
            LastStrikeDisposition = frameIndex == 5 ? (0.45f + strike * 0.55f) : (strike - windup * 0.45f);
            LastFootStrideOffset = (frameIndex == 1 || frameIndex == 3) ? 0.28f : (frameIndex == 2 ? 0.14f : 0f);

            // Update atlas cell UVs when frame changes (3 columns x 2 rows)
            if (frameIndex != lastFrameIndex)
            {
                lastFrameIndex = frameIndex;
                int col = frameIndex % 3;
                int row = frameIndex / 3;
                float uMin = (col + 0.004f) / 3f;
                float uMax = (col + 0.996f) / 3f;
                // Row 0 in top-down PNG is top half of texture in Unity UV space (v: 0.5..1.0)
                float vMin = row == 0 ? 0.502f : 0.002f;
                float vMax = row == 0 ? 0.998f : 0.498f;

                int uvIdx = 0;
                for (int y = 0; y <= GridRows; y++)
                {
                    float ny = y / (float)GridRows;
                    float v = Mathf.Lerp(vMin, vMax, ny);
                    for (int x = 0; x <= GridCols; x++)
                    {
                        float nx = x / (float)GridCols;
                        float uCoord = Mathf.Lerp(uMin, uMax, nx);
                        uvs[uvIdx++] = new Vector2(uCoord, v);
                    }
                }
                mesh.uv = uvs;
            }

            // Dynamic attack dynamics: strong forward torso stretch, anticipation squash, impact recoil
            float recoil = u.hurtFlashTimer > 0f ? Mathf.Sin((u.hurtFlashTimer / 0.16f) * Mathf.PI) : 0f;
            float targetLean = (smoothMoveWeight * 2.2f - windup * 6.5f + strike * 12.5f - recoil * 9.0f) * faceDir;
            smoothLean = Mathf.Lerp(smoothLean, targetLean, 1f - Mathf.Exp(-28f * Mathf.Max(0.01f, dt)));
            float leanRad = -smoothLean * Mathf.Deg2Rad;
            float cosL = Mathf.Cos(leanRad);
            float sinL = Mathf.Sin(leanRad);

            // Character-specific attack stretch & squash
            float forwardStretch = (strike * 0.22f - windup * 0.10f) * faceDir;
            float verticalSquash = -windup * 0.07f + strike * 0.08f;

            Color32 vertColor = u.hurtFlashTimer > 0f
                ? new Color32(255, 175, 175, 255)
                : (strike > 0.4f ? new Color32(255, 255, 230, 255) : new Color32(255, 255, 255, 255));

            int idx = 0;
            for (int y = 0; y <= GridRows; y++)
            {
                float ny = y / (float)GridRows;
                float torsoWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.30f, 0.72f, ny));
                float headWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.68f, 1.0f, ny));
                float breath = Mathf.Sin(walkCycle * 0.7f + u.instanceId) * 0.016f * torsoWeight;

                for (int x = 0; x <= GridCols; x++)
                {
                    Vector3 b = baseVertices[idx];
                    float px = b.x * flipX;
                    float py = b.y;

                    float waistY = 0.92f;
                    float rx = px * cosL - (py - waistY) * sinL;
                    float ry = px * sinL + (py - waistY) * cosL + waistY;
                    px = Mathf.Lerp(px, rx, torsoWeight);
                    py = Mathf.Lerp(py, ry, torsoWeight) + breath;

                    // Apply explosive attack stretch to torso, arms, and head!
                    px += forwardStretch * torsoWeight;
                    py += verticalSquash * headWeight;

                    deformedVertices[idx] = new Vector3(px, py, 0f);
                    colors[idx] = vertColor;
                    idx++;
                }
            }

            mesh.vertices = deformedVertices;
            mesh.colors32 = colors;
            mesh.colors32 = colors;
        }
    }
}
