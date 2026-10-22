using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay
{
    internal static class NativeResultsPresentation
    {
        // Swap existing native cells, preserving their numbers, rich text and all other rows.
        internal static string Reorder(string result, string minusLabel, string plusLabel)
        {
            if (string.IsNullOrEmpty(result) || string.IsNullOrWhiteSpace(minusLabel) || string.IsNullOrWhiteSpace(plusLabel) || minusLabel == plusLabel)
                return result;
            string[] lines = result.Split('\n');
            var cells = new string[lines.Length][];
            var carriage = new bool[lines.Length];
            int minusLine = -1, minusCell = -1, plusLine = -1, plusCell = -1, minusMatches = 0, plusMatches = 0;
            for (int line = 0; line < lines.Length; line++)
            {
                carriage[line] = lines[line].EndsWith("\r", StringComparison.Ordinal);
                string content = carriage[line] ? lines[line].Substring(0, lines[line].Length - 1) : lines[line];
                cells[line] = content.Split(new[] { "     " }, StringSplitOptions.None);
                for (int cell = 0; cell < cells[line].Length; cell++)
                {
                    if (cells[line][cell].IndexOf(minusLabel, StringComparison.Ordinal) >= 0)
                    {
                        minusLine = line;
                        minusCell = cell;
                        minusMatches++;
                    }

                    if (cells[line][cell].IndexOf(plusLabel, StringComparison.Ordinal) >= 0)
                    {
                        plusLine = line;
                        plusCell = cell;
                        plusMatches++;
                    }
                }
            }

            if (minusMatches != 1 || plusMatches != 1 || (minusLine == plusLine && minusCell == plusCell))
                return result;
            // Native PerfectMinus is early. Keep/swap it into the first cell.
            if (minusLine < plusLine || (minusLine == plusLine && minusCell < plusCell))
                return result;
            string value = cells[minusLine][minusCell];
            cells[minusLine][minusCell] = cells[plusLine][plusCell];
            cells[plusLine][plusCell] = value;
            for (int line = 0; line < lines.Length; line++)
                lines[line] = string.Join("     ", cells[line]) + (carriage[line] ? "\r" : string.Empty);
            return string.Join("\n", lines);
        }
    }
}
