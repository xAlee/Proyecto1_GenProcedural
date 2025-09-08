using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Globalization;

public class UIManager : MonoBehaviour
{
    [SerializeField] private NoiseGen noiseGen;

    [SerializeField] private TMP_InputField widthText;
    [SerializeField] private TMP_InputField heightText;
    [SerializeField] private TMP_InputField chanceText;

    public void StartNoise()
    {
        if (widthText.text == "") widthText.text = "85";
        else
        {
            if (int.Parse(widthText.text) > 0) noiseGen.noise_width = int.Parse(widthText.text);
            else noiseGen.noise_width = 85;
        }

        if (heightText.text == "") heightText.text = "65";
        else
        {
            if (int.Parse(heightText.text) > 0) noiseGen.noise_height = int.Parse(heightText.text);
            else noiseGen.noise_height = 65;
        }
        
        if (chanceText.text == "") chanceText.text = "0.5";
        else
        {
            if (float.Parse(chanceText.text, CultureInfo.InvariantCulture) >= 0 && float.Parse(chanceText.text, CultureInfo.InvariantCulture) <= 1)
                noiseGen.chanceRange = float.Parse(chanceText.text, CultureInfo.InvariantCulture);
            else noiseGen.chanceRange = 0.5f;
        }
        

        noiseGen.reset = true;
    }

    public void StartCell()
    {
        noiseGen.cel = true;
    }
}
