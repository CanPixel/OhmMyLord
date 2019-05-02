using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SynergySystem : MonoBehaviour {
    public string targetRecipe;

    [HideInInspector]
    public Recipe availableRecipe;
    public List<Recipe> recipes = new List<Recipe>();

    private static SynergySystem self;
    private List<Enemy> synergies = new List<Enemy>();

    private static float synergyDelay = 0;

    void Start() {
        self = this;
    }

    void FixedUpdate() {
        if(synergyDelay > 0) synergyDelay -= Time.deltaTime;
    }

    public static void DelayNextSynergy() {
        synergyDelay = Random.Range(4, 8);
    }

    public static bool SynergyPossible() {
        return self.synergies.Count <= 0 && synergyDelay <= 0;
    }

    public static void AddSynergy(Enemy e1, Enemy e2) {
        if(!SynergyPossible()) return;
        e1.synergy = e2.synergy = true;
        self.synergies.Add(e1);
        self.synergies.Add(e2);
    }

    public static void RemoveSynergy(Enemy e1, Enemy e2) {
        e1.synergy = e2.synergy = false;
        self.synergies.Remove(e1);
        self.synergies.Remove(e2);
    }

    public static Recipe GetAvailableRecipe() {
        return self.availableRecipe;
    }

    [System.Serializable]
    public class Recipe {
        public Enemy[] recipe;
        public Enemy result;

        public Dictionary<string, int> enemies = new Dictionary<string, int>();
    }

    public Recipe GetPossibleRecipe(Dictionary<string, int> enemies) {
        foreach(Recipe rec in recipes) if(RecipePossible(enemies, rec)) return rec;
        return null;
    }

    public bool RecipePossible(Dictionary<string, int> enemies, Recipe recipe) {
        bool recipePossible = true;
        foreach(Enemy en in recipe.recipe) {
            if(enemies[en.name] <= 0) recipePossible = false;
        }
        return recipePossible;
    }

    public static Enemy GetMatchingComponent(Enemy origin, Enemy[] list, Recipe recipe) {
        Enemy partner = null;
        if(recipe == null) return null;
        foreach(Enemy e in recipe.recipe) if(e.name != origin.name) partner = e;
        if(partner == null) return null;
        foreach(Enemy en in list) {
            if(en.name == partner.name) return en;
        }
        return null;
    }
}
