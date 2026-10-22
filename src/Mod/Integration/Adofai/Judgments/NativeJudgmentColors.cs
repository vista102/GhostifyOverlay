using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    // Own only the tint we apply. Native text, font, scale, hiding and particles are untouched.
    internal static class NativeJudgmentColors
    {
        private sealed class State
        {
            internal TMP_Text Text;
            internal Color Original, Applied;
        }

        private static readonly OwnedStateRegistry<scrHitTextMesh, State> States = new OwnedStateRegistry<scrHitTextMesh, State>();
        internal static bool HasSnapshots
        {
            get
            {
                return States.Count > 0;
            }
        }

        internal static bool TryColor(HitMargin hit, HitMarginPerfectTextPreset preset, out Color color)
        {
            color = Color.white;
            if (hit == HitMargin.XPerfect && HitMarginHelper.IsShowXPerfect(preset, false))
            {
                color = DQColors.XPerfect;
                return true;
            }

            if ((hit == HitMargin.PerfectPlus || hit == HitMargin.PerfectMinus) && HitMarginHelper.IsShowSignedPerfects(preset))
            {
                color = DQColors.PlusMinusPerfect;
                return true;
            }

            return false;
        }

        internal static void Restore(scrHitTextMesh mesh)
        {
            States.Restore(RestoreState, target => ReferenceEquals(target, mesh));
        }

        internal static Color RestoreTint(Color current, Color original, Color applied)
        {
            // The game animates alpha after Show. Restore our RGB while retaining that fade.
            // A different RGB belongs to the game or another mod and must remain unchanged.
            if (current.r == applied.r && current.g == applied.g && current.b == applied.b)
            {
                original.a = current.a;
                return original;
            }

            return current;
        }

        private static void RestoreState(scrHitTextMesh mesh, State state)
        {
            if (state.Text != null)
                state.Text.color = RestoreTint(state.Text.color, state.Original, state.Applied);
        }

        internal static void Apply(scrHitTextMesh mesh)
        {
            if (!Main.Enabled || mesh == null || mesh.text == null)
                return;
            // Respect the player's explicit early/late colour preset.
            if (Persistence.hitMarginColor != HitMarginColorPreset.Default || Persistence.hitMarginText == HitMarginTextPreset.MsDifferenceColorSign)
                return;
            Color color;
            if (!TryColor(mesh.hitMargin, Persistence.hitMarginPerfectText, out color))
                return;
            States.Remember(mesh, new State { Text = mesh.text, Original = mesh.text.color, Applied = color }, false);
            mesh.text.color = color;
        }

        internal static void RestoreAll()
        {
            States.Restore(RestoreState);
        }

        internal static void RestoreOldScene(Scene current)
        {
            States.Restore(RestoreState, mesh => mesh == null || mesh.gameObject.scene != current);
        }
    }
}
