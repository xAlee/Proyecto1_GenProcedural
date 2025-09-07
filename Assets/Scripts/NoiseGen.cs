using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class NoiseGen : MonoBehaviour
{
    [SerializeField] public int noise_width;
    [SerializeField] public int noise_height;
    [SerializeField] public float chanceRange;

    [SerializeField] GameObject boxel;
    GameObject boxelinstance;
    private int type;

    public List<GameObject> currentBoxels = new List<GameObject>();

    [SerializeField] public bool reset = false;
    [SerializeField] public bool cel = false;

    BoxelDataList dataList = new BoxelDataList();
    [SerializeField] private CellularAutomata cellularAutomata;

    private string path => Application.dataPath + "/boxelgrid.bin";

    // Start is called before the first frame update
    void Start()
    {
        Delete_binary_file();
        Noise_Start();
    }

    private void Noise_Start()
    {
        dataList.items = new List<BoxelData>();

        for (int i = 0; i < noise_width; i++)
        {
            for (int j = 0; j < noise_height; j++)
            {
                boxelinstance = Instantiate(boxel, new Vector3(i, 0, j), Quaternion.identity);

                float chance = Random.Range(0.0f, 1.0f);

                if (chance > chanceRange)
                {
                    type = 0;
                }
                else
                {
                    type = 1;
                }

                BoxelData data = new BoxelData
                {
                    x = i,
                    y = j,
                    type = type
                };

                dataList.items.Add(data);
                currentBoxels.Add(boxelinstance);
            }
        }

        // Convert to compact bit-packed binary file
        var types = dataList.items.OrderBy(d => d.x).ThenBy(d => d.y).Select(d => d.type).ToList();
        BoxelBinary.WriteBitPacked(path, noise_width, noise_height, types);
    }

    private void Delete_binary_file()
    {
        dataList.items = new List<BoxelData>();

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void Delete_Boxels()
    {
        foreach (GameObject boxel in currentBoxels)
        {
            Destroy(boxel);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (reset)
        {
            reset = false;
            Delete_binary_file();
            Delete_Boxels();
            Noise_Start();
        }

        if (cel)
        {
            cel = false;
            cellularAutomata.ExecuteAutomata();
        }
    }
}