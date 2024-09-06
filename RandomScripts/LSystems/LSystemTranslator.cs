using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LSystemTranslator
{
    public static void Translate(LSystem input, PrefabFactory factory)
    {

    }
}

public class PrefabFactory : MonoBehaviour
{
    public List<Part> Parts { get; set; }
    public List<Plan> Plans { get; set; }
    public List<Package> Packages { get; set; }
    public string Blueprint { get; set; }

    public PrefabFactory(string bp = "")
    {
        Parts = new List<Part>();
        Plans = new List<Plan>();
        Blueprint = bp;
    }
    
    public void Plan()
    {
        if(!string.IsNullOrWhiteSpace(Blueprint))
        {
            // Use LSystem generated sentence (here, called a "blueprint") to build objects 
            // using a collection of "parts" which is a simple object containing a part ID attached to
            // a gameobject. Blueprints tell the factory what parts to use for what object type requested
            // and the factory instantiates the part based on the ID that the part is associated with

            var partIds = Blueprint.ToCharArray();

            if (partIds.Length > 0)
            {
                foreach (var pid in partIds)
                {
                    var part = PickPart(pid);
                    Parts.Add(part);
                }
            }
        }
    }

    public void Assemble()
    {
        if(Parts != null && Parts.Count > 0)
        {
            foreach(var p in Parts)
            {
                var plan = PickPlan(p.PartId);
                Plans.Add(plan);
            }
        }
    }

    public void Package()
    {
        if((Parts != null && Parts.Count > 0) && (Plans != null && Plans.Count > 0))
        {
            foreach(var p in Parts)
            {
                var pkg = PickPackage(p.PartId);
                Packages.Add(pkg);
            }
        }
    }

    public void Build()
    {
        if(Packages != null && Packages.Count > 0)
        {
            foreach(var pkg in Packages)
            {
                var go = pkg.Part.Prefab;
                
                if(go != null)
                {
                    go.transform.position = pkg.Plan.Instructions.Location;
                    go.transform.rotation = pkg.Plan.Instructions.Rotation;
                    go.transform.localScale = go.transform.localScale * pkg.Plan.Instructions.Scale;

                    Instantiate(go);
                }
            }
        }
    }

    private Part PickPart(char bpi)
    {
        return Parts.Where(p => p.PartId == bpi).Single();
    }

    private Plan PickPlan(char pid)
    {
        return Plans.Where(p => p.Instructions.PartId == pid).Single();
    }

    private Package PickPackage(char pid)
    {
        var part = Parts.Where(p => p.PartId == pid).Single();
        var plan = Plans.Where(p => p.Part.PartId == pid).Single();

        return new Package(part, plan);
    }
}

public struct Part
{
    public readonly GameObject Prefab { get; }
    public readonly char PartId { get; }    

    public Part(GameObject go, char pid)
    {
        Prefab = go;
        PartId = pid;        
    }  
    public override string ToString()
    {
        return $"{Prefab.name}:{PartId}";
    }
}

public struct Plan
{
    public readonly Part Part { get; }
    public readonly AssemblyInstructions Instructions { get; }

    public Plan(Part part, AssemblyInstructions instructions)
    {
        Part = part;
        Instructions = instructions;
    }
}

public struct Package
{
    public readonly Part Part { get; }
    public readonly Plan Plan { get; }

    public Package(Part part, Plan plan)
    {
        Part = part;
        Plan = plan;
    }
}

public class AssemblyInstructions : MonoBehaviour
{
    public string Name { get; set; }
    public char PartId { get; set; }
    public Vector3 Location { get; set; }
    public Quaternion Rotation { get; set; }
    public float Scale { get; set; }
    public List<Material> Materials { get; set; }
}
