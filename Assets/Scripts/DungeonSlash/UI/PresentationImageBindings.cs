using System;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class PresentationImageBindings:MonoBehaviour
    {
        [Serializable]public sealed class Binding{public Image image;public Sprite arcade;public Color color;public bool overrideMaterial;public Material arcadeMaterial;}
        public Binding[] bindings;
        public GameFlowController flow;
        public Sprite[] geometryEvents,arcadeEvents;
        public void Apply(bool arcade)
        {
            if(flow!=null)flow.SetEventArt(arcade?arcadeEvents:geometryEvents);
            foreach(var binding in bindings)
            {
                if(binding.image==null)continue;
                binding.image.sprite=arcade?binding.arcade:null;
                binding.image.color=arcade?binding.color:new Color(.09f,.12f,.17f,binding.color.a);
                if(binding.overrideMaterial)binding.image.material=arcade?binding.arcadeMaterial:null;
            }
        }
    }
}
