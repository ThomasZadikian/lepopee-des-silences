using System;
using System.Threading.Tasks;
using UnityEngine;

public class SkillService : MonoBehaviour
{
    public static SkillService Instance { get; private set; }
    public SkillData[] PlayerSkills { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> LoadPlayerSkillsAsync()
    {
        try
        {
            var wrapper = await RPG.Network.ApiClient.Instance
                .GetAsync<PlayerSkillArrayWrapper>("/api/playerskills/me");

            // Extraire les SkillData depuis les PlayerSkillResponse
            var skills = new System.Collections.Generic.List<SkillData>();
            foreach (var ps in wrapper.items)
                if (ps.skill != null)
                    skills.Add(ps.skill);

            PlayerSkills = skills.ToArray();
            Debug.Log($"[Skill] {PlayerSkills.Length} sorts charges.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Skill] Chargement : {e.Message}");
            return false;
        }
    }
}