using UnityEngine;
using UnityEngine.SceneManagement;
namespace DungeonSlash
{
    public static class RunSession
    {
        public static PlayerCombatData SelectedCharacter;
        public static int? ForcedSeed;
        public static void Start(PlayerCombatData character) { SelectedCharacter = character; SceneManager.LoadScene("MainGameScene"); }
        public static void ReturnToSelection() => SceneManager.LoadScene("OutGameScene");
        public static void ReturnToTitle() { SelectedCharacter=null;ForcedSeed=null;SceneManager.LoadScene("OutGameScene"); }
    }
}
