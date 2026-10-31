using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    public sealed class ResultView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI title,details;
        [SerializeField] private Button restart;
        public CanvasGroup veil,headingGroup,actions;
        public Image mourningBand;
        public float revealSeconds=1.4f,actionsDelay=2.8f;
        public bool IsRevealing {get;private set;}
        public void Configure(GameObject newPanel,TextMeshProUGUI newTitle,TextMeshProUGUI newDetails,Button newRestart)
        {panel=newPanel;title=newTitle;details=newDetails;restart=newRestart;}
        public void Show(string heading,string body,Action restartAction,bool victory=false)
        {
            StopAllCoroutines();panel.SetActive(true);title.text=heading;details.text=body;
            title.color=victory?new Color32(225,197,126,255):new Color32(190,45,39,255);
            restart.onClick.RemoveAllListeners();restart.onClick.AddListener(()=>restartAction());
            if(veil==null||headingGroup==null||actions==null)return;
            StartCoroutine(Reveal());
        }
        IEnumerator Reveal()
        {
            IsRevealing=true;veil.alpha=headingGroup.alpha=actions.alpha=0;actions.interactable=actions.blocksRaycasts=false;
            restart.interactable=false;
            for(float elapsed=0;elapsed<actionsDelay+.5f;elapsed+=Time.unscaledDeltaTime)
            {
                veil.alpha=Mathf.SmoothStep(0,1,elapsed/.7f);
                headingGroup.alpha=Mathf.SmoothStep(0,1,(elapsed-.25f)/Mathf.Max(.01f,revealSeconds));
                title.characterSpacing=Mathf.Lerp(8,20,Mathf.Clamp01(elapsed/actionsDelay));
                actions.alpha=Mathf.SmoothStep(0,1,(elapsed-actionsDelay)/.5f);
                yield return null;
            }
            veil.alpha=headingGroup.alpha=actions.alpha=1;restart.interactable=true;actions.interactable=actions.blocksRaycasts=true;IsRevealing=false;
        }
        public void Hide(){StopAllCoroutines();IsRevealing=false;panel.SetActive(false);}
    }
}
