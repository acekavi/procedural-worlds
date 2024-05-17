using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using SimplexNoise;

public class TerrainGenerator : MonoBehaviour
{
    #region User Inputs
    [Header("Customize Map")]
    [SerializeField] public int chunkSize = 20;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Material[] materials;
    [SerializeField] private GameObject[] tileContainers;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Thresholds")]
    [SerializeField] private long seed = 86594;
    [SerializeField] private float waterThreshold = 0.3f;
    [SerializeField] private float pathThreshold = 0.5f;
    [SerializeField] private float forestThreshold = 0.8f;
    [Range(0.01f, 0.1f)][SerializeField] private float noiseFrequency;

    [Header("Collectibles")]
    [SerializeField] private GameObject[] collectibleObjects;
    [SerializeField] private int maxCollectibles = 10;
    [SerializeField] private GameObject collectiblesContainer;

    [Header("Enemies")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private int maxEnemies = 5;
    [SerializeField] private GameObject enemiesContainer;

    [Header("Path Visualization")]
    #endregion

    #region Collectibles properties
    private List<Vector3> collectiblePositions = new(); // List to store the positions of spawned collectibles
    private Dictionary<GameObject, GameObject> tileCollectibleMap = new();
    private List<GameObject> spawnedCollectibles = new();
    #endregion

    #region Enemy properties
    private List<Vector3> enemyPositions = new(); // List to store the positions of spawned enemies
    private Dictionary<GameObject, GameObject> tileEnemyMap = new();
    private List<GameObject> spawnedEnemies = new();
    #endregion

    #region Tiles properties
    public float tileSize = 1f;
    private Renderer tileRenderer = null;
    private TilePoolManager tilePoolManager = null;
    private readonly Dictionary<Vector3, GameObject> activeTiles = new();
    [System.NonSerialized] public Vector3 lastUpdatedPosition = Vector3.zero;
    #endregion

    #region Player properties
    private Transform playerTransform = null;
    #endregion

    #region NavMesh properties
    private NavMeshData navMeshData = null;
    private NavMeshSurface navMeshSurface = null;

    private LineRenderer lineRenderer;
    #endregion

    #region Debug properties
    public Node[,] nodeGrid;
    #endregion
    #region Unity Functions
    void Start()
    {
        // Add a NavMeshSurface component to the pathContainer so we can bake the NavMesh
        navMeshSurface = GetComponent<NavMeshSurface>();
        navMeshSurface.layerMask = LayerMask.GetMask("Walkable");
        navMeshData = new NavMeshData();
        navMeshSurface.navMeshData = navMeshData;

        // Get the tile size from the tile prefab
        tileSize = tilePrefab.transform.localScale.x;

        // Get the TilePoolManager component
        tilePoolManager = GetComponent<TilePoolManager>();
        lineRenderer = GetComponent<LineRenderer>();

        // Generate the initial tiles
        GenerateInitialTiles();
        MovePlayerToPath();
    }

    void Update()
    {
        if (playerTransform != null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            float updateRadius = chunkSize - (chunkSize / 4); // Adjust this if needed based on your grid setup

            // Calculate distance between current position and last updated position
            float distanceMoved = Vector3.Distance(playerTransform.position, lastUpdatedPosition);

            if (distanceMoved > updateRadius)
            {
                lastUpdatedPosition = playerTransform.position;

                int playerGridX = Mathf.RoundToInt(playerTransform.position.x / tileSize);
                int playerGridZ = Mathf.RoundToInt(playerTransform.position.z / tileSize);
                // Update the grid around the player
                StartCoroutine(UpdateTileGridAroundPlayer(playerGridX, playerGridZ));
            }
        }
        else
        {
            // Handle the case where playerTransform is not yet assigned
            StartCoroutine(WaitForPlayerInitialization());
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            // Visualize the path
            VisualizePath();
        }
    }
    #endregion

    #region Terrain Generation Functions
    private void GenerateInitialTiles()
    {
        for (int x = -chunkSize; x <= chunkSize; x++)
        {
            for (int z = -chunkSize; z <= chunkSize; z++)
            {
                Vector3 tilePosition = new(x * tileSize, 0, z * tileSize);
                float noiseValue = CalculateTerrainHeight(tilePosition);
                GameObject currentTile = CreateTile(tilePosition, noiseValue);

                // Add the initially generated tiles to the active tiles collection
                activeTiles.Add(tilePosition, currentTile);
            }
        }
        lastUpdatedPosition = Vector3.zero;
        BuildNavMeshAndAddCollectibles();
    }

    private GameObject CreateTile(Vector3 tilePosition, float noiseValue)
    {
        GameObject tile = tilePoolManager.GetTileFromPool(tilePosition);
        float tileHeight = CategorizeTile(noiseValue, tile);
        tile.transform.localScale = new Vector3(tile.transform.localScale.x, tileHeight, tile.transform.localScale.z);
        tile.transform.position = new Vector3(tilePosition.x, tileHeight / 2, tilePosition.z);
        tile.isStatic = true;

        return tile;
    }
    private float CategorizeTile(float noiseValue, GameObject thisTile)
    {
        int index;
        float tileHeight;
        string layerName;

        if (noiseValue < waterThreshold)
        {
            index = 0;
            tileHeight = 1f;
            layerName = "Not Walkable";
        }
        else if (noiseValue < pathThreshold)
        {
            index = 1;
            tileHeight = 2f;
            layerName = "Walkable";
        }
        else if (noiseValue < forestThreshold)
        {
            index = 2;
            // tileHeight = (noiseValue * 20f) - 7f; // This is the original formula
            tileHeight = (noiseValue * 10f) - 2.5f;
            layerName = "Walkable";
        }
        else
        {
            index = 3;
            tileHeight = Random.Range(9f, 12f);
            layerName = "Not Walkable";
        }

        SetTileProperties(thisTile, index, layerName);

        return tileHeight;
    }
    private void SetTileProperties(GameObject tile, int materialIndex, string layerName)
    {
        tile.transform.SetParent(tileContainers[materialIndex].transform);
        tile.layer = LayerMask.NameToLayer(layerName);
        tileRenderer = tile.GetComponent<Renderer>();
        tileRenderer.material = materials[materialIndex];
    }
    void RemoveFarTiles(Vector3 playerPos, float maxDistance)
    {
        List<Vector3> tilesToRemove = new();

        // Iterate through active tiles and check their distance from the player
        foreach (var pair in activeTiles)
        {
            Vector3 tilePos = pair.Key;
            GameObject tileObject = pair.Value;

            float distance = Vector3.Distance(tilePos, playerPos);

            // Check if the distance exceeds the maximum allowed distance
            if (distance > maxDistance)
            {
                // Remove the tile's collectible if it has one
                if (tileCollectibleMap.ContainsKey(tileObject))
                {
                    GameObject collectible = tileCollectibleMap[tileObject];
                    spawnedCollectibles.Remove(collectible);
                    Destroy(collectible);
                    tileCollectibleMap.Remove(tileObject);
                }

                // Remove the tile's collectible if it has one
                if (tileEnemyMap.ContainsKey(tileObject))
                {
                    GameObject enemy = tileEnemyMap[tileObject];
                    spawnedEnemies.Remove(enemy);
                    Destroy(enemy);
                    tileEnemyMap.Remove(tileObject);
                }

                // Deactivate or return the tile to the pool
                tileObject.SetActive(false); // Assuming you deactivate the tile
                tilePoolManager.ReturnTileToPool(tileObject);
                tilesToRemove.Add(tilePos); // Mark this tile for removal from the active tiles collection
            }
        }

        // Remove tiles that are too far from the player from the activeTiles dictionary
        foreach (var tilePosToRemove in tilesToRemove)
        {
            activeTiles.Remove(tilePosToRemove);

        }
    }
    IEnumerator WaitForPlayerInitialization()
    {
        while (playerTransform == null)
        {
            // Continue checking until playerTransform is assigned
            yield return null; // Wait for the next frame before checking again
        }
    }
    private IEnumerator UpdateTileGridAroundPlayer(int playerGridX, int playerGridZ)
    {
        Vector3 playerPosition = new(playerGridX * tileSize, 0, playerGridZ * tileSize);

        // Remove tiles that are too far from the player
        RemoveFarTiles(playerPosition, chunkSize * tileSize);

        // Add new tiles around the player within the radius
        for (int x = playerGridX - chunkSize; x <= playerGridX + chunkSize; x++)
        {
            for (int z = playerGridZ - chunkSize; z <= playerGridZ + chunkSize; z++)
            {
                Vector3 tilePosition = new(x * tileSize, 0, z * tileSize);
                GameObject currentTile;
                // Check if the tile is already active
                if (!activeTiles.ContainsKey(tilePosition))
                {
                    float noiseValue = CalculateTerrainHeight(tilePosition);
                    currentTile = CreateTile(tilePosition, noiseValue);

                    // Add the initially generated tiles to the active tiles collection
                    activeTiles.Add(tilePosition, currentTile);
                }
            }
        }

        // Wait for the next frame to allow Unity to render the newly generated terrain
        yield return null;

        // Update the NavMesh over several frames
        // Wait for half a second before updating the NavMesh
        int framesToWait = 5;
        for (int i = 0; i < framesToWait; i++)
        {
            yield return null;
        }

        // After waiting, update the NavMesh
        BuildNavMeshAndAddCollectibles();
    }
    float CalculateTerrainHeight(Vector3 tilePosition)
    {
        float heightValue = OpenSimplex2.Noise3_ImproveXZ(seed, tilePosition.x * noiseFrequency, tilePosition.z * noiseFrequency, tilePosition.y * noiseFrequency);

        // Normalize the OpenSimplex noise value to [0, 1]
        heightValue = Mathf.InverseLerp(-1f, 1f, heightValue);

        // float heightValue = Mathf.PerlinNoise((tilePosition.x + seed) * noiseFrequency, (tilePosition.z + seed) * noiseFrequency);
        return heightValue;
    }
    private void BuildNavMeshAndAddCollectibles()
    {
        navMeshSurface.BuildNavMesh();
        GenerateCollectibles(); // Add this line to generate collectibles
        GenerateEnemies(); // Add this line to generate enemies
    }
    #endregion

    #region Collectibles Functions
    private void GenerateCollectibles()
    {
        // Get all the tiles in the tileContainers[2]
        Transform[] tiles = tileContainers[2].GetComponentsInChildren<Transform>();

        foreach (Transform tile in tiles)
        {
            // Check if the tile has a valid NavMesh
            if (NavMesh.SamplePosition(tile.position, out _, 1.5f, NavMesh.AllAreas))
            {
                // Check if the new position is far enough from existing collectibles
                Vector3 newPosition = tile.position + new Vector3(0, tile.localScale.y, 0);
                if (IsFarEnough(newPosition, chunkSize, collectiblePositions))
                {
                    // Spawn a random collectible at the tile's position
                    GameObject collectiblePrefab = collectibleObjects[Random.Range(0, collectibleObjects.Length)];
                    GameObject spawnedCollectible = Instantiate(collectiblePrefab, newPosition, Quaternion.identity);
                    spawnedCollectible.transform.SetParent(collectiblesContainer.transform);

                    // Add the spawned collectible and its position to the lists
                    spawnedCollectibles.Add(spawnedCollectible);
                    collectiblePositions.Add(newPosition);
                    tileCollectibleMap.Add(tile.gameObject, spawnedCollectible);

                    // Stop spawning collectibles if we've reached the maximum
                    if (spawnedCollectibles.Count >= maxCollectibles)
                    {
                        break;
                    }
                }
            }
        }
    }

    private bool IsFarEnough(Vector3 newPosition, float minDistance, List<Vector3> positionsList)
    {
        foreach (Vector3 position in positionsList)
        {
            if (Vector3.Distance(position, newPosition) < minDistance)
            {
                return false;
            }
        }
        return true;
    }
    #endregion

    #region Player Related Functions
    private void MovePlayerToPath()
    {
        GameObject pathContainer = tileContainers[1];
        if (pathContainer.transform.childCount > 0)
        {
            int middleIndex = pathContainer.transform.childCount / 2;
            int deviation = Mathf.Max(1, pathContainer.transform.childCount / 10); // Allow a deviation of up to 10% from the middle
            int randomIndex = Random.Range(Mathf.Max(0, middleIndex - deviation), Mathf.Min(pathContainer.transform.childCount, middleIndex + deviation));
            Transform spawnTile = pathContainer.transform.GetChild(randomIndex);

            Vector3 spawnPosition = spawnTile.position + new Vector3(0, spawnTile.localScale.y / 2 + playerPrefab.transform.localScale.y / 2, 0);
            GameObject playerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            // playerPrefab.transform.position = spawnPosition;
            playerTransform = spawnTile;
        }
        else
        {
            Debug.LogError("No tiles in pathContainer to spawn player on!");
        }
    }
    #endregion

    #region Enemy Related Functions
    private void GenerateEnemies()
    {
        // Get all the tiles in the tileContainers[2]
        Transform[] tiles = tileContainers[1].GetComponentsInChildren<Transform>();

        foreach (Transform tile in tiles)
        {
            // Check if the tile has a valid NavMesh
            if (NavMesh.SamplePosition(tile.position, out _, 1.5f, NavMesh.AllAreas))
            {
                // Check if the new position is far enough from existing collectibles
                Vector3 newPosition = tile.position + new Vector3(0, tile.localScale.y, 0);
                if (IsFarEnough(newPosition, chunkSize, enemyPositions))
                {
                    // Spawn a random collectible at the tile's position
                    GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                    GameObject spawnedEnemy = Instantiate(enemyPrefab, newPosition, Quaternion.identity);
                    spawnedEnemy.transform.SetParent(enemiesContainer.transform);

                    // Add the spawned collectible and its position to the lists
                    spawnedEnemies.Add(spawnedEnemy);
                    enemyPositions.Add(newPosition);
                    tileEnemyMap.Add(tile.gameObject, spawnedEnemy);

                    // Stop spawning collectibles if we've reached the maximum enemy number
                    if (spawnedEnemies.Count >= maxEnemies)
                    {
                        break;
                    }
                }
            }
        }
    }
    #endregion

    #region Path Visualization Functions
    private void VisualizePath()
    {
        try
        {
            GameObject trackCoin = GameObject.FindGameObjectsWithTag("Coins")[0];
            NavMeshPath path = new();
            NavMesh.CalculatePath(playerTransform.position, trackCoin.transform.position, NavMesh.AllAreas, path);
            Vector3[] elevatedCorners = new Vector3[path.corners.Length];
            float yOffset = 10f;  // Adjust this value as needed

            for (int i = 0; i < path.corners.Length; i++)
            {
                Vector3 elevatedCorner = path.corners[i];
                elevatedCorner.y += yOffset;
                elevatedCorners[i] = elevatedCorner;
            }

            lineRenderer.positionCount = elevatedCorners.Length;
            lineRenderer.SetPositions(elevatedCorners);
            lineRenderer.enabled = true;

            // Hide the path after 5 seconds
            StartCoroutine(HidePathAfterSeconds(5));
        }
        catch (System.Exception)
        {
            PlayerManager playerManager = playerTransform.GetComponentInParent<PlayerManager>();
            playerManager.ShowFeedback("No more coins to track!", 5);
            return;
        }
    }

    private IEnumerator HidePathAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        lineRenderer.enabled = false;
    }
    #endregion

}
