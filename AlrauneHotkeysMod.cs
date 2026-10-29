// LWF Alraune Hotkeys
//
// Lazy Witch's Factory の農耕アルラウネ（FarmerObject）に、カーソルを載せてキーを押すだけで
// 命令（交配・現金・採掘）を出す BepInEx Mod。
//
// -- 本体の仕様（2026-10-26 に 0.30.1 で確認）---------------------
// アルラウネの命令はレシピ（CrafterKind.Farmer）。
//   採掘: MinePlant / MineRock / MineSafe   … 置いた時の既定。採掘ポイントの種類で決まる
//   交配: ClonePlant / CloneRock / CloneSafe … 自分のマスに採掘ポイントを複製して消える
//   現金: HarvestableSale                   … 採掘ポイントを壊して現金 +10、消える
// 本体の流れは「アルラウネをクリック → CraftWindow → レシピを選ぶ → CraftWindow.SetOnGoingRecipe
// （isUserOperation）→ PlayerMapInteractionInputRouter.RequestMapInteractionExitPause(ignorePointerOverUI:true)
// → FarmerObject.CommandRecipe(recipe)」。CommandRecipe が採掘ポイントの種類に合わせて正規化し、
// 合わなければ何もしない（TryNormalizeRecipeForTarget）。
//
// -- 何をするか ---------------------------------------------------------
// 毎フレーム（Update）キーを見て、カーソル下（CursorHitChecker.HitMapObjectClass）が
// アルラウネなら、ウィンドウと同じ口（RequestMapInteractionExitPause → CommandRecipe）で命令する。
//   ・既に同じ命令なら何もしない（本体の ChangeRecipe も同じ命令なら NoChange。手前で止めるだけ）
//   ・キーを押したままカーソルをなぞると、通った先のアルラウネにも同じ命令が入る（cfg で切れる）
//   ・UI の上では効かない（PointerOverFinder.IsPointerOverUI）
//   ・キーコンフィグがキーを取り込んでいる間は効かない（KeyConfigInputCapturePolicy.IsInputSuppressed）
// Harmony のパッチは無い。本体の public な口だけを叩く。
//
// -- キーの既定 ---------------------------------------------------------
// 本体が使うキー（GameData の KeyConfigDefinition）: W A S D Space Z T R Shift Ctrl Tab F Alt+Enter
// X Esc C M B V F1-F4 Q E 1-0 -。空いている G H J を並びで使う。
//
// -- 書き方の制約 -------------------------------------------------------
// C# 5 コンパイラ（csc.exe / .NET Framework 4.0）でビルドするため、
// 文字列補間・?. 演算子・式形式メンバ・out var・nameof は使えない。
// キーは新 Input System（Keyboard.current）だけ。UnityEngine.Input は起動不能になる。

using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameState;
using Input.PlayerAction;
using Items;
using Items.Craft;
using Map.MapObjects.PureClass;
using Map.MapObjects.PureClass.FamObjects;
using UI.Cursor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace LwfAlrauneHotkeys
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class AlrauneHotkeysPlugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "kiyonakanata.lwfalraunehotkeys";
        internal const string PluginName = "LWF Alraune Hotkeys";
        internal const string PluginVersion = "1.0.1";

        private enum Command { None, Crossbreed, Cash, Mine }

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> ApplyWhileHeld;
        internal static ConfigEntry<Key> KeyCrossbreed;
        internal static ConfigEntry<Key> KeyCash;
        internal static ConfigEntry<Key> KeyMine;

        // 本体は 0.29.1 から、起動中に例外が出るとゲームごと起動を止める（エラー画面）。
        // 本体の更新で型が消えても MOD だけ止まって起動は通るよう、全部を受け止める
        private void Awake()
        {
            Log = Logger;
            try
            {
                Boot();
            }
            catch (Exception e)
            {
                Log.LogWarning("[boot] " + PluginName + " " + PluginVersion
                    + " disabled (the game version may have changed): " + e);
                enabled = false;
            }
        }

        private void Boot()
        {
            Enabled = Config.Bind("1. General", "Enabled", true, "");
            ApplyWhileHeld = Config.Bind("1. General", "Apply while held", true, "");
            // 項目名はゲームの英語表記。説明文は機能の言い換え 1 語（ゲーム用語だけでは何をするか分からないため・2026-10-27）
            KeyCrossbreed = Config.Bind("2. Keys", "Crossbreeding", Key.G, "Clone");
            KeyCash = Config.Bind("2. Keys", "Cash", Key.H, "Remove");
            KeyMine = Config.Bind("2. Keys", "Mining", Key.J, "Mine");

            // 本体の型に触れておく。無ければここで例外になり、Awake が MOD を止める
            Type probe = typeof(FarmerObject);
            probe = typeof(CursorHitChecker);
            probe = typeof(PlayerMapInteractionInputRouter);

            Log.LogInfo("[boot] " + PluginName + " " + PluginVersion
                + " keys: Crossbreeding=" + KeyCrossbreed.Value
                + " Cash=" + KeyCash.Value
                + " Mining=" + KeyMine.Value);
        }

        // Update で毎フレーム（本体の Update と同じタイミング）。例外は握って MOD だけ止める
        private void Update()
        {
            if (!Enabled.Value) { return; }
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                Log.LogWarning("[tick] disabled after an error: " + e);
                enabled = false;
            }
        }

        private void Tick()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) { return; }
            // キーコンフィグがキーを取り込んでいる間は読まない（本体のデバッグ UI と同じ守り）
            if (Input.KeyConfigInputCapturePolicy.IsInputSuppressed) { return; }

            Command cmd = ReadCommand(kb);
            if (cmd == Command.None) { return; }

            GameStateManager gs = GameStateManager.Instance;
            if (gs == null || !gs.HasTimerStarted) { return; }
            if (PointerOverFinder.IsPointerOverUI) { return; }

            CursorHitChecker hit = gs.GetCursorHitChecker();
            if (hit == null) { return; }
            FarmerObject farmer = hit.HitMapObjectClass as FarmerObject;
            // IsPivotDestroyed は protected。ChangeRecipe が同じ検査をするので IsLeaseValid だけ見る
            if (farmer == null || !farmer.IsLeaseValid) { return; }

            Recipe recipe = ResolveRecipe(farmer, cmd);
            if (recipe == null) { return; }
            Recipe current = farmer.OnGoingRecipe;
            if (current != null && current.RecipeID == recipe.RecipeID) { return; }

            // ウィンドウの「レシピを選ぶ」と同じ前段（チュートリアルのヒント中・ポーズ中の扱い）
            if (!PlayerMapInteractionInputRouter.Instance.RequestMapInteractionExitPause(true)) { return; }
            farmer.CommandRecipe(recipe);
        }

        // 押された（または押したままの）キーに対応する命令。並びの先勝ち
        private static Command ReadCommand(Keyboard kb)
        {
            bool held = ApplyWhileHeld.Value;
            if (IsActive(kb, KeyCrossbreed.Value, held)) { return Command.Crossbreed; }
            if (IsActive(kb, KeyCash.Value, held)) { return Command.Cash; }
            if (IsActive(kb, KeyMine.Value, held)) { return Command.Mine; }
            return Command.None;
        }

        private static bool IsActive(Keyboard kb, Key key, bool held)
        {
            if (key == Key.None) { return false; }
            KeyControl control;
            try { control = kb[key]; }
            catch (Exception) { return false; }   // 配列に無いキー（IMESelected など）
            if (control == null) { return false; }
            return held ? control.isPressed : control.wasPressedThisFrame;
        }

        // 命令 → そのアルラウネが向いている採掘ポイントに合うレシピ。
        // 採掘は MineX、交配は MineX の Mine を Clone に替えた ID、現金は HarvestableSale（種類を問わない）
        private static Recipe ResolveRecipe(FarmerObject farmer, Command cmd)
        {
            HarvestableObject target;
            if (!farmer.TryGetWorkingHarvestable(out target) || target == null) { return null; }
            string id;
            if (cmd == Command.Cash)
            {
                // 現金は相手の種類を問わない（定義が Any）。開封済みの金庫（SafeOpened）のように
                // 採掘レシピが無い相手にも通る。採掘 ID を先に要求すると、ここで止まってしまう（1.0.1 で修正）
                id = FarmerCultivationRecipes.HarvestableSaleRecipeID;
            }
            else
            {
                string mining = FarmerCultivationRecipes.GetMiningRecipeID(target.GetHarvestableType());
                if (string.IsNullOrEmpty(mining)) { return null; }
                id = (cmd == Command.Mine) ? mining : "Clone" + mining.Substring("Mine".Length);
            }

            Recipe recipe;
            if (!FarmerCultivationRecipes.IsAvailableRecipe(RecipeManagers.instance, id, out recipe)) { return null; }
            return recipe;
        }
    }
}
