using UnityEngine;
using System.IO;

public class SphereLoader : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TextAsset file = Resources.Load<TextAsset>("sphere_coordinates");
        string[] lines = file.text.Split('\n');
        int count = 0;

        foreach (string line in lines)
        {
            if (line.Trim() == "") continue;
            string[] parts = line.Split(',');
            int id = int.Parse(parts[0]);
            float x = float.Parse(parts[1]);
            float y = float.Parse(parts[2]);
            float z = float.Parse(parts[3]);
            float d = float.Parse(parts[4]);
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Sphere_" + id;
            sphere.transform.SetParent(transform, false);
            sphere.transform.localPosition = new Vector3(x, y, z);
            sphere.transform.localScale = new Vector3(d, d, d);
            count++;
        }

        Debug.Log("SphereLoader made " + count + " spheres");
    }
        
}
