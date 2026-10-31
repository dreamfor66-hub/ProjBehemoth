using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed partial class DebugCheatOverlay
    {
        [Header("Chapter and world-object inspection")]
        public GameObject chapterTools, catalogHoverHint;
        public GameFlowController chapterFlow;
        public Button[] chapterButtons, objectDirectionButtons;
        public Button objectStateButton, placeObjectButton, clearObjectButton;
        public TextMeshProUGUI objectStateLabel;
        public List<RoomObjectData> roomObjectCatalog = new();
        private RoomObjectData selectedRoomObject;
        private int objectDirection;
        private bool objectUsed;

        private void WireChapterButtons()
        {
            if (chapterFlow == null) chapterFlow = FindFirstObjectByType<GameFlowController>();
            if (chapterButtons != null) for (var i = 0; i < chapterButtons.Length; i++)
            {
                var act = i + 1; chapterButtons[i].onClick.RemoveAllListeners();
                chapterButtons[i].onClick.AddListener(() =>
                {
                    if (chapterFlow == null || !chapterFlow.DebugJumpToChapter(act)) { SetStatus(GameText.Get("ui.debugchapter.unavailable")); return; }
                    objectUsed = false; SetOpen(false);
                });
            }
            if (objectDirectionButtons != null) for (var i = 0; i < objectDirectionButtons.Length; i++)
            {
                var direction = i; objectDirectionButtons[i].onClick.RemoveAllListeners();
                objectDirectionButtons[i].onClick.AddListener(() => { objectDirection = direction; RefreshChapterTools(); });
            }
            WireButton(objectStateButton, () => { objectUsed = !objectUsed; RefreshChapterTools(); });
            WireButton(placeObjectButton, () =>
            {
                if (chapterFlow == null || !chapterFlow.DebugPlaceRoomObject(selectedRoomObject, objectDirection, objectUsed))
                { SetStatus(GameText.Get("ui.debugchapter.unavailable")); return; }
                SetOpen(false);
            });
            WireButton(clearObjectButton, () =>
            {
                if (chapterFlow == null || !chapterFlow.DebugClearInspectionRoom()) return;
                SetOpen(false);
            });
        }
        private void RefreshChapterTools()
        {
            if (chapterTools == null) return;
            var wall = selectedRoomObject != null && (selectedRoomObject.wallMounted || chapterFlow != null && chapterFlow.DebugIsPassageArtwork(selectedRoomObject));
            if (wall && objectDirection == 2) objectDirection = 0;
            if (objectStateLabel != null) objectStateLabel.text = GameText.Get(objectUsed ? "ui.debugchapter.used" : "ui.debugchapter.available");
            if (objectDirectionButtons != null) for (var i = 0; i < objectDirectionButtons.Length; i++)
            {
                var button = objectDirectionButtons[i]; button.interactable = !wall || i != 2;
                button.GetComponentInChildren<TextMeshProUGUI>().text = GameText.Get("ui.debugchapter." + (wall ? "wall" : "view") + i);
                button.image.color = i == objectDirection ? new Color32(114, 83, 40, 255) : new Color32(39, 41, 47, 255);
            }
            if (chapterButtons != null) for (var i = 0; i < chapterButtons.Length; i++)
                chapterButtons[i].image.color = run?.State != null && run.State.CurrentAct == i + 1 ? new Color32(114, 83, 40, 255) : new Color32(39, 41, 47, 255);
            if (placeObjectButton != null) placeObjectButton.interactable = selectedRoomObject != null && chapterFlow != null;
        }
    }
}
