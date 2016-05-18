using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System;

public class Grapher1 : MonoBehaviour {

    [Range(10, 100)]
    public int resolution = 10;

    public TextAsset curvePoints;

    private ParticleSystem.Particle[] points;
    private ParticleSystem.Particle[] splinePoints;

    void Start() {
        curvePoints = Resources.Load("pointsdata") as TextAsset;
        splinePoints = new ParticleSystem.Particle[resolution];
    }

    private void CreatePoint(int id, float x, float y) {
        splinePoints[id].position = new Vector3(x, y, 0f);
        splinePoints[id].startColor = new Color(x, 0f, 0f);
        splinePoints[id].startSize = 0.1f;
    }

    private void ReadPointsFromFile() {
        string text = curvePoints.text;
        string[] fLines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

        Debug.Log(fLines.Length);

        for (int i = 0; i < fLines.Length; i++) {
            string valueLine = fLines[i];
            string[] pointsRead = Regex.Split(valueLine, " ");
            points[i].position = new Vector3(float.Parse(pointsRead[0]), float.Parse(pointsRead[1]), 0f);
        }
    }

    private Vector3 CatmullRom(int id, int t) {
        Vector3 p0 = points[id - 1].position;
        Vector3 p1 = points[id].position;
        Vector3 p2 = points[id + 1].position;
        Vector3 p3 = points[id + 2].position;

        Vector3 a = 0.5f * (2f * p1);
        Vector3 b = 0.5f * (p2 - p0);
        Vector3 c = 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3);
        Vector3 d = 0.5f * (-p0 + 3f * p1 - 3f * p2 + p3);

        Vector3 pos = a + (b * t) + (c * t * t) + (d * t * t * t);

        return pos;
    }

    private void PerformSpline() {
         ReadPointsFromFile();

        for (int i = 0; i < points.Length; i++) {
            if ((i != 0 && i != (points.Length - 2) && i != (points.Length - 1))) {
                Vector3 splinedPos = CatmullRom(i, 1);
                CreatePoint(i, splinedPos.x, splinedPos.y);
            }
        }
    }

    void Update() {
        if (points == null)
        {
            points = new ParticleSystem.Particle[resolution];
            PerformSpline();
        }
        for (int i = 0; i < resolution; i++)
        {
            Vector3 p = points[i].position;
            Color c = points[i].startColor;
            c.g = p.y;
            points[i].startColor = c;
        }
        GetComponent<ParticleSystem>().SetParticles(points, points.Length);
    }
}
