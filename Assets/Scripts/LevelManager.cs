using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AStar;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour {
    public AStarGrid aStarGrid;
    public GameObject player;
    public GameObject floorTemplate, wallTemplate, gateTemplate, outletTemplate;

    [HideInInspector]
    public GameObject outlet;

    public static LevelManager self;
    public SynergySystem synergySystem;

    public int rooms = 5;
    public bool generateOnStart = true, genEnemy = true;
    private int generateCount = 0;
    public float wallHeight = 6, wallThickness = 0.1f;
    private float unit;

    [HideInInspector]
    public int powerOutput;

    private List<Enemy> enemyList = new List<Enemy>();
    private Dictionary<string, int> enemyCounts = new Dictionary<string, int>();

    private List<Vector3> gateWalls = new List<Vector3>();
    private List<Vector3> floorPositions = new List<Vector3>();
    private List<WallValues> wallNodes = new List<WallValues>();
    private List<WallDetect> walls = new List<WallDetect>();
    public class WallValues {
        public readonly WallDetect wall;
        public readonly Vector2 fact;
        public readonly Transform parent;

        public WallValues(WallDetect wall, Vector2 fact, Transform parent) {
            this.wall = wall;
            this.fact = fact;
            this.parent = parent;
        }

        public bool ShouldBePlaced() {
            return wall.Detect();
        }
    }

    [Header("NPC Spawning")]
    public Enemy[] allEnemies;
    public Enemy[] enemiesToSpawn;
    public bool debugAI;

    public static bool debuggingAI;

    void Awake() {
        self = this;
        CleanUp();
        debuggingAI = debugAI;
        if(generateOnStart) GenerateWorld();
        if(genEnemy) SpawnEnemy();
    }

    public void CleanUp() {
        floorPositions.Clear();
        wallNodes.Clear();
        wallPositions.Clear();
        gateWalls.Clear();
        enemyList.Clear();
    }

    protected void SpawnEnemy() {
        foreach(Enemy e in allEnemies) enemyCounts.Add(e.name, 0);

        int count = 4;//Random.Range(2, 5);
        for(int i = 0; i < count; i++) {
            Vector3 pos = floorPositions[Random.Range(0, floorPositions.Count)] * unit;
            Enemy enemy = enemiesToSpawn[Random.Range(0, enemiesToSpawn.Length)];
            Enemy en = Instantiate(enemy, pos, Quaternion.identity);
            en.transform.position = pos;
            en.name = enemy.name;
            en.player = player;
            en.StartAI(outlet);
            enemyList.Add(en);
            enemyCounts[en.name]++;
        }
    }

    public static void SpawnSynergy(SynergySystem.Recipe recipe, Vector3 pos) {
        Enemy enemy = recipe.result;
        Enemy en = Instantiate(enemy, pos, Quaternion.identity);
        en.transform.position = pos;
        en.name = enemy.name;
        en.player = self.player;
        en.StartAI(self.outlet);
        self.enemyList.Add(en);
        self.enemyCounts[en.name]++;
    }

    void FixedUpdate() {
        ClearSynergy();
        LookForSynergy();
    }

    public void ClearSynergy() {
        synergySystem.targetRecipe = "";
        synergySystem.availableRecipe = null;
    }

    public void LookForSynergy() {
        CalculatePower();
        var recipe = synergySystem.GetPossibleRecipe(enemyCounts);
        if(recipe == null) return;
        if(debuggingAI) Debug.LogError(recipe.result.name + " recipe found!");

        //Calculates whether the enemy combo is  an efficient tradeoff
        //Current Power Output = [All enemy power values summed up] * (amount of enemies)
        int currentPower = powerOutput;
        int recipeGain = recipe.result.powerValue;
        if(debuggingAI) Debug.Log("Current Power Output: " + currentPower);

        int resultPower = powerOutput / enemyList.Count;
        foreach(Enemy ingredient in recipe.recipe) resultPower -= ingredient.powerValue;
        
        resultPower += recipeGain;
        resultPower *= enemyList.Count - 1;
        bool condition =  (resultPower > currentPower / 2);
        if(debuggingAI) Debug.Log("Resulting Power Output: " + resultPower);
        if(debuggingAI) Debug.Log("Worth it?  " + condition);
        if(debuggingAI) Debug.Log("=====================");

        if(condition) {
            synergySystem.availableRecipe = recipe;
            synergySystem.targetRecipe = recipe.result.name;
        }
    }

    public void CalculatePower() {
        powerOutput = 0;
        foreach(Enemy en in enemyList) powerOutput += en.powerValue;
        powerOutput *= enemyList.Count;
    }

    public static void RemoveEnemyFromList(Enemy en) {
        foreach(Enemy enemy in self.enemyList) {
            if(enemy.GetInstanceID() == en.GetInstanceID()) {
                self.enemyCounts[enemy.name]--;
                self.enemyList.Remove(enemy);
                return;
            }
        }
    }

    protected void SpawnOutlet(Vector3 pos) {
        outlet = Instantiate(outletTemplate);
        outlet.transform.position = new Vector3(pos.x, 0, pos.z) * unit;
    }

    protected void GenerateWorld() {
        ClearWorld();
        unit = floorTemplate.transform.localScale.x;
        SpawnFloor(Vector3.zero, "Base Floor");

        //Generate Floorplan
        Vector3 oldPos = Vector3.zero;
        for(int l = 0; l < rooms; l++) {
            int x = Random.Range(-1, 2);
            int y = Random.Range(-1, 2);
            if(x == 0 && y == 0) x = 1;
            if(x != 0 && y != 0) {
                l--;
                continue;
            }
            oldPos += new Vector3(x, 0, y);
            SpawnFloor(oldPos);
        }
        OpenWalls();
        GenerateGate();
        SpawnOutlet(floorPositions[Random.Range(0, floorPositions.Count)]);
        
        aStarGrid.GenNodes();
    }

    public static void GenerateNewWorld() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    protected void OpenWalls() {
        for(int i = 0; i < wallNodes.Count; i++) {
            if(wallNodes[i].ShouldBePlaced()) SpawnWalls(wallNodes[i]);
        }
        foreach(WallDetect wall in walls) wall.RemoveIndoorWalls();
    }

    protected void GenerateGate() {
        Vector3 pos = gateWalls[Random.Range(0, gateWalls.Count)];
        GameObject gateObj = Instantiate(gateTemplate, pos, Quaternion.identity);
        gateObj.name = gateTemplate.name;
        gateObj.transform.localPosition = new Vector3(gateObj.transform.localPosition.x, 1.5f, gateObj.transform.localPosition.z);
        gateObj.transform.localRotation = Quaternion.Euler(0, 90, 0);
    }

    protected GameObject SpawnFloor(Vector3 pos, string name = "Floor") {
        return SpawnFloor(pos, Vector2.one, 0, name);
    }
    protected GameObject SpawnFloor(Vector3 pos, Vector2 fact, float angle = 0, string name = "Floor") {
        if(floorPositions.Contains(pos)) return null;
        floorPositions.Add(pos);
        GameObject corridor = new GameObject(name);
        corridor.transform.SetParent(transform);
        corridor.transform.position = Vector3.zero;
        corridor.transform.localPosition = Vector3.zero;
        corridor.transform.localRotation = Quaternion.identity;
        corridor.transform.localScale = new Vector3(unit, unit, 1);
        GameObject go = Instantiate(floorTemplate);
        go.name = name;
        go.transform.SetParent(corridor.transform);
        go.transform.localRotation = Quaternion.Euler(0, 0, angle);
        go.transform.localScale = new Vector3(fact.x, fact.y, 1);
        go.transform.localPosition = new Vector3(pos.x + (fact.x - 1) / 2, pos.z + (fact.y - 1) / 2, pos.y + Random.Range(generateCount, generateCount + 1f) / 1000);
        generateCount++;
        go.GetComponent<MeshRenderer>().material.SetTextureScale("_MainTex", new Vector2(unit * 2 * fact.x, unit * 2 * fact.y));
        foreach(Transform t in go.transform) t.GetComponent<WallDetect>().CleanDuplicateDetectors();

        foreach(Transform t in go.transform) {
            WallDetect wallNode = t.GetComponent<WallDetect>();
            if(t.tag == "WallDetector" && wallNode.Detect()) wallNodes.Add(new WallValues(wallNode, fact, go.transform));
        }
        return go;
    }
    
    private List<Vector3> wallPositions = new List<Vector3>();
    protected void SpawnWalls(WallValues val) {
        float wallThicc = wallThickness / 10f;
        for(int i = -1; i <= 1; i += 2) {
            for(int j = -1; j <= 1; j += 2) {
                GameObject go = Instantiate(wallTemplate);
                go.name = "Wall";
                go.transform.SetParent(val.parent);
                go.transform.localRotation = Quaternion.Euler(0, 90, 0);
                go.transform.localScale = new Vector3(wallHeight, 0.5f, wallThicc);
                go.transform.localPosition = new Vector3(0.5f * i, j * 0.25f, 0);
                go.GetComponent<MeshRenderer>().material.SetTextureScale("_MainTex", new Vector2(30, 60 * go.transform.localScale.y));
                 if(Util.ContainsVec3(wallPositions, go.transform.position)) {
                     Destroy(go.gameObject);
                     continue;
                 }
                wallPositions.Add(go.transform.position);
                walls.Add(go.GetComponent<WallDetect>());
            }
        }
        for(int i = -1; i <= 1; i += 2) {
            for(int j = -1; j <= 1; j += 2) {
                GameObject go = Instantiate(wallTemplate);
                go.name = "Wall";
                go.transform.SetParent(val.parent);
                go.transform.localRotation = Quaternion.Euler(0, 90, 0);
                go.transform.localScale = new Vector3(wallHeight, wallThicc, 0.5f);
                go.transform.localPosition = new Vector3(0.25f * j, 0.5f * i, 0);
                float fa = 1;
                if(val.parent.localScale.x != 1) fa = val.parent.localScale.x;
                go.GetComponent<MeshRenderer>().material.SetTextureScale("_MainTex", new Vector2(30, 60 * fa * go.transform.localScale.z));
                if(Util.ContainsVec3(wallPositions, go.transform.position)) {
                     Destroy(go.gameObject);
                     continue;
                 }
                wallPositions.Add(go.transform.position);
                walls.Add(go.GetComponent<WallDetect>());
                if(j == 1 && i == 1) gateWalls.Add(go.transform.position);
            }
        }
    }

    public void ClearWorld() {
        foreach(Transform chil in transform) if(chil.tag != "Don't destroy") Destroy(chil.gameObject);
    }
}
