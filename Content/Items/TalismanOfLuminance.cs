using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TalismanOfFriendship.Content.Items {
    public class TalismanOfLuminance : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Stores your light pets for safekeeping\nRight click with a light pet in your cursor to drop it in\nRight click to release all contained light pets\nEquip to summon your horde!\nThis is a temporary tooltip");
        }

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 28;
            Item.value = Item.sellPrice(gold: 1);
            Item.rare = ItemRarityID.Green;
            Item.buffType = ModContent.BuffType<TotallyALightPetBuff>();
            Item.shoot = ModContent.ProjectileType<TotallyALightPetProjectile>();
        }

        public override void AddRecipes() {
            CreateRecipe()
                    .AddIngredient(ItemID.Ruby, 5)
                    .AddIngredient(ItemID.Chain)
                    .AddTile(TileID.Tables)
                    .AddTile(TileID.Chairs)
                    .Register();
        }

        private List<Item> _pets = new();
        public List<Item> Pets => _pets;

        public override void UpdateAccessory(Player player, bool hideVisual) {
            foreach (var pet in _pets) {
                player.AddBuff(pet.buffType, 10);
            }
        }

        public override bool CanRightClick() => true;

        public override void RightClick(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                if (Main.mouseItem.type == ItemID.None) {
                    foreach (var pet in _pets) {
                        player.QuickSpawnItem(Item.GetSource_OpenItem(pet.type), pet.type);
                    }
                    _pets = new();
                }
                else if (Main.lightPet[Main.mouseItem.buffType]) {
                    if (_pets.Find(i => i.type == Main.mouseItem.type) == null) {
                        _pets.Add(new(Main.mouseItem.type));
                        Main.mouseItem.stack--;
                    }
                }
                Item.stack++;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            int index = tooltips.FindIndex(tip => tip.Text == "This is a temporary tooltip") + 1;
            tooltips[index - 1].Text = "";
            foreach (var pet in _pets) {
                tooltips.Insert(index, new(Mod, $"Pet{index}", $"[i:{pet.type}] {pet.Name}"));
                index++;
            }
        }

        public override void SaveData(TagCompound tag) {
            List<int> types = new();
            foreach (var pet in _pets) {
                types.Add(pet.type);
            }
            tag["petTypes"] = types;
        }

        public override void LoadData(TagCompound tag) {
            List<int> types = tag.GetList<int>("petNames").ToList();
            foreach (var type in types) {
                _pets.Add(new Item(type));
            }
        }
    }

    public class TalismanOfLuminancePlayer : ModPlayer {
        public override void UpdateEquips() {
            var equippedPet = Player.miscEquips[1];
            if (equippedPet.type == ModContent.ItemType<TalismanOfLuminance>()) {
                var modItem = equippedPet.ModItem as TalismanOfLuminance;
                foreach (var pet in modItem.Pets) {
                    Player.AddBuff(pet.buffType, 10);
                }
            }
        }

        public override void Load() {
            On.Terraria.Player.AddBuff_RemoveOldPetBuffsOfMatchingType += Player_AddBuff_RemoveOldPetBuffsOfMatchingType;
        }

        public override void Unload() {
            On.Terraria.Player.AddBuff_RemoveOldPetBuffsOfMatchingType -= Player_AddBuff_RemoveOldPetBuffsOfMatchingType;
        }

        private void Player_AddBuff_RemoveOldPetBuffsOfMatchingType(On.Terraria.Player.orig_AddBuff_RemoveOldPetBuffsOfMatchingType orig, Player self, int type) {
            if (self.miscEquips[0].type == ModContent.ItemType<TalismanOfLuminance>()) {
                return;
            }

            orig(self, type);
        }
    }

    // These two just allow Terraria to see our talisman as a pet

    public class TotallyALightPetBuff : ModBuff {
        public override void SetStaticDefaults() {
            Main.lightPet[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.DelBuff(buffIndex);
        }
    }

    public class TotallyALightPetProjectile : ModProjectile {
        public override void SetStaticDefaults() {
            Main.projPet[Type] = true;
            ProjectileID.Sets.LightPet[Type] = true;
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
