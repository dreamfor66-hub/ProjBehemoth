using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>Shows the earned values and pauses at an EXP level boundary for perk selection.</summary>
    public sealed class RewardPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text experienceText;
        [SerializeField] private Text goldText;
        [SerializeField] private Image experienceFill;
        [SerializeField] private Button confirmButton;
        [SerializeField, Min(.1f)] private float experienceAnimationSeconds = .8f;

        private Coroutine animation;

        public void Configure(GameObject newPanel, Text newExperienceText, Text newGoldText, Image newExperienceFill, Button newConfirmButton)
        {
            panel = newPanel;
            experienceText = newExperienceText;
            goldText = newGoldText;
            experienceFill = newExperienceFill;
            confirmButton = newConfirmButton;
        }

        public void Show(CombatReward reward, int startingLevel, int startingExperience, int startingRequirement, RunState finalState, Action reachedLevel, Action confirmed)
        {
            if (panel == null || finalState == null) return;
            panel.SetActive(true);
            PrepareRewardText(reward, confirmed);
            if (reward.Experience <= 0)
            {
                StopAnimation();
                SetConfirmInteractable(true);
                return;
            }
            var crossedLevel = finalState.Level > startingLevel;
            var target = crossedLevel ? 1f : Ratio(finalState.Experience, finalState.RequiredExperience);
            StartAnimation(startingLevel, startingExperience, startingRequirement, target, crossedLevel, reachedLevel);
        }

        public void ResumeAfterPerk(CombatReward reward, RunState state, Action confirmed)
        {
            if (panel == null || state == null) return;
            panel.SetActive(true);
            StopAnimation();
            PrepareRewardText(reward, confirmed);
            if (reward.Experience > 0)
                SetExperience(state.Level, state.Experience, state.RequiredExperience, Ratio(state.Experience, state.RequiredExperience));
            SetConfirmInteractable(true);
        }

        public void Hide()
        {
            StopAnimation();
            if (panel != null) panel.SetActive(false);
        }

        private void PrepareRewardText(CombatReward reward, Action confirmed)
        {
            SetExperienceVisible(reward.Experience > 0);
            SetGoldVisible(reward.Gold > 0);
            if (goldText != null && reward.Gold > 0) goldText.text = $"GOLD  +{reward.Gold:0000}";
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(() => confirmed?.Invoke());
            }
            SetConfirmInteractable(false);
        }

        private void StartAnimation(int level, int experience, int requirement, float target, bool crossedLevel, Action reachedLevel)
        {
            StopAnimation();
            animation = StartCoroutine(AnimateExperience(level, experience, requirement, target, crossedLevel, reachedLevel));
        }

        private IEnumerator AnimateExperience(int level, int experience, int requirement, float target, bool crossedLevel, Action reachedLevel)
        {
            var from = Ratio(experience, requirement);
            var elapsed = 0f;
            while (elapsed < experienceAnimationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / experienceAnimationSeconds);
                var ratio = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, progress));
                var shownExperience = Mathf.RoundToInt(ratio * requirement);
                SetExperience(level, shownExperience, requirement, ratio);
                yield return null;
            }
            SetExperience(level, Mathf.RoundToInt(target * requirement), requirement, target);
            animation = null;
            if (crossedLevel) reachedLevel?.Invoke();
            else SetConfirmInteractable(true);
        }

        private void SetExperience(int level, int experience, int requirement, float ratio)
        {
            if (experienceText != null) experienceText.text = $"LV {level}  EXP {experience:0000} / {requirement:0000}";
            if (experienceFill != null)
            {
                var normalized = Mathf.Clamp01(ratio);
                experienceFill.type = Image.Type.Simple;
                experienceFill.fillAmount = normalized;
                var rect = experienceFill.rectTransform;
                if (rect.parent is RectTransform parent)
                {
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, .5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(parent.rect.width * normalized, 0f);
                }
            }
        }

        private void SetExperienceVisible(bool visible)
        {
            if (experienceText != null) experienceText.gameObject.SetActive(visible);
            if (experienceFill != null && experienceFill.transform.parent != null)
                experienceFill.transform.parent.gameObject.SetActive(visible);
        }

        private void SetGoldVisible(bool visible)
        {
            if (goldText != null) goldText.gameObject.SetActive(visible);
        }

        private static float Ratio(int value, int maximum) => maximum <= 0 ? 0f : Mathf.Clamp01((float)value / maximum);
        private void SetConfirmInteractable(bool value) { if (confirmButton != null) confirmButton.interactable = value; }
        private void StopAnimation() { if (animation != null) StopCoroutine(animation); animation = null; }
    }
}
