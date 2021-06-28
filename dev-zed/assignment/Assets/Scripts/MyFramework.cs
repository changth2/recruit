﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;


public class MyFramework : MonoBehaviour
{
    ///////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public class APIResponseInfo
    {
        public bool success;
        public int code;
        public DongDataInfo[] data;
    }

    [Serializable]
    public class DongDataInfo
    {
        public TypeDataInfo[] roomtypes;
        public DongDataMetaInfo meta;
    }

    [Serializable]
    public class DongDataMetaInfo
    {
        public int bd_id;
        public string 동;
        public int 지면높이;
    }

    [Serializable]
    public class TypeDataInfo
    {
        public string[] coordinatesBase64s;
        public TypeDataMetaInfo meta;
    }

    [Serializable]
    public class TypeDataMetaInfo
    {
        public int 룸타입id;
    }


    ///////////////////////////////////////////////////////////////////////////////////////

    public Material _baseMaterial;

    APIResponseInfo _apiResponse = null;
    int _floatSize = sizeof(float);
    List<GameObject> _polygonObjectList = new List<GameObject>();


    ///////////////////////////////////////////////////////////////////////////////////////
    void Awake()
    {
        LoadJsonDataFromFile();
    }

    void Start()
    {
        if (_apiResponse != null && _apiResponse.success)
        {
            ConstructPolygonMesh();
        }
    }

    void OnDestroy()
    {
        for (int i=0; i<_polygonObjectList.Count; ++i)
        {
            Destroy(_polygonObjectList[i]);
        }
        _polygonObjectList = null;
    }

    //void Update()
    //{
    //}

    void LoadJsonDataFromFile()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(Application.dataPath);
        sb.Append("/Samples/json/dong.json");

        string jsonData = "";
        _apiResponse = null;

        using ( StreamReader reader = new StreamReader(sb.ToString(), Encoding.Default) )
        {
            jsonData = reader.ReadToEnd();
        }

        _apiResponse = JsonUtility.FromJson<APIResponseInfo>(jsonData);

        sb = null;
    }

    void ConstructPolygonMesh()
    {
        StringBuilder sb = new StringBuilder();
        int polygonCount = 0;

        for(int i=0; i<_apiResponse.data.Length; ++i)
        {
            for(int j=0; j<_apiResponse.data[i].roomtypes.Length; ++j)
            {
                for(int h=0; h<_apiResponse.data[i].roomtypes[j].coordinatesBase64s.Length; ++h)
                {
                    sb.Append("Polygon ");
                    sb.Append(++polygonCount);

                    GameObject polygonObject = new GameObject(sb.ToString());
                    //Debug.Log("[[[   polygonObject.name : " + polygonObject.name + "   ]]]");

                    MeshFilter meshFilter = polygonObject.AddComponent<MeshFilter>();
                    Mesh mesh = new Mesh();

                    // vertex
                    Vector3[] vertexArray = ConvertVector3Array(_apiResponse.data[i].roomtypes[j].coordinatesBase64s[h]);
                    mesh.vertices = vertexArray;

                    // triangles
                    int[] triangleArray = MakeTriangles(vertexArray);
                    mesh.triangles = triangleArray;

                    mesh.RecalculateNormals();

                    // uv
                    Vector2[] uvArray = MakeUV(vertexArray, mesh.normals);
                    mesh.uv = uvArray;

                    meshFilter.mesh = mesh;

                    // material
                    MeshRenderer meshRenderer = polygonObject.AddComponent<MeshRenderer>();
                    Material material = new Material(_baseMaterial);

                    Vector2 textureScale = CalculateTextureScale(vertexArray);
                    material.SetTextureScale("_MainTex", textureScale);

                    meshRenderer.material = material;

                    //
                    _polygonObjectList.Add(polygonObject);

                    sb.Clear();

                    //break;
                }
                //break;
            }
            //break;
        }

        sb = null;
    }

    Vector3[] ConvertVector3Array(string coordinatesBase64s)
    {
        byte[] bytes = Convert.FromBase64String(coordinatesBase64s);

        int floatCount = bytes.Length / _floatSize;
        int vectorCount = floatCount / 3;

        Vector3[] vectorArray = new Vector3[vectorCount];
        int vertexArrayIndex = 0;

        int byteIndex = 0;
        while (byteIndex < bytes.Length)
        {
            float x = BitConverter.ToSingle(bytes, byteIndex);
            byteIndex += _floatSize;
            float z = BitConverter.ToSingle(bytes, byteIndex);
            byteIndex += _floatSize;
            float y = BitConverter.ToSingle(bytes, byteIndex);
            byteIndex += _floatSize;

            vectorArray[vertexArrayIndex++].Set(x, y, z);
        }

        return vectorArray;
    }

    int[] MakeTriangles(Vector3[] vertexArray)
    {
        int[] triangles = new int[vertexArray.Length];

        for (int k = 0; k < triangles.Length; ++k)
        {
            triangles[k] = k;
        }

        return triangles;
    }

    Vector2[] MakeUV(Vector3[] vertexArray, Vector3[] normalArray)
    {
        Vector2[] uvArray = new Vector2[vertexArray.Length];

        int vertexIndex = 0;
        while(vertexIndex < vertexArray.Length)
        {
            // entire uv
            uvArray[vertexIndex    ] = new Vector2(1, 0);
            uvArray[vertexIndex + 1] = new Vector2(0, 0);
            uvArray[vertexIndex + 2] = new Vector2(0, 1);

            if( vertexArray[vertexIndex + 3] == vertexArray[vertexIndex + 2] )
            {
                uvArray[vertexIndex + 3] = new Vector2(0, 1);
                uvArray[vertexIndex + 4] = new Vector2(1, 1);
                uvArray[vertexIndex + 5] = new Vector2(1, 0);
            }
            else
            if (vertexArray[vertexIndex + 3] == vertexArray[vertexIndex])
            {
                uvArray[vertexIndex + 3] = new Vector2(1, 0);
                uvArray[vertexIndex + 4] = new Vector2(0, 1);
                uvArray[vertexIndex + 5] = new Vector2(1, 1);
            }
            else
            {
                uvArray[vertexIndex + 3] = new Vector2(1, 1);
                uvArray[vertexIndex + 4] = new Vector2(1, 0);
                uvArray[vertexIndex + 5] = new Vector2(0, 1);
            }

            // check angle
            //Debug.Log("<<<<<  normalArray[" + vertexIndex.ToString() + "] : " +
            //          normalArray[vertexIndex].x.ToString() + ", " +
            //          normalArray[vertexIndex].y.ToString() + ", " +
            //          normalArray[vertexIndex].z.ToString() + " >>>>>");

            // top or bottom
            if( normalArray[vertexIndex] == Vector3.up ||
                normalArray[vertexIndex] == Vector3.down )
            {
                //Debug.Log("texture part is 3");

                // 3th
                for (int i=vertexIndex; i < vertexIndex + 6; ++i)
                {
                    uvArray[i].x = (uvArray[i].x + 3.0f) * 0.25f;
                    uvArray[i].y = (uvArray[i].y + 1.0f) * 0.5f;
                }
            }
            else
            {
                float forwardNormalAngle = Vector3.Angle(Vector3.forward, normalArray[vertexIndex]);
                //Debug.Log("forwardNormalAngle : " + forwardNormalAngle.ToString());

                float rightNormalDot = Vector3.Dot(Vector3.right, normalArray[vertexIndex]);
                //Debug.Log("rightNormalDot : " + rightNormalDot.ToString());

                if (rightNormalDot < 0.0f)
                {
                    forwardNormalAngle = 360.0f - forwardNormalAngle;
                    //Debug.Log("New forwardNormalAngle : " + forwardNormalAngle.ToString());
                }

                // 1th
                if(180.0f <= forwardNormalAngle && forwardNormalAngle <= 220.0f)
                {
                    //Debug.Log("texture part is 1");

                    for (int i = vertexIndex; i < vertexIndex + 6; ++i)
                    {
                        uvArray[i].x = uvArray[i].x * 0.5f;
                        uvArray[i].y = (uvArray[i].y + 1.0f) * 0.5f;
                    }
                }
                else // 2th
                {
                    //Debug.Log("texture part is 2");

                    for (int i = vertexIndex; i < vertexIndex + 6; ++i)
                    {
                        uvArray[i].x = (uvArray[i].x * 0.25f) + 0.5f;
                        uvArray[i].y = (uvArray[i].y + 1.0f) * 0.5f;
                    }
                }
            }

            vertexIndex += 6;
        }

        return uvArray;
    }

    Vector2 CalculateTextureScale(Vector3[] vertexArray)
    {
        float dongHeight = CalculateDongHeight(vertexArray);

        int floorCount = (int)dongHeight / 3;
        floorCount = (floorCount == 0) ? 1 : floorCount;
        //Debug.Log("floorCount : " + floorCount.ToString());

        return new Vector2(1.0f, floorCount);
    }

    float CalculateDongHeight(Vector3[] vertexArray)
    {
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        for(int i=0; i<6 && i<vertexArray.Length; ++i)
        {
            if (vertexArray[i].y < minHeight)
            {
                minHeight = vertexArray[i].y;
            }

            if ( vertexArray[i].y > maxHeight )
            {
                maxHeight = vertexArray[i].y;
            }
        }

        return (maxHeight - minHeight);
    }


}
