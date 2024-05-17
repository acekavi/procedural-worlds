using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class NodeGrid : MonoBehaviour
{
    #region Debug properties
    [NonSerialized] public List<Node> path;
    #endregion

    #region Terrain properties
    [SerializeField] private TerrainGenerator terrainGenerator;
    private int chunkSize;
    private float tileSize;
    [NonSerialized] public Vector2 gridWorldSize;
    [NonSerialized] public int gridSizeX, gridSizeY;
    #endregion

    private Node[,] _nodeGrid;
    public Node[,] nodeGrid
    {
        get
        {
            return _nodeGrid;
        }
        set
        {
            if (value != _nodeGrid)
            {
                _nodeGrid = value;
            }
        }
    }
    private Vector3 _lastUpdatedPosition;
    public Vector3 lastUpdatedPosition
    {
        get
        {
            return _lastUpdatedPosition;
        }
        set
        {
            if (value != _lastUpdatedPosition)
            {
                _lastUpdatedPosition = value;
            }
        }
    }

    void Awake()
    {
        chunkSize = terrainGenerator.chunkSize;
        tileSize = terrainGenerator.tileSize;
        gridWorldSize = new Vector2(chunkSize * 2 + tileSize, chunkSize * 2 + tileSize);
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / tileSize);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / tileSize);
    }

    void Start()
    {
        lastUpdatedPosition = terrainGenerator.lastUpdatedPosition;
        nodeGrid = CreateNodeGrid(lastUpdatedPosition, chunkSize);
    }

    void Update()
    {
        Vector3 currentUpdatedPosition = terrainGenerator.lastUpdatedPosition;
        if (currentUpdatedPosition != lastUpdatedPosition)
        {
            lastUpdatedPosition = currentUpdatedPosition;
            nodeGrid = CreateNodeGrid(lastUpdatedPosition, chunkSize);
        }
    }
    #region Node Functions
    public Node[,] CreateNodeGrid(Vector3 centerPosition, int chunkSize)
    {
        Node[,] nodeGrid = new Node[2 * chunkSize + 1, 2 * chunkSize + 1];

        for (int x = -chunkSize; x <= chunkSize; x++)
        {
            for (int z = -chunkSize; z <= chunkSize; z++)
            {
                Vector3 worldPoint = centerPosition + new Vector3(x, 1, z);
                LayerMask walkableMask = LayerMask.GetMask("Walkable");
                bool walkable = Physics.CheckSphere(worldPoint, 0.1f, walkableMask);
                Vector3 nodePosition = new(worldPoint.x, 13, worldPoint.z);
                nodeGrid[x + chunkSize, z + chunkSize] = new Node(walkable, nodePosition, x + chunkSize, z + chunkSize);
            }
        }
        return nodeGrid;
    }
    #endregion
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        Node closestNode = null;
        float closestDistance = Mathf.Infinity;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Node node = nodeGrid[x, y];
                float distance = Vector3.Distance(worldPosition, node.worldPosition);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = node;
                }
            }
        }

        return closestNode;
    }

    void OnDrawGizmos() // draw path in debug view
    {
        if (nodeGrid != null)
        {
            foreach (Node n in nodeGrid)
            {
                Gizmos.color = n.walkable ? Color.white : Color.red;
                if (path != null)
                    if (path.Contains(n))
                        Gizmos.color = Color.black;
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (tileSize - .1f));
            }
        }
    }
}
