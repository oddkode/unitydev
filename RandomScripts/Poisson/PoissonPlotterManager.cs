using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoissonPlotterManager : MonoBehaviour
{
    public GameObject plotterPrefab;
    public PoissonPlotterSettings[] plotterSettings;
    public bool active;
    public bool realTime;    
    public int NumberOfSpawnPoints;
   

    [HideInInspector]
    private List<SamplePoint> plotPoints;

    [HideInInspector]
    private GameObject[] plotters;

    [ExecuteAlways]
    private void OnValidate()
    {
        if(plotters != null && plotters.Length > 0)
        {
            for (int i = 0; i < plotters.Length; i++)
            {
                GameObject plotterGo = plotters[i];

                if (plotterGo != null)
                {
                    PoissonPlotter plotter = plotterGo.GetComponent<PoissonPlotter>();

                    if (plotter != null)
                    {
                        if (!plotter.settings.overrideParent)
                        {
                            plotter.settings.active = active;
                            plotter.settings.realTime = realTime;
                        }

                        if (plotter.settings.preventOverlap == true && plotter.settings.encourageClumping == true)
                        {
                            plotter.settings.preventOverlap = false;
                        }
                    }
                }
            }
        }
    }

    [ExecuteAlways]
    public void Spawn()
    {
        Initialize();

        plotPoints = new List<SamplePoint>();
        plotters = new GameObject[plotterSettings.Length];

        if (active)
        {
            if (plotterSettings != null && plotterSettings.Length > 0)
            {
                for (int i = 0; i < plotterSettings.Length; i++)
                {
                    if(!plotterSettings[i].active)
                    {
                        continue;
                    }

                    GameObject plotterGo = Instantiate(plotterPrefab, this.transform, false);
                    PoissonPlotter plotter = plotterGo.GetComponent<PoissonPlotter>();

                    plotter.settings = plotterSettings[i];

                    plotters[i] = plotterGo;
                }
            }

            if (plotters != null && plotters.Length > 0)
            {
                for (int i = 0; i < plotters.Length; i++)
                {
                    GameObject plotterGo = plotters[i];

                    if (plotterGo != null)
                    {
                        PoissonPlotter plotter = plotterGo.GetComponent<PoissonPlotter>();

                        if (plotter != null && plotter.settings.active)
                        {
                            plotter.PlottingCompleted += PlottingCompleted;
                            plotter.Spawn();
                        }                        
                    }
                }
            }
        }
    }

    [ExecuteAlways]
    public void Initialize()
    {        
        if (plotters != null && plotters.Length > 0)
        {
            for (int i = 0; i < plotters.Length; i++)
            {
                GameObject plotterGo = plotters[i];

                if (plotterGo != null)
                {
                    PoissonPlotter plotter = plotterGo.GetComponent<PoissonPlotter>();

                    if (plotter != null)
                    {
                        plotter.Initialize();
                    }

                    DestroyImmediate(plotter, false);
                    DestroyImmediate(plotterGo, false);

                    plotters[i] = null;
                }
            }
        }       
    }

    [ExecuteAlways]
    private void PlottingCompleted(object sender, PlottingCompletedEventArgs e)
    {
        if(e != null && e.PlotPoints != null && e.PlotPoints.Count > 0)
        {
            plotPoints.AddRange(e.PlotPoints);
            NumberOfSpawnPoints = plotPoints.Count;
        }
    }
}
