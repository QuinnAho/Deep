using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Builds cave meshes from 2D map data using marching squares algorithm.
/// Ported from Procedural-Cave-Generation and adapted for Deep's architecture.
/// </summary>
public class CaveMeshBuilder : MonoBehaviour
{
    public MeshFilter caveMeshFilter;
    public MeshFilter wallMeshFilter;

    private SquareGrid squareGrid;
    private List<Vector3> vertices;
    private List<int> triangles;

    private Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();
    private List<List<int>> outlines = new List<List<int>>();
    private HashSet<int> checkedVertices = new HashSet<int>();

    public Mesh CaveMesh => caveMeshFilter != null ? caveMeshFilter.sharedMesh : null;
    public Mesh WallMesh => wallMeshFilter != null ? wallMeshFilter.sharedMesh : null;

    public void GenerateMesh(int[,] map, CaveGenerationConfig config)
    {
        triangleDictionary.Clear();
        outlines.Clear();
        checkedVertices.Clear();

        squareGrid = new SquareGrid(map, config.squareSize);

        vertices = new List<Vector3>();
        triangles = new List<int>();

        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        Mesh caveMesh = new Mesh();
        caveMesh.name = "Cave Mesh";
        caveMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        caveMesh.vertices = vertices.ToArray();
        caveMesh.triangles = triangles.ToArray();
        caveMesh.RecalculateNormals();

        Vector2[] uvs = new Vector2[vertices.Count];
        float mapSize = map.GetLength(0) / 2f * config.squareSize;

        for (int i = 0; i < vertices.Count; i++)
        {
            float percentX = Mathf.InverseLerp(-mapSize, mapSize, vertices[i].x) * config.tileAmount;
            float percentY = Mathf.InverseLerp(-mapSize, mapSize, vertices[i].y) * config.tileAmount;
            uvs[i] = new Vector2(percentX, percentY);
        }
        caveMesh.uv = uvs;

        if (caveMeshFilter != null)
        {
            caveMeshFilter.mesh = caveMesh;

            MeshRenderer renderer = caveMeshFilter.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (config.caveMaterial != null)
                {
                    renderer.sharedMaterial = config.caveMaterial;
                }
                else if (renderer.sharedMaterial == null)
                {
                    renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                }
            }
        }

        if (config.is2D)
        {
            Generate2DColliders();
        }
        else
        {
            CreateWallMesh(config);
        }
    }

    private void CreateWallMesh(CaveGenerationConfig config)
    {
        MeshCollider currentCollider = GetComponent<MeshCollider>();
        if (currentCollider != null)
        {
            DestroyImmediate(currentCollider);
        }

        CalculateMeshOutlines();

        List<Vector3> volumeVertices = new List<Vector3>();
        List<int> volumeTriangles = new List<int>();
        Mesh wallMesh = new Mesh();
        wallMesh.name = "Wall Mesh";
        wallMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        float wallHeight = config.depthThickness;
        Vector3 extrudeAxis = Vector3.forward;

        // Capture vertical bounds for later filtering of surface/bottom edges
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < vertices.Count; i++)
        {
            float y = vertices[i].y;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        // Floor (reuse existing triangles; no ceiling to keep the surface open)
        volumeVertices.AddRange(vertices);
        volumeTriangles.AddRange(triangles);

        // Determine the "surface" edge (open) and keep the bottom sealed.
        Vector3 planeUp = Vector3.up;
        float surfaceCoord = float.MinValue;
        for (int i = 0; i < vertices.Count; i++)
        {
            float coord = Vector3.Dot(vertices[i], planeUp);
            if (coord > surfaceCoord)
            {
                surfaceCoord = coord;
            }
        }
        const float edgeTolerance = 0.001f;

        // Sides around each outline (skip the surface edge to leave it open)
        foreach (List<int> outline in outlines)
        {
            for (int i = 0; i < outline.Count - 1; i++)
            {
                int startIndex = volumeVertices.Count;

                Vector3 vA = vertices[outline[i]];
                Vector3 vB = vertices[outline[i + 1]];
                Vector3 vAHigh = vA + extrudeAxis * wallHeight;
                Vector3 vBHigh = vB + extrudeAxis * wallHeight;

                bool isSurfaceEdge = Mathf.Abs(Vector3.Dot(vA, planeUp) - surfaceCoord) < edgeTolerance
                                   && Mathf.Abs(Vector3.Dot(vB, planeUp) - surfaceCoord) < edgeTolerance;
                bool isBottomEdge = Mathf.Abs(vA.y - minY) < edgeTolerance && Mathf.Abs(vB.y - minY) < edgeTolerance;
                if (isBottomEdge)
                {
                    continue;
                }
                if (isSurfaceEdge)
                {
                    continue;
                }

                volumeVertices.Add(vA);
                volumeVertices.Add(vB);
                volumeVertices.Add(vAHigh);
                volumeVertices.Add(vBHigh);

                volumeTriangles.Add(startIndex + 0);
                volumeTriangles.Add(startIndex + 2);
                volumeTriangles.Add(startIndex + 3);

                volumeTriangles.Add(startIndex + 3);
                volumeTriangles.Add(startIndex + 1);
                volumeTriangles.Add(startIndex + 0);
            }
        }

        wallMesh.SetVertices(volumeVertices);
        wallMesh.SetTriangles(volumeTriangles, 0);
        wallMesh.RecalculateNormals();

        if (wallMeshFilter != null)
        {
            wallMeshFilter.mesh = wallMesh;

            MeshRenderer renderer = wallMeshFilter.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (config.wallMaterial != null)
                {
                    renderer.sharedMaterial = config.wallMaterial;
                }
                else if (renderer.sharedMaterial == null)
                {
                    renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                }
            }
        }

        MeshCollider wallCollider = gameObject.AddComponent<MeshCollider>();
        wallCollider.sharedMesh = wallMesh;
    }

    private void Generate2DColliders()
    {
        EdgeCollider2D[] currentColliders = gameObject.GetComponents<EdgeCollider2D>();
        for (int i = 0; i < currentColliders.Length; i++)
        {
            DestroyImmediate(currentColliders[i]);
        }

        CalculateMeshOutlines();

        foreach (List<int> outline in outlines)
        {
            EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            Vector2[] edgePoints = new Vector2[outline.Count];

            for (int i = 0; i < outline.Count; i++)
            {
                Vector3 v = vertices[outline[i]];
                edgePoints[i] = new Vector2(v.x, v.y);
            }
            edgeCollider.points = edgePoints;
        }
    }

    private void TriangulateSquare(Square square)
    {
        switch (square.configuration)
        {
            case 0:
                break;

            // 1 point
            case 1:
                MeshFromPoints(square.centreLeft, square.centreBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centreBottom, square.centreRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centreRight, square.centreTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
                break;

            // 2 points
            case 3:
                MeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                break;
            case 6:
                MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreLeft);
                break;
            case 5:
                MeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.centreBottom, square.centreLeft);
                break;

            // 3 points
            case 7:
                MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centreBottom, square.centreLeft);
                break;

            // 4 points
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                checkedVertices.Add(square.topLeft.vertexIndex);
                checkedVertices.Add(square.topRight.vertexIndex);
                checkedVertices.Add(square.bottomRight.vertexIndex);
                checkedVertices.Add(square.bottomLeft.vertexIndex);
                break;
        }
    }

    private void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);

        if (points.Length >= 3)
            CreateTriangle(points[0], points[1], points[2]);
        if (points.Length >= 4)
            CreateTriangle(points[0], points[2], points[3]);
        if (points.Length >= 5)
            CreateTriangle(points[0], points[3], points[4]);
        if (points.Length >= 6)
            CreateTriangle(points[0], points[4], points[5]);
    }

    private void AssignVertices(Node[] points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].vertexIndex == -1)
            {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
            }
        }
    }

    private void CreateTriangle(Node a, Node b, Node c)
    {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    private void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        if (triangleDictionary.ContainsKey(vertexIndexKey))
        {
            triangleDictionary[vertexIndexKey].Add(triangle);
        }
        else
        {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDictionary.Add(vertexIndexKey, triangleList);
        }
    }

    private void CalculateMeshOutlines()
    {
        for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
        {
            if (!checkedVertices.Contains(vertexIndex))
            {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                if (newOutlineVertex != -1)
                {
                    checkedVertices.Add(vertexIndex);

                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlines.Count - 1);
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
        }

        SimplifyMeshOutlines();
    }

    private void SimplifyMeshOutlines()
    {
        for (int outlineIndex = 0; outlineIndex < outlines.Count; outlineIndex++)
        {
            List<int> simplifiedOutline = new List<int>();
            Vector3 dirOld = Vector3.zero;
            for (int i = 0; i < outlines[outlineIndex].Count; i++)
            {
                Vector3 p1 = vertices[outlines[outlineIndex][i]];
                Vector3 p2 = vertices[outlines[outlineIndex][(i + 1) % outlines[outlineIndex].Count]];
                Vector3 dir = p1 - p2;
                if (dir != dirOld)
                {
                    dirOld = dir;
                    simplifiedOutline.Add(outlines[outlineIndex][i]);
                }
            }
            outlines[outlineIndex] = simplifiedOutline;
        }
    }

    private void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

        if (nextVertexIndex != -1)
        {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    private int GetConnectedOutlineVertex(int vertexIndex)
    {
        List<Triangle> trianglesContainingVertex = triangleDictionary[vertexIndex];

        for (int i = 0; i < trianglesContainingVertex.Count; i++)
        {
            Triangle triangle = trianglesContainingVertex[i];

            for (int j = 0; j < 3; j++)
            {
                int vertexB = triangle[j];
                if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if (IsOutlineEdge(vertexIndex, vertexB))
                    {
                        return vertexB;
                    }
                }
            }
        }

        return -1;
    }

    private bool IsOutlineEdge(int vertexA, int vertexB)
    {
        List<Triangle> trianglesContainingVertexA = triangleDictionary[vertexA];
        int sharedTriangleCount = 0;

        for (int i = 0; i < trianglesContainingVertexA.Count; i++)
        {
            if (trianglesContainingVertexA[i].Contains(vertexB))
            {
                sharedTriangleCount++;
                if (sharedTriangleCount > 1)
                {
                    break;
                }
            }
        }
        return sharedTriangleCount == 1;
    }

    #region Data Structures

    private struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;
        private int[] vertices;

        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }

        public int this[int i]
        {
            get { return vertices[i]; }
        }

        public bool Contains(int vertexIndex)
        {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }

    public class SquareGrid
    {
        public Square[,] squares;

        public SquareGrid(int[,] map, float squareSize)
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

            for (int x = 0; x < nodeCountX; x++)
            {
                for (int y = 0; y < nodeCountY; y++)
                {
                    Vector3 pos = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, -mapHeight / 2 + y * squareSize + squareSize / 2, 0);
                    controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
                }
            }

            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX - 1; x++)
            {
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
            }
        }
    }

    public class Square
    {
        public ControlNode topLeft, topRight, bottomRight, bottomLeft;
        public Node centreTop, centreRight, centreBottom, centreLeft;
        public int configuration;

        public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft)
        {
            topLeft = _topLeft;
            topRight = _topRight;
            bottomRight = _bottomRight;
            bottomLeft = _bottomLeft;

            centreTop = topLeft.right;
            centreRight = bottomRight.above;
            centreBottom = bottomLeft.right;
            centreLeft = bottomLeft.above;

            if (topLeft.active)
                configuration += 8;
            if (topRight.active)
                configuration += 4;
            if (bottomRight.active)
                configuration += 2;
            if (bottomLeft.active)
                configuration += 1;
        }
    }

    public class Node
    {
        public Vector3 position;
        public int vertexIndex = -1;

        public Node(Vector3 _pos)
        {
            position = _pos;
        }
    }

    public class ControlNode : Node
    {
        public bool active;
        public Node above, right;

        public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos)
        {
            active = _active;
            above = new Node(position + Vector3.up * squareSize / 2f);
            right = new Node(position + Vector3.right * squareSize / 2f);
        }
    }

    #endregion
}
