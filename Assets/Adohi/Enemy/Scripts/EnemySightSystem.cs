using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Clipper2Lib;
using LibTessDotNet;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using com.cyborgAssets.inspectorButtonPro;
using static ZombieRun.Adohi.Enemy.Enemy;

namespace ZombieRun.Adohi.Enemy
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class EnemySightSystem : MonoBehaviour
    {
        [Header("시야 설정")]
        public Transform sightOrigin;
        [SerializeField] private float width = 4f;
        [SerializeField] private float distance = 5f;
        [SerializeField] private float maxDistance = 5f;
        [SerializeField] private float leftAngle = 0f;
        [SerializeField] private float rightAngle = 0f;
        [SerializeField] private Color sightColor = new Color(1f, 0f, 0f, 0.3f);

        [Header("블렌드 모드")]
        [SerializeField] private BlendMode blendMode = BlendMode.Alpha;

        [Header("Emission 설정")]
        [SerializeField] private bool useEmission = true;
        [SerializeField] private Color emissionColor = new Color(1f, 0f, 0f, 1f);
        [SerializeField][Range(0f, 10f)] private float emissionIntensity = 2f;

        public enum BlendMode
        {
            Alpha,      // 기본 알파 블렌딩
            Additive,   // 가산 블렌딩
            Screen,     // 스크린 블렌딩 (포토샵 Screen)
            Multiply    // 곱셈 블렌딩
        }

        [Header("레이캐스트 설정")]
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float shadowLength = 1f;

        [Header("Clipper2 설정")]
        [SerializeField] private bool useClipper2 = true;  // Clipper2 사용 여부

        [Header("2D 렌더링 설정")]
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 0;
        [SerializeField] private Texture2D sightTexture = null;  // 텍스처 (선택사항)
        [SerializeField] private float textureScale = 1f;  // 텍스처 타일링 크기 (1 = 1유닛당 1타일)

        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private Enemy enemy;
        private bool isAttacking = false;

        // 그림자 사다리꼴 정의 (로컬 좌표)
        struct ShadowTrapezoid
        {
            public Vector2 topLeft;
            public Vector2 topRight;
            public Vector2 bottomLeft;
            public Vector2 bottomRight;
        }

        public void Initialize(Enemy enemy)
        {
            this.enemy = enemy;
        }

        void Start()
        {
            // sightOrigin이 설정되지 않았으면 자기 자신을 사용
            if (sightOrigin == null)
            {
                sightOrigin = transform;
            }

            SetAmplify(Random.Range(1f, 1.5f));
            OffsetAngle(Random.Range(-10f, 10f));

            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            mesh = new Mesh();
            meshFilter.mesh = mesh;

            // 블렌드 모드에 따라 적절한 셰이더 선택
            Shader shader = GetShaderForBlendMode(blendMode);

            meshRenderer.material = new Material(shader);

            // 블렌드 모드에 따라 색상 처리
            Color finalColor = sightColor;
            if (blendMode == BlendMode.Screen || blendMode == BlendMode.Additive)
            {
                // Screen/Additive 모드는 밝기 기반이므로 알파가 낮아도 효과적
                // 하지만 색상 자체는 그대로 사용 (사용자가 조정 가능하도록)
            }
            meshRenderer.material.color = finalColor;

            // 블렌드 모드 설정 (추가 설정)
            ApplyBlendMode(meshRenderer.material, blendMode);

            // Emission 설정
            if (useEmission)
            {
                meshRenderer.material.EnableKeyword("_EMISSION");
                Color finalEmissionColor = emissionColor * emissionIntensity;
                meshRenderer.material.SetColor("_EmissionColor", finalEmissionColor);
            }

            // 텍스처 적용
            if (sightTexture != null)
            {
                // 타일링을 위해 Wrap Mode를 Repeat로 설정
                sightTexture.wrapMode = TextureWrapMode.Repeat;
                meshRenderer.material.mainTexture = sightTexture;
            }

            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = sortingOrder;



            CreateRectangle();
        }

        void Update()
        {
            CreateRectangle();

            if (isAttacking && !enemy.isAttackPlayer)
            {
                switch (enemy.enemyType)
                {
                    case EnemyType.Soilder:
                        //인비저블 조건으로 다 바꿔야함
                        if (IsVisible(EnemyManager.Instance.player.Value.transform.position))
                        {
                            Debug.Log("Player is visible");
                            enemy.isAttackPlayer = true;
                        }
                        else
                        {
                            Debug.Log("Player is invisible");
                        }
                        break;
                    case EnemyType.Grandma:
                        //인비저블 조건으로 다 바꿔야함
                        if (!IsVisible(EnemyManager.Instance.player.Value.transform.position))
                        {
                            Debug.Log("Player is visible");
                            enemy.isAttackPlayer = true;
                        }
                        else
                        {
                            Debug.Log("Player is invisible");
                        }
                        break;
                    case EnemyType.Teacher:
                        //인비저블 조건으로 다 바꿔야함
                        if (IsVisible(EnemyManager.Instance.player.Value.transform.position))
                        {
                            Debug.Log("Player is visible");
                            enemy.isAttackPlayer = true;
                        }
                        else
                        {
                            Debug.Log("Player is invisible");
                        }
                        break;
                }
            }
        }

        void OnDestroy()
        {
            currentTween?.Kill();
        }

        public void SetAmplify(float amplify)
        {
            leftAngle = leftAngle * amplify;
            rightAngle = rightAngle * amplify;
        }

        public void OffsetAngle(float offset)
        {
            leftAngle = leftAngle + offset;
            rightAngle = rightAngle + offset;
        }




        private Tween currentTween;

        // t초 동안 distance를 0에서 maxDistance까지 증가
        [ProButton]
        public async UniTask DoSight(float duration, Ease ease = Ease.OutQuad)
        {
            currentTween?.Kill();
            isAttacking = true;
            distance = 0f;
            currentTween = DOTween.To(() => distance, x => distance = x, maxDistance, duration)
                .SetEase(ease);
            await currentTween.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
            await UniTask.Delay(500);
            isAttacking = false;
            distance = 0f;
        }

        // t초 동안 distance를 현재값에서 0으로 감소
        public async UniTask HideSight(float duration, Ease ease = Ease.InQuad)
        {
            currentTween?.Kill();
            currentTween = DOTween.To(() => distance, x => distance = x, 0f, duration)
                .SetEase(ease);
            await currentTween.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        // 즉시 시야 설정
        public void SetSightDistance(float dist)
        {
            currentTween?.Kill();
            distance = Mathf.Clamp(dist, 0f, maxDistance);
        }

        // 시야 토글
        public async UniTask ToggleSight(float duration, Ease ease = Ease.InOutQuad)
        {
            if (distance > maxDistance * 0.5f)
            {
                await HideSight(duration, ease);
            }
            else
            {
                await DoSight(duration, ease);
            }
        }

        // Emission 강도 설정
        public void SetEmissionIntensity(float intensity)
        {
            emissionIntensity = intensity;
            if (meshRenderer != null && meshRenderer.material != null && useEmission)
            {
                Color finalEmissionColor = emissionColor * emissionIntensity;
                meshRenderer.material.SetColor("_EmissionColor", finalEmissionColor);
            }
        }

        // Emission 컬러 설정
        public void SetEmissionColor(Color color)
        {
            emissionColor = color;
            if (meshRenderer != null && meshRenderer.material != null && useEmission)
            {
                Color finalEmissionColor = emissionColor * emissionIntensity;
                meshRenderer.material.SetColor("_EmissionColor", finalEmissionColor);
            }
        }

        // 블렌드 모드에 적합한 셰이더 선택
        private Shader GetShaderForBlendMode(BlendMode mode)
        {
            // 커스텀 블렌드 셰이더 사용 (블렌드 모드 파라미터 지원)
            Shader shader = Shader.Find("Custom/BlendModeShader");

            // 폴백: Sprites/Default
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
                Debug.LogWarning("Custom/BlendModeShader를 찾을 수 없습니다. Sprites/Default를 사용합니다.");
            }

            return shader;
        }

        // 블렌드 모드 적용
        private void ApplyBlendMode(Material material, BlendMode mode)
        {
            switch (mode)
            {
                case BlendMode.Alpha:
                    // 표준 알파 블렌딩: SrcAlpha OneMinusSrcAlpha
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;

                case BlendMode.Additive:
                    // 가산 블렌딩: SrcAlpha One
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    break;

                case BlendMode.Screen:
                    // 스크린 블렌딩 (포토샵 Screen): One OneMinusSrcColor
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
                    break;

                case BlendMode.Multiply:
                    // 곱셈 블렌딩: DstColor Zero
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    break;
            }

            material.SetInt("_ZWrite", 0);
            material.renderQueue = 3000;

            Debug.Log($"블렌드 모드 적용: {mode}, 셰이더: {material.shader.name}, SrcBlend: {material.GetInt("_SrcBlend")}, DstBlend: {material.GetInt("_DstBlend")}");
        }

        // 블렌드 모드 변경 (런타임)
        public void SetBlendMode(BlendMode mode)
        {
            blendMode = mode;
            if (meshRenderer != null)
            {
                // 셰이더를 새로 선택하고 머티리얼 재생성
                Shader shader = GetShaderForBlendMode(mode);
                Color prevColor = meshRenderer.material.color;
                Texture prevTexture = meshRenderer.material.mainTexture;

                meshRenderer.material = new Material(shader);
                meshRenderer.material.color = prevColor;
                if (prevTexture != null)
                    meshRenderer.material.mainTexture = prevTexture;

                ApplyBlendMode(meshRenderer.material, mode);

                // Emission 재적용
                if (useEmission)
                {
                    meshRenderer.material.EnableKeyword("_EMISSION");
                    Color finalEmissionColor = emissionColor * emissionIntensity;
                    meshRenderer.material.SetColor("_EmissionColor", finalEmissionColor);
                }
            }
        }

        void CreateRectangle()
        {
            // 메시 오브젝트의 위치와 회전을 sightOrigin과 동기화
            if (sightOrigin != null && sightOrigin != transform)
            {
                transform.position = sightOrigin.position;
                transform.rotation = sightOrigin.rotation;
            }

            mesh.Clear();

            float halfWidth = width / 2f;
            float leftAngleRad = leftAngle * Mathf.Deg2Rad;
            float rightAngleRad = rightAngle * Mathf.Deg2Rad;

            // 사다리꼴 시야 영역
            Vector2 topLeft = new Vector2(-halfWidth, 0);
            Vector2 topRight = new Vector2(halfWidth, 0);

            // 각도 방향으로 광선을 쏴서 y = -distance 평면과의 교차점 찾기
            Vector2 leftDir = new Vector2(Mathf.Sin(leftAngleRad), -Mathf.Cos(leftAngleRad));
            Vector2 rightDir = new Vector2(Mathf.Sin(rightAngleRad), -Mathf.Cos(rightAngleRad));

            // topLeft + leftDir * t 가 y = -distance에 도달하는 t 계산
            // 0 + leftDir.y * t = -distance => t = -distance / leftDir.y
            float leftT = leftDir.y != 0 ? -distance / leftDir.y : distance;
            float rightT = rightDir.y != 0 ? -distance / rightDir.y : distance;

            Vector2 bottomLeft = topLeft + leftDir * leftT;
            Vector2 bottomRight = topRight + rightDir * rightT;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            // 1단계: 박스콜라이더 찾아서 그림자 사다리꼴로 변환
            List<ShadowTrapezoid> shadowTrapezoids = new List<ShadowTrapezoid>();

            Collider2D[] colliders = Physics2D.OverlapBoxAll(
                sightOrigin.position + sightOrigin.TransformDirection(new Vector2(0, -distance / 2f)),
                new Vector2(width + 2f, distance + 2f),
                sightOrigin.eulerAngles.z,
                obstacleLayer
            );

            // 시야의 진짜 원점 = 사다리꼴 양쪽 변을 위로 연장했을 때 만나는 점
            Vector2 origin = CalculateSightOrigin(topLeft, topRight, bottomLeft, bottomRight);

            foreach (Collider2D col in colliders)
            {
                if (col is BoxCollider2D boxCol)
                {
                    // 박스의 중심 (월드 좌표)
                    Vector2 boxWorldCenter = (Vector2)col.transform.position + boxCol.offset;
                    Vector2 boxSize = boxCol.size;

                    // 박스의 4개 코너를 박스 로컬 좌표계에서 계산
                    Vector2[] boxLocalCorners = new Vector2[]
                    {
                    new Vector2(-boxSize.x / 2f, -boxSize.y / 2f), // 좌하
                    new Vector2(boxSize.x / 2f, -boxSize.y / 2f),  // 우하
                    new Vector2(-boxSize.x / 2f, boxSize.y / 2f),  // 좌상
                    new Vector2(boxSize.x / 2f, boxSize.y / 2f)    // 우상
                    };

                    // 각 코너를 박스의 회전과 스케일을 적용하여 월드 좌표로 변환
                    Vector2[] worldCorners = new Vector2[4];
                    for (int i = 0; i < 4; i++)
                    {
                        // 스케일 적용
                        Vector2 scaled = new Vector2(
                            boxLocalCorners[i].x * col.transform.lossyScale.x,
                            boxLocalCorners[i].y * col.transform.lossyScale.y
                        );

                        // 회전 적용 (2D 회전 행렬)
                        float angle = col.transform.eulerAngles.z * Mathf.Deg2Rad;
                        float cos = Mathf.Cos(angle);
                        float sin = Mathf.Sin(angle);
                        Vector2 rotated = new Vector2(
                            scaled.x * cos - scaled.y * sin,
                            scaled.x * sin + scaled.y * cos
                        );

                        // 월드 좌표로 변환
                        worldCorners[i] = boxWorldCenter + rotated;
                    }

                    // 월드 좌표를 메시 로컬 좌표로 변환 (메시는 이 오브젝트에 그려짐)
                    Vector2 cornerBL = transform.InverseTransformPoint(worldCorners[0]);
                    Vector2 cornerBR = transform.InverseTransformPoint(worldCorners[1]);
                    Vector2 cornerTL = transform.InverseTransformPoint(worldCorners[2]);
                    Vector2 cornerTR = transform.InverseTransformPoint(worldCorners[3]);

                    // 박스 중심도 메시 로컬 좌표로 변환
                    Vector2 boxLocalCenter = transform.InverseTransformPoint(boxWorldCenter);

                    // 박스가 원점보다 아래에 있는지 확인 (시야 범위 내에 있어야 함)
                    if (boxLocalCenter.y >= origin.y)
                    {
                        Debug.LogWarning($"박스가 시야 원점보다 위에 있음: {col.name}, boxY={boxLocalCenter.y}, originY={origin.y}");
                        continue; // 원점보다 위는 무시
                    }

                    // 원점에서 볼 때 박스의 왼쪽 끝과 오른쪽 끝 코너 찾기
                    // 4개 코너 중 원점에서 봤을 때 가장 왼쪽, 오른쪽에 있는 코너
                    Vector2[] corners = { cornerBL, cornerBR, cornerTL, cornerTR };

                    // 원점 기준으로 각도 계산하여 가장 왼쪽/오른쪽 코너 찾기
                    float minAngle = float.MaxValue;
                    float maxAngle = float.MinValue;
                    Vector2 leftCorner = cornerTL;
                    Vector2 rightCorner = cornerTR;

                    foreach (var corner in corners)
                    {
                        Vector2 dir = corner - origin;
                        float angle = Mathf.Atan2(dir.x, -dir.y); // x기준 각도 (왼쪽이 음수)

                        if (angle < minAngle)
                        {
                            minAngle = angle;
                            leftCorner = corner;
                        }
                        if (angle > maxAngle)
                        {
                            maxAngle = angle;
                            rightCorner = corner;
                        }
                    }

                    // 원점에서 각 코너로 향하는 방향
                    Vector2 dirLeft = (leftCorner - origin).normalized;
                    Vector2 dirRight = (rightCorner - origin).normalized;

                    // 그림자 끝점 계산 (원점 기준으로!)
                    float hitDistLeft = Vector2.Distance(origin, leftCorner);
                    float hitDistRight = Vector2.Distance(origin, rightCorner);
                    float shadowEndDistLeft = hitDistLeft + shadowLength;
                    float shadowEndDistRight = hitDistRight + shadowLength;

                    ShadowTrapezoid shadow;
                    shadow.topLeft = leftCorner;
                    shadow.topRight = rightCorner;
                    shadow.bottomLeft = origin + dirLeft * shadowEndDistLeft;
                    shadow.bottomRight = origin + dirRight * shadowEndDistRight;

                    shadowTrapezoids.Add(shadow);

                    Debug.Log($"박스: {col.name}, 로컬중심: {boxLocalCenter}, 그림자: TL={shadow.topLeft}, TR={shadow.topRight}, BL={shadow.bottomLeft}, BR={shadow.bottomRight}");
                }
            }

            // 2단계: Clipper2로 시야 생성 (그림자 영역 제외)
            CreateSightWithClipper(topLeft, topRight, bottomLeft, bottomRight, shadowTrapezoids, vertices, triangles, uvs);

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
        }

        // Clipper2를 사용한 시야 생성 (그림자 영역 제외)
        void CreateSightWithClipper(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight,
                                    List<ShadowTrapezoid> shadows, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            // 1. 시야 폴리곤
            PathD sight = new PathD {
            new PointD(topLeft.x, topLeft.y),
            new PointD(bottomLeft.x, bottomLeft.y),
            new PointD(bottomRight.x, bottomRight.y),
            new PointD(topRight.x, topRight.y)
        };

            Debug.Log($"[시야] TL({topLeft.x:F2},{topLeft.y:F2}) BL({bottomLeft.x:F2},{bottomLeft.y:F2}) BR({bottomRight.x:F2},{bottomRight.y:F2}) TR({topRight.x:F2},{topRight.y:F2})");

            // 2. 그림자 폴리곤들
            PathsD shadowList = new PathsD();
            int shadowIdx = 0;
            foreach (var s in shadows)
            {
                // 실제 y 좌표 기준으로 정렬 (위에서 아래로: y 큰 값 -> 작은 값)
                // topLeft/topRight가 실제로는 박스 위치 (y 작음)
                // bottomLeft/bottomRight가 그림자 끝 (y 더 작음)
                // 그래서 순서를 바꿔야 함
                Debug.Log($"[그림자#{shadowIdx}] TL({s.topLeft.x:F2},{s.topLeft.y:F2}) TR({s.topRight.x:F2},{s.topRight.y:F2}) BL({s.bottomLeft.x:F2},{s.bottomLeft.y:F2}) BR({s.bottomRight.x:F2},{s.bottomRight.y:F2})");

                shadowList.Add(new PathD {
                new PointD(s.topLeft.x, s.topLeft.y),
                new PointD(s.topRight.x, s.topRight.y),
                new PointD(s.bottomRight.x, s.bottomRight.y),
                new PointD(s.bottomLeft.x, s.bottomLeft.y)
            });
                shadowIdx++;
            }

            // 3. 차집합 연산
            PathsD result = Clipper.Difference(new PathsD { sight }, shadowList, FillRule.NonZero);
            if (result.Count == 0) result.Add(sight);

            Debug.Log($"차집합 결과: {result.Count}개 경로");

            // 4. 외곽 폴리곤들과 구멍들 분리 및 그룹화
            List<PathD> outers = new List<PathD>();
            List<PathD> holes = new List<PathD>();

            foreach (var path in result)
            {
                if (path.Count < 3) continue;

                double area = Clipper.Area(path);
                Debug.Log($"경로: {path.Count}꼭지점, Area={area:F4}");

                if (area > 0)
                {
                    outers.Add(path);
                    Debug.Log("  -> 외곽 폴리곤");
                }
                else
                {
                    holes.Add(path);
                    Debug.Log("  -> 구멍 폴리곤");
                }
            }

            // 5. 각 외곽 폴리곤에 대해 메시 생성
            Debug.Log($"총 {outers.Count}개의 독립된 폴리곤 발견");

            foreach (var outer in outers)
            {
                // 이 외곽 폴리곤에 속하는 구멍들 찾기 (간단히 모든 구멍 포함)
                // 더 정확한 구현: 각 구멍이 어느 외곽에 속하는지 판별 필요
                if (holes.Count == 0)
                {
                    // 구멍 없음 - 단순 삼각분할
                    Debug.Log($"폴리곤 삼각분할 (구멍 없음): {outer.Count}꼭지점");
                    Vector2[] points2D = new Vector2[outer.Count];
                    for (int i = 0; i < outer.Count; i++)
                        points2D[i] = new Vector2((float)outer[i].x, (float)outer[i].y);

                    Triangulator tr = new Triangulator(points2D);
                    int[] tris = tr.Triangulate();

                    int vStart = vertices.Count;
                    foreach (var p in points2D)
                    {
                        vertices.Add(new Vector3(p.x, p.y, 0));
                        // UV 계산 (월드 좌표 기반 타일링)
                        float u = p.x / textureScale;
                        float v = p.y / textureScale;
                        uvs.Add(new Vector2(u, v));
                    }

                    foreach (var idx in tris)
                        triangles.Add(vStart + idx);

                    Debug.Log($"  -> {tris.Length / 3}개 삼각형");
                }
                else
                {
                    // 첫 번째 외곽 폴리곤에만 구멍을 적용 (더 정확한 로직 필요 시 개선)
                    if (outers.Count == 1 || outer == outers[0])
                    {
                        // 구멍 있음 - LibTessDotNet 사용
                        Debug.Log($"폴리곤 삼각분할 (구멍 {holes.Count}개 포함)");
                        TriangulateWithLibTess(outer, holes, vertices, triangles, uvs);
                    }
                    else
                    {
                        // 나머지 폴리곤은 구멍 없이 처리
                        Debug.Log($"폴리곤 삼각분할 (구멍 없이): {outer.Count}꼭지점");
                        Vector2[] points2D = new Vector2[outer.Count];
                        for (int i = 0; i < outer.Count; i++)
                            points2D[i] = new Vector2((float)outer[i].x, (float)outer[i].y);

                        Triangulator tr = new Triangulator(points2D);
                        int[] tris = tr.Triangulate();

                        int vStart = vertices.Count;
                        foreach (var p in points2D)
                        {
                            vertices.Add(new Vector3(p.x, p.y, 0));
                            float u = p.x / textureScale;
                            float v = p.y / textureScale;
                            uvs.Add(new Vector2(u, v));
                        }

                        foreach (var idx in tris)
                            triangles.Add(vStart + idx);

                        Debug.Log($"  -> {tris.Length / 3}개 삼각형");
                    }
                }
            }
        }



        // LibTessDotNet을 사용한 구멍 처리 삼각분할
        void TriangulateWithLibTess(PathD outer, List<PathD> holes, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            var tess = new Tess();

            // 외곽 추가
            var outerContour = new ContourVertex[outer.Count];
            for (int i = 0; i < outer.Count; i++)
            {
                outerContour[i].Position = new Vec3 { X = (float)outer[i].x, Y = (float)outer[i].y, Z = 0 };
            }
            tess.AddContour(outerContour, ContourOrientation.CounterClockwise);

            // 구멍들 추가
            foreach (var hole in holes)
            {
                var holeContour = new ContourVertex[hole.Count];
                for (int i = 0; i < hole.Count; i++)
                {
                    holeContour[i].Position = new Vec3 { X = (float)hole[i].x, Y = (float)hole[i].y, Z = 0 };
                }
                // 구멍은 반대 방향 (Clockwise)
                tess.AddContour(holeContour, ContourOrientation.Clockwise);
            }

            // 삼각분할
            tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

            // 버텍스 및 UV 추가
            int vStart = vertices.Count;
            for (int i = 0; i < tess.VertexCount; i++)
            {
                var v = tess.Vertices[i].Position;
                vertices.Add(new Vector3(v.X, v.Y, 0));

                // UV 계산 (월드 좌표 기반 타일링)
                float u = v.X / textureScale;
                float uv_v = v.Y / textureScale;
                uvs.Add(new Vector2(u, uv_v));
            }

            // 삼각형 인덱스 추가
            for (int i = 0; i < tess.ElementCount; i++)
            {
                triangles.Add(vStart + tess.Elements[i * 3 + 0]);
                triangles.Add(vStart + tess.Elements[i * 3 + 1]);
                triangles.Add(vStart + tess.Elements[i * 3 + 2]);
            }

            Debug.Log($"LibTess 완료: {tess.VertexCount}버텍스, {tess.ElementCount}삼각형");
        }

        // 사다리꼴의 진짜 원점 계산 (양쪽 변의 연장선이 만나는 점)
        Vector2 CalculateSightOrigin(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight)
        {
            Vector2 d1 = bottomLeft - topLeft;
            Vector2 d2 = bottomRight - topRight;

            float denom = d1.x * d2.y - d1.y * d2.x;

            if (Mathf.Abs(denom) < 0.0001f)
            {
                return new Vector2((topLeft.x + topRight.x) / 2f, 0);
            }

            float t = ((topRight.x - topLeft.x) * d2.y - (topRight.y - topLeft.y) * d2.x) / denom;

            Vector2 origin = topLeft + t * d1;
            return origin;
        }

        // 월드 좌표가 시야 사다리꼴 안에 있는지 판별
        public bool IsInSight(Vector3 worldPosition)
        {
            // 월드 좌표를 로컬 좌표로 변환
            Vector2 localPos = transform.InverseTransformPoint(worldPosition);

            // 현재 시야 사다리꼴 계산
            float halfWidth = width / 2f;
            float leftAngleRad = leftAngle * Mathf.Deg2Rad;
            float rightAngleRad = rightAngle * Mathf.Deg2Rad;

            Vector2 topLeft = new Vector2(-halfWidth, 0);
            Vector2 topRight = new Vector2(halfWidth, 0);

            Vector2 leftDir = new Vector2(Mathf.Sin(leftAngleRad), -Mathf.Cos(leftAngleRad));
            Vector2 rightDir = new Vector2(Mathf.Sin(rightAngleRad), -Mathf.Cos(rightAngleRad));

            float leftT = leftDir.y != 0 ? -distance / leftDir.y : distance;
            float rightT = rightDir.y != 0 ? -distance / rightDir.y : distance;

            Vector2 bottomLeft = topLeft + leftDir * leftT;
            Vector2 bottomRight = topRight + rightDir * rightT;

            // 사다리꼴 내부 판정
            return IsPointInTrapezoid(localPos, topLeft, topRight, bottomRight, bottomLeft);
        }

        // 월드 좌표가 그림자 사다리꼴 안에 있는지 판별
        public bool IsInShadow(Vector3 worldPosition)
        {
            // 월드 좌표를 로컬 좌표로 변환
            Vector2 localPos = transform.InverseTransformPoint(worldPosition);

            // 현재 그림자들 계산
            float halfWidth = width / 2f;
            float leftAngleRad = leftAngle * Mathf.Deg2Rad;
            float rightAngleRad = rightAngle * Mathf.Deg2Rad;

            Vector2 topLeft = new Vector2(-halfWidth, 0);
            Vector2 topRight = new Vector2(halfWidth, 0);

            Vector2 leftDir = new Vector2(Mathf.Sin(leftAngleRad), -Mathf.Cos(leftAngleRad));
            Vector2 rightDir = new Vector2(Mathf.Sin(rightAngleRad), -Mathf.Cos(rightAngleRad));

            float leftT = leftDir.y != 0 ? -distance / leftDir.y : distance;
            float rightT = rightDir.y != 0 ? -distance / rightDir.y : distance;

            Vector2 bottomLeft = topLeft + leftDir * leftT;
            Vector2 bottomRight = topRight + rightDir * rightT;

            // 그림자 계산
            Collider2D[] colliders = Physics2D.OverlapBoxAll(
                sightOrigin.position + sightOrigin.TransformDirection(new Vector2(0, -distance / 2f)),
                new Vector2(width + 2f, distance + 2f),
                sightOrigin.eulerAngles.z,
                obstacleLayer
            );

            Vector2 origin = CalculateSightOrigin(topLeft, topRight, bottomLeft, bottomRight);

            foreach (Collider2D col in colliders)
            {
                if (col is BoxCollider2D boxCol)
                {
                    Vector2 boxWorldCenter = (Vector2)col.transform.position + boxCol.offset;
                    Vector2 boxSize = boxCol.size;

                    Vector2[] boxLocalCorners = new Vector2[]
                    {
                         new Vector2(-boxSize.x / 2f, -boxSize.y / 2f),
                         new Vector2(boxSize.x / 2f, -boxSize.y / 2f),
                         new Vector2(-boxSize.x / 2f, boxSize.y / 2f),
                         new Vector2(boxSize.x / 2f, boxSize.y / 2f)
                    };

                    Vector2[] worldCorners = new Vector2[4];
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 scaled = new Vector2(
                            boxLocalCorners[i].x * col.transform.lossyScale.x,
                            boxLocalCorners[i].y * col.transform.lossyScale.y
                        );

                        float angle = col.transform.eulerAngles.z * Mathf.Deg2Rad;
                        float cos = Mathf.Cos(angle);
                        float sin = Mathf.Sin(angle);
                        Vector2 rotated = new Vector2(
                            scaled.x * cos - scaled.y * sin,
                            scaled.x * sin + scaled.y * cos
                        );

                        worldCorners[i] = boxWorldCenter + rotated;
                    }

                    Vector2 cornerBL = transform.InverseTransformPoint(worldCorners[0]);
                    Vector2 cornerBR = transform.InverseTransformPoint(worldCorners[1]);
                    Vector2 cornerTL = transform.InverseTransformPoint(worldCorners[2]);
                    Vector2 cornerTR = transform.InverseTransformPoint(worldCorners[3]);

                    Vector2 boxLocalCenter = transform.InverseTransformPoint(boxWorldCenter);

                    if (boxLocalCenter.y >= origin.y) continue;

                    Vector2[] corners = { cornerBL, cornerBR, cornerTL, cornerTR };

                    float minAngle = float.MaxValue;
                    float maxAngle = float.MinValue;
                    Vector2 leftCorner = cornerTL;
                    Vector2 rightCorner = cornerTR;

                    foreach (var corner in corners)
                    {
                        Vector2 dir = corner - origin;
                        float cornerAngle = Mathf.Atan2(dir.x, -dir.y);

                        if (cornerAngle < minAngle)
                        {
                            minAngle = cornerAngle;
                            leftCorner = corner;
                        }
                        if (cornerAngle > maxAngle)
                        {
                            maxAngle = cornerAngle;
                            rightCorner = corner;
                        }
                    }

                    Vector2 dirLeft = (leftCorner - origin).normalized;
                    Vector2 dirRight = (rightCorner - origin).normalized;

                    float hitDistLeft = Vector2.Distance(origin, leftCorner);
                    float hitDistRight = Vector2.Distance(origin, rightCorner);
                    float shadowEndDistLeft = hitDistLeft + shadowLength;
                    float shadowEndDistRight = hitDistRight + shadowLength;

                    Vector2 shadowTL = leftCorner;
                    Vector2 shadowTR = rightCorner;
                    Vector2 shadowBL = origin + dirLeft * shadowEndDistLeft;
                    Vector2 shadowBR = origin + dirRight * shadowEndDistRight;

                    // 그림자 사다리꼴 내부 판정
                    if (IsPointInTrapezoid(localPos, shadowTL, shadowTR, shadowBR, shadowBL))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 시야 안에 있지만 그림자 안에는 없는지 판별 (실제로 보임)
        public bool IsVisible(Vector3 worldPosition)
        {
            return IsInSight(worldPosition) && !IsInShadow(worldPosition);
        }

        // 사다리꼴 내부 판정 (4개 꼭지점)
        private bool IsPointInTrapezoid(Vector2 point, Vector2 tl, Vector2 tr, Vector2 br, Vector2 bl)
        {
            Vector2[] corners = new Vector2[] { tl, tr, br, bl };

            int sign = 0;

            for (int i = 0; i < 4; i++)
            {
                Vector2 v1 = corners[i];
                Vector2 v2 = corners[(i + 1) % 4];

                Vector2 edge = v2 - v1;
                Vector2 toPoint = point - v1;

                float cross = edge.x * toPoint.y - edge.y * toPoint.x;

                if (Mathf.Abs(cross) < 0.0001f)
                {
                    continue;
                }

                int currentSign = cross > 0 ? 1 : -1;

                if (sign == 0)
                {
                    sign = currentSign;
                }
                else if (sign != currentSign)
                {
                    return false;
                }
            }

            return true;
        }
    }

    // 간단한 Ear Clipping Triangulator (Unity 위키에서 발췌)
    public class Triangulator
    {
        private List<Vector2> m_points = new List<Vector2>();

        public Triangulator(Vector2[] points)
        {
            m_points = new List<Vector2>(points);
        }

        public int[] Triangulate()
        {
            List<int> indices = new List<int>();

            int n = m_points.Count;
            if (n < 3)
                return indices.ToArray();

            int[] V = new int[n];
            if (Area() > 0)
            {
                for (int v = 0; v < n; v++)
                    V[v] = v;
            }
            else
            {
                for (int v = 0; v < n; v++)
                    V[v] = (n - 1) - v;
            }

            int nv = n;
            int count = 2 * nv;
            for (int v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0)
                    return indices.ToArray();

                int u = v;
                if (nv <= u)
                    u = 0;
                v = u + 1;
                if (nv <= v)
                    v = 0;
                int w = v + 1;
                if (nv <= w)
                    w = 0;

                if (Snip(u, v, w, nv, V))
                {
                    int a, b, c, s, t;
                    a = V[u];
                    b = V[v];
                    c = V[w];
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);
                    for (s = v, t = v + 1; t < nv; s++, t++)
                        V[s] = V[t];
                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices.ToArray();
        }

        private float Area()
        {
            int n = m_points.Count;
            float A = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 pval = m_points[p];
                Vector2 qval = m_points[q];
                A += pval.x * qval.y - qval.x * pval.y;
            }
            return (A * 0.5f);
        }

        private bool Snip(int u, int v, int w, int n, int[] V)
        {
            int p;
            Vector2 A = m_points[V[u]];
            Vector2 B = m_points[V[v]];
            Vector2 C = m_points[V[w]];
            if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
                return false;
            for (p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w))
                    continue;
                Vector2 P = m_points[V[p]];
                if (InsideTriangle(A, B, C, P))
                    return false;
            }
            return true;
        }

        private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;

            ax = C.x - B.x; ay = C.y - B.y;
            bx = A.x - C.x; by = A.y - C.y;
            cx = B.x - A.x; cy = B.y - A.y;
            apx = P.x - A.x; apy = P.y - A.y;
            bpx = P.x - B.x; bpy = P.y - B.y;
            cpx = P.x - C.x; cpy = P.y - C.y;

            aCROSSbp = ax * bpy - ay * bpx;
            cCROSSap = cx * apy - cy * apx;
            bCROSScp = bx * cpy - by * cpx;

            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }
    }

}
