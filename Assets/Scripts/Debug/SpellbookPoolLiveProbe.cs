#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class SpellbookPoolLiveProbe:MonoBehaviour
{
    const string Log="Logs/SpellbookPool-Live.txt";
    bool background;
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy(){Application.runInBackground=background;}
    void Check(bool ok,string text){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)Debug.LogError("Spellbook pool: "+text);}
    public void Run()=>StartCoroutine(Verify());
    IEnumerator Verify()
    {
        File.WriteAllText(Log,"Scoped saved-pool / natural chest and first-merchant checks. Existing Unity instance.\n");yield return null;
        var run=Object.FindFirstObjectByType<RunController>();var combat=Object.FindFirstObjectByType<CombatController>();var content=combat.Content;
        var expansion=Object.FindFirstObjectByType<RunExpansionView>();expansion.HideModal();Object.FindFirstObjectByType<RoomEventView>(FindObjectsInactive.Include)?.Hide();
        var additions=new[]{"Tome_EnchantFire","Tome_EnchantIce","Tome_EnchantLightning","Tome_PoisonMist","Tome_Cleanse","Tome_LightningMark"}.Select(id=>content.equipment.Single(e=>e.id==id)).ToArray();
        Check(additions.All(b=>run.Equipment.Contains(b))&&run.Equipment.Count==run.Equipment.Distinct().Count(),"all six ordinary books exist once in the saved playable equipment pool");
        Check(additions.All(b=>b.grantedSpell.tier<2&&b.scope==RelicScope.Shared&&b.price==38),"existing ordinary classification and prices preserved");
        var regular=content.equipment.Where(b=>b!=null&&b.IsRelic&&b.scope==RelicScope.Shared&&b.grantedSpell!=null&&b.grantedSpell.tier<2).ToArray();
        Check(regular.All(b=>run.Equipment.Contains(b))&&regular.Length==12,"all twelve regular spellbooks reach the playable pool");
        Check(run.Equipment.Where(b=>b!=null&&b.IsRelic&&b.scope==RelicScope.Shared&&b.grantedSpell!=null).All(b=>b.grantedSpell.tier<2),"advanced spells remain outside ordinary book rewards");
        var chestHits=additions.ToDictionary(b=>b,b=>0);var shopHits=additions.ToDictionary(b=>b,b=>0);var noEnchantShops=0;var sizes=true;
        IReadOnlyList<EquipmentData> sample=null;EquipmentData purchase=null,chest=null;var sampleSeed=0;
        for(var seed=0;seed<1024;seed++)
        {
            var drop=run.GetChestRelic(seed,37);if(drop!=null&&chestHits.ContainsKey(drop))chestHits[drop]++;
            if(chest==null&&drop!=null&&additions.Take(3).Contains(drop))chest=drop;
            var stock=run.GetShopInventory(seed,1);sizes&=stock.Count==6&&stock.Count(e=>e.IsPotion)==3&&stock.Count(e=>e.IsRelic)==3;
            foreach(var b in additions)if(stock.Contains(b))shopHits[b]++;
            if(!stock.Any(b=>additions.Take(3).Contains(b)))noEnchantShops++;
            var candidate=stock.FirstOrDefault(b=>additions.Take(3).Contains(b)&&b!=chest);
            if(sample==null&&candidate!=null){sample=stock;purchase=candidate;sampleSeed=seed;}
        }
        foreach(var b in additions){Check(chestHits[b]>0,"natural B1 chest can offer "+b.grantedSpell.displayName);Check(shopHits[b]>0,"natural first merchant can stock "+b.grantedSpell.displayName);File.AppendAllText(Log,$"{b.name}: chest {chestHits[b]}/1024; merchant {shopHits[b]}/1024\n");}
        Check(noEnchantShops>0&&noEnchantShops<1024,"enchantments are possible, never a forced merchant slot");Check(sizes,"merchant remains three ordinary items plus three potions");
        var shop=Object.FindFirstObjectByType<ShopView>(FindObjectsInactive.Include);bool bought=false;combat.InputPaused=true;
        shop.Show(sample,run.State,run.Shop,b=>{bought=run.TryBuy(b);},()=>{shop.Hide();combat.InputPaused=false;});yield return null;
        var card=shop.GetComponentsInChildren<ShopItemView>().Single(c=>c.Data==purchase);card.GetComponent<Button>().onClick.Invoke();yield return null;yield return new WaitForEndOfFrame();MortalisLiveProbe.Capture("NaturalEnchantmentShop");
        Check(shop.focusedDescription.text.Contains(purchase.displayName)&&shop.purchase.interactable,"natural enchantment merchant entry previews and is purchasable with starting gold");
        var gold=run.State.Gold;shop.purchase.onClick.Invoke();yield return null;
        Check(bought&&run.State.RuntimeFor(purchase.grantedSpell)!=null&&run.State.Gold==gold-purchase.price,"merchant purchase spends gold once and learns the existing enchantment");
        Check(sample.SequenceEqual(run.GetShopInventory(sampleSeed,1)),"purchase does not refill or reroll merchant stock");shop.Hide();combat.InputPaused=false;
        // Find another naturally drawn enchantment to exercise occupied-direction storage through the real acquisition UI.
        chest=Enumerable.Range(0,1024).Select(seed=>run.GetChestRelic(seed,37)).First(b=>b!=null&&additions.Take(3).Contains(b)&&b!=purchase);
        var slot=run.State.Slot(chest.grantedSpell.defaultSlot);Check(run.TryGrantRelic(chest),"naturally drawn chest book is granted through normal relic acquisition");
        var resolved=false;expansion.ResolvePendingSpells(()=>resolved=true);yield return null;
        if(!resolved)expansion.choices[1].button.onClick.Invoke();yield return null;
        Check(resolved&&run.State.RuntimeFor(chest.grantedSpell)!=null&&run.State.Slot(chest.grantedSpell.defaultSlot)==slot,"chest enchantment can be stored without replacing the prepared spell");
        File.AppendAllText(Log,"COMPLETE\n");
    }
}
#endif
