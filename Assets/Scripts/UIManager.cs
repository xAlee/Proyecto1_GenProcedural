using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Globalization;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject panelNoise;
    [SerializeField] private GameObject panelTree;
    [SerializeField] private GameObject panelCellular;

    [SerializeField] private NoiseGen noiseGen;

    [SerializeField] private TMP_InputField width;
    [SerializeField] private TMP_InputField height;

    [SerializeField] private TMP_InputField pHeight;
    [SerializeField] private TMP_InputField pSeparation;
    [SerializeField] private TMP_InputField pSeed;
    [SerializeField] private TMP_InputField pNoise;
    [SerializeField] private TMP_InputField pOctaves;
    [SerializeField] private TMP_InputField pPersistence;
    [SerializeField] private TMP_InputField pLacuranity;

    [SerializeField] private TMP_InputField tRules;

    [SerializeField] private TMP_InputField cChance;
    [SerializeField] private TMP_InputField cExtrude;
    [SerializeField] private TMP_InputField cCeiling;

    [SerializeField] private CamRotationToAnchor cam;

    public void SwapVisibility()
    {
        if (panelNoise.activeInHierarchy)
        {
            panelNoise.SetActive(false);
            panelTree.SetActive(true);
            panelCellular.SetActive(false);
        }
        else if (panelTree.activeInHierarchy)
        {
            panelNoise.SetActive(false);
            panelTree.SetActive(false);
            panelCellular.SetActive(true);
        }
        else if (panelCellular.activeInHierarchy)
        {
            panelNoise.SetActive(true);
            panelTree.SetActive(false);
            panelCellular.SetActive(false);
        }
    }

    private void Start()
    {
        panelNoise.SetActive(true);
        panelTree.SetActive(false);
        panelCellular.SetActive(false);
        init();
    }

    private void init()
    {
        if (width.text == "") width.text = "50";
        else
        {
            if (int.Parse(width.text) > 0) noiseGen.noise_width = int.Parse(width.text);
            else noiseGen.noise_width = 50;
        }

        if (height.text == "") height.text = "50";
        else
        {
            if (int.Parse(height.text) > 0) noiseGen.noise_height = int.Parse(height.text);
            else noiseGen.noise_height = 50;
        }

        cam.height = float.Parse(height.text);
        cam.width = float.Parse(width.text);
    }

    public void ResetNoise()
    {
        init();

        if (cChance.text == "") cChance.text = "0.5";
        else
        {
            if (float.Parse(cChance.text, CultureInfo.InvariantCulture) >= 0 && float.Parse(cChance.text, CultureInfo.InvariantCulture) <= 1)
                noiseGen.chanceRange = float.Parse(cChance.text, CultureInfo.InvariantCulture);
            else noiseGen.chanceRange = 0.5f;
        }

        noiseGen.reset = true;
    }

    public void StartAutomata()
    {
        init();

        if (cChance.text == "") cChance.text = "0.5";
        else
        {
            if (float.Parse(cChance.text, CultureInfo.InvariantCulture) >= 0 && float.Parse(cChance.text, CultureInfo.InvariantCulture) <= 1)
                noiseGen.chanceRange = float.Parse(cChance.text, CultureInfo.InvariantCulture);
            else noiseGen.chanceRange = 0.5f;
        }

        if (cExtrude.text == "") cExtrude.text = "6";
        else
        {
            if (int.Parse(cExtrude.text) > 0) noiseGen.cellularAutomata.instancer.maxExtrudeHeight = int.Parse(cExtrude.text);
            else noiseGen.cellularAutomata.instancer.maxExtrudeHeight = 6;
        }

        if (cCeiling.text == "") cCeiling.text = "12";
        else
        {
            if (float.Parse(cCeiling.text, CultureInfo.InvariantCulture) > 0) noiseGen.cellularAutomata.instancer.caveHeight = float.Parse(cCeiling.text, CultureInfo.InvariantCulture);
            else noiseGen.cellularAutomata.instancer.caveHeight = 12f;
        }

        noiseGen.cel = true;
    }
}
