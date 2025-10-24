using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Clipper2Lib;
using LibTessDotNet;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnemySightSystem : MonoBehaviour
{
    [Header("시야 설정")]
    [SerializeField] private float width = 4f;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float leftAngle = 0f;
    [SerializeField] private float rightAngle = 0f;
    [SerializeField] private Color sightColor = new Color(1f, 0f, 0f, 0.3f);

    [Header("레이캐스트 설정")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float shadowLength = 1f;

    [Header("Clipper2 설정")]
    [SerializeField] private bool useClipper2 = true;  // Clipper2 사용 여부

    [Header("2D 렌더링 설정")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 0;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    // 그림자 사다리꼴 정의 (로컬 좌표)
    struct ShadowTrapezoid
    {
        public Vector2 topLeft;
        public Vector2 topRight;
        public Vector2 bottomLeft;
        public Vector2 bottomRight;
    }

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh();
        meshFilter.mesh = mesh;

        meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        meshRenderer.material.color = sightColor;

        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;

        CreateRectangle();
    }

    void Update()
    {
        CreateRectangle();
    }

    void CreateRectangle()
    {
        mesh.Clear();

        float halfWidth = width / 2f;
        float leftAngleRad = leftAngle * Mathf.Deg2Rad;
        float rightAngleRad = rightAngle * Mathf.Deg2Rad;

        // 사다리꼴 시야 영역
        Vector2 topLeft = new Vector2(-halfWidth, 0);
        Vector2 topRight = new Vector2(halfWidth, 0);

        float leftXOffset = Mathf.Sin(leftAngleRad) * distance;
        float rightXOffset = Mathf.Sin(rightAngleRad) * distance;
        float yOffset = distance;

        Vector2 bottomLeft = new Vector2(-halfWidth + leftXOffset, -yOffset);
        Vector2 bottomRight = new Vector2(halfWidth + rightXOffset, -yOffset);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // 1단계: 박스콜라이더 찾아서 그림자 사다리꼴로 변환
        List<ShadowTrapezoid> shadowTrapezoids = new List<ShadowTrapezoid>();

        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            transform.position + transform.TransformDirection(new Vector2(0, -distance / 2f)),
            new Vector2(width + 2f, distance + 2f),
            transform.eulerAngles.z,
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

                // 월드 좌표를 EnemySightSystem의 로컬 좌표로 변환
                Vector2 cornerBL = transform.InverseTransformPoint(worldCorners[0]);
                Vector2 cornerBR = transform.InverseTransformPoint(worldCorners[1]);
                Vector2 cornerTL = transform.InverseTransformPoint(worldCorners[2]);
                Vector2 cornerTR = transform.InverseTransformPoint(worldCorners[3]);

                // 박스 중심도 로컬 좌표로 변환
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
        CreateSightWithClipper(topLeft, topRight, bottomLeft, bottomRight, shadowTrapezoids, vertices, triangles);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    // Clipper2를 사용한 시야 생성 (그림자 영역 제외)
    void CreateSightWithClipper(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight,
                                List<ShadowTrapezoid> shadows, List<Vector3> vertices, List<int> triangles)
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

        // 4. 외곽과 구멍 분리
        PathD outer = null;
        List<PathD> holes = new List<PathD>();

        foreach (var path in result)
        {
            if (path.Count < 3) continue;

            double area = Clipper.Area(path);
            Debug.Log($"경로: {path.Count}꼭지점, Area={area:F4}");

            if (area > 0)
            {
                outer = path;
                Debug.Log("  -> 외곽 폴리곤");
            }
            else
            {
                holes.Add(path);
                Debug.Log("  -> 구멍 폴리곤");
            }
        }

        // 5. 메시 생성
        if (outer != null)
        {
            if (holes.Count == 0)
            {
                // 구멍 없음 - 단순 삼각분할
                Debug.Log("구멍 없음 - 단순 삼각분할");
                Vector2[] points2D = new Vector2[outer.Count];
                for (int i = 0; i < outer.Count; i++)
                    points2D[i] = new Vector2((float)outer[i].x, (float)outer[i].y);

                Triangulator tr = new Triangulator(points2D);
                int[] tris = tr.Triangulate();

                int vStart = vertices.Count;
                foreach (var p in points2D)
                    vertices.Add(new Vector3(p.x, p.y, 0));

                foreach (var idx in tris)
                    triangles.Add(vStart + idx);

                Debug.Log($"  -> {tris.Length / 3}개 삼각형");
            }
            else
            {
                // 구멍 있음 - LibTessDotNet 사용
                Debug.Log($"구멍 {holes.Count}개 - LibTessDotNet으로 삼각분할");
                TriangulateWithLibTess(outer, holes, vertices, triangles);
            }
        }
    }



    // LibTessDotNet을 사용한 구멍 처리 삼각분할
    void TriangulateWithLibTess(PathD outer, List<PathD> holes, List<Vector3> vertices, List<int> triangles)
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

        // 버텍스 추가
        int vStart = vertices.Count;
        for (int i = 0; i < tess.VertexCount; i++)
        {
            var v = tess.Vertices[i].Position;
            vertices.Add(new Vector3(v.X, v.Y, 0));
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
