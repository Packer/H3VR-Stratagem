using UnityEngine;

namespace Stratagem
{
    [CreateAssetMenu(fileName = "Stratagem", menuName = "Stratagem/New Stratagem", order = 1)]
    public class Stratagem_Package : ScriptableObject
    {
        [Tooltip("Name of the package, should match the Assets/FOLDERNAME")]
        public string packageName;

        [Tooltip("Name of the stratagem")]
        public string title;
        [Tooltip("Short description of what this stratagem does")]
        public string description;
        public Sprite icon;
        [Tooltip("The input combination to spawn this stratagem \nU - Up\nD - Down\nL - Left\nR - Right")]
        public string inputs = "UDLR";
        [Tooltip("Instant = Instantly spawns all spawn items")]
        public DeployType deployType = DeployType.Instant;
        [Tooltip("How long before this deploys the spawns, set to 0 for Hellpods")]
        public float deployTimer = 5;
        [Tooltip("Time between each use of this stratagem")]
        public float cooldown = 20;
        [Tooltip("The color of the stratagem ball and beam")]
        public DeployColor deployColor = DeployColor.Blue;
        [Tooltip("The color of the type of stratagem, \nRed = Strike\nYellow = Emplacement/Supply")]
        public TextColor textColor = TextColor.Yellow;

        [HideInInspector, Tooltip("The time stamp this stratagem can be used again")]
        public float timeout = 0;


        [Header("Spawn")]
        [Tooltip("GameObjects that will spawn as soon as the Grenade activates")]
        public GameObject[] spawnInstantPrefab;
        [Tooltip("GameObjects that will spawn as soon as the deploy type has triggered")]
        public GameObject[] spawnPrefabs;
        [Tooltip("All Object IDs that will spawn when the deploy type has triggered")]
        public string[] spawnObjectIDs;
        [Tooltip("All Sosigs that will spawn when the deploy type has triggered")]
        public int[] spawnSosigEnemyIDs;

        public enum DeployType
        {
            Instant = 0,
            Hellpod = 1,
        }

        public enum DeployColor
        {
            Blue = 0,
            Red = 1,
        }

        public enum TextColor
        {
            Yellow = 0,
            Red = 1,
        }
    }
}
