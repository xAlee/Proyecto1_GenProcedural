using UnityEngine;
using TMPro;

public class RuleInputPanel : MonoBehaviour
{
    [SerializeField] private TreeOnGrassSpawner spawner;
    [SerializeField] private TMP_InputField ruleInput;

    public void OnApplyRuleClicked()
    {
        if (!spawner)
        {
            Debug.LogWarning("[RuleInputPanel] Falta referencia al spawner.");
            return;
        }

        string rule = ruleInput ? ruleInput.text : null;
        if (rule != null) rule = rule.Trim();

        spawner.ApplyCustomRuleToForest(rule);
    }
}
