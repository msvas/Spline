using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;

public class Grapher1 : MonoBehaviour {
    private int pointsTotal = 4;

    public TextAsset curvePoints;

    [Range(10, 100)]
    public int resolution = 10;

    private LineRenderer line;
    private List<Vector3> linePoints;
    private List<Vector3> pointsFromFile;

    private bool ready = false;

    void Start() {
        curvePoints = Resources.Load("pointsdata") as TextAsset;
        linePoints = new List<Vector3>();
        pointsFromFile = new List<Vector3>();

        line = gameObject.AddComponent<LineRenderer>();
        line.SetColors(Color.yellow, Color.red);
        line.SetWidth(0.2F, 0.2F);
    }

    private void CreatePoint(float x, float y) {
        Vector3 newPoint = new Vector3(x, y, 0f);
        linePoints.Add(newPoint);
        line.SetPosition(linePoints.Count - 1, newPoint);
    }

    private void ReadPointsFromFile() {
        string text = curvePoints.text;
        string[] fLines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

        pointsTotal = fLines.Length;
        line.SetVertexCount((pointsTotal - 3) * resolution);

        for (int i = 0; i < fLines.Length; i++) {
            string valueLine = fLines[i];
            string[] pointsRead = Regex.Split(valueLine, " ");
            pointsFromFile.Add(new Vector3(float.Parse(pointsRead[0]), float.Parse(pointsRead[1]), 0f));
        }
    }

    private Vector3 calculatePoints(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
        Vector3 a = 0.5f * (2f * p1);
        Vector3 b = 0.5f * (p2 - p0);
        Vector3 c = 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3);
        Vector3 d = 0.5f * (-p0 + 3f * p1 - 3f * p2 + p3);

        Vector3 pos = a + (b * t) + (c * t * t) + (d * t * t * t);

        return pos;
    }

    private void CatmullRom(int id) {
        Vector3 p0 = pointsFromFile[id - 1];
        Vector3 p1 = pointsFromFile[id];
        Vector3 p2 = pointsFromFile[id + 1];
        Vector3 p3 = pointsFromFile[id + 2];

        Vector3 lastPos = new Vector3(0, 0, 0);

        float increment = (1f / resolution);

        float t = 0;
        for (int i = 0; i < resolution; i++) {
            Vector3 newPos = calculatePoints(t, p0, p1, p2, p3);
            CreatePoint(newPos.x, newPos.y);
            t += increment;
        }
    }

    private void PerformSpline() {
        ReadPointsFromFile();

        ready = true;
 
        for (int i = 0; i < pointsTotal; i++) {
            if ((i != 0 && i != (pointsTotal - 2) && i != (pointsTotal - 1))) {
                CatmullRom(i);
            }
        }
    }

    public Vector3 GetPoint(float t) {
        if (ready) {
            int segmentNumber = Mathf.FloorToInt(t);

            Vector3 p0 = pointsFromFile[segmentNumber - 1];
            Vector3 p1 = pointsFromFile[segmentNumber];
            Vector3 p2 = pointsFromFile[segmentNumber + 1];
            Vector3 p3 = pointsFromFile[segmentNumber + 2];

            return calculatePoints(t - segmentNumber, p0, p1, p2, p3);
        } else {
            return new Vector3(0, 0, 0);
        }
    }

    void Update() {
        if (linePoints.Count < (pointsTotal - 3) * resolution) {
            PerformSpline();
        }
    }
}
