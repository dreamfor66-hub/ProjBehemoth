using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class StageBackdropView : MonoBehaviour
    {
        public RunController run;
        public Image background;
        public Sprite[] stages;
        private int current = -1;
        private void LateUpdate()
        {
            if (run?.State == null || background == null || stages == null || stages.Length == 0) return;
            var index = Mathf.Clamp(run.State.CurrentAct-1,0,stages.Length-1);
            if (index == current) return; current = index; background.sprite = stages[index];
        }
    }
}
