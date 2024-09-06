using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PoissonPlotter : MonoBehaviour
{
    [SerializeField]
    public PoissonPlotterSettings settings;

    [HideInInspector]
    private Vector3 center;

    [HideInInspector]
    private Vector3 targetDimensions;

    [HideInInspector]
    private List<GameObject> items = new List<GameObject>();   

    [HideInInspector]
    public List<SamplePoint> plotPoints = new List<SamplePoint>();

    [HideInInspector]
    private GameObject container; 

    [ExecuteAlways]
    private void OnDrawGizmos()
    {
        targetDimensions = settings.target.GetDimensions().HasValue ? settings.target.GetDimensions().Value : new Vector3(settings.manualSampleRegionSize.x, 10, settings.manualSampleRegionSize.y);
        center = new Vector3(targetDimensions.x / 2, targetDimensions.y * 2, targetDimensions.y * 2.5f);

        if (settings.showSampleBounds)
        {
            Color original = Gizmos.color;
            Gizmos.color = settings.sampleBoundsColor;

            Gizmos.DrawWireCube(center, new Vector3(targetDimensions.x, 10, targetDimensions.y * 5));

            Gizmos.color = original;
        }
        
        if(plotPoints != null && plotPoints.Count > 0)
        {
            foreach(SamplePoint sp in plotPoints)
            {
                Vector3 dot = new Vector3(sp.position.x, center.y, sp.position.z);

                if (settings.showSampleRays)
                {
                    Debug.DrawLine(dot, sp.position, settings.sampleRayColor);
                }

                if (settings.showSamplePoints)
                {
                    Color original = Gizmos.color;
                    Gizmos.color = settings.sampleGizmoColor;

                    Gizmos.DrawSphere(dot, settings.sampleGizmoSize);

                    Gizmos.color = original;
                }                
            }

            if (items != null && items.Count > 0)
            {
                foreach (GameObject item in items)
                {
                    if (settings.showOverlapRadius)
                    {
                        Color original = UnityEditor.Handles.color;
                        UnityEditor.Handles.color = settings.overlapGizmoColor;

                        UnityEditor.Handles.DrawWireDisc(item.transform.position, Vector3.up, settings.overlapRadius);                        

                        UnityEditor.Handles.color = original;
                    }

                    if (settings.showClumpingRadius)
                    {
                        Color original = UnityEditor.Handles.color;
                        UnityEditor.Handles.color = settings.clumpingGizmoColor;

                        UnityEditor.Handles.DrawWireDisc(item.transform.position, Vector3.up, settings.clumpingRadius);

                        UnityEditor.Handles.color = original;
                    }
                }
            }
        }
    }

    [ExecuteAlways]
    public void Spawn()
    {
        Initialize();

        if (settings.active && (settings.target != null && settings.spawnItemPrefab != null))
        {
            container = new GameObject(string.IsNullOrWhiteSpace(settings.name) ? "Poisson Plotter Container" : settings.name);
            container.transform.SetParent(settings.target.transform);

            targetDimensions = settings.target.GetDimensions().HasValue ? settings.target.GetDimensions().Value : new Vector3(settings.manualSampleRegionSize.x, 10, settings.manualSampleRegionSize.y);
            center = new Vector3(targetDimensions.x / 2, targetDimensions.y * 2, targetDimensions.y * 2.5f);

            Vector2 regionSize = new Vector2(targetDimensions.x, targetDimensions.z);

            this.transform.localScale = new Vector3(targetDimensions.x / 10f, 1f, targetDimensions.y / 10f);
            this.transform.position = center;

            plotPoints = PoissonSampler.GeneratePoints3D(settings.sampleRadius, regionSize, center.y, settings.seed, new string[] { "Water" }, Mathf.Infinity, true);

            if(plotPoints != null && plotPoints.Count > 0)
            {
                List<SamplePoint> sp;

                if (settings.maxSamples > 0) 
                {
                    sp = plotPoints.Take(settings.maxSamples).ToList();
                }

                else
                {
                    sp = plotPoints;
                }

                foreach(SamplePoint p in sp)
                {
                    
                    if(settings.limitInstantiationBySlope)
                    {
                        float slope = Vector3.Angle(p.normal, Vector3.up);

                        if(slope > settings.maxSlopeAngle)
                        {
                            continue;
                        } 
                    }

                    if(settings.preventOverlap && !settings.encourageClumping)
                    {
                        int layerMask = ~LayerMask.GetMask("Terrain", "Water");

                        if (Physics.CheckSphere(p.position, settings.overlapRadius, layerMask, QueryTriggerInteraction.Collide))
                        {
                            continue;
                        }
                    }
                    
                    if (settings.orientItemToLandscape)
                    {
                        var item = Instantiate(settings.spawnItemPrefab, p.position, Quaternion.identity);

                        item.transform.rotation = Quaternion.FromToRotation(Vector3.up, p.normal);

                        if (settings.jitterRotation)
                        {
                            Vector3 jitterAmount = new Vector3(
                                Random.Range(-settings.jitterRotationRange.x, settings.jitterRotationRange.x),
                                Random.Range(-settings.jitterRotationRange.y, settings.jitterRotationRange.y),
                                Random.Range(-settings.jitterRotationRange.z, settings.jitterRotationRange.z)
                            );
                            
                            Vector3 originalRotation = item.transform.rotation.eulerAngles;
                            Vector3 newRotation;

                            if(Time.time % 2 == 0)
                            {
                                newRotation = originalRotation + jitterAmount;
                            }

                            else
                            {
                                newRotation = originalRotation - jitterAmount;
                            }

                            item.transform.rotation = Quaternion.Euler(newRotation);
                        }

                        item.transform.SetParent(container.transform, true);

                        items.Add(item);
                    }

                    else
                    {
                        var item = Instantiate(settings.spawnItemPrefab, container.transform, true);

                        item.transform.position = p.position;

                        if (settings.jitterRotation)
                        {
                            Vector3 jitterAmount = new Vector3(
                                Random.Range(-settings.jitterRotationRange.x, settings.jitterRotationRange.x),
                                Random.Range(-settings.jitterRotationRange.y, settings.jitterRotationRange.y),
                                Random.Range(-settings.jitterRotationRange.z, settings.jitterRotationRange.z)
                            );

                            Vector3 originalRotation = item.transform.rotation.eulerAngles;
                            Vector3 newRotation;

                            if (Time.time % 2 == 0)
                            {
                                newRotation = originalRotation + jitterAmount;
                            }

                            else
                            {
                                newRotation = originalRotation - jitterAmount;
                            }

                            item.transform.rotation = Quaternion.Euler(newRotation);
                        }

                        items.Add(item);
                    }                    
                }


                // TODO: Start here - figure out clumping mechanics
                if (settings.encourageClumping)
                {
                    int layerMask = ~LayerMask.GetMask("Terrain", "Water");

                    if (settings.clumpingStrength > 0 && settings.clumpingRadius > 0)
                    {
                        List<int> clumped = new List<int>();

                        foreach (GameObject go in items)
                        {                            
                            if(clumped.Any(g => g == go.GetInstanceID()))
                            {
                                continue;
                            }

                            if (go.name != $"{settings.spawnItemPrefab.name}(Clone)")
                            {
                                continue;
                            }

                            Collider[] cols = Physics.OverlapSphere(go.transform.position, settings.clumpingRadius, layerMask, QueryTriggerInteraction.Collide);

                            if (cols != null && cols.Length > 0)
                            {
                                
                                clumped.Add(go.GetInstanceID());

                                for (int i = 0; i < cols.Length; i++)
                                {
                                    if (cols[i].gameObject.name != $"{settings.spawnItemPrefab.name}(Clone)")
                                    {
                                        continue;
                                    }

                                    if (clumped.Any(g => g == cols[i].gameObject.GetInstanceID()))
                                    {
                                        continue;
                                    }
                                                                                                            
                                    Vector2 randomCircle = new Vector2(go.transform.position.x, go.transform.position.z) + Random.insideUnitCircle * settings.clumpingRadius / settings.clumpingStrength;
                                    cols[i].gameObject.transform.localPosition = gameObject.transform.InverseTransformPoint(new Vector3(randomCircle.x, go.transform.position.y, randomCircle.y));
                                    
                                    clumped.Add(cols[i].gameObject.GetInstanceID());
                                    
                                }                                
                            }
                        }
                    }
                }

                OnPlottingCompleted(new PlottingCompletedEventArgs() { PlotPoints = sp });
            }
        }
    }

    [ExecuteAlways]
    public void Initialize()
    {
        if (items != null && items.Count > 0)
        {
            foreach (GameObject go in items)
            {
                DestroyImmediate(go, false);
            }

            items.Clear();            
        }
        
        if(plotPoints != null && plotPoints.Count > 0)
        {
            plotPoints.Clear();
        }

        if(container != null)
        {
            DestroyImmediate(container, false);
            container = null;
        }
    }
    
    protected virtual void OnPlottingCompleted(PlottingCompletedEventArgs e)
    {
        System.EventHandler<PlottingCompletedEventArgs> handler = PlottingCompleted;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    [HideInInspector]
    public event System.EventHandler<PlottingCompletedEventArgs> PlottingCompleted;
}

public class PlottingCompletedEventArgs : System.EventArgs
{
    public List<SamplePoint> PlotPoints { get; set; }
}
