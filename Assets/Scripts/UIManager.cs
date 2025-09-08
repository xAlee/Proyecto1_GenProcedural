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
    [SerializeField] private GridGen gridGen;

    [SerializeField] private TMP_InputField width;
    [SerializeField] private TMP_InputField height;

    [SerializeField] private TMP_InputField pHeight;
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

        StartAutomata();
        RegenTerrain();
    }

    private void init()
    {
        if (width.text == "") width.text = "50";
        else
        {
            if (int.Parse(width.text) > 0)
            {
                noiseGen.noise_width = int.Parse(width.text);
                gridGen.sizeX = int.Parse(width.text);
            }
            else
            {
                noiseGen.noise_width = 50;
                gridGen.sizeX = 50;
            }
        }

        if (height.text == "") height.text = "50";
        else
        {
            if (int.Parse(height.text) > 0)
            {
                noiseGen.noise_height = int.Parse(height.text);
                gridGen.sizeZ = int.Parse(height.text);
            }
            else
            {
                noiseGen.noise_height = 50;
                gridGen.sizeZ = 50;
            }
        }

        cam.height = float.Parse(height.text);
        cam.width = float.Parse(width.text);
    }

    public void Prev1()
    {
        width.text = 50.ToString();
        height.text = 50.ToString();

        pHeight.text = 8.ToString();
        pSeed.text = 12345.ToString();
        pNoise.text = 0.1f.ToString(CultureInfo.InvariantCulture);
        pOctaves.text = 4.ToString();
        pPersistence.text = 0.5f.ToString(CultureInfo.InvariantCulture);
        pLacuranity.text = 2.0f.ToString(CultureInfo.InvariantCulture);

        tRules.text = "F->F[+F]F[-F]F";

        cChance.text = 0.5f.ToString(CultureInfo.InvariantCulture);
        cExtrude.text = 6.ToString();
        cCeiling.text = 12.ToString();

        ResetNoise();
        StartAutomata();
        RegenTerrain();
    }

    public void Prev2()
    {
        width.text = 100.ToString();
        height.text = 100.ToString();

        pHeight.text = 4.ToString();
        pSeed.text = 235456.ToString();
        pNoise.text = 0.2f.ToString(CultureInfo.InvariantCulture);
        pOctaves.text = 3.ToString();
        pPersistence.text = 0.4f.ToString(CultureInfo.InvariantCulture);
        pLacuranity.text = 2.0f.ToString(CultureInfo.InvariantCulture);

        tRules.text = "F->F[+F]F[-F]F";

        cChance.text = 0.65f.ToString(CultureInfo.InvariantCulture);
        cExtrude.text = 4.ToString();
        cCeiling.text = 12.ToString();

        ResetNoise();
        StartAutomata();
        RegenTerrain();
    }

    public void Prev3()
    {
        width.text = 150.ToString();
        height.text = 150.ToString();

        pHeight.text = 12.ToString();
        pSeed.text = 45634.ToString();
        pNoise.text = 0.2f.ToString(CultureInfo.InvariantCulture);
        pOctaves.text = 6.ToString();
        pPersistence.text = 0.4f.ToString(CultureInfo.InvariantCulture);
        pLacuranity.text = 2.0f.ToString(CultureInfo.InvariantCulture);

        tRules.text = "F->F[+F]F[-F]F";

        cChance.text = 0.4f.ToString(CultureInfo.InvariantCulture);
        cExtrude.text = 2.ToString();
        cCeiling.text = 12.ToString();

        ResetNoise();
        StartAutomata();
        RegenTerrain();
    }

    public void SwapTerrainType()
    {
        if (gridGen.currentContext == GridGen.ContextType.SoloTierra)
            gridGen.currentContext = GridGen.ContextType.ConEdificios;
        else
            gridGen.currentContext = GridGen.ContextType.SoloTierra;
        RegenTerrain();
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

    public void RegenTerrain()
    {
        init();

        if (pHeight.text == "") pHeight.text = "8";
        else
        {
            if (int.Parse(pHeight.text) > 0) gridGen.noiseHeight = int.Parse(pHeight.text);
            else gridGen.noiseHeight = 8;
        }

        if (pSeed.text == "") pSeed.text = "12345";
        else
        {
            if (int.Parse(pSeed.text) > 0) gridGen.seed = int.Parse(pSeed.text);
            else gridGen.seed = 12345;
        }

        if (pNoise.text == "") pNoise.text = "0.1";
        else
        {
            if (float.Parse(pNoise.text, CultureInfo.InvariantCulture) > 0) gridGen.noiseScale = float.Parse(pNoise.text, CultureInfo.InvariantCulture);
            else gridGen.noiseScale = 0.1f;
        }

        if (pOctaves.text == "") pOctaves.text = "4";
        else
        {
            if (int.Parse(pOctaves.text) > 0) gridGen.octaves = int.Parse(pOctaves.text);
            else gridGen.octaves = 4;
        }

        if (pPersistence.text == "") pPersistence.text = "0.5";
        else
        {
            if (float.Parse(pPersistence.text, CultureInfo.InvariantCulture) > 0) gridGen.persistence = float.Parse(pPersistence.text, CultureInfo.InvariantCulture);
            else gridGen.persistence = 0.5f;
        }

        if (pLacuranity.text == "") pLacuranity.text = "2.0";
        else
        {
            if (float.Parse(pLacuranity.text, CultureInfo.InvariantCulture) > 0) gridGen.lacunarity = float.Parse(pLacuranity.text, CultureInfo.InvariantCulture);
            else gridGen.lacunarity = 2.0f;
        }

        gridGen.regenerarMapa = true;
    }
}
