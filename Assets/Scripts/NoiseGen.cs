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

    private int type;

    [SerializeField] public bool reset = false;
    [SerializeField] public bool cel = false;

    BoxelDataList dataList = new BoxelDataList();
    [SerializeField] private CellularAutomata cellularAutomata;

    [SerializeField] private BoxelInstancer instancer;

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
            }
        }

        // Guardar binario compacto
        var types = dataList.items.OrderBy(d => d.x).ThenBy(d => d.y).Select(d => d.type).ToList();
        BoxelBinary.WriteBitPacked(path, noise_width, noise_height, types);

        // Actualizar instancer para dibujar sin GameObjects
        if (instancer != null)
        {
            instancer.UpdateBatchesFromDataList(dataList.items, noise_width, noise_height, parallel: true);
        }
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
        // ya no destruimos GameObjects; limpiar instancer y la lista
        if (instancer != null)
        {
            instancer.Clear();
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

            // NO eliminar la previsualización aquí — queremos verla antes de ejecutar el autómata.
            // Iniciar autómata (ExecuteAutomata delega a la versión async)
            if (cellularAutomata != null)
            {
                cellularAutomata.ExecuteAutomata();
            }
        }
    }
}