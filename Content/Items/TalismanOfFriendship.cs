using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using TalismanOfFriendship.Helpers;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TalismanOfFriendship.Content.Items {
    public class TalismanOfFriendship : ModItem {
        public override void SetStaticDefaults() {
            // Tooltip.SetDefault("Stores your pets for safekeeping\nRight click with a pet in your cursor to drop it in\nShift right click to release all contained pets\nEquip to summon your horde!\nThis is a temporary tooltip");
        }

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 28;
            Item.value = Item.sellPrice(gold: 1);
            Item.rare = ItemRarityID.Green;
            Item.buffType = ModContent.BuffType<TotallyAPetBuff>();
            Item.shoot = ModContent.ProjectileType<TotallyAPetProjectile>();
        }

        public override void AddRecipes() {
            CreateRecipe()
                    .AddIngredient(ItemID.Sapphire, 5)
                    .AddIngredient(ItemID.Chain)
                    .AddTile(TileID.Tables)
                    .AddTile(TileID.Chairs)
                    .Register();
        }

        private List<Item> _pets;
        public List<Item> Pets {
            get {
                _pets ??= new();
                return _pets;
            }
            set {
                _pets = value;
            }
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            foreach (var pet in Pets) {
                player.AddBuff(pet.buffType, 10);
            }
        }

        public override bool CanRightClick() => Main.keyState.IsKeyDown(Keys.LeftShift) || (Main.vanityPet[Main.mouseItem.buffType] && Main.mouseItem.type != ItemID.None);

        public override void RightClick(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                if (Main.mouseItem.type == ItemID.None && Main.keyState.IsKeyDown(Keys.LeftShift)) {
                    foreach (var pet in Pets) {
                        player.QuickSpawnItem(Item.GetSource_Loot(), pet.type);
                    }
                    Pets = new();
                } else if (Main.vanityPet[Main.mouseItem.buffType] && Main.mouseItem.type != ItemID.None) {
                    if (Pets.Find(i => i.type == Main.mouseItem.type) == null) {
                        Pets.Add(new(Main.mouseItem.type));
                        Main.mouseItem.stack--;
                    }
                }
                Item.stack++;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.InsertTooltips(GetItemTooltips(), "EtherianManaWarning");
        }
        
        private IEnumerable<TooltipLine> GetItemTooltips() {
            return Pets.Select((pet, index) => new TooltipLine(Mod, $"Pet{index}", $"[i:{pet.type}] {pet.Name}"));
        }

        public override void SaveData(TagCompound tag) {
            tag["pets"] = Pets;
        }

        public override void LoadData(TagCompound tag) {
            Pets = tag.Get<List<Item>>("pets");
        }
    }

    public class TalismanOfFriendshipPlayer : ModPlayer {
        public override void UpdateEquips() {
            var equippedPet = Player.miscEquips[0];
            if (equippedPet.type == ModContent.ItemType<TalismanOfFriendship>()) {
                var modItem = equippedPet.ModItem as TalismanOfFriendship;
                foreach (var pet in modItem.Pets) {
                    Player.AddBuff(pet.buffType, 10);
                }
            }
        }

        public override void Load() {
            Terraria.On_Player.AddBuff_RemoveOldPetBuffsOfMatchingType += Player_AddBuff_RemoveOldPetBuffsOfMatchingType;
        }

        public override void Unload() {
            Terraria.On_Player.AddBuff_RemoveOldPetBuffsOfMatchingType -= Player_AddBuff_RemoveOldPetBuffsOfMatchingType;
        }

        private void Player_AddBuff_RemoveOldPetBuffsOfMatchingType(Terraria.On_Player.orig_AddBuff_RemoveOldPetBuffsOfMatchingType orig, Player self, int type) {
            if (self.miscEquips[0].type == ModContent.ItemType<TalismanOfFriendship>()) {
                return;
            }

            orig(self, type);
        }
    }

    // These two just allow Terraria to see our talisman as a pet

    public class TotallyAPetBuff : ModBuff {
        public override void SetStaticDefaults() {
            Main.vanityPet[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.DelBuff(buffIndex);
        }
    }

    public class TotallyAPetProjectile : ModProjectile {
        public override void SetStaticDefaults() {
            Main.projPet[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = 2;
            Projectile.height = 2;
        }

        public override void AI() {
            Projectile.Kill();
        }
    }
}
