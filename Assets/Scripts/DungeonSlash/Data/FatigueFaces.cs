using System;
using UnityEngine;
namespace DungeonSlash
{
    [CreateAssetMenu(menuName="Dungeon Slash/Fatigue Faces")]
    public sealed class FatigueFaces:ScriptableObject
    {
        [Serializable]public sealed class Hero {public CharacterKind character;public Sprite[] expressions=new Sprite[4];}
        public Hero[] heroes=Array.Empty<Hero>();
        public Sprite Face(CharacterKind character,int stage)
        {foreach(var hero in heroes)if(hero.character==character)return hero.expressions[Mathf.Clamp(stage,0,hero.expressions.Length-1)];return null;}
    }
}
