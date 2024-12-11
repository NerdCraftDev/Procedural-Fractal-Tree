using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace TreeFractal2
{
    public class TreeFractal2 : MonoBehaviour
    {
        public List<Tree> trees = new List<Tree>();
        [Header("Debug Drawer Settings")]
        public bool drawBranchPoints = true;
        public Color gizmoPointColor = Color.green;
        public bool drawLines = true;
        public Color gizmoLineColor = Color.white;
        public bool drawLeaves = true;
        public Color gizmoLeafColor = Color.green;
        public bool drawVertices = false;
        public float branchPointScale = 1;
        public float leafScale = 1;
        [Header("Tree Generation Settings")]
        public int maxGenerationDistance = 10;
        public float maxRotationOffset = 10;
        public float extraWidthPercentToBranch = 0.25f;
        public float percentLengthIncrease = 0.05f;
        public float percentWidthIncrease = 0.05f;
        public float randomGrowthFactor = 0.1f;

        private bool isGrowing = false;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                int iterations = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1;
                for (int i = 0; i < iterations; i++)
                {
                    Vector3[] posData = GetValidTreePosition();
                    Tree tree = CreateTree(posData[0], posData[1]);
                    trees.Add(tree);
                    Debug.Log("Added tree");
                }
            }
            if (Input.GetKeyDown(KeyCode.Space) && !isGrowing)
            {
                isGrowing = true;
                GrowTreesAsync();
            }
        }

        void OnDrawGizmos()
        {
            foreach (var tree in trees)
            {
                if (drawBranchPoints)
                {
                    // Draw the tree as branches in the scene for visualization
                    foreach (var point in tree.branches)
                    {
                        Gizmos.color = gizmoPointColor;
                        Gizmos.DrawSphere(point.position, point.width * branchPointScale); // Scale down the sphere size
                    }
                }

                if (drawLines || drawLeaves)
                {
                    // Connect branches with lines
                    foreach (var point in tree.branches)
                    {
                        if (point.parent != null && drawLines)
                        {
                            Gizmos.color = gizmoLineColor;
                            Gizmos.DrawLine(point.position, point.parent.position);
                        }

                        // Draw leaves
                        if (point.children.Count == 0 && drawLeaves)
                        {
                            Gizmos.color = gizmoLeafColor;
                            Gizmos.DrawCube(point.position, point.width * leafScale * Vector3.one);
                        }
                    }
                }
            }
        }

        public Vector3[] GetValidTreePosition()
        {
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
                    float distance = Vector3.Distance(candidatePos, tree.branches[0].position);
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

        private async void GrowTreesAsync()
        {
            // Run the growth computations asynchronously
            await Task.Run(() =>
            {
                Parallel.ForEach(trees, tree =>
                {
                    GrowTree(tree);
                });
            });

            isGrowing = false;
        }

        private void GrowTree(Tree tree)
        {
            List<BranchData> newBranches = new List<BranchData>();

            // Perform calculations without Unity API calls
            foreach (var point in tree.branches)
            {
                // Increase the length and width of each branch by 5%
                point.length *= 1 + tree.percentLengthIncrease;
                point.width *= 1 + tree.percentWidthIncrease;

                // Update the position of the branch based on the new length
                if (point.parent != null)
                {
                    point.position = point.parent.position + point.parent.direction * point.parent.length;
                }

                // Check if the branch should add new branches
                float combinedChildrenWidth = 0;
                foreach (var child in point.children)
                {
                    combinedChildrenWidth += child.width;
                }
                if (point.width > (1 + tree.extraWidthPercentToBranch) * combinedChildrenWidth)
                {
                    // Collect data for new branches
                    newBranches.Add(new BranchData
                    {
                        parent = point,
                        direction = Vector3.zero, // Temporary value, will be set on the main thread
                        width = point.width * 0.75f,
                        length = point.length * 0.75f
                    });
                }
            }

            // Apply changes to Unity objects on the main thread
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                foreach (var data in newBranches)
                {
                    data.direction = Tree.GetOffsetDirection(data.parent.direction, maxRotationOffset);
                    tree.AddBranch(data.parent, data.direction, data.width, data.length);
                }
            });
        }

        // Helper class to store data for new branches
        private class BranchData
        {
            public Branch parent;
            public Vector3 direction;
            public float width;
            public float length;
        }
        
        public Tree CreateTree(Vector3 position, Vector3 trunkNormal)
        {
            float[] growthData = GetGrowthData();
            return new Tree(position, trunkNormal, maxRotationOffset, growthData[0], growthData[1], growthData[2]);
        }

        public float[] GetGrowthData()
        {
            // Return growth data with random variation
            float offsetPercent = 1 + Random.Range(-randomGrowthFactor, randomGrowthFactor);
            float extraWidth = extraWidthPercentToBranch * offsetPercent;
            float lengthIncrease = percentLengthIncrease * offsetPercent;
            float widthIncrease = percentWidthIncrease * offsetPercent;
            return new float[3] { extraWidth, lengthIncrease, widthIncrease };
        }
    }

    public class Tree
    {
        public List<Branch> branches = new List<Branch>();
        public Vector3 position = Vector3.zero;
        public float extraWidthPercentToBranch;
        public float percentLengthIncrease;
        public float percentWidthIncrease;

        public Tree(Vector3 position, Vector3 trunkNormal, float maxRotationOffset, float extraWidthPercentToBranch, float percentLengthIncrease, float percentWidthIncrease)
        {
            this.position = position;
            this.extraWidthPercentToBranch = extraWidthPercentToBranch;
            this.percentLengthIncrease = percentLengthIncrease;
            this.percentWidthIncrease = percentWidthIncrease;
            // Create tree trunk
            branches.Add(new Branch(this, null, trunkNormal, 1, 5));
            branches.Add(new Branch(this, branches[0], GetOffsetDirection(branches[0].direction, maxRotationOffset), 0.75f, 5f));
        }

        public static Vector3 GetOffsetDirection(Vector3 direction, float maxRotationOffset)
        {
            Quaternion rotation = Quaternion.Euler(
                Random.Range(-maxRotationOffset, maxRotationOffset),
                Random.Range(-maxRotationOffset, maxRotationOffset),
                Random.Range(-maxRotationOffset, maxRotationOffset)
            );
            return rotation * direction;
        }

        public void AddBranch(Branch parent, Vector3 direction, float width, float length)
        {
            var branch = new Branch(this, parent, direction, width, length);
            branches.Add(branch);
        }
    }

    public class Branch
    {
        public Tree tree;
        public Vector3 position;
        public float width = 1;
        public float length = 1;
        public Vector3 direction = Vector3.up;
        public Branch parent;
        public List<Branch> children = new List<Branch>();

        public Branch(Tree tree, Branch parent, Vector3 direction, float width, float length)
        {
            this.tree = tree;           // Set the tree of this branch
            this.parent = parent;       // Set the parent of this branch
            this.width = width;         // Set the width of this branch
            this.length = length;       // Set the length of this branch
            this.direction = direction; // Set the direction of this branch
            position = parent != null ? parent.position + parent.direction * parent.length : tree.position; // Calculate the position

            if (parent != null)
            {
                parent.children.Add(this); // Add this branch to the parent's children
            }
        }
    }

    // Utility class to enqueue actions on the main thread
    public static class UnityMainThreadDispatcher
    {
        private static readonly Queue<System.Action> _executionQueue = new Queue<System.Action>();

        public static void Enqueue(System.Action action)
        {
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var obj = new GameObject("MainThreadDispatcher");
            obj.AddComponent<Dispatcher>();
            GameObject.DontDestroyOnLoad(obj);
        }

        private class Dispatcher : MonoBehaviour
        {
            void Update()
            {
                lock (_executionQueue)
                {
                    while (_executionQueue.Count > 0)
                    {
                        _executionQueue.Dequeue()?.Invoke();
                    }
                }
            }
        }
    }
}