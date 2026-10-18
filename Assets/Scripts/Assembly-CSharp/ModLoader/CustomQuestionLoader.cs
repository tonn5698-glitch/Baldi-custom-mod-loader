using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class CustomQuestionLoader
{
    private static readonly List<CustomQuestionEntry> questions = new List<CustomQuestionEntry>();
    private static bool loaded;

    public static CustomQuestionEntry GetQuestion(int oneBasedIndex)
    {
        LoadIfNeeded();
        int index = oneBasedIndex - 1;
        return index >= 0 && index < questions.Count ? questions[index] : null;
    }

    private static void LoadIfNeeded()
    {
        if (loaded) return;
        loaded = true;
        questions.Clear();

        string modsPath = Path.Combine(Application.persistentDataPath, "Mods");
        if (!Directory.Exists(modsPath)) return;

        foreach (string folder in Directory.GetDirectories(modsPath))
        {
            string manifestPath = Path.Combine(folder, "mod.json");
            string questionsPath = Path.Combine(folder, "questions.json");
            if (!File.Exists(manifestPath) || !File.Exists(questionsPath)) continue;

            try
            {
                ModManifest manifest = JsonUtility.FromJson<ModManifest>(File.ReadAllText(manifestPath));
                if (manifest == null || !manifest.enabled) continue;

                CustomQuestionFile file = JsonUtility.FromJson<CustomQuestionFile>(File.ReadAllText(questionsPath));
                if (file == null || file.questions == null) continue;

                foreach (CustomQuestionEntry question in file.questions)
                {
                    if (question != null && !string.IsNullOrEmpty(question.text) && question.answer != null)
                        questions.Add(question);
                }

                Debug.Log("[BaldiModLoader] Loaded custom questions from " + manifest.id);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[BaldiModLoader] Could not read questions in " + folder + ": " + ex.Message);
            }
        }
    }
}

[Serializable]
public class CustomQuestionFile
{
    public CustomQuestionEntry[] questions;
}

[Serializable]
public class CustomQuestionEntry
{
    public string text;
    public string answer;
}
