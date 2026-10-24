using UnityEngine;

namespace DungeonSlash
{
    public static class GamePreferences
    {
        private const string Prefix="Behemoth.Options.";
        public static float Volume=>PlayerPrefs.GetFloat(Prefix+"Volume",1);
        public static bool Fullscreen=>PlayerPrefs.GetInt(Prefix+"Fullscreen",0)!=0;
        public static int WindowScale=>Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"WindowScale",3),2,4);
        public static bool ScreenShake=>PlayerPrefs.GetInt(Prefix+"ScreenShake",1)!=0;
        public static void SetVolume(float value){PlayerPrefs.SetFloat(Prefix+"Volume",Mathf.Clamp01(value));AudioListener.volume=Volume;}
        public static void SetScreenShake(bool value)=>PlayerPrefs.SetInt(Prefix+"ScreenShake",value?1:0);
        public static void SetFullscreen(bool value){PlayerPrefs.SetInt(Prefix+"Fullscreen",value?1:0);ApplyDisplay();}
        public static void SetWindowScale(int value){PlayerPrefs.SetInt(Prefix+"WindowScale",Mathf.Clamp(value,2,4));ApplyDisplay();}
        public static void Apply(){AudioListener.volume=Volume;ApplyDisplay();}
        private static void ApplyDisplay()
        {
            if(Application.isEditor)return;
            if(Fullscreen)Screen.SetResolution(Display.main.systemWidth,Display.main.systemHeight,FullScreenMode.FullScreenWindow);
            else Screen.SetResolution(384*WindowScale,224*WindowScale,FullScreenMode.Windowed);
        }
        public static void Save()=>PlayerPrefs.Save();
    }
}
