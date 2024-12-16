using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TreeFractal : MonoBehaviour
{
    [Header("Tree Parameters")]
    // public float initialWidth = 1.0f;      // Width of the trunk
    // public float initialLength = 5.0f;     // Initial length of branches
    public float minStartingWidth = 0.1f;          // Minimum starting width of branches
    public float MaxStartingWidth = 0.5f;          // Maximum starting width of branches
    public float minStartingLength = 0.5f;         // Minimum starting length of branches
    public float maxStartingLength = 1.0f;         // Maximum starting length of branches
    public int minSplits = 2;              // Minimum number of splits
    public int maxSplits = 3;              // Maximum number of splits
    public float lengthDecay = 0.9f;       // How much the branch length shrinks
    public float maxRotationAngle = 30f;   // Maximum initial angle deviation
    public float maxRotationAngleBase = 10f; // Maximum angle deviation for the base point
    public float angleIncreasePerDepth = 10f; // How much deviation grows with depth
    public int verticesCount = 8;          // Number of vertices for the circle around each point
    public Material leafMaterial;          // Material for the leaves

    public List<Tree> trees = new List<Tree>(); // List of trees
    private Coroutine routine; // Stores the current coroutine
    [Header("Generation Settings")]
    public float maxGenerationDistance = 10f; // Maximum distance from the center
    public int minDepth = 1;            // Minimum recursion depth
    public int maxDepth = 5;            // Maximum recursion depth
    public float iterationDelay = 0.1f; // Delay between iterations in seconds
    public bool drawLines = true;       // Draw lines between points
    public bool drawPoints = true;      // Draw points in the scene
    public bool drawVertices = true;   // Draw vertices of the circle around points
    public float pointScale = 1f;       // Scale of the points
    public bool autoGenerate = true;    // Generate the tree on start
    public int treeCount = 1;           // Number of trees to generate

    void Start()
    {
        // Initialize tree generation
        if (autoGenerate) {
            for (int i = 0; i < treeCount; i++) {
                Vector3[] posData = GetValidTreePosition();
                float[] startingData = GetStartingData();
                Tree tree = GenerateTree(posData[0], posData[1], startingData[0], startingData[1], startingData[2], Mathf.RoundToInt(startingData[3]));
                tree.GenerateMesh(this);
            }
        }
    }

    public Tree GenerateTree(Vector3 position, Vector3 normal, float initialWidth, float initialLength, float leafSize, int depth)
    {
        // Create a new tree
        Tree tree = new Tree(this);
        trees.Add(tree);

        // Start with the root point
        Point root = new Point(position, initialWidth, initialLength, null, 0, verticesCount);
        root.direction = normal;
        tree.points.Add(root);
        
        // Calculate a random rotation for the base point
        Quaternion randomRotation = Quaternion.Euler(
            Random.Range(-maxRotationAngleBase, maxRotationAngleBase),
            Random.Range(-maxRotationAngleBase, maxRotationAngleBase),
            Random.Range(-maxRotationAngleBase, maxRotationAngleBase)
        );
        // Create an initial point just above the root
        Point fractalBasePoint = new Point(position + randomRotation * Vector3.up * initialLength, initialWidth, initialLength, root, 1, verticesCount);

        tree.points.Add(fractalBasePoint);
        GrowBranches(tree, fractalBasePoint, 1, leafSize, depth);
        return tree;
    }

    void GrowBranches(Tree tree, Point parent, int currentDepth, float leafSize, int maxDepth)
    {
        if (currentDepth >= maxDepth) {
            AddLeaves(tree, parent, leafSize);
            return;
        }

        // Determine the number of splits
        int splits = Random.Range(minSplits, maxSplits + 1);

        for (int i = 0; i < splits; i++)
        {
            // Calculate the base rotation for spreading branches in a circle
            float baseAngle = (360f / splits) * i;
            Quaternion baseRotation = Quaternion.AngleAxis(baseAngle, parent.direction);

            // Apply jitter to the rotation
            float angleDeviation = maxRotationAngle + angleIncreasePerDepth * currentDepth;
            Quaternion jitterRotation = Quaternion.Euler(
                Random.Range(-angleDeviation, angleDeviation), 
                Random.Range(-angleDeviation, angleDeviation), 
                Random.Range(-angleDeviation, angleDeviation)
            );

            // Calculate the new branch direction
            Vector3 direction = (baseRotation * jitterRotation) * parent.direction;

            // Calculate the new point's position
            Vector3 newPosition = parent.position + direction * parent.length;

            // Create the new point
            Point newPoint = new Point(
                newPosition,
                parent.width / splits,
                parent.length * lengthDecay,
                parent,
                currentDepth + 1,
                verticesCount
            );

            // Add the point and recurse
            tree.points.Add(newPoint);
            GrowBranches(tree, newPoint, currentDepth + 1, leafSize, maxDepth);
        }
    }

    void AddLeaves(Tree tree, Point branchEnd, float leafSize)
    {
        // Create a leaf mesh and add it to the tree
        GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Quad);
        leaf.transform.localScale = new Vector3(leafSize, leafSize, leafSize);
        leaf.transform.parent = tree.gameObject.transform;
        
        // Align the leaf with the branch direction and add a random rotation offset
        Quaternion randomRotation = Quaternion.Euler(
            Random.Range(-maxRotationAngle, maxRotationAngle),
            Random.Range(-maxRotationAngle, maxRotationAngle),
            Random.Range(-maxRotationAngle, maxRotationAngle)
        );
        leaf.transform.rotation = Quaternion.LookRotation(branchEnd.direction) * randomRotation;

        leaf.transform.position = branchEnd.position + -leaf.transform.right * leafSize/2;
        
        leaf.GetComponent<Renderer>().material = leafMaterial; // Assign the leaf material
        tree.leaves.Add(leaf);
    }

    public Vector3[] GetValidTreePosition() {
        Vector3 pos = Vector3.zero;
        Vector3 normal = Vector3.up;
        float maxDistance = 0f;

        for (int i = 0; i < 10; i++) // Try 10 times to find a good position
        {
            float x = Random.Range(-maxGenerationDistance, maxGenerationDistance);
            float y = 0f;
            float z = Random.Range(-maxGenerationDistance, maxGenerationDistance);
            Vector3 candidatePos = new Vector3(x, y, z);

            float minDistance = float.MaxValue;
            foreach (var tree in trees)
            {
            float distance = Vector3.Distance(candidatePos, tree.points[0].position);
            if (distance < minDistance)
            {
                minDistance = distance;
            }
            }

            if (minDistance > maxDistance)
            {
                maxDistance = minDistance;
                pos = candidatePos;
            }
        }

        return new Vector3[2] { pos, normal };
    }

    public float[] GetStartingData() {
        float scalePercent = Random.Range(0f,1f);
        float width = Mathf.Lerp(minStartingWidth, MaxStartingWidth, scalePercent);
        float length = Mathf.Lerp(minStartingLength, maxStartingLength, scalePercent);
        float leafSize = Mathf.Lerp(0.5f, 2f, scalePercent);
        float depth = Mathf.Lerp(minDepth, maxDepth, scalePercent);
        return new float[4] { width, length, leafSize, depth };
    }

    void OnDrawGizmos()
    {
        foreach (var tree in trees) {
            if (drawPoints) {
                // Draw the tree as points in the scene for visualization
                foreach (var point in tree.points)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(point.position, point.width * pointScale); // Scale down the sphere size
                    if (drawVertices) {
                        foreach (var vertex in point.vertices)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawSphere(vertex, 0.05f);
                        }
                    }
                }
            }

            if (drawLines) {
                // Connect points with lines
                Gizmos.color = Color.white;
                foreach (var point in tree.points)
                {
                    if (point.parent != null)
                    {
                        Gizmos.DrawLine(point.position, point.parent.position);
                    }
                }
            }
        }
    }

    public class Point
    {
        public Vector3 position;          // Position of the point
        public float width;               // Width at this point
        public float length;              // Length of the branch starting here
        public Point parent;              // Parent point
        public List<Point> children;      // Children points
        public Vector3[] vertices;        // Vertices of the circle around the point
        public int depth;                 // Depth of the point in the tree
        public Vector3 direction;                // Direction of the branch

        public Point(Vector3 position, float width, float length, Point parent, int depth, int verticesCount)
        {
            this.position = position;
            this.width = width;
            this.length = length;
            this.parent = parent;
            this.depth = depth;
            direction = Vector3.up;
            this.children = new List<Point>();
            this.vertices = new Vector3[verticesCount];

            if (parent != null)
            {
                parent.children.Add(this);
                parent.CalculateVertices(verticesCount);
            }

            CalculateVertices(verticesCount);
        }

        private void CalculateVertices(int verticesCount)
        {
            if (depth > 0) {
                direction = Vector3.zero;
                // Include vector from parent's parent to parent
                if (parent != null)
                {
                    direction += (position - parent.position).normalized;
                }

                // Include vectors from parent to its children
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        direction += (child.position - position).normalized;
                    }
                }

                // Use normal calculation if direction is zero
                if (direction == Vector3.zero)
                {
                    direction = parent != null ? (position - parent.position).normalized : Vector3.up;
                }
                else
                {
                    direction = direction.normalized;
                }
            }

            float radius = width / 2;
            float angleStep = 360f / verticesCount;

            for (int i = 0; i < verticesCount; i++)
            {
                float angle = i * angleStep;
                Quaternion rotation = Quaternion.AngleAxis(angle, direction);
                Vector3 offset = rotation * Vector3.right * radius;
                vertices[i] = position + offset;
            }
        }
    }

    public class Tree
    {
        public List<Point> points = new List<Point>();
        public List<GameObject> leaves = new List<GameObject>();
        public Mesh mesh;
        public GameObject gameObject;
        private TreeFractal treeFractal;

        public Tree(TreeFractal treeFractal)
        {
            this.treeFractal = treeFractal;
            gameObject = new GameObject("Tree " + (treeFractal.trees.Count + 1));
            gameObject.transform.parent = treeFractal.transform;
        }

        public void ClearPoints()
        {
            points.Clear();
            // Stop the current coroutine if it's running
            if (treeFractal.routine != null)
            {
                treeFractal.StopCoroutine(treeFractal.routine);
            }
        }
        public void ClearMesh()
        {
            treeFractal.trees.Remove(this);
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        public void GenerateMesh(TreeFractal treeFractal) {
            // Create a new mesh
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            int verticesCount = treeFractal.verticesCount;

            // Dictionary to keep track of the indices of each point's vertices
            var pointIndices = new Dictionary<Point, int[]>();

            // Add vertices for each point and store their indices
            foreach (var point in points)
            {
                int[] indices = new int[verticesCount];
                for (int i = 0; i < verticesCount; i++)
                {
                    indices[i] = vertices.Count;
                    vertices.Add(point.vertices[i]);
                }
                pointIndices[point] = indices;
            }

            // Create triangles between each point and its parent
            foreach (var point in points)
            {
                if (point.parent != null)
                {
                    int[] parentIndices = pointIndices[point.parent];
                    int[] currentIndices = pointIndices[point];

                    for (int i = 0; i < verticesCount; i++)
                    {
                        int nextI = (i + 1) % verticesCount;

                        // First triangle
                        triangles.Add(parentIndices[i]);
                        triangles.Add(currentIndices[nextI]);
                        triangles.Add(currentIndices[i]);

                        // Second triangle
                        triangles.Add(parentIndices[i]);
                        triangles.Add(parentIndices[nextI]);
                        triangles.Add(currentIndices[nextI]);
                    }
                }
            }

            // Assign the vertices and triangles to the mesh
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();

            // Recalculate normals for proper lighting
            mesh.RecalculateNormals();

            // Assign the mesh to the mesh filter
            if (!gameObject.TryGetComponent(out MeshFilter meshFilter))
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            meshFilter.mesh = mesh;

            // Ensure there is a MeshRenderer component
            if (!gameObject.TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            // Assign a default material if none is present
            if (meshRenderer.sharedMaterial == null)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new Color(0.55f, 0.27f, 0.07f) // Brown Color
                };
                meshRenderer.sharedMaterial = material;
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TreeFractal))]
public class TreeFractalEditor : Editor
{
    private int selectedTreeIndex = 0;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TreeFractal treeFractal = (TreeFractal)target;

        // Header for the tree generation section
        GUILayout.Space(10);
        GUILayout.Label("Tree Generation Controls", EditorStyles.boldLabel);

        // Dropdown for selecting a tree
        string[] treeOptions = new string[treeFractal.trees.Count];
        for (int i = 0; i < treeOptions.Length; i++)
        {
            treeOptions[i] = "Tree " + (i+1);
        }
        selectedTreeIndex = EditorGUILayout.Popup("Select Tree", selectedTreeIndex, treeOptions);

        if (GUILayout.Button("Clear Points"))
        {
            if (treeFractal.trees.Count > 0)
            {
                treeFractal.trees[selectedTreeIndex].ClearPoints();
            }
        }
        if (GUILayout.Button("Clear Mesh"))
        {
            treeFractal.trees[selectedTreeIndex].ClearMesh();
            selectedTreeIndex = Mathf.Clamp(selectedTreeIndex, 0, treeFractal.trees.Count - 1);
        }
        if (GUILayout.Button("Generate Tree"))
        {
            Vector3[] posData = treeFractal.GetValidTreePosition();
            float[] startingData = treeFractal.GetStartingData();
            treeFractal.GenerateTree(posData[0], posData[1], startingData[0], startingData[1], startingData[2], Mathf.RoundToInt(startingData[3]));
            selectedTreeIndex = treeFractal.trees.Count - 1;
        }
        if (GUILayout.Button("Generate Mesh"))
        {
            if (treeFractal.trees.Count > 0)
            {
                treeFractal.trees[selectedTreeIndex].GenerateMesh(treeFractal);
            }
        }
    }
}
#endif