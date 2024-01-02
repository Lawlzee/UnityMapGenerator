using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grandient : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //Texture2D texture = ColorPatelette.Create(256, 100, new System.Random());

        //Texture2D texture = new Texture2D(256, 256);
        //
        //Color c1 = Color.red;
        //Color c2 = Color.blue;
        //Color c3 = Color.green;
        //Color c4 = Color.yellow;
        //
        //texture.SetPixel(0, 0, Color.red);
        //texture.SetPixel(0, 1, Color.blue);
        //texture.SetPixel(1, 0, Color.green);
        //texture.SetPixel(1, 1, Color.yellow);
        //
        //for (int x = 0; x < 256; x++)
        //{
        //    for (int y = 0; y < 256; y++)
        //    {
        //        Color a = Color.Lerp(c1, c2, x / 255f);
        //        Color b = Color.Lerp(c3, c4, x / 255f);
        //
        //        Color color = Color.Lerp(a, b, y / 255f);
        //
        //        texture.SetPixel(x, y, color);
        //    }
        //}
        //
        ////texture.Resize(256, 256, TextureFormat.);
        //texture.Apply();

        //GetComponent<MeshRenderer>().material.mainTexture = texture;

        //Debug.Log();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Start();
        }
    }

}
